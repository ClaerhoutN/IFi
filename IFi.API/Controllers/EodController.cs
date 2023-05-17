using IFi.Domain.ApiResponse;
using IFi.Utilities.JsonConverters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Web;

namespace IFi.API.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class EodController : ControllerBase
    {
        private readonly HttpClient _client;
        private readonly StockRepository _stockRepository;
        public EodController(IHttpClientFactory clientFactory, StockRepository stockRepository)
        {
            _client = clientFactory.CreateClient(Constants.MarketStackClientName);
            _stockRepository = stockRepository;
        }
        [HttpGet("")]
        public async Task<IActionResult> Historical()
        {
            var eod = await GetEodAsync("eod", Request.QueryString);
            if(eod == null) return NotFound();
            return Ok(eod);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> Latest()
        {
            var eod = await GetEodAsync("eod/latest", Request.QueryString);
            if (eod == null) return NotFound();
            return Ok(eod);
        }

        private async Task<Eod> GetEodAsync(string uri, QueryString queryString)
        {
            var paramValues = HttpUtility.ParseQueryString(queryString.Value);
            string[] symbols = paramValues["symbols"].Split(',');
            if (uri == "eod/latest")
            {
                var stocks = await _stockRepository.GetLatestStocksAsync(symbols);
                if(stocks != null)
                    return new Eod { Pagination = new Pagination {  }, Data = stocks };

            }
            else if (uri == "eod")
            {
                //todo: account for limit/offset parameters?
                DateTime from = DateTime.ParseExact(paramValues["date_from"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime to = DateTime.ParseExact(paramValues["date_to"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                var stocksDictionary = await _stockRepository.GetStocksAsync(symbols, from, to);
                var stocks = stocksDictionary.Values.SelectMany(x => x).ToArray();
                if (stocks != null)
                    return new Eod { Pagination = new Pagination { Total = stocks.Length, Count = stocks.Length }, Data = stocks };
            }
            else
            {
                throw new NotImplementedException();
            }
            return null;
        }
    }
}
