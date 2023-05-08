using Microsoft.AspNetCore.Mvc;

namespace IFi.API.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class TickersController : ControllerBase
    {
        private readonly HttpClient _client;
        public TickersController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient(Constants.MarketStackClientName);
        }
        [HttpGet("")]
        public async Task<IActionResult> TickerSearch([FromQuery]string search)
        {
            var response = await _client.GetAsync($"tickers?search={search}");
            return new HttpResponseMessageResult(response);
        }
        [HttpGet("{ticker}")]
        public async Task<IActionResult> Ticker(string ticker)
        {
            var response = await _client.GetAsync($"tickers/{ticker}");
            return new HttpResponseMessageResult(response);
        }
    }
}
