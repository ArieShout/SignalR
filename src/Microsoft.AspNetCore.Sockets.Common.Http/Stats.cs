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
        private long _lastReadDataSize;
        private long _lastWriteDataSize;
        private long _readRate;
        private long _writeRate;

        private long _recvRequest;
        private long _sendRequest;

        private long _tcpReadSize;
        private long _tcpWriteSize;
        private long _serviceSend2ClientReq;
        private long _serviceRecvFromClientReq;
        private long _serviceSend2ServerReq;
        private long _serviceRecvFromServerReq;
        private long _serverPendingWrite;
        private long _servicePendingWrite;
        private long _write2Channel;
        private long _readFromChannel;

        public Stats()
        {
        }

        public long LastReadDataSize => Interlocked.Read(ref _lastReadDataSize);

        public long LastWriteDataSize => Interlocked.Read(ref _lastWriteDataSize);

        public void ReportLastReadDataSize(long lr)
        {
            Interlocked.Exchange(ref _lastReadDataSize, lr);
        }

        public void ReportLastWriteDataSize(long lw)
        {
            Interlocked.Exchange(ref _lastWriteDataSize, lw);
        }
        public long ReadRate => Interlocked.Read(ref _readRate);
        public long WriteRate => Interlocked.Read(ref _writeRate);

        public void ReportReadRate(long r)
        {
            Interlocked.Exchange(ref _readRate, r);
        }

        public void ReportWriteRate(long w)
        {
            Interlocked.Exchange(ref _writeRate, w);
        }

        public long Write2Channel => Interlocked.Read(ref _write2Channel);
        public long ReadFromChannel => Interlocked.Read(ref _readFromChannel);
        public long WriteDataSize => Interlocked.Read(ref _writeDataSize);

        public long ReadDataSize => Interlocked.Read(ref _readDataSize);

        public long TCPReadSize => Interlocked.Read(ref _tcpReadSize);

        public long TCPWriteSize => Interlocked.Read(ref _tcpWriteSize);

        public long ServiceSend2ClientReq => Interlocked.Read(ref _serviceSend2ClientReq);

        public long ServiceRecvFromClientReq => Interlocked.Read(ref _serviceRecvFromClientReq);

        public long ServiceSend2ServerReq => Interlocked.Read(ref _serviceSend2ServerReq);

        public long ServiceRecvFromServerReq => Interlocked.Read(ref _serviceRecvFromServerReq);

        public long ServerPendingWrite => Interlocked.Read(ref _serverPendingWrite);

        public long ServicePendingWrite => Interlocked.Read(ref _servicePendingWrite);

        public long RecvRequset => Interlocked.Read(ref _recvRequest);

        public long SendRequest => Interlocked.Read(ref _sendRequest);

        public void AddWrite2Channel(long bytes)
        {
            Interlocked.Add(ref _write2Channel, bytes);
        }

        public void AddReadFromChannel(long bytes)
        {
            Interlocked.Add(ref _readFromChannel, bytes);
        }

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

        public void AddServerPendingWrite(long count)
        {
            Interlocked.Add(ref _serverPendingWrite, count);
        }

        public void AddServicePendingWrite(long count)
        {
            Interlocked.Add(ref _servicePendingWrite, count);
        }

        public void AddServiceSend2ClientReq(long count)
        {
            Interlocked.Add(ref _serviceSend2ClientReq, count);
        }

        public void AddServiceRecvFromClientReq(long count)
        {
            Interlocked.Add(ref _serviceRecvFromClientReq, count);
        }

        public void AddServiceSend2ServerReq(long count)
        {
            Interlocked.Add(ref _serviceSend2ServerReq, count);
        }
        public void AddServiceRecvFromServerReq(long count)
        {
            Interlocked.Add(ref _serviceRecvFromServerReq, count);
        }

        public void AddRecvRequest(long count)
        {
            Interlocked.Add(ref _recvRequest, count);
        }

        public void AddSendRequest(long count)
        {
            Interlocked.Add(ref _sendRequest, count);
        }
    }
}