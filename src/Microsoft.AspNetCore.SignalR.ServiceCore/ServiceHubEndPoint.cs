// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Internal;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.ServiceCore.Internal;
using Microsoft.AspNetCore.SignalR.ServiceCore.API;
using Microsoft.AspNetCore.SignalR.ServiceCore.Connection;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class ServiceHubEndPoint<THub> : IServiceHubConnection where THub : ServiceHub
    {
        private readonly ILogger<ServiceHubEndPoint<THub>> _logger;
        private readonly ServiceHubLifetimeMgr<THub> _lifetimeMgr;
        private HubConnection _hubConnection;
        private readonly IServiceHubContext<THub> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);
        public ServiceHubEndPoint(ServiceHubLifetimeMgr<THub> lifetimeMgr,
            ILogger<ServiceHubEndPoint<THub>> logger,
            IServiceScopeFactory serviceScopeFactory,
            IServiceHubContext<THub> hubContext)
        {
            _lifetimeMgr = lifetimeMgr;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _hubContext = hubContext;
            DiscoverHubMethods();
        }
        public void UseHub(string path)
        {
            _hubConnection = new HubConnectionBuilder()
                                .WithUrl(path)
                                .Build();
            _hubConnection.setReceivedCallback(this);
        }

        public async Task StartAsync()
        {
            await _hubConnection.StartAsync();
        }

        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);
            var hubTypeInfo = hubType.GetTypeInfo();

            foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
            {
                var methodName = methodInfo.Name;

                if (_methods.ContainsKey(methodName))
                {
                    throw new NotSupportedException($"Duplicate definitions of '{methodName}'. Overloading is not supported.");
                }

                var executor = ObjectMethodExecutor.Create(methodInfo, hubTypeInfo);
                var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                _methods[methodName] = new HubMethodDescriptor(executor, authorizeAttributes);

                _logger.HubMethodBound(methodName);
            }
        }

        public Task OnDataReceivedAsync(InvocationMessage invocation)
        {
            return ProcessInvocation(invocation, false);
        }

        private async Task ProcessInvocation(HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            try
            {
                // If an unexpected exception occurs then we want to kill the entire connection
                // by ending the processing loop
                if (!_methods.TryGetValue(hubMethodInvocationMessage.Target, out var descriptor))
                {
                    // Send an error to the client. Then let the normal completion process occur
                    _logger.UnknownHubMethod(hubMethodInvocationMessage.Target);
                    /*
                    await SendMessageAsync(connection, CompletionMessage.WithError(
                        hubMethodInvocationMessage.InvocationId, $"Unknown hub method '{hubMethodInvocationMessage.Target}'"));
                    */
                }
                else
                {
                    await Invoke(descriptor, hubMethodInvocationMessage, isStreamedInvocation);
                }
            }
            catch (Exception)
            {
                // Abort the entire connection if the invocation fails in an unexpected way
                //connection.Abort(ex);
            }
        }

        private async Task SendMessageAsync(HubInvocationMessage hubMessage)
        {
            await _hubConnection.SendHubMessage(hubMessage);
        }

        private async Task SendInvocationError(HubMethodInvocationMessage hubMethodInvocationMessage,
            string errorMessage)
        {
            if (hubMethodInvocationMessage.NonBlocking)
            {
                return;
            }

            await SendMessageAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId, errorMessage));
        }

        private void InitializeHub(THub hub)
        {
            hub.Clients = _hubContext.Clients;
        }

        private async Task<bool> IsHubMethodAuthorized(IServiceProvider provider, ClaimsPrincipal principal, IList<IAuthorizeData> policies)
        {
            // If there are no policies we don't need to run auth
            if (!policies.Any())
            {
                return true;
            }

            var authService = provider.GetRequiredService<IAuthorizationService>();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            // AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
            Debug.Assert(authorizePolicy != null);

            var authorizationResult = await authService.AuthorizeAsync(principal, authorizePolicy);
            // Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
            return authorizationResult.Succeeded;
        }

        private async Task<bool> ValidateInvocationMode(Type resultType, bool isStreamedInvocation,
            HubMethodInvocationMessage hubMethodInvocationMessage)
        {
            var isStreamedResult = IsStreamed(resultType);
            if (isStreamedResult && !isStreamedInvocation)
            {
                if (!hubMethodInvocationMessage.NonBlocking)
                {
                    _logger.StreamingMethodCalledWithInvoke(hubMethodInvocationMessage);
                    await SendMessageAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                        $"The client attempted to invoke the streaming '{hubMethodInvocationMessage.Target}' method in a non-streaming fashion."));
                }

                return false;
            }

            if (!isStreamedResult && isStreamedInvocation)
            {
                _logger.NonStreamingMethodCalledWithStream(hubMethodInvocationMessage);
                await SendMessageAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                    $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method in a streaming fashion."));

                return false;
            }

            return true;
        }

        private static bool IsChannel(Type type, out Type payloadType)
        {
            var channelType = type.AllBaseTypes().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ChannelReader<>));
            if (channelType == null)
            {
                payloadType = null;
                return false;
            }
            else
            {
                payloadType = channelType.GetGenericArguments()[0];
                return true;
            }
        }

        private static bool IsIObservable(Type iface)
        {
            return iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>);
        }

        private static bool IsStreamed(Type resultType)
        {
            var observableInterface = IsIObservable(resultType) ?
                resultType :
                resultType.GetInterfaces().FirstOrDefault(IsIObservable);

            if (observableInterface != null)
            {
                return true;
            }

            if (IsChannel(resultType, out _))
            {
                return true;
            }

            return false;
        }
        /*TODO
        private IAsyncEnumerator<object> GetStreamingEnumerator(string invocationId, ObjectMethodExecutor methodExecutor, object result, Type resultType)
        {
            if (result != null)
            {
                var observableInterface = IsIObservable(resultType) ?
                    resultType :
                    resultType.GetInterfaces().FirstOrDefault(IsIObservable);
                if (observableInterface != null)
                {
                    return AsyncEnumeratorAdapters.FromObservable(result, observableInterface, CreateCancellation());
                }

                if (IsChannel(resultType, out var payloadType))
                {
                    return AsyncEnumeratorAdapters.FromChannel(result, payloadType, CreateCancellation());
                }
            }

            _logger.InvalidReturnValueFromStreamingMethod(methodExecutor.MethodInfo.Name);
            throw new InvalidOperationException($"The value returned by the streaming method '{methodExecutor.MethodInfo.Name}' is null, does not implement the IObservable<> interface or is not a ReadableChannel<>.");
            
            CancellationToken CreateCancellation()
            {
                var streamCts = new CancellationTokenSource();
                connection.ActiveRequestCancellationSources.TryAdd(invocationId, streamCts);
                return CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAbortedToken, streamCts.Token).Token;
            }
        }
        */
        private async Task Invoke(HubMethodDescriptor descriptor,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            var methodExecutor = descriptor.MethodExecutor;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                /*TODO
                if (!await IsHubMethodAuthorized(scope.ServiceProvider, descriptor.Policies))
                {
                    _logger.HubMethodNotAuthorized(hubMethodInvocationMessage.Target);
                    await SendInvocationError(hubMethodInvocationMessage,
                        $"Failed to invoke '{hubMethodInvocationMessage.Target}' because user is unauthorized");
                    return;
                }
                */
                if (!await ValidateInvocationMode(methodExecutor.MethodReturnType, isStreamedInvocation, hubMethodInvocationMessage))
                {
                    return;
                }

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();

                try
                {
                    InitializeHub(hub);

                    var result = await ExecuteHubMethod(methodExecutor, hub, hubMethodInvocationMessage.Arguments);

                    if (isStreamedInvocation)
                    {
                        //TODO
                        /*
                        var enumerator = GetStreamingEnumerator(hubMethodInvocationMessage.InvocationId, methodExecutor, result, methodExecutor.MethodReturnType);
                        _logger.StreamingResult(hubMethodInvocationMessage.InvocationId, methodExecutor.MethodReturnType.FullName);
                        await StreamResultsAsync(hubMethodInvocationMessage.InvocationId, connection, enumerator);
                        */
                    }
                    else if (!hubMethodInvocationMessage.NonBlocking)
                    {
                        _logger.SendingResult(hubMethodInvocationMessage.InvocationId, methodExecutor.MethodReturnType.FullName);
                        await SendMessageAsync(CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, result));
                    }
                }
                catch (TargetInvocationException ex)
                {
                    _logger.FailedInvokingHubMethod(hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage, ex.InnerException.Message);
                }
                catch (Exception ex)
                {
                    _logger.FailedInvokingHubMethod(hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage, ex.Message);
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
        }

        private static async Task<object> ExecuteHubMethod(ObjectMethodExecutor methodExecutor, THub hub, object[] arguments)
        {
            // ReadableChannel is awaitable but we don't want to await it.
            if (methodExecutor.IsMethodAsync && !IsChannel(methodExecutor.MethodReturnType, out _))
            {
                if (methodExecutor.MethodReturnType == typeof(Task))
                {
                    await (Task)methodExecutor.Execute(hub, arguments);
                }
                else
                {
                    return await methodExecutor.ExecuteAsync(hub, arguments);
                }
            }
            else
            {
                return methodExecutor.Execute(hub, arguments);
            }

            return null;
        }
        private class HubMethodDescriptor
        {
            public HubMethodDescriptor(ObjectMethodExecutor methodExecutor, IEnumerable<IAuthorizeData> policies)
            {
                MethodExecutor = methodExecutor;
                ParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
                Policies = policies.ToArray();
            }

            public ObjectMethodExecutor MethodExecutor { get; }

            public Type[] ParameterTypes { get; }

            public IList<IAuthorizeData> Policies { get; }
        }
    }

    public static class HubReflectionHelper
    {
        private static readonly Type[] _excludeInterfaces = new[] { typeof(IDisposable) };

        public static IEnumerable<MethodInfo> GetHubMethods(Type hubType)
        {
            var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(hubType, i));

            return methods.Except(allInterfaceMethods).Where(m => IsHubMethod(m));
        }

        private static IEnumerable<MethodInfo> GetInterfaceMethods(Type type, Type iface)
        {
            if (!iface.IsAssignableFrom(type))
            {
                return Enumerable.Empty<MethodInfo>();
            }

            return type.GetInterfaceMap(iface).TargetMethods;
        }

        private static bool IsHubMethod(MethodInfo methodInfo)
        {
            var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType;
            if (typeof(object) == baseDefinition || methodInfo.IsSpecialName)
            {
                return false;
            }

            var baseType = baseDefinition.GetTypeInfo().IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
            return typeof(ServiceHub) != baseType;
        }
    }    
}
