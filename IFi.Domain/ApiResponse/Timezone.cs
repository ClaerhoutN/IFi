using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFi.Domain.ApiResponse
{
    public class Timezone
    {
        [JsonPropertyName("timezone")]
        public string Timezone_ { get; set; }
        [JsonPropertyName("abbr")]
        public string Abbr { get; set; }
        [JsonPropertyName("abbr_dst")]
        public string AbbrDst { get; set; }
    }
}
