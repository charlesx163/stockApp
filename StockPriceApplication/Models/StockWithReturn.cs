namespace StockPriceApplication.Models
{
    public class StockWithReturn
    {
        public string Date { get; set; }
        public decimal RelativeReturn { get; set; }
        public decimal StockNetChange { get; set; }
        public decimal IndexNetChange { get; set; }
    }
}
