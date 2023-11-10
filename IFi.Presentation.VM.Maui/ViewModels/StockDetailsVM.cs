using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Extensions;
using IFi.Domain.ApiResponse;

namespace IFi.Presentation.VM.Maui.ViewModels
{
    public class StockDetailsVM : ObservableObject
    {
        public StockPosition StockPosition { get; }
        public Stock Stock => StockPosition.Stock;
        public Ticker Ticker => StockPosition.Ticker;
        public ISeries[] Series { get; }
        public Axis[] XAxes { get; }
        public bool ChartVisibile => StockPosition.HistoricalData.Length >= 2;
        public int Position
        {
            get => StockPosition.Position;
            set
            {
                StockPosition.Position = value;
                RefreshStockPositions();
                _mainVm.Save();
            }
        }
        public float TargetHoldingPct
        {
            get => StockPosition.TargetHoldingPct * 100;
            set
            {
                StockPosition.TargetHoldingPct = value / 100f;
                RefreshStockPositions();
                _mainVm.Save();
            }
        }
        public ICommand DeleteStockCommand { get; }
        public ICommand PositionEnteredCommand { get; }
        public ICommand TargetHoldingPctEnteredCommand { get; }
        private readonly MainVM _mainVm;
        public StockDetailsVM(MainVM mainVm, StockPosition stockPosition, Func<Task<Page>> navigateBack)
        {
            _mainVm = mainVm;
            StockPosition = stockPosition;
            DeleteStockCommand = new Command(
                async () =>
                {
                    await mainVm.DeleteStockPositionAsync(StockPosition);
                    await navigateBack();
                });
            PositionEnteredCommand = new Command<Microsoft.Maui.Controls.Entry>(entry =>
            {
                entry.Unfocus();
            });
            TargetHoldingPctEnteredCommand = new Command<Microsoft.Maui.Controls.Entry>(entry =>
            {
                entry.Unfocus();
            });

            Series = new ISeries[]
            {
                new CandlesticksSeries<Stock>
                {
                    Values = StockPosition.HistoricalData,
                    TooltipLabelFormatter = (p) => $"H: {p.PrimaryValue:N2}, O: {p.TertiaryValue:N2}, C: {p.QuaternaryValue:N2}, L: {p.QuinaryValue:N2}\r\n{p.Model.Date.ToString("dd/MM/yyyy")}",
                }
            };
            XAxes = new Axis[]
            {
                new Axis
                {
                    //IsVisible = false
                    UnitWidth = TimeSpan.FromDays(1).Ticks,
                    LabelsRotation = 20,
                    Labeler = p => new DateTime((long)p).ToString("dd/MM/yyyy"),
                }
            };
        }

        private void RefreshStockPositions()
        {
            foreach (var stockPosition in _mainVm.StockPositions)
            {
                stockPosition.Refresh(_mainVm.StockPositions);
            }
        }
    }
}
