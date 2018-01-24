using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ServiceChatSample
{
    [Route("signalr-auth")]
    public class SignalRAuthController : Controller
    {
        private readonly SignalR _signalr;

        public SignalRAuthController(SignalR signalr)
        {
            _signalr = signalr;
        }

        [HttpGet("chat")]
        public IActionResult GenerateJwtBearer()
        {
            var token = _signalr.GenerateClientToken<Chat>(new[]
            {
                new Claim(ClaimTypes.Name, "username"),
                new Claim(ClaimTypes.NameIdentifier, "user_id")
            });
            return new OkObjectResult(token);
        }
    }
}