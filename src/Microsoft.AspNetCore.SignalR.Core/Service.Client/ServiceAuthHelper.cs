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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceAuthHelper
    {
        private static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
        private readonly ServiceOptions _options;

        public ServiceAuthHelper(IOptions<ServiceOptions> serviceHubOptions)
        {
            _options = serviceHubOptions.Value;
        }

        public async Task GetServiceEndpoint<THub>(HttpContext context, IList<IAuthorizeData> authorizeData,
            ServiceCredential credential) where THub : Hub
        {
            // TODO: import AuthorizeHelper from Microsoft.AspNetCore.Sockets.Http package
            /*
            if (!await AuthorizeHelper.AuthorizeAsync(context, authorizeData))
            {
                return;
            }
            */

            var serviceUrl = GetClientUrl<THub>(credential);
            var jwtBearer = GetClientToken<THub>(context, credential);
            var uid = Guid.NewGuid().ToString();

            await context.Response.WriteAsync(
                JsonConvert.SerializeObject(new
                {
                    uid,
                    serviceUrl,
                    jwtBearer
                }));
        }

        public string GetClientUrl<THub>(ServiceCredential config) where THub : Hub
        {
            return $"http://{config.HostName}/client/{typeof(THub).Name.ToLower()}";
        }

        public string GetClientToken<THub>(HttpContext context, ServiceCredential credential) where THub : Hub
        {
            return GenerateJwtBearer(
                audience: GetClientUrl<THub>(credential),
                claims: _options.ClaimProvider(context),
                expires: DateTime.UtcNow.Add(_options.JwtBearerLifetime),
                signingKey: credential.AccessKey
            );
        }

        public string GetServerUrl<THub>(ServiceCredential credential) where THub : Hub
        {
            return $"http://{credential.HostName}/server/{typeof(THub).Name.ToLower()}";
        }

        public string GetServerToken<THub>(ServiceCredential config) where THub : Hub
        {
            return GenerateJwtBearer(
                audience: GetServerUrl<THub>(config),
                expires: DateTime.UtcNow.Add(_options.JwtBearerLifetime),
                signingKey: config.AccessKey
            );
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
    }
}
