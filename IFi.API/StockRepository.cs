using IFi.Domain.ApiResponse;
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
        private static readonly ConcurrentDictionary<string, Stock[]> _stockCache = new ConcurrentDictionary<string, Stock[]>();
        private const int limit = 1000;
        public async Task<Dictionary<string, Stock[]>> GetStocksAsync(string[] symbols, DateTime from, DateTime to)
        {
            //todo: check cache to find missing dates
            List<Stock> data = new List<Stock>();
            int offset = 0;
            int total = -1;
            do
            {
                try
                {
                    var response = await _client.GetAsync($"eod?symbols={string.Join(',', symbols)}&date_from={from.ToString("yyyy-MM-dd")}&date_to={to.ToString("yyyy-MM-dd")}&limit={limit}&offset={offset}");
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
                Stock[] stocks = stockGroup.ToArray();
                _stockCache.AddOrUpdate(stockGroup.Key, symbol => stocks, (symbol, _stocks) => Merge(_stocks, stocks));
                result.Add(stockGroup.Key, stocks);
            }

            return result;
        }
        public async Task<Stock[]> GetLatestStocksAsync(string[] symbols)
        {
            //todo: check cache by current date
            var response = await _client.GetAsync($"eod/latest?symbols={string.Join(',', symbols)}");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var eod = JsonSerializer.Deserialize<Eod>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
                return eod?.Data;
            }

            return null;
        }

        private static Stock[] Merge(Stock[] group1, Stock[] group2)
        {
            //todo: check dates
            return group1.Concat(group2).ToArray();
        }
    }
}
