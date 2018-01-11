namespace Microsoft.AspNetCore.SignalR.Client
{
    public class MessageResult
    {
        public bool IsSuccess { get; set; }

        public int? ErrorCode { get; set; }

        public string Message { get; set; }

        public static MessageResult Success()
        {
            return new MessageResult
            {
                IsSuccess = true
            };
        }

        public static MessageResult Error(int? code, string message)
        {
            return new MessageResult
            {
                IsSuccess = false,
                ErrorCode = code,
                Message = message
            };
        }
    }
}
