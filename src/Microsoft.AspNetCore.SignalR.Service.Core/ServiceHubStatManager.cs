using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubStatManager<THub> : IHubStatManager<THub> where THub : Hub
    {
        private Stats _stat;
        private long _lastReadBytes;
        private long _lastWriteBytes;
        private long _readRate;
        private long _writeRate;
        private readonly object _lock = new object();
        public ServiceHubStatManager()
        {
            _stat = new Stats();
        }

        public Stats Stat() => _stat;
        public Task GetHubStat(HttpContext context)
        {
            long readRate, writeRate;
            lock (_lock)
            {
                readRate = _readRate;
                writeRate = _writeRate;
            }
            return context.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                ReadTotalSize = _stat.ReadDataSize,
                WriteTotalSize = _stat.WriteDataSize,
                TCPReadSize = _stat.TCPReadSize,
                TCPWriteSize = _stat.TCPWriteSize,
                ReadRate = readRate,
                WriteRate = writeRate,
                PendingWrite = _stat.PendingWrite
            }));
        }

        public void Tick(DateTimeOffset now)
        {
            lock (_lock)
            {
                _readRate = _stat.ReadDataSize - _lastReadBytes;
                _writeRate = _stat.WriteDataSize - _lastWriteBytes;
            }
            Interlocked.Exchange(ref _lastReadBytes, _stat.ReadDataSize);
            Interlocked.Exchange(ref _lastWriteBytes, _stat.WriteDataSize);
        }
    }
}
