using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFi.Domain.ApiResponse
{
    internal class Tickers
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }
        [JsonPropertyName("data")]
        public Ticker[] Data { get; set; }
    }
}
