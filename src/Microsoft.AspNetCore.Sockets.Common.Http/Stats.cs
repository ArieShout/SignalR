using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.AspNetCore.Sockets
{
    public class Stats
    {
        // statistic information
        private long _readDataSize;
        private long _writeDataSize;
        private long _tcpReadSize;
        private long _tcpWriteSize;
        private long _pendingWrite;
        public Stats()
        {
        }
        public long WriteDataSize => Interlocked.Read(ref _writeDataSize);

        public long ReadDataSize => Interlocked.Read(ref _readDataSize);

        public long TCPReadSize => Interlocked.Read(ref _tcpReadSize);

        public long TCPWriteSize => Interlocked.Read(ref _tcpWriteSize);

        public long PendingWrite => Interlocked.Read(ref _pendingWrite);

        public void BytesWrite(long bytes)
        {
            Interlocked.Add(ref _writeDataSize, bytes);
        }

        public void BytesRead(long bytes)
        {
            Interlocked.Add(ref _readDataSize, bytes);
        }

        public void TCPBytesWrite(long bytes)
        {
            Interlocked.Add(ref _tcpWriteSize, bytes);
        }
        public void TCPBytesRead(long bytes)
        {
            Interlocked.Add(ref _tcpReadSize, bytes);
        }

        public void AddPendingWrite(long count)
        {
            Interlocked.Add(ref _pendingWrite, count);
        }
    }
}
