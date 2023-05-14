using CommunityToolkit.Mvvm.ComponentModel;
using IFi.Domain;
using IFi.Domain.ApiResponse;
using IFi.Domain.Models;
using IFi.Utilities.JsonConverters;
using LiveChartsCore.Measure;
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
        public StockMarketDataFileService FileService { get; } //todo: make private and construct StockMarketDataFileService with DI
        [ObservableProperty]
        private IEnumerable<StockPosition> _stockPositions;
        public decimal TotalValue => StockPositions?.Sum(x => x.Value) ?? 0m;
        public float TotalTargetHoldingPct => StockPositions?.Sum(x => x.TargetHoldingPct) ?? 0f;
        private readonly Func<Page, Task> _navigate;
        public ICommand NavigateCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand ReverseSortOrderCommand { get; }
        public IEnumerable<string> ChangePeriods { get; } = new List<string> { "1 day", "7 days", "1 month", "3 months", "1 year" };
        [ObservableProperty]
        private string _selectedPeriod;
        [ObservableProperty]
        private string _sortOrder;
        #region _isSorted_
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
        #endregion #region _isSorted_
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
            FileService = new(sp => StockPositions = sp, () => StockPositions, StockPosition_PropertyChanged);
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
            StockPositions = (await FileService.GetStockPositionsAsync()).OrderBy(x => x.Stock.Symbol);
            foreach(var stockPosition in StockPositions)
            {
                stockPosition.PropertyChanged += StockPosition_PropertyChanged;
            }
        }
        private SemaphoreSlim _fileLocker = new (1, 1);
        public async void Save()
        {
            await _fileLocker.WaitAsync();
            try
            {
                await FileService.SaveStockPositionsAsync(StockPositions.ToList());
            }
            finally
            {
                _fileLocker.Release();
            }
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
