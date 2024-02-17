using CommunityToolkit.Mvvm.ComponentModel;
using IFi.Domain.ApiResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFi.Presentation.VM.Maui.ViewModels
{
    public partial class StockPosition : ObservableObject
    {
        public Stock Stock { get; set; }
        private Stock[] _historicalData;
        [JsonIgnore]
        public Stock[] HistoricalData //order from old to new
        {
            get => _historicalData;
            set => _historicalData = value.OrderBy(x => x.Date).ToArray();
        }
        public float Change1Day => GetStockCloseDifferenceByPeriod(TimeSpan.FromDays(1));
        public float Change7Days => GetStockCloseDifferenceByPeriod(TimeSpan.FromDays(7));
        public float Change1Month => GetStockCloseDifferenceByPeriod(TimeSpan.FromDays(30));
        public float Change3Months => GetStockCloseDifferenceByPeriod(TimeSpan.FromDays(90));
        public float Change1Year => GetStockCloseDifferenceByPeriod(TimeSpan.FromDays(365));
        public decimal Value => (Stock.Close ?? 0) * Position;
        [ObservableProperty]
        private float _currentHoldingPct;
        [ObservableProperty]
        public decimal _targetValue;
        [ObservableProperty]
        private float _targetHoldingPct;
        [ObservableProperty]
        private int _position;
        public Color BackgroundColor => GetBackgroundColor();

        public Ticker Ticker { get; set; }
        public StockPosition() //for json deserialization
        {
            PropertyChanged += StockPosition_PropertyChanged;
        }
        public StockPosition(Stock stock, Ticker ticker)
        {
            Stock = stock;
            Ticker = ticker;
            Position = 0;
            TargetHoldingPct = 0;
            PropertyChanged += StockPosition_PropertyChanged;
        }

        private void StockPosition_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(Position))
            {
                OnPropertyChanged(nameof(Value));
            }
        }

        internal void Refresh(IEnumerable<StockPosition> stockPositions)
        {
            if (stockPositions.Sum(x => x.Value) > 0)
            {
                CurrentHoldingPct = (float)(Value / stockPositions.Sum(x => x.Value));
                TargetValue = (decimal)TargetHoldingPct * stockPositions.Sum(x => x.Value);
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        private float GetStockCloseDifferenceByPeriod(TimeSpan period)
        {
            if (HistoricalData.Length <= 1)
                return 0;
            Stock newest = HistoricalData.Last();
            DateTimeOffset targetDateFrom = newest.Date.Add(period.Negate());
            Stock oldest = null;
            for(int i = HistoricalData.Length - 2; i >= 0; --i)
            {
                if(i == 0 || HistoricalData[i].Date <= targetDateFrom)
                {
                    oldest = HistoricalData[i];
                    break;
                }
            }
            return (float)((newest.Close - oldest.Close) / oldest.Close);
        }

        private Color GetBackgroundColor()
        {
            float currentOnTargetHoldingRatio = CurrentHoldingPct - TargetHoldingPct;
            byte r = 255;
            byte g = 255, b = 255;
            if(TargetHoldingPct != 0f)
                g = b = (byte)((1f - (Math.Abs(currentOnTargetHoldingRatio) / TargetHoldingPct)) * 255f);

            return Color.FromRgb(r, g, b);
        }
    }
}
