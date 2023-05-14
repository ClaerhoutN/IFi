using IFi.Domain.ApiResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFi.Domain.Models
{
    public static class Currency
    {
        private static readonly Stock EUR_Stock = new() { Symbol = "EUR", Close = 1m };
        private static readonly Ticker EUR_Ticker = new() { Symbol = "EUR", Name = "Euro" };
        public static readonly IReadOnlyList<Stock> AllStocks = new List<Stock>() { EUR_Stock };
        public static readonly IReadOnlyList<Ticker> AllTickers = new List<Ticker>() { EUR_Ticker };
        public static bool IsCurrency(Ticker ticker) => AllTickers.Any(x => x.Symbol == ticker.Symbol);
        public static bool IsCurrency(Stock stock) => AllStocks.Any(x => x.Symbol == stock.Symbol);
        public static bool IsCurrency(string ticker) => AllTickers.Any(x => x.Symbol == ticker);
        public static Stock AsStock(Ticker ticker) => AllStocks.FirstOrDefault(x => x.Symbol == ticker.Symbol);
        public static Ticker FindTicker(Stock stock) => AllTickers.FirstOrDefault(x => x.Symbol == stock.Symbol);
    }
}
