// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class ServiceHubProtocolResolver : IHubProtocolResolver
    {
        private readonly IOptions<HubOptions> _options;

        public ServiceHubProtocolResolver(IOptions<HubOptions> options)
        {
            _options = options;
        }

        public IHubProtocol GetProtocol(string protocolName, HubConnectionContext connection)
        {
            switch (protocolName?.ToLowerInvariant())
            {
                case "json":
                    return new JsonHubProtocol2(JsonSerializer.Create(_options.Value.JsonSerializerSettings));
                case "messagepack":
                    return new MessagePackHubProtocol2(_options.Value.MessagePackSerializationContext, true);
                default:
                    throw new NotSupportedException($"The protocol '{protocolName ?? "(null)"}' is not supported.");
            }
        }
    }
}
