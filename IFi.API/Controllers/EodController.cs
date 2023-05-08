using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IFi.API.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class EodController : ControllerBase
    {
        private readonly HttpClient _client;
        public EodController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient(Constants.MarketStackClientName);
        }
        [HttpGet("")]
        public async Task<IActionResult> Historical()
        {
            var response = await _client.GetAsync("eod" + Request.QueryString);
            return new HttpResponseMessageResult(response);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> Latest()
        {
            var response = await _client.GetAsync("eod/latest" + Request.QueryString);
            return new HttpResponseMessageResult(response);
        }
    }
}
