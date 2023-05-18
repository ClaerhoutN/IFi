using IFi.Domain.ApiResponse;
using IFi.Utilities;
using IFi.Utilities.JsonConverters;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;

namespace IFi.API
{
    public class StockRepository
    {
        private readonly HttpClient _client;
        public StockRepository(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient(Constants.MarketStackClientName);
        }
        private static readonly ConcurrentDictionary<string, List<Stock>> _stockCache = new ConcurrentDictionary<string, List<Stock>>();
        private const int limit = 1000;
        public async Task<Dictionary<string, Stock[]>> GetStocksAsync(string[] symbols, DateTime from, DateTime to, string exchange = null)
        {
            //todo: check cache to find missing dates
            List<Stock> data = new List<Stock>();
            int offset = 0;
            int total = -1;
            do
            {
                try
                {
                    string requestUri = $"eod?symbols={string.Join(',', symbols)}&date_from={from.ToString("yyyy-MM-dd")}&date_to={to.ToString("yyyy-MM-dd")}&limit={limit}&offset={offset}";
                    if (exchange != null)
                        requestUri += $"&exchange={exchange}";
                    var response = await _client.GetAsync(requestUri);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var eod = JsonSerializer.Deserialize<Eod>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);

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

            Dictionary<string, Stock[]> result = new Dictionary<string, Stock[]>();
            foreach (var stockGroup in data.GroupBy(x => x.Symbol))
            {
                _stockCache.AddOrUpdate(stockGroup.Key, symbol => stockGroup.ToList(), (symbol, _stocks) => Merge(_stocks, stockGroup));
                result.Add(stockGroup.Key, stockGroup.ToArray());
            }

            return result;
        }
        public async Task<Stock[]> GetLatestStocksAsync(string[] symbols, string exchange = null)
        {
            //todo: check cache by current date
            string requestUri = $"eod/latest?symbols={string.Join(',', symbols)}";
            if (exchange != null)
                requestUri += $"&exchange={exchange}";
            var response = await _client.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var eod = JsonSerializer.Deserialize<Eod>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
                foreach (var stock in eod.Data)
                {
                    _stockCache.AddOrUpdate(stock.Symbol, symbol => new(){ stock }, (symbol, _stocks) => Merge(_stocks, stock));
                }
                return eod.Data;
            }

            return null;
        }

        private static List<Stock> Merge(IEnumerable<Stock> group1, IEnumerable<Stock> group2)
        {
            //todo: check dates
            return group1.Concat(group2).ToList();
        }
        private static List<Stock> Merge(IEnumerable<Stock> group, Stock stock)
        {
            //todo: check dates
            return group.Concat(stock.Yield()).ToList();
        }
    }
}
