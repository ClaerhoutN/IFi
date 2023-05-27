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
        public async Task<Dictionary<string, Stock[]>> GetStocksAsync(string[] symbols, DateTime from, DateTime to, string exchange = null, bool descending = true)
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
                    else return null;
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
                result.Add(stockGroup.Key, (descending ? stockGroup.OrderByDescending(x => x.Date) : stockGroup.OrderBy(x => x.Date)).ToArray());
            }

            return result;
        }
        public async Task<Stock[]> GetLatestStocksAsync(string[] symbols, string exchange = null)
        {
            DateTimeOffset now = DateTimeOffset.Now; //what about closed days?
            List<string> symbolsNotFound = symbols.ToList();
            List<Stock> stocksFound = new List<Stock>();
            foreach(string symbol in symbols)
            {
                if(_stockCache.TryGetValue(symbol, out var stocks))
                {
                    Stock stock = stocks.FirstOrDefault(x => x.Date == now);
                    if(stock != null)
                    {
                        stocksFound.Add(stock);
                        symbolsNotFound.Remove(symbol);
                    }
                }
            }

            //todo: check cache by current date
            if (symbolsNotFound.Any())
            {
                string requestUri = $"eod/latest?symbols={string.Join(',', symbolsNotFound)}";
                if (exchange != null)
                    requestUri += $"&exchange={exchange}";
                var response = await _client.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var eod = JsonSerializer.Deserialize<Eod>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
                    foreach (var stock in eod.Data)
                    {
                        _stockCache.AddOrUpdate(stock.Symbol, symbol => new() { stock }, (symbol, _stocks) => Merge(_stocks, stock));
                    }
                    return stocksFound.Concat(eod.Data).ToArray();
                }
            }
            else
                return stocksFound.ToArray();

            return null;
        }

        private static List<Stock> Merge(IEnumerable<Stock> group1, IEnumerable<Stock> group2)
        {
            List<Stock> sortedGroup1 = group1.OrderBy(x => x.Date).ToList();
            List<Stock> result = sortedGroup1;
            int offset = 0;
            foreach(var stock2 in group2.OrderBy(stock => stock.Date))
            {
                bool found = false;
                for(int i = offset; i < sortedGroup1.Count; ++i)
                {
                    Stock stock1 = sortedGroup1[i];
                    if(stock1.Date < stock2.Date)
                        offset = i+1;
                    if(stock1.Date == stock2.Date)
                    {
                        found = true;
                        break;
                    }
                }
                if(!found)
                    result.Add(stock2);
            }
            return result;
        }
        private static List<Stock> Merge(IEnumerable<Stock> group, Stock stock) => Merge(group, stock.Yield());
    }
}
