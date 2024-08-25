using StockPriceApplication.Models;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace StockPriceApplication.Services
{
    public interface ISseRelativeReturnCalculator
    {
        Dictionary<string, StockWithReturn> Calculate(
            Dictionary<string, decimal> stockNetChangesDic,
            Dictionary<string, decimal> indexDic);
    }
    public class SseRelativeReturnCalculator : ISseRelativeReturnCalculator
    {
        private readonly IStockNetChangeCalculator _calculator;
        private readonly ISseIndexNetChangeCalculator _indexNetChangeCalculator;
        public SseRelativeReturnCalculator(IStockNetChangeCalculator calculator, ISseIndexNetChangeCalculator indexNetChangeCalculator)
        {
            _calculator = calculator;
            _indexNetChangeCalculator = indexNetChangeCalculator;
        }
        public Dictionary<string, StockWithReturn> Calculate(Dictionary<string, decimal> stockNetChanges,
            Dictionary<string,decimal> indexDic)
        {
            var keys = stockNetChanges.Keys.ToList();
            var indexNetChangesDic = _indexNetChangeCalculator.GetSseIndexNetChangeNew(keys, indexDic);
            var relativeReturnDic = new Dictionary<string, StockWithReturn>();
            var cachePreday = string.Empty;
            foreach (var stockNetChange in stockNetChanges)
            {
                var stockWithReturn = new StockWithReturn
                {
                    Date = stockNetChange.Key,
                    StockNetChange = stockNetChange.Value,
                    IndexNetChange = indexNetChangesDic[stockNetChange.Key],
                    RelativeReturn = relativeReturnDic.Count()==0 ? 1
                    : GetRelativeReutrn(stockNetChange.Value, indexNetChangesDic[stockNetChange.Key], relativeReturnDic[cachePreday].RelativeReturn)
                };
                relativeReturnDic[stockNetChange.Key] = stockWithReturn;
                cachePreday = stockNetChange.Key;
            }
            return relativeReturnDic;
        }

        private decimal GetRelativeReutrn(decimal stockNetChange, decimal indexNetChange, decimal relativeReturn)
        {
            return Math.Round(((stockNetChange - indexNetChange) + 1) * relativeReturn,2);
        }
    }
}
