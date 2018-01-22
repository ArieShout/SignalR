using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Core
{
    public class ServiceMetrics
    {
        private const string ReceiveMsgFromClientStage = "A";
        private const string SendMsgToServerStage = "B";
        private const string ReceiveMsgFromServiceStage = "C";
        private const string SendMsgToServiceStage = "D";
        private const string ReceiveMsgFromServerStage = "E";
        private const string SendMsgToClientStage = "F";

        public static void MarkReceiveMsgFromClientStage(IDictionary<string, string> meta)
        {
            meta.Add(ReceiveMsgFromClientStage, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        public static void MarkSendMsgToServerStage(IDictionary<string, string> meta)
        {
            meta.Add(SendMsgToServerStage, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        public static void MarkReceiveMsgFromServiceStage(IDictionary<string, string> meta)
        {
            meta.Add(ReceiveMsgFromServiceStage, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        public static void MarkSendMsgToServiceStage(IDictionary<string, string> meta)
        {
            if (!meta.ContainsKey(SendMsgToServiceStage))
            {
                meta.Add(SendMsgToServiceStage, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            }
        }

        public static void MarkReceiveMsgFromServerStage(IDictionary<string, string> meta)
        {
            meta.Add(ReceiveMsgFromServerStage, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        public static void MarkSendMsgToClientStage(IDictionary<string, string> meta)
        {
            meta.Add(SendMsgToClientStage, Convert.ToString(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        public static long GetServerHandleDuration(IDictionary<string, string> meta)
        {
            if (meta.ContainsKey(ReceiveMsgFromServerStage) && meta.ContainsKey(ReceiveMsgFromServerStage))
            {
                meta.TryGetValue(ReceiveMsgFromServerStage, out var recvFromServer);
                meta.TryGetValue(SendMsgToServerStage, out var sendToServer);
                Int64 dur = (Convert.ToInt64(recvFromServer) - Convert.ToInt64(sendToServer)) / 1000000;
                return (long)dur;
            }
            return 0;
        }
    }
}
