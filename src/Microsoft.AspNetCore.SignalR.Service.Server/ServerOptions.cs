﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServerOptions
    {
        public Func<string, IEnumerable<string>> AudienceProvider { get; set; } = null;

        public Func<IEnumerable<string>> SigningKeyProvider { get; set; } = null;

        public bool EnableStickySession { get; set; } = false;
    }

    internal class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly ServerOptions _serverOptions;
        private readonly IHttpContextAccessor _contextAccessor;

        private string HubName => _contextAccessor.HttpContext.Request.Query["hub"].FirstOrDefault();

        public ConfigureJwtBearerOptions(IOptions<ServerOptions> options, IHttpContextAccessor contextAccessor)
        {
            _serverOptions = options.Value;
            _contextAccessor = contextAccessor;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            Configure(options);
        }

        public void Configure(JwtBearerOptions options)
        {
            ConfigureTokenValidationParameters(options.TokenValidationParameters);
            ConfigureEvents(options);
        }

        private void ConfigureTokenValidationParameters(TokenValidationParameters validationParams)
        {
            // TODO: support validation of issuer
            validationParams.ValidateIssuer = false;

            validationParams.ValidateLifetime = true;
            validationParams.LifetimeValidator =
                (before, expires, token, parameters) => expires > DateTime.UtcNow;

            validationParams.ValidateAudience = _serverOptions.AudienceProvider != null;
            validationParams.ValidAudiences = _serverOptions.AudienceProvider?.Invoke(HubName);

            validationParams.ValidateIssuerSigningKey = _serverOptions.SigningKeyProvider != null;
            validationParams.IssuerSigningKeys = _serverOptions.SigningKeyProvider?.Invoke()
                .Select(x => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(x)));
        }

        private void ConfigureEvents(JwtBearerOptions options)
        {
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