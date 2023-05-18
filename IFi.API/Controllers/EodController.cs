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
            string exchange = paramValues["exchange"];
            IEnumerable<Stock> allStocks = null;

            if (uri == "eod/latest")
            {
                allStocks = await _stockRepository.GetLatestStocksAsync(symbols, exchange);
            }
            else if (uri == "eod")
            {
                if (!DateTime.TryParseExact(paramValues["date_from"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime from))
                    throw new ArgumentException("date_from parameter missing or is in an incorrect format.");
                if (!DateTime.TryParseExact(paramValues["date_to"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime to))
                    throw new ArgumentException("date_to parameter missing or is in an incorrect format.");
                var stocksDictionary = await _stockRepository.GetStocksAsync(symbols, from, to, exchange);
                allStocks = stocksDictionary.Values.SelectMany(x => x);                
            }
            else
            {
                throw new NotImplementedException();
            }

            IEnumerable<Stock> stocks = allStocks;
            if (int.TryParse(paramValues["offset"], out int offset))
                stocks = stocks.Skip(offset);
            if (int.TryParse(paramValues["limit"], out int limit))
                stocks = stocks.Take(limit);
            Stock[] stocksArray = stocks.ToArray();
            if (stocks != null)
                return new Eod { Pagination = new Pagination { Total = allStocks.Count(), Count = stocksArray.Length, Limit = limit, Offset = offset }, Data = stocksArray };

            return null;
        }
    }
}
