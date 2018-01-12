using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using static Microsoft.AspNetCore.SignalR.Client.Channel;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class ServiceCredential
    {
        private static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

        public string HostName { get; private set; }

        public string AccessKey { get; private set; }

        // TODO: HTTPS
        public string ServiceUrl => string.IsNullOrEmpty(HostName) ? string.Empty : $"http://{HostName}/signalr";

        public string GenerateJwtBearer(string channel, AccessPermission permission = AccessPermission.Publish | AccessPermission.Subscribe, DateTime? expires = null)
        {
            return GenerateJwtBearer(new Channel
            {
                Name = channel,
                Permission = permission
            });
        }

        public string GenerateJwtBearer(Channel channel, DateTime? expires = null)
        {

            return GenerateJwtBearer(new[] {channel}, expires);
        }

        public string GenerateJwtBearer(IEnumerable<Channel> channels, DateTime? expires = null)
        {
            if (channels.GroupBy(c => c.Name).Any(g => g.Count() > 1))
            {
                throw new ArgumentException("Duplicate channels.");
            }

            return GenerateJwtBearer(channels.Select(c => new Claim("scope", c.ToString())), expires);
        }

        private string GenerateJwtBearer(IEnumerable<Claim> claims = null, DateTime? expires = null)
        {
            SigningCredentials credentials = null;
            if (!string.IsNullOrEmpty(AccessKey))
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AccessKey));
                credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            }

            var token = new JwtSecurityToken(
                audience: HostName,
                claims: claims,
                expires: expires ?? DateTime.UtcNow.AddSeconds(60),
                signingCredentials: credentials);
            var tokenString = JwtTokenHandler.WriteToken(token);
            Console.WriteLine($"JWT = {tokenString}");
            return tokenString;
        }

        public static bool TryParse(string connectionString, out ServiceCredential credential)
        {
            credential = null;
            if (string.IsNullOrEmpty(connectionString)) return false;

            var dict = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] { '=' }, 2))
                .ToDictionary(t => t[0].Trim().ToLower(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            if (!dict.ContainsKey("hostname") || !dict.ContainsKey("accesskey")) return false;

            credential = new ServiceCredential
            {
                HostName = dict["hostname"],
                AccessKey = dict["accesskey"]
            };
            return true;
        }
    }
}
