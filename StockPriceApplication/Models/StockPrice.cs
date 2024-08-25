using StockPriceApplication.Models.Enums;

namespace StockPriceApplication.Models
{
    public class StockPrice
    {
        public DateTime DateTime { get; set; }
        public StockType Name { get; set; }
        public decimal Price { get; set; }
    }
}
