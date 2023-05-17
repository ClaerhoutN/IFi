using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IFi.Domain.ApiResponse
{
    public class Eod
    {
        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; }
        [JsonPropertyName("data")]
        public Stock[] Data { get; set; }
    }
}
