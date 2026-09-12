using Microsoft.AspNetCore.Mvc;
using SwiftCare.Services.AI;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Controllers
{
    public class SwiftCareLlmTestController : Controller
    {
        private readonly ISwiftCareLlmService _llmService;

        public SwiftCareLlmTestController(
            ISwiftCareLlmService llmService)
        {
            _llmService = llmService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _llmService.GenerateAsync(
                    "You are a test assistant for the SwiftCare volunteer management system.",
                    "Reply with exactly: SWIFTCARE LLM IS WORKING",
                    cancellationToken);

                return Content(
                    response,
                    "text/plain; charset=utf-8");
            }
            catch (System.Exception ex)
            {
                return Content(
                    "SWIFTCARE LLM TEST FAILED\n\n" +
                    ex.ToString(),
                    "text/plain; charset=utf-8");
            }
        }
    }
}