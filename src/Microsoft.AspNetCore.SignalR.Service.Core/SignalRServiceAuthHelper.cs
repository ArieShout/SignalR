// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class SignalRServiceAuthHelper
    {
        private static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
        private readonly ServiceHubOptions _serviceHubOptions;

        public SignalRServiceAuthHelper(IOptions<ServiceHubOptions> serviceHubOptions)
        {
            _serviceHubOptions = serviceHubOptions.Value;
        }

        private static string GenerateJwtBearer(string issuer = null, string audience = null,
            IEnumerable<Claim> claims = null, DateTime? expires = null, string signingKey = null)
        {
            SigningCredentials credentials = null;
            if (!string.IsNullOrEmpty(signingKey))
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
                credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);
            return JwtTokenHandler.WriteToken(token);
        }

        public async Task GetServiceEndpoint<THub>(HttpContext context, IList<IAuthorizeData> authorizeData,
            SignalRServiceConfiguration config) where THub : Hub
        {
            if (!await AuthorizeHelper.AuthorizeAsync(context, authorizeData))
            {
                return;
            }

            var serviceUrl = GetClientUrl<THub>(config);
            var jwtBearer = GetClientToken(context, config);

            await context.Response.WriteAsync(
                JsonConvert.SerializeObject(new
                {
                    serviceUrl,
                    jwtBearer
                }));
        }

        public string GetClientUrl<THub>(SignalRServiceConfiguration config) where THub: Hub
        {
            return $"http://{config.HostName}/client/{typeof(THub).Name.ToLower()}";
        }

        public string GetClientToken(HttpContext context, SignalRServiceConfiguration config)
        {
            return GenerateJwtBearer(
                audience: $"{config.HostName}/client/",
                claims: _serviceHubOptions.ClaimProvider(context),
                expires: DateTime.UtcNow.AddSeconds(30),
                signingKey: config.Key
            );
        }

        public string GetServerUrl<THub>(SignalRServiceConfiguration config) where THub : Hub
        {
            return $"http://{config.HostName}/server/{typeof(THub).Name.ToLower()}";
        }

        public string GetServerToken(SignalRServiceConfiguration config)
        {
            return GenerateJwtBearer(
                audience: $"{config.HostName}/server/",
                expires: DateTime.UtcNow.AddSeconds(30),
                signingKey: config.Key
            );
        }
    }
}
