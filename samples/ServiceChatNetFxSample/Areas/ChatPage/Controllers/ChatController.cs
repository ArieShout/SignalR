using System.Web.Http;
using System.Web.Mvc;

namespace ServiceChatNetFxSample.Areas.ChatPage.Controllers
{
    /// <summary>
    /// The controller that will handle requests for the help page.
    /// </summary>
    public class ChatController : Controller
    {
        private const string ErrorViewName = "Error";

        public ChatController()
            : this(GlobalConfiguration.Configuration)
        {
        }

        public ChatController(HttpConfiguration config)
        {
            Configuration = config;
        }

        public HttpConfiguration Configuration { get; private set; }

        public ActionResult Index()
        {
            ViewBag.DocumentationProvider = Configuration.Services.GetDocumentationProvider();
            return View("Index");
        }
    }
}