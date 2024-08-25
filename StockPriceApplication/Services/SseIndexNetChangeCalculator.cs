using DocumentFormat.OpenXml.Packaging;
using System.Collections;

namespace StockPriceApplication.Services
{
    //https://www.cnblogs.com/enych/p/10298396.html
    public interface ISseIndexNetChangeCalculator
    {
        Dictionary<string, decimal> GetSseIndexNetChange(string startDate, string endDate, Dictionary<string, decimal> indexDic);
        Dictionary<string, decimal> GetSseIndexNetChangeNew(List<string> keys, Dictionary<string, decimal> indexDic);

    }
    public class SseIndexNetChangeCalculator : ISseIndexNetChangeCalculator
    {
        public Dictionary<string, decimal> GetSseIndexNetChange(string startDate, string endDate, Dictionary<string, decimal> indexDic)
        {
            var dic = new Dictionary<string, decimal>();
            var days = (DateTime.Parse(endDate) - DateTime.Parse(startDate)).Days;
            var cacheBeginDayAsPreDay = string.Empty;
            for (int i = 0; i <= days; i++)
            {
                var currentDay = DateTime.Parse(startDate).AddDays(i).ToShortDateString();
                if (indexDic.TryGetValue(currentDay,out var value))
                {
                    if (i == 0)
                    {
                        dic[currentDay] = 0;
                        cacheBeginDayAsPreDay = currentDay;
                        continue;
                    }
                    if (indexDic.TryGetValue(cacheBeginDayAsPreDay, out var preValue))
                    {
                        dic[currentDay] = Math.Round(indexDic[currentDay] / indexDic[cacheBeginDayAsPreDay] - 1, 4);
                    }
                    cacheBeginDayAsPreDay = currentDay;

                }  
            }
            return dic;
        }

        public Dictionary<string, decimal> GetSseIndexNetChangeNew(List<string> keys, Dictionary<string, decimal> indexDic)
        {
            var dic = new Dictionary<string, decimal>();
            var cacheBeginDayAsPreDay = string.Empty;
            for (int i = 0; i < keys.Count; i++)
            {
                var currentDay = keys[i];
                if (indexDic.TryGetValue(currentDay, out var value))
                {
                    if (i == 0)
                    {
                        dic[currentDay] = 0;
                        cacheBeginDayAsPreDay = currentDay;
                        continue;
                    }
                    if (indexDic.TryGetValue(cacheBeginDayAsPreDay, out var preValue))
                    {
                        dic[currentDay] = Math.Round(indexDic[currentDay] / indexDic[cacheBeginDayAsPreDay] - 1, 4);
                    }
                    cacheBeginDayAsPreDay = currentDay;

                }
            }
            return dic;
        }
    }
}
