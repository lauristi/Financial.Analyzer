using Microsoft.AspNetCore.Mvc;
using Server.Api.Domain.Model;
using Server.Api.Domain.Service.InfrastrutureService.Interface;

namespace Server.Api.Controllers
{
    public class ClipboardController : Controller
    {
        //Injetando ICrypto (Because injeção é legal)
        private readonly IEncryptionService _encryptionService;

        private readonly ILogger<ClipboardController> _logger;

        private static Dictionary<string, string> clipboardData = new Dictionary<string, string>();

        public ClipboardController(IEncryptionService encryptionService,
                                   ILogger<ClipboardController> logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/clipboard/post")]
        public Task<IActionResult> PostClipBoard([FromBody] Transmition transmition)
        {
            transmition.Content = transmition.Content ?? "";
            clipboardData["0"] = _encryptionService.Encrypt(transmition.Content);
            //------------------------------------------------------------
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route("api/clipboard/get")]
        public Task<IActionResult> GetClipBoard()
        {
            ClipboardResponse clipboardResponse = new ClipboardResponse();

            if (clipboardData.ContainsKey("0"))
            {
                clipboardResponse.Clipboard = _encryptionService.Decrypt(clipboardData["0"]);

                return Task.FromResult<IActionResult>(Ok(clipboardResponse));
            }
            else
            {
                return Task.FromResult<IActionResult>(Ok(clipboardResponse));
            }
        }
    }
}