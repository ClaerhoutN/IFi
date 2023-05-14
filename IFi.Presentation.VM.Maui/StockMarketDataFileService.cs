using IFi.Domain;
using IFi.Domain.ApiResponse;
using IFi.Domain.Models;
using IFi.Presentation.VM.Maui.ViewModels;
using IFi.Utilities.JsonConverters;
using LiveChartsCore.Measure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IFi.Presentation.VM.Maui
{
    public class StockMarketDataFileService
    {
        private readonly string _historicalDataDirectory = "historicalData";
        private readonly string _stockPositionsFileName = Path.Combine(FileSystem.Current.AppDataDirectory, "stockPositions.txt");

        private readonly StockMarketDataRepository _repo = new();
        private DateTime HistoricalDataFrom => DateTime.Today.AddYears(-1);
        private DateTime HistoricalDataTo => DateTime.Today;

        private readonly Action<IEnumerable<StockPosition>> _setStockPositions;
        private readonly Func<IEnumerable<StockPosition>> _getStockPositions;
        private readonly PropertyChangedEventHandler _stockPositionPropertyChanged;

        public StockMarketDataFileService(Action<IEnumerable<StockPosition>> setStockPositions, Func<IEnumerable<StockPosition>> getStockPositions, PropertyChangedEventHandler stockPositionPropertyChanged)
        {
            _setStockPositions = setStockPositions;
            _getStockPositions = getStockPositions;
            _stockPositionPropertyChanged = stockPositionPropertyChanged;
        }

        public async Task<IReadOnlyList<StockPosition>> GetStockPositionsAsync()
        {
            var (stockPositions, needsUpdate) = await ReadStockPositionsAsync();
            if (needsUpdate)
            {
                var stocks = await _repo.GetStocksAsync(stockPositions
                    .Where(x => !Currency.IsCurrency(x.Stock))
                    .Select(x => x.Stock.Symbol).ToArray());
                foreach (var stockPosition in stockPositions)
                    stockPosition.Stock = stocks.FirstOrDefault(x => x.Symbol == stockPosition.Stock.Symbol) ?? stockPosition.Stock;
            }
            var historicalData = await GetHistoricalDataAsync(HistoricalDataFrom, HistoricalDataTo, stockPositions.Select(x => x.Stock.Symbol).ToArray());
            foreach (var stockPosition in stockPositions)
            {
                if (string.IsNullOrEmpty(stockPosition.Ticker?.Name))
                {
                    if(Currency.IsCurrency(stockPosition.Stock))
                        stockPosition.Ticker = Currency.FindTicker(stockPosition.Stock);
                    else                    
                        stockPosition.Ticker = await _repo.GetTickerAsync(stockPosition.Stock.Symbol);
                    needsUpdate = true;
                }
                stockPosition.HistoricalData = historicalData[stockPosition.Stock.Symbol];
            }
            foreach (var stockPosition in stockPositions)
            {
                stockPosition.Refresh(stockPositions);
            }
            if (needsUpdate)
                await SaveStockPositionsAsync(stockPositions);
            return stockPositions;
        }
        //guaranteed to have all symbols as keys
        public async Task<Dictionary<string, Stock[]>> GetHistoricalDataAsync(DateTime from, DateTime to, params string[] symbols)
        {
            Dictionary<string, Stock[]> dataBySymbol = new Dictionary<string, Stock[]>();
            foreach (string symbol in symbols)
            {
                Stock[] data = null;
                if (Currency.IsCurrency(symbol))
                    data = new Stock[0];
                else
                    data = await ReadHistoricalDataAsync(symbol);
                if (data != null)
                    dataBySymbol.Add(symbol, data);
            }
            string[] symbolsToFetch = symbols.Where(x => !dataBySymbol.ContainsKey(x)).ToArray();
            var historicalData = await _repo.GetHistoricalDataAsync(symbolsToFetch, from, to);
            foreach (string symbol in symbolsToFetch)
            {
                if (historicalData.TryGetValue(symbol, out var value))
                {
                    dataBySymbol.Add(symbol, value);
                    await SaveHistoricalDataAsync(value, symbol);
                }
                else
                    dataBySymbol.Add(symbol, new Stock[0]);
            }
            return dataBySymbol;
        }
        private async Task<Stock[]> ReadHistoricalDataAsync(string symbol)
        {
            string path = Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory, $"{symbol}.txt");
            Stock[] data = null;
            if (File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path);
                data = JsonSerializer.Deserialize<Stock[]>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            }
            return data;
        }
        private Task SaveHistoricalDataAsync(Stock[] data, string symbol)
        {
            if (!Directory.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory)))
                Directory.CreateDirectory(Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory));
            string path = Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory, $"{symbol}.txt");
            string json = JsonSerializer.Serialize(data, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            return File.WriteAllTextAsync(path, json);
        }

        private async Task<(List<StockPosition>, bool)> ReadStockPositionsAsync()
        {
            bool needsUpdate = false;
            FileInfo fi = new(_stockPositionsFileName);
            if (fi.Exists)
            {
                if (fi.LastWriteTime.Date != DateTime.Today)
                    needsUpdate = true;
                string json = await File.ReadAllTextAsync(_stockPositionsFileName);
                var stockPositions = JsonSerializer.Deserialize<List<StockPosition>>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
                return (stockPositions, needsUpdate);
            }
            return (new List<StockPosition>(), needsUpdate);
        }

        public Task SaveStockPositionsAsync(IReadOnlyList<StockPosition> stockPositions)
        {
            string json = JsonSerializer.Serialize(stockPositions, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            return File.WriteAllTextAsync(_stockPositionsFileName, json);
        }

        internal async Task AddStockPositionAsync(string symbol)
        {
            var ticker = await _repo.GetTickerAsync(symbol);
            await AddStockPositionAsync(ticker);
        }

        internal async Task AddStockPositionAsync(Ticker ticker)
        {
            var stockPositions = _getStockPositions().ToList();
            Stock[] historicalData = null;
            Stock stock = null;
            if (Currency.IsCurrency(ticker))
            {
                historicalData = new Stock[0];
                stock = Currency.AsStock(ticker);
            }
            else
            {
                historicalData = (await GetHistoricalDataAsync(HistoricalDataFrom, HistoricalDataTo, ticker.Symbol)).Values.First();
                stock = historicalData.OrderByDescending(x => x.Date).FirstOrDefault();
            }
            var stockPosition = new StockPosition(
                stock ?? (await _repo.GetStocksAsync(new[] { ticker.Symbol })).Single(), ticker);
            stockPosition.HistoricalData = historicalData;
            stockPositions.Add(stockPosition);
            foreach (var _stockPosition in stockPositions)
                _stockPosition.Refresh(stockPositions);
            stockPosition.PropertyChanged += _stockPositionPropertyChanged;
            await SaveStockPositionsAsync(stockPositions);

            _setStockPositions(stockPositions);
        }

        internal async Task DeleteStockPositionAsync(StockPosition stockPosition)
        {
            var stockPositions = _getStockPositions().ToList();
            stockPositions.Remove(stockPosition);
            foreach (var _stockPosition in stockPositions)
                _stockPosition.Refresh(stockPositions);
            stockPosition.PropertyChanged -= _stockPositionPropertyChanged;
            await SaveStockPositionsAsync(stockPositions);

            _setStockPositions(stockPositions);
        }
    }
}
