using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR.Service;

namespace Microsoft.AspNetCore.SignalR
{
    public class SignalR
    {
        private const string HostNameProperty = "hostname";
        private const string AccessKeyProperty = "accesskey";

        private readonly ServiceOptions _options;

        public TimeSpan JwtBearerLifetime
        {
            get => _options.JwtBearerLifetime;
            set => _options.JwtBearerLifetime = value;
        }

        public int ConnectionNumber
        {
            get => _options.ConnectionNumber;
            set => _options.ConnectionNumber = value;
        }

        public string ApiVersion
        {
            get => _options.ApiVersion;
            set => _options.ApiVersion = value;
        }

        public string HostName { get; }

        public string AccessKey { get; }

        public SignalR(string hostName, string accessKey, ServiceOptions options = null)
        {
            HostName = hostName;
            AccessKey = accessKey;
            _options = options ?? new ServiceOptions();
        }

        #region Public Methods

        public string GetClientUrl<THub>() where THub : Hub
        {
            // TODO: Use HTTPS
            return $"http://{HostName}/client/?hub={typeof(THub).Name.ToLower()}";
        }

        public string GetServerUrl<THub>() where THub : Hub
        {
            // TODO: Use HTTPS
            return $"http://{HostName}/server/?hub={typeof(THub).Name.ToLower()}";
        }

        public string GenerateClientToken<THub>(IEnumerable<Claim> claims = null) where THub : Hub
        {
            return AuthenticationHelper.GenerateJwtBearer(
                audience: GetClientUrl<THub>(),
                claims: claims,
                expires: DateTime.UtcNow.Add(JwtBearerLifetime),
                signingKey: AccessKey
            );
        }

        public string GenerateServerToken<THub>() where THub : Hub
        {
            return AuthenticationHelper.GenerateJwtBearer(
                audience: GetServerUrl<THub>(),
                expires: DateTime.UtcNow.Add(JwtBearerLifetime),
                signingKey: AccessKey
            );
        }

        public void CreateServiceClient<THub>() where THub : Hub
        {
            throw new NotImplementedException();
        }

        public IHubClients<IServiceClientProxy> CreateHubClientsProxy<THub>() where THub : Hub
        {
            return new HubClientsProxy<THub>(this);
        }

        #endregion

        #region Static Methods

        public static bool TryParse(string connectionString, out SignalR signalr)
        {
            signalr = null;
            if (string.IsNullOrEmpty(connectionString)) return false;

            var dict = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] { '=' }, 2))
                .ToDictionary(t => t[0].Trim().ToLower(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            if (!dict.ContainsKey(HostNameProperty) || !dict.ContainsKey(AccessKeyProperty)) return false;

            signalr = new SignalR(dict[HostNameProperty], dict[AccessKeyProperty]);
            return true;
        }

        public static SignalR Parse(string connectionString)
        {
            return TryParse(connectionString, out var signalr)
                ? signalr
                : throw new ArgumentException($"Invalid connection string: {connectionString}");
        }

        #endregion
    }
}
