using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IFi.Domain.ApiResponse
{
    public class Pagination
    {
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
