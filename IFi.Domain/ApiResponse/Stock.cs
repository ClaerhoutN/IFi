using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IFi.Domain.ApiResponse
{
    public class Stock
    {
        [JsonPropertyName("open")]
        public decimal Open { get; set; }
        [JsonPropertyName("high")]
        public decimal High { get; set; }
        [JsonPropertyName("low")]
        public decimal Low { get; set; }
        [JsonPropertyName("close")]
        public decimal Close { get; set; }
        [JsonPropertyName("volume")]
        public decimal? Volume { get; set; }
        [JsonPropertyName("adj_high")]
        public decimal? AdjHigh { get; set; }
        [JsonPropertyName("adj_low")]
        public decimal? AdjLow { get; set; }
        [JsonPropertyName("adj_close")]
        public decimal? AdjClose { get; set; }
        [JsonPropertyName("adj_open")]
        public decimal? AdjOpen { get; set; }
        [JsonPropertyName("adj_volume")]
        public decimal? AdjVolume { get; set; }
        [JsonPropertyName("split_factor")]
        public decimal SplitFactor { get; set; }
        [JsonPropertyName("dividend")]
        public decimal Dividend { get; set; }
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }
        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("date")]
        public DateTimeOffset Date { get; set; }

    }
}
