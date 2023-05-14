using CommunityToolkit.Mvvm.ComponentModel;
using IFi.Domain;
using IFi.Domain.ApiResponse;
using IFi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IFi.Presentation.VM.Maui.ViewModels
{
    public partial class AddStockVM : ObservableObject
    {
        private readonly MainVM _mainVm;
        public ICommand AddStockCommand { get; }
        public ICommand SearchCommand { get; }
        [ObservableProperty]
        private IEnumerable<Ticker> _tickersResult;
        [ObservableProperty]
        private Ticker _ticker;
        private readonly StockMarketDataRepository _repo = new();
        public AddStockVM(MainVM mainVm, Func<Task<Page>> navigateBack)
        {
            _mainVm = mainVm;
            AddStockCommand = new Command(async () =>
            {
                await _mainVm.FileService.AddStockPositionAsync(Ticker);
                await navigateBack();
            });
            SearchCommand = new Command<string>(async s => await Search(s));
        }
        private async Task Search(string search)
        {
            Ticker = null;
            var tickers = await _repo.GetTickersAsync(search);
            TickersResult = tickers
                .Concat(Currency.AllTickers.Where(x => 
                    x.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase) 
                    || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(x => x.Name).ToList();
        }
    }
}
