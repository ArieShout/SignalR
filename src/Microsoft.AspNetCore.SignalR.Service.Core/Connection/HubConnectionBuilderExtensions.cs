// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Service.Core;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder WithConsoleLogger(this IHubConnectionBuilder hubConnectionBuilder)
        {
            return hubConnectionBuilder.WithLogger(loggerFactory =>
            {
                loggerFactory.AddConsole();
            });
        }

        public static IHubConnectionBuilder WithConsoleLogger(this IHubConnectionBuilder hubConnectionBuilder, LogLevel logLevel)
        {
            return hubConnectionBuilder.WithLogger(loggerFactory =>
            {
                loggerFactory.AddConsole(logLevel);
            });
        }

        public static IHubConnectionBuilder WithHubProtocol(this IHubConnectionBuilder hubConnectionBuilder, IHubProtocol hubProtocol)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.HubProtocolKey, hubProtocol);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithJsonProtocol(this IHubConnectionBuilder hubConnectionBuilder)
        {
            return hubConnectionBuilder.WithHubProtocol(new JsonHubProtocol());
        }

        public static IHubConnectionBuilder WithJsonProtocol(this IHubConnectionBuilder hubConnectionBuilder, JsonSerializerSettings serializerSettings)
        {
            return hubConnectionBuilder.WithHubProtocol(new JsonHubProtocol(JsonSerializer.Create(serializerSettings)));
        }

        public static IHubConnectionBuilder WithJsonProtocol(this IHubConnectionBuilder hubConnectionBuilder, JsonSerializer jsonSerializer)
        {
            return hubConnectionBuilder.WithHubProtocol(new JsonHubProtocol(jsonSerializer));
        }

        public static IHubConnectionBuilder WithMessagePackProtocol(this IHubConnectionBuilder hubConnectionBuilder)
        {
            return hubConnectionBuilder.WithHubProtocol(new MessagePackHubProtocol());
        }

        public static IHubConnectionBuilder WithMessagePackProtocol(this IHubConnectionBuilder hubConnectionBuilder, SerializationContext serializationContext)
        {
            if (serializationContext == null)
            {
                throw new ArgumentNullException(nameof(serializationContext));
            }

            return hubConnectionBuilder.WithHubProtocol(new MessagePackHubProtocol(serializationContext));
        }

        public static IHubConnectionBuilder WithLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder, ILoggerFactory loggerFactory)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.LoggerFactoryKey,
                loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)));
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithLogger(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggerFactory> configureLogging)
        {
            var loggerFactory = hubConnectionBuilder.GetLoggerFactory() ?? new LoggerFactory();
            configureLogging(loggerFactory);
            return hubConnectionBuilder.WithLoggerFactory(loggerFactory);
        }

        public static IHubConnectionBuilder WithHubBinder(this IHubConnectionBuilder hubConnectionBuilder, IInvocationBinder serviceHubBinder)
        {
            if (serviceHubBinder == null)
            {
                throw new ArgumentNullException(nameof(serviceHubBinder));
            }
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.HubBinderKey, serviceHubBinder);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithMessageQueue(this IHubConnectionBuilder hubConnectionBuilder, Channel<HubConnectionMessageWrapper> requestHandlingQ)
        {
            if (requestHandlingQ == null)
            {
                throw new ArgumentNullException(nameof(requestHandlingQ));
            }
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.RequestQueueKey, requestHandlingQ);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithStat(this IHubConnectionBuilder hubConnectionBuilder, Stats stat)
        {
            if (stat == null)
            {
                throw new ArgumentNullException(nameof(stat));
            }
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.StatKey, stat);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithIndex(this IHubConnectionBuilder hubConnectionBuilder, int index)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.IndexKey, index);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithEnableMetrics(this IHubConnectionBuilder hubConnectionBuilder, bool enableMetrics)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.EnableMetricsKey, enableMetrics);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithHubInvoker(this IHubConnectionBuilder hubConnectionBuilder, IHubInvoker hubInvoker)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.HubInvoker, hubInvoker);
            return hubConnectionBuilder;
        }

        public static ILoggerFactory GetLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<ILoggerFactory>(HubConnectionBuilderDefaults.LoggerFactoryKey, out var loggerFactory);
            return loggerFactory;
        }

        public static IHubProtocol GetHubProtocol(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<IHubProtocol>(HubConnectionBuilderDefaults.HubProtocolKey, out var hubProtocol);
            return hubProtocol;
        }

        public static IInvocationBinder GetServiceHubBinder(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<IInvocationBinder>(HubConnectionBuilderDefaults.HubBinderKey, out var hubReceiver);
            return hubReceiver;
        }

        public static Channel<HubConnectionMessageWrapper> GetRequestHandlingQ(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<Channel<HubConnectionMessageWrapper>>(HubConnectionBuilderDefaults.RequestQueueKey, out var requestHandlingQ);
            return requestHandlingQ;
        }

        public static Stats GetServiceStat(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<Stats>(HubConnectionBuilderDefaults.StatKey, out var stat);
            return stat;
        }
        public static bool isMetricsEnabled(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<bool>(HubConnectionBuilderDefaults.EnableMetricsKey, out var enabledMetrics);
            return enabledMetrics;
        }

        public static int GetIndex(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<int>(HubConnectionBuilderDefaults.IndexKey, out var index);
            return index;
        }

        public static IHubInvoker GetHubInvoker(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<IHubInvoker>(HubConnectionBuilderDefaults.HubInvoker, out var hubInvoker);
            return hubInvoker;
        }
    }
}
