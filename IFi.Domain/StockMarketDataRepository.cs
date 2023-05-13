using IFi.Domain.ApiResponse;
using IFi.Utilities.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IFi.Domain
{
    public class StockMarketDataRepository
    {
        private readonly static HttpClient _httpClient = new HttpClient();
        static StockMarketDataRepository()
        {
            _httpClient.BaseAddress = new Uri("https://ifi-dev.azurewebsites.net/");
        }
        public Task<IReadOnlyList<Stock>> GetStocksAsync(string[] tickers) => GetStocksAsync(tickers, null);
        public async Task<IReadOnlyList<Stock>> GetStocksAsync(string[] tickers, string exchange)
        {
            try
            {
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                string requestUri = $"eod/latest?symbols={string.Join(',', tickers)}";
                if (!string.IsNullOrWhiteSpace(exchange))
                    requestUri += "&exchange={exchange}";
                var response = await _httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.Converters.Add(new DateTimeOffsetConverter_ISO8601());

                    var eod = JsonSerializer.Deserialize<Eod>(json, options);
                    return eod.Data.ToList();
                }
                return new List<Stock>() { }; //todo
            }
            catch
            {
                return new List<Stock>() { }; //todo
            }
        }
        public async Task<Ticker> GetTickerAsync(string ticker)
        {
            try
            {

                string requestUri = $"tickers/{ticker}";
                var response = await _httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.Converters.Add(new DateTimeOffsetConverter_ISO8601());

                    return JsonSerializer.Deserialize<Ticker>(json, options);
                }
                return null; //todo
            }
            catch
            {
                return null; //todo
            }
        }
        public async Task<Ticker[]> GetTickersAsync(string search)
        {
            try
            {

                string requestUri = $"tickers?search={search}";
                var response = await _httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.Converters.Add(new DateTimeOffsetConverter_ISO8601());

                    var tickers = JsonSerializer.Deserialize<Tickers>(json, options);
                    return tickers.Data;
                }
                return new Ticker[0]; //todo
            }
            catch
            {
                return new Ticker[0]; //todo
            }
        }
        private const int limit = 1000;
        public async Task<Dictionary<string, Stock[]>> GetHistoricalDataAsync(string[] tickers, DateTime from, DateTime to)
        {
            List<Stock> data = new List<Stock>();
            int offset = 0;
            int total = -1;
            do
            {
                try
                {
                    string requestUri = $"eod?symbols={string.Join(',', tickers)}&date_from={from.ToString("yyyy-MM-dd")}&date_to={to.ToString("yyyy-MM-dd")}&limit={limit}&offset={offset}";
                    var response = await _httpClient.GetAsync(requestUri);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();

                        JsonSerializerOptions options = new JsonSerializerOptions();
                        options.Converters.Add(new DateTimeOffsetConverter_ISO8601());

                        var eod = JsonSerializer.Deserialize<Eod>(json, options);

                        data.AddRange(eod.Data);

                        total = eod.Pagination.Total;
                        offset += eod.Pagination.Count;
                    }
                    else break;
                }
                catch
                {
                    break;
                }
            }
            while (offset < total);

            return data.GroupBy(x => x.Symbol).ToDictionary(x => x.Key, x => x.ToArray());
        }
    }
}
