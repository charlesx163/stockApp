using DocumentFormat.OpenXml.Wordprocessing;

namespace StockPriceApplication.Services
{
    public interface IStockNetChangeCalculator
    {
        public Dictionary<string, decimal> Calculator(string startDate, string endDate, Dictionary<string, decimal> stocks);
    }
    public class StockNetChangeCalculator : IStockNetChangeCalculator
    {
        public Dictionary<string, decimal> Calculator(string startDate, string endDate, Dictionary<string, decimal> stocks)
        {
            var stockNetChanges = new Dictionary<string, decimal>();
            var days = (DateTime.Parse(endDate) - DateTime.Parse(startDate)).Days;
            var cacheBeginDayAsPreDay = string.Empty;
            for (int i = 0; i <= days; i++)
            {
                var currentDay = DateTime.Parse(startDate).AddDays(i).ToShortDateString();
                if (!stocks.TryGetValue(currentDay, out var value))
                {
                    continue;
                }
                if (stockNetChanges.Count()==0)
                {
                    stockNetChanges[currentDay] = 0;
                    cacheBeginDayAsPreDay = currentDay;
                    continue;
                }
                stockNetChanges[currentDay] = Math.Round(stocks[currentDay] / stocks[cacheBeginDayAsPreDay] - 1, 4);
                cacheBeginDayAsPreDay = currentDay;
            }
            return stockNetChanges;
        }
    }
}
