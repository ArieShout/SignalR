// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Sockets;
using System.Threading;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class DefaultHubStatusManager : IHubStatusManager
    {
        private readonly ConcurrentDictionary<string, int> _clientConnections = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _serverConnections = new ConcurrentDictionary<string, int>();

        private readonly ConcurrentDictionary<string, int> _clientMessages = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _serverMessages = new ConcurrentDictionary<string, int>();

        private Stats _stat = new Stats();
        private Stats _clientStat = new Stats();
        private Stats _serverStat = new Stats();

        public async Task AddClientConnection(string hubName)
        {
            await Task.CompletedTask;
            _clientConnections.AddOrUpdate(hubName, _ => 1, (_, x) => x + 1);
        }

        public async Task AddServerConnection(string hubName)
        {
            await Task.CompletedTask;
            _serverConnections.AddOrUpdate(hubName, _ => 1, (_, x) => x + 1);
        }

        public async Task RemoveClientConnection(string hubName)
        {
            await Task.CompletedTask;
            _clientConnections.TryUpdate(hubName, x => x - 1);
        }

        public async Task RemoveServerConnection(string hubName)
        {
            await Task.CompletedTask;
            _serverConnections.TryUpdate(hubName, x => x - 1);
        }

        public async Task AddClientMessage(string hubName)
        {
            await Task.CompletedTask;
            _clientMessages.AddOrUpdate(hubName, _ => 1, (_, x) => x + 1);
        }

        public async Task AddServerMessage(string hubName)
        {
            await Task.CompletedTask;
            _serverMessages.AddOrUpdate(hubName, _ => 1, (_, x) => x + 1);
        }

        public Task GetHubStatus(HttpContext context)
        {
            var clientStats = _clientConnections.Select(x => BuildHubStatus(x, _clientMessages)).ToArray();
            var serverStats = _serverConnections.Select(x => BuildHubStatus(x, _serverMessages)).ToArray();
            var clientOverall = new HubStatus
            {
                ConnectionCount = clientStats.Sum(x => x.ConnectionCount),
                MessageCount = clientStats.Sum(x => x.MessageCount)
            };
            var serverOverall = new HubStatus
            {
                ConnectionCount = serverStats.Sum(x => x.ConnectionCount),
                MessageCount = serverStats.Sum(x => x.MessageCount)
            };

            int worker = 0;
            int io = 0;
            ThreadPool.GetAvailableThreads(out worker, out io);

            return context.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                ConnectionId = context.Connection.Id,
                overall = new
                {
                    client = clientOverall,
                    server = serverOverall
                },
                AvailableWorker = worker,
                AvailableIO = io,
                client = clientStats,
                server = serverStats,
                ClientWrite2Channel = _clientStat.Write2Channel,
                ClientReadFromChannel = _clientStat.ReadFromChannel,
                ServerWrite2Channel = _serverStat.Write2Channel,
                ServerReadFromChannel = _serverStat.ReadFromChannel,
                Send2ClientReq = _stat.ServiceSend2ClientReq,
                RecvFromClientReq = _stat.ServiceRecvFromClientReq,
                Send2ServerReq = _stat.ServiceSend2ServerReq,
                RecvFromServerReq = _stat.ServiceRecvFromServerReq,
                //ServicePendingWrite = _stat.ServicePendingWrite,
                ServerLastReadData = _serverStat.LastReadDataSize,
                ServerLastWriteData = _serverStat.LastWriteDataSize,
                ServerTotalReadData = _serverStat.ReadDataSize,
                ServerTotalWriteData = _serverStat.WriteDataSize,
                ServerReadRate = _serverStat.ReadRate,
                ServerWriteRate = _serverStat.WriteRate,
                ClientLastReadData = _clientStat.LastReadDataSize,
                ClientLastWriteData = _clientStat.LastWriteDataSize,
                ClientTotalReadData = _clientStat.ReadDataSize,
                ClientTotalWriteData = _clientStat.WriteDataSize,
                ClientReadRate = _clientStat.ReadRate,
                ClientWriteRate = _clientStat.WriteRate
            }));
        }

        private static HubStatus BuildHubStatus(KeyValuePair<string, int> kvp,
            IReadOnlyDictionary<string, int> messageStat)
        {
            return new HubStatus
            {
                HubName = kvp.Key,
                ConnectionCount = kvp.Value,
                MessageCount = messageStat.TryGetValue(kvp.Key, out var count) ? count : 0
            };
        }

        public void AddServicePendingWrite(long count)
        {
            _stat.AddServicePendingWrite(count);
        }

        public void AddSend2ClientReq(long count)
        {
            _stat.AddServiceSend2ClientReq(count);
        }

        public void AddRecvFromClientReq(long count)
        {
            _stat.AddServiceRecvFromClientReq(count);
        }

        public void AddSend2ServerReq(long count)
        {
            _stat.AddServiceSend2ServerReq(count);
        }

        public void AddRecvFromServerReq(long count)
        {
            _stat.AddServiceRecvFromServerReq(count);
        }

        public Stats GetGlobalStat4Client()
        {
            return _clientStat;
        }

        public Stats GetGlobalStat4Server()
        {
            return _serverStat;
        }
    }

    internal class HubStatus
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string HubName { get; set; }

        public int ConnectionCount { get; set; }

        public int MessageCount { get; set; }
    }
}
