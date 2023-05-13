using CommunityToolkit.Mvvm.ComponentModel;
using IFi.Domain;
using IFi.Domain.ApiResponse;
using IFi.Domain.Models;
using IFi.Utilities.JsonConverters;
using Microsoft.Maui.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IFi.Presentation.VM.Maui.ViewModels
{
    public partial class MainVM : ObservableObject
    {
        private readonly string _historicalDataDirectory = "historicalData";
        private readonly string _stockPositionsFileName = Path.Combine(FileSystem.Current.AppDataDirectory, "stockPositions.txt");

        private readonly StockMarketDataRepository _repo = new();
        [ObservableProperty]
        private IEnumerable<StockPosition> _stockPositions;
        public decimal TotalValue => StockPositions?.Sum(x => x.Value) ?? 0m;
        public float TotalTargetHoldingPct => StockPositions?.Sum(x => x.TargetHoldingPct) ?? 0f;
        private DateTime HistoricalDataFrom => DateTime.Today.AddYears(-1);
        private DateTime HistoricalDataTo => DateTime.Today;
        private readonly Func<Page, Task> _navigate;
        public ICommand NavigateCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand ReverseSortOrderCommand { get; }
        public IEnumerable<string> ChangePeriods { get; } = new List<string> { "1 day", "7 days", "1 month", "3 months", "1 year" };
        [ObservableProperty]
        private string _selectedPeriod;
        [ObservableProperty]
        private string _sortOrder;
        [ObservableProperty]
        private bool _isSorted_StockSymbol;
        [ObservableProperty]
        private bool _isSorted_Position;
        [ObservableProperty]
        private bool _isSorted_StockClose;
        [ObservableProperty]
        private bool _isSorted_Value;
        [ObservableProperty]
        private bool _isSorted_TargetValue;
        [ObservableProperty]
        private bool _isSorted_Change1Day;
        [ObservableProperty]
        private bool _isSorted_Change7Days;
        [ObservableProperty]
        private bool _isSorted_Change1Month;
        [ObservableProperty]
        private bool _isSorted_Change3Months;
        [ObservableProperty]
        private bool _isSorted_Change1Year;
        private string _sortedField;
        public MainVM(Func<Page, Task> navigate)
        {
            _navigate = navigate;
            NavigateCommand = new Command<Type>(
                async (Type pageType) =>
                {
                    Page page = (Page)Activator.CreateInstance(pageType, this);
                    await _navigate(page);
                });
            SortCommand = new Command<string>(Sort);
            ReverseSortOrderCommand = new Command(() => Sort(_sortedField));
            SelectedPeriod = "1 day";
            PropertyChanged += MainVM_PropertyChanged;
        }

        private void MainVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StockPositions))
            {
                OnPropertyChanged(nameof(TotalValue));
                OnPropertyChanged(nameof(TotalTargetHoldingPct));
            }
            if(e.PropertyName == nameof(SelectedPeriod))
                Sort(SelectedPeriod);

        }

        private void StockPosition_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StockPosition.Value))
            {
                OnPropertyChanged(nameof(TotalValue));
            }
            if (e.PropertyName == nameof(StockPosition.TargetHoldingPct))
            {
                OnPropertyChanged(nameof(TotalTargetHoldingPct));
            }
        }

        public async Task InitializeAsync()
        {
            StockPositions = (await GetStockPositionsAsync()).OrderBy(x => x.Stock.Symbol);
        }

        private async Task<IReadOnlyList<StockPosition>> GetStockPositionsAsync()
        {
            var (stockPositions, needsUpdate) = await ReadStockPositionsAsync();
            if (needsUpdate)
            {
                var stocks = await _repo.GetStocksAsync(stockPositions
                    .Where(x => !Currencies.All.Any(y => y.Symbol == x.Stock.Symbol))
                    .Select(x => x.Stock.Symbol).ToArray());
                foreach (var stockPosition in stockPositions)
                    stockPosition.Stock = stocks.FirstOrDefault(x => x.Symbol == stockPosition.Stock.Symbol) ?? stockPosition.Stock;
            }
            if(! stockPositions.Any(x => x.Stock.Symbol == "EUR"))
            {
                stockPositions.Add(new StockPosition(Currencies.EUR, new(){ Name = "EUR" }));
                needsUpdate = true;
            }
            var historicalData = await GetHistoricalDataAsync(HistoricalDataFrom, HistoricalDataTo, stockPositions.Select(x => x.Stock.Symbol).ToArray());
            foreach (var stockPosition in stockPositions)
            {
                if (string.IsNullOrEmpty(stockPosition.Ticker?.Name))
                {
                    stockPosition.Ticker = await _repo.GetTickerAsync(stockPosition.Stock.Symbol);
                    needsUpdate = true;
                }
                stockPosition.HistoricalData = historicalData[stockPosition.Stock.Symbol];
            }
            foreach (var stockPosition in stockPositions)
            {
                stockPosition.Refresh(stockPositions);
                stockPosition.PropertyChanged += StockPosition_PropertyChanged;
            }
            if(needsUpdate)
                await SaveStockPositionsAsync(stockPositions);
            return stockPositions;
        }
        //guaranteed to have all symbols as keys
        private async Task<Dictionary<string, Stock[]>> GetHistoricalDataAsync(DateTime from, DateTime to, params string[] symbols)
        {
            Dictionary<string, Stock[]> dataBySymbol = new Dictionary<string, Stock[]>();
            foreach (string symbol in symbols)
            {
                Stock[] data = null;
                if (Currencies.All.Any(y => y.Symbol == symbol))
                    data = new Stock[0];
                else
                    data = await ReadHistoricalDataAsync(symbol);
                if (data != null)
                    dataBySymbol.Add(symbol, data);
            }
            string[] symbolsToFetch = symbols.Where(x => !dataBySymbol.ContainsKey(x)).ToArray();
            var historicalData = await _repo.GetHistoricalDataAsync(symbolsToFetch, from, to);
            foreach(string symbol in symbolsToFetch)
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
            if(!Directory.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory)))
                Directory.CreateDirectory(Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory));
            string path = Path.Combine(FileSystem.Current.AppDataDirectory, _historicalDataDirectory, $"{symbol}.txt");
            string json = JsonSerializer.Serialize(data, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            return File.WriteAllTextAsync(path, json);
        }

        private async Task<(List<StockPosition>, bool)> ReadStockPositionsAsync()
        {
            bool needsUpdate = false;
            FileInfo fi = new (_stockPositionsFileName);
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

        private Task SaveStockPositionsAsync(IReadOnlyList<StockPosition> stockPositions)
        {
            string json = JsonSerializer.Serialize(stockPositions, DateTimeOffsetConverter_ISO8601.DefaultJsonSerializerOptions);
            return File.WriteAllTextAsync(_stockPositionsFileName, json);
        }
        private SemaphoreSlim _fileLocker = new (1, 1);
        public async void Save()
        {
            await _fileLocker.WaitAsync();
            try
            {
                await SaveStockPositionsAsync(StockPositions.ToList());
            }
            finally
            {
                _fileLocker.Release();
            }
        }

        internal async Task AddStockPositionAsync(string symbol)
        {
            var ticker = await _repo.GetTickerAsync(symbol);
            await AddStockPositionAsync(ticker);
        }

        internal async Task AddStockPositionAsync(Ticker ticker)
        {
            var stockPositions = StockPositions.ToList();
            var historicalData = (await GetHistoricalDataAsync(HistoricalDataFrom, HistoricalDataTo, ticker.Symbol)).Values.First();
            var stock = historicalData.OrderByDescending(x => x.Date).FirstOrDefault();
            var stockPosition = new StockPosition(
                stock ?? (await _repo.GetStocksAsync(new[] { ticker.Symbol })).Single(), ticker);
            stockPosition.HistoricalData = historicalData;
            stockPositions.Add(stockPosition);
            foreach (var _stockPosition in stockPositions)
                _stockPosition.Refresh(stockPositions);
            stockPosition.PropertyChanged += StockPosition_PropertyChanged;
            await SaveStockPositionsAsync(stockPositions);

            StockPositions = stockPositions;
        }

        internal async Task DeleteStockPositionAsync (StockPosition stockPosition)
        {
            var stockPositions = StockPositions.ToList();            
            stockPositions.Remove(stockPosition);
            foreach (var _stockPosition in stockPositions)
                _stockPosition.Refresh(stockPositions);
            stockPosition.PropertyChanged -= StockPosition_PropertyChanged;
            await SaveStockPositionsAsync(stockPositions);

            StockPositions = stockPositions;
        }
        private void Sort(string field)
        {
            _sortedField = field;
            SortOrder = SortOrder == "Desc" ? "Asc" : "Desc";
            bool desc = SortOrder == "Desc";
            IsSorted_StockSymbol = IsSorted_Position = IsSorted_StockClose = IsSorted_Value = IsSorted_TargetValue =
                IsSorted_Change1Day = IsSorted_Change7Days = IsSorted_Change1Month = IsSorted_Change3Months = IsSorted_Change1Year = false;
            IOrderedEnumerable<StockPosition> stockPositions = null;
            switch (field)
            {
                case "Stock.Symbol":
                    {
                        if (desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Stock.Symbol);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Stock.Symbol);
                        IsSorted_StockSymbol = true;
                            break;
                    }
                case "Position":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Position);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Position);
                        IsSorted_Position = true;
                        break;
                    }
                case "Stock.Close":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Stock.Close);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Stock.Close);
                        IsSorted_StockClose = true;
                        break;
                    }
                case "Value":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Value);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Value);
                        IsSorted_Value = true;
                        break;
                    }
                case "TargetValue":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.TargetValue);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.TargetValue);
                        IsSorted_TargetValue = true;
                        break;
                    }
                case "1 day":
                case "Change1Day":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Change1Day);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Change1Day);
                        IsSorted_Change1Day = true;
                        break;
                    }
                case "7 days":
                case "Change7Days":
                    {
                        if (desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Change7Days);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Change7Days);
                        IsSorted_Change7Days = true;
                        break;
                    }
                case "1 month":
                case "Change1Month":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Change1Month);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Change1Month);
                        IsSorted_Change1Month = true;
                        break;
                    }
                case "3 months":
                case "Change3Months":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Change3Months);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Change3Months);
                        IsSorted_Change3Months = true;
                        break;
                    }
                case "1 year":
                case "Change1Year":
                    {
                        if(desc)
                            stockPositions = StockPositions.OrderByDescending(x => x.Change1Year);
                        else
                            stockPositions = StockPositions.OrderBy(x => x.Change1Year);
                        IsSorted_Change1Year = true;
                        break;
                    }
            }
            if(stockPositions != null)
                StockPositions = stockPositions.ToList();
        }
    }
}
