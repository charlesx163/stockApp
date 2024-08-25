using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using StockPriceApplication.Models;
using StockPriceApplication.Services;
using System.Diagnostics;
using System.Dynamic;
using System.Security.Policy;
using System.Text.Json;

namespace StockPriceApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly List<StockOption> _options;
        private readonly ISseIndexNetChangeCalculator _sseIndexNetChangeCalculator;
        private readonly ISseRelativeReturnCalculator _seRelativeReturnCalculator;
        private readonly IStockNetChangeCalculator _stockNetChangeCalculator;


        private static Dictionary<string, decimal> indexsDic = new Dictionary<string, decimal>();
        private static Dictionary<string, decimal> pingAnDic = new Dictionary<string, decimal>();
        private static Dictionary<string, decimal> maoTaiDic = new Dictionary<string, decimal>();
        private static Dictionary<string, decimal> zhongXinJianTouDic = new Dictionary<string, decimal>();
        private static Dictionary<string, decimal> huaXingYuanChuangDic = new Dictionary<string, decimal>();
        private static Dictionary<string, decimal> tongDaChuangYeDic = new Dictionary<string, decimal>();
        private static Dictionary<int, string> dateDic = new Dictionary<int, string>();

        public HomeController(ILogger<HomeController> logger, 
            IOptions<List<StockOption>> options,
            ISseRelativeReturnCalculator seRelativeReturnCalculator,
            ISseIndexNetChangeCalculator sseIndexNetChangeCalculator,
            IStockNetChangeCalculator stockNetChangeCalculator)
        {
            _logger = logger;
            _options = options.Value;
            _seRelativeReturnCalculator = seRelativeReturnCalculator;
            _sseIndexNetChangeCalculator = sseIndexNetChangeCalculator;
            _stockNetChangeCalculator = stockNetChangeCalculator;
        }

        [HttpPost]
        public IActionResult ImportExcel(IFormFile file)
        {
            ClearDic();
            var stockOptions = _options;
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);

            using var xlPackage = new XLWorkbook(memoryStream);
            var worksheet = xlPackage.Worksheet(2);
            var range = worksheet.RangeUsed();
            ProcessExcelNew(worksheet, range);
            return Ok("�ϴ��ɹ�");
        }
        public IActionResult Index()
        {
            var items = _options.Select(o => new SelectListItem { Text = o.Name, Value = $"{o.Name}({o.Number})" });
            SelectList list = new SelectList(items, "Value", "Text");
            ViewBag.Stocks = list;
            return View();
        }

        public IActionResult Privacy()
        {
            return View(new List<Stock>());
        }

        //[HttpPost]
        public IActionResult GenerateEcharts(string sd, string ed, List<string> stockTypeList)
        {
            dynamic sp;
            var xValues = new List<string>();//
            var charts = new List<dynamic>();
            var Legend = new List<string>();
            var stockMappings = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "ƽ������(000001)", pingAnDic },
                { "����ę́(600519)", maoTaiDic},
                { "���Ž�Ͷ(601066)", zhongXinJianTouDic },
                { "����Դ��(688001)", huaXingYuanChuangDic },
                { "ͬ�ﴴҵ(600647)", tongDaChuangYeDic }
            };
            //var indexNetChanges = _sseIndexNetChangeCalculator.GetSseIndexNetChange(sd, ed, indexsDic);
            var stockTypes = stockTypeList.Count()>0?stockTypeList[0].Split(',').ToList():new List<string>();
            foreach (var stockType in stockTypes)
            {
                Legend.Add(stockType);
                var stocksDic = stockMappings[stockType];

                var stockNetChanges = _stockNetChangeCalculator.Calculator(sd, ed, stocksDic);
                var relativeReturns = _seRelativeReturnCalculator.Calculate(stockNetChanges, indexsDic);
                var yValueDic = new Dictionary<string,decimal>();
                foreach ( var relativeReturn in  relativeReturns) 
                {
                    if (!xValues.Contains(relativeReturn.Key))
                    {
                        xValues.Add(relativeReturn.Key);
                    }
                    yValueDic.Add(relativeReturn.Key,relativeReturn.Value.RelativeReturn);
                }
                sp = new ExpandoObject();
                sp.name = stockType;
                sp.type = "line";
                sp.Stack = "Total";
                sp.data = GetYValues(yValueDic,xValues);//yValues.ToArray();
                charts.Add(sp);

            }
            return Json(new { xAxis = new { data = xValues.ToArray() }, legend = new { data = Legend.ToArray() }, Series = new { series = JsonSerializer.Serialize(charts) } });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private void ProcessExcelNew(IXLWorksheet worksheet, IXLRange range)
        {
            // �����е�ӳ�䣬������Ҫ������ֵ�Ͷ�Ӧ���ֵ�
            var stockMappings = new Dictionary<int, (Dictionary<string, decimal> dict, string skipValue)>
            {
                { 2, (indexsDic, "��ָ֤��") },
                { 3, (pingAnDic, "ƽ������(000001)") },
                { 4, (maoTaiDic, "����ę́(600519)") },
                { 5, (zhongXinJianTouDic, "���Ž�Ͷ(601066)") },
                { 6, (huaXingYuanChuangDic, "����Դ��(688001)") },
                { 7, (tongDaChuangYeDic, "ͬ�ﴴҵ(600647)") }
            };
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                for (int row = 1; row <= range.RowCount(); row++)
                {
                    // ��ȡ��Ԫ������
                    var cell = worksheet.Cell(row, col);
                    var value = cell.GetValue<string>();

                    // �����һ���ǿ�ֵ��ֹͣ����
                    if (string.IsNullOrEmpty(value) && row == 1)
                        break;

                    if (col == 1)
                    {
                        if (value == "����")
                        {
                            continue;
                        }

                        dateDic[row] = DateTime.Parse(value).ToShortDateString();
                    }
                    else
                    {
                        // ���ҵ�ǰ�е�ӳ��
                        if (stockMappings.TryGetValue(col, out var mapping))
                        {
                            // ���ֵ��������ֵ�������
                            if (value == mapping.skipValue)
                            {
                                continue;
                            }
                            // ��ȡ���ڵ�key
                            string key = dateDic[row];
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                mapping.dict[key] = decimal.Parse(value);
                            }
                        }
                    }
                }
            }
        }
        private void ClearDic()
        {
            indexsDic.Clear();
            pingAnDic.Clear();
            maoTaiDic.Clear();
            zhongXinJianTouDic.Clear();
            huaXingYuanChuangDic.Clear();
            tongDaChuangYeDic.Clear();
        }

        private List<string> GetEChartXValue(string startDate, string endDate)
        {
            var days = (DateTime.Parse(endDate) - DateTime.Parse(startDate)).Days;
            var list = new List<string>();
            for (var i = 0; i <= days; i++)
            {
                list.Add(DateTime.Parse(startDate).AddDays(i).ToShortDateString());
            }
            return list;
        }


        private List<decimal> GetYValues(Dictionary<string,decimal> yValueDic,List<string> xValues)
        {
            foreach (var xValue in xValues)
            {
                if(!yValueDic.TryGetValue(xValue, out decimal value))
                {
                    yValueDic.Add(xValue, 0);
                }
            }
            var sortedYValues= yValueDic.OrderBy(x => x.Key).ToDictionary(y=>y.Key,y=>y.Value);
            return sortedYValues.Values.ToList();
        }

    }
}