using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFi.Domain.ApiResponse
{
    public class Ticker
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }
        [JsonPropertyName("stock_exchange")]
        public StockExchange StockExchange { get; set; }
        public override string ToString()
        {
            return $"{Symbol} - {Name}";
        }
    }
}
