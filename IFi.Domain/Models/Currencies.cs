using IFi.Domain.ApiResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFi.Domain.Models
{
    public static class Currencies
    {
        public static readonly Stock EUR = new() { Symbol = "EUR", Close = 1m };
        public static readonly IReadOnlyList<Stock> All = new List<Stock>() { EUR };
    }
}
