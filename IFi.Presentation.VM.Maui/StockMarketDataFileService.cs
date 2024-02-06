using IFi.Domain;
using IFi.Domain.ApiResponse;
using IFi.Domain.Models;
using IFi.Presentation.VM.Maui.ViewModels;
using IFi.Utilities;
using IFi.Utilities.Collections.ObjectModel;
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

        private readonly SilentObservableCollection<StockPosition> _stockPositions;
        private readonly PropertyChangedEventHandler _stockPositionPropertyChanged;

        public StockMarketDataFileService(SilentObservableCollection<StockPosition> stockPositions, PropertyChangedEventHandler stockPositionPropertyChanged)
        {
            _stockPositionPropertyChanged = stockPositionPropertyChanged;
            _stockPositions = stockPositions;
        }

        public async Task<IReadOnlyList<StockPosition>> GetStockPositionsAsync()
        {
            var stockPositions = await ReadStockPositionsAsync();
            var symbols = stockPositions.Select(x => x.Stock?.Symbol ?? x.Ticker.Symbol);
            var historicalData = await GetHistoricalDataAsync(HistoricalDataFrom, HistoricalDataTo, symbols.ToArray());
            bool needsUpdate = false;
            foreach (var stockPosition in stockPositions)
            {
                Stock stock = null;
                if (historicalData.TryGetValue(stockPosition.Stock?.Symbol ?? stockPosition.Ticker.Symbol, out var data))
                    stock = data.OrderByDescending(x => x.Date).FirstOrDefault();
                stockPosition.HistoricalData = data;

                if (stock == null)
                {
                    if (Currency.IsCurrency(stockPosition.Ticker))
                        stock = Currency.AsStock(stockPosition.Ticker);
                    else
                    {
                        stock = (await _repo.GetStocksAsync(new[] { stockPosition.Stock?.Symbol ?? stockPosition.Ticker.Symbol })).FirstOrDefault();
                        AdjustForSplits(stock);
                    }
                    needsUpdate = true;
                }
                else
                {
                    if (stockPosition.Stock == null || stockPosition.Stock.Date != stock.Date)
                        needsUpdate = true;
                }
                stockPosition.Stock = stock;
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
                {
                    DateTime lastWriteTime;
                    (data, lastWriteTime) = await ReadHistoricalDataAsync(symbol);
                    if (data == null || (lastWriteTime.Date != DateTime.Today && !IsHistoricalDataComplete(data, from, to)))
                        continue;
                }
                dataBySymbol.Add(symbol, data);
            }
            string[] symbolsToFetch = symbols.Where(x => !dataBySymbol.ContainsKey(x)).ToArray();
            var historicalData = await _repo.GetHistoricalDataAsync(symbolsToFetch, from, to);
            foreach (string symbol in symbolsToFetch)
            {
                if (historicalData.TryGetValue(symbol, out var value))
                {
                    dataBySymbol.Add(symbol, value);
                    AdjustForSplits(value);
                    await SaveHistoricalDataAsync(value, symbol);
                }
                else
                    dataBySymbol.Add(symbol, new Stock[0]);
            }
            return dataBySymbol;
        }
        private async Task<(Stock[] stocks, DateTime lastWriteTime)> ReadHistoricalDataAsync(string symbol)
        {
            string path = Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory, $"{symbol}.txt");
            Stock[] data = null;
            var fi = new FileInfo(path);
            if (fi.Exists)
            {
                string json = await File.ReadAllTextAsync(path);
                data = JsonSerializer.Deserialize<Stock[]>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);                
            }
            return (data, fi.LastWriteTime);
        }
        private Task SaveHistoricalDataAsync(Stock[] data, string symbol)
        {
            if (!Directory.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory)))
                Directory.CreateDirectory(Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory));
            string path = Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory, $"{symbol}.txt");
            string json = JsonSerializer.Serialize(data, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            return File.WriteAllTextAsync(path, json);
        }

        private async Task<List<StockPosition>> ReadStockPositionsAsync()
        {
            FileInfo fi = new(_stockPositionsFileName);
            if (fi.Exists)
            {
                string json = await File.ReadAllTextAsync(_stockPositionsFileName);
                var stockPositions = JsonSerializer.Deserialize<List<StockPosition>>(json, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
                return stockPositions;
            }
            return new List<StockPosition>();
        }

        public Task SaveStockPositionsAsync(IReadOnlyList<StockPosition> stockPositions)
        {
            string json = JsonSerializer.Serialize(stockPositions, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            return File.WriteAllTextAsync(_stockPositionsFileName, json);
        }

        internal async Task AddStockPositionAsync(Ticker ticker)
        {
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
            stock = stock ?? (await _repo.GetStocksAsync(new[] { ticker.Symbol })).Single();
            AdjustForSplits(stock);
            var stockPosition = new StockPosition(stock, ticker);
            stockPosition.HistoricalData = historicalData;
            _stockPositions.Add(stockPosition);
            foreach (var _stockPosition in _stockPositions)
                _stockPosition.Refresh(_stockPositions);
            stockPosition.PropertyChanged += _stockPositionPropertyChanged;
            await SaveStockPositionsAsync(_stockPositions);
        }

        internal async Task DeleteStockPositionAsync(StockPosition stockPosition)
        {
            _stockPositions.Remove(stockPosition);
            foreach (var _stockPosition in _stockPositions)
                _stockPosition.Refresh(_stockPositions);
            stockPosition.PropertyChanged -= _stockPositionPropertyChanged;
            await SaveStockPositionsAsync(_stockPositions);
        }

        internal static bool IsHistoricalDataComplete(IEnumerable<Stock> historicalData, DateTime from, DateTime to)
        {
            from = from.GetClosestWeekDay(false);
            to = to.GetClosestWeekDay(true);
            var ordered = historicalData.OrderBy(x => x.Date);
            return ordered.First().Date.Date <= from && ordered.Last().Date.Date >= to;
            //does not account for gaps -> TODO, but how?
        }
        private static void AdjustForSplits(Stock stock)
        {
            stock.Low = stock.Low * stock.SplitFactor;
            stock.High = stock.High * stock.SplitFactor;
            stock.Open = stock.Open * stock.SplitFactor;
            stock.Close = stock.Close * stock.SplitFactor;
            stock.SplitFactor = 1;
        }
        private static void AdjustForSplits(IEnumerable<Stock> data)
        {
            foreach (var stock in data)
            {
                AdjustForSplits(stock);
            }
        }
    }
}
