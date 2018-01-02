﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServerOptions
    {
        public Func<IEnumerable<string>> AudienceProvider { get; set; } = null;

        public Func<IEnumerable<string>> SigningKeyProvider { get; set; } = null;

        public bool EnableStickySession { get; set; } = false;
    }

    internal class ConfigureSignalRServiceOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly ServerOptions _serverOptions;

        public ConfigureSignalRServiceOptions(IOptions<ServerOptions> options)
        {
            _serverOptions = options.Value;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            Configure(options);
        }

        public void Configure(JwtBearerOptions options)
        {
            var validationParams = options.TokenValidationParameters;
            // TODO: support validation of issuer
            validationParams.ValidateIssuer = false;

            validationParams.ValidateLifetime = true;
            validationParams.LifetimeValidator =
                (before, expires, token, parameters) => expires > DateTime.UtcNow;

            validationParams.ValidateAudience = _serverOptions.AudienceProvider != null;
            validationParams.ValidAudiences = _serverOptions.AudienceProvider?.Invoke();

            validationParams.ValidateIssuerSigningKey = _serverOptions.SigningKeyProvider != null;
            validationParams.IssuerSigningKeys = _serverOptions.SigningKeyProvider?.Invoke()
                .Select(x => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(x)));


            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Query.TryGetValue("signalRTokenHeader", out var signalRTokenHeader) &&
                        !string.IsNullOrEmpty(signalRTokenHeader) &&
                        context.IsTokenFromQueryString())
                    {
                        context.Token = signalRTokenHeader;
                    }
                    return Task.CompletedTask;
                }
            };
        }
    }
}
