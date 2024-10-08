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
            IStockNetChangeCalculator stockNetChangeCalculator)
        {
            _logger = logger;
            _options = options.Value;
            _seRelativeReturnCalculator = seRelativeReturnCalculator;
            _stockNetChangeCalculator = stockNetChangeCalculator;
        }

        [HttpPost]
        public IActionResult ImportExcel(IFormFile file)
        {
            ClearDic();
            var stockOptions = _options;
            if (file == null || file.Length == 0)
            {
                return BadRequest("请选择文件.");
            }

            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);

            using var xlPackage = new XLWorkbook(memoryStream);
            var worksheet = xlPackage.Worksheet(2);
            var range = worksheet.RangeUsed();
            ProcessExcelNew(worksheet, range);
            return Ok("上传成功");
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
            if (sd == null || ed == null || DateTime.Compare(DateTime.Parse(sd), DateTime.Parse(ed)) >= 0)
            {
                return Json(new { message = "请选择正确的日期" });
            }
            if (stockTypeList.Count == 0 || stockTypeList[0] == null)
            {
                return Json(new { message = "请选择查询的股票" });
            }
            dynamic sp;
            var xValues = new List<string>();//
            var charts = new List<dynamic>();
            var legend = new List<string>();
            var stockMappings = new Dictionary<string, Dictionary<string, decimal>>
            {
                { "平安银行(000001)", pingAnDic },
                { "贵州茅台(600519)", maoTaiDic},
                { "中信建投(601066)", zhongXinJianTouDic },
                { "华兴源创(688001)", huaXingYuanChuangDic },
                { "同达创业(600647)", tongDaChuangYeDic }
            };
            var stockTypes = stockTypeList.Count() > 0 ? stockTypeList[0].Split(',').ToList() : new List<string>();
            foreach (var stockType in stockTypes)
            {
                legend.Add(stockType);
                var stocksDic = stockMappings[stockType];

                var stockNetChanges = _stockNetChangeCalculator.Calculator(sd, ed, stocksDic);
                var relativeReturns = _seRelativeReturnCalculator.Calculator(stockNetChanges, indexsDic);
                var yValues = new List<decimal>();
                foreach (var relativeReturn in relativeReturns)
                {
                    if (!xValues.Contains(relativeReturn.Key))
                    {
                        xValues.Add(relativeReturn.Key);
                    }
                    yValues.Add(relativeReturn.Value.RelativeReturn);
                }
                sp = new ExpandoObject();
                sp.name = stockType;
                sp.type = "line";
                sp.data = yValues.ToArray();//yValues.ToArray();
                charts.Add(sp);

            }
            return Json(new { xAxis = new { data = xValues.ToArray() }, legend = new { data = legend.ToArray() }, Series = new { series = JsonSerializer.Serialize(charts) }, message="" });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private void ProcessExcelNew(IXLWorksheet worksheet, IXLRange range)
        {
            // 定义列的映射，包含需要跳过的值和对应的字典
            var stockMappings = new Dictionary<int, (Dictionary<string, decimal> dict, string skipValue)>
            {
                { 2, (indexsDic, "上证指数") },
                { 3, (pingAnDic, "平安银行(000001)") },
                { 4, (maoTaiDic, "贵州茅台(600519)") },
                { 5, (zhongXinJianTouDic, "中信建投(601066)") },
                { 6, (huaXingYuanChuangDic, "华兴源创(688001)") },
                { 7, (tongDaChuangYeDic, "同达创业(600647)") }
            };
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                for (int row = 1; row <= range.RowCount(); row++)
                {
                    // 获取单元格数据
                    var cell = worksheet.Cell(row, col);
                    var value = cell.GetValue<string>();

                    // 如果第一行是空值，停止处理
                    if (string.IsNullOrEmpty(value) && row == 1)
                        break;

                    if (col == 1)
                    {
                        if (value == "日期")
                        {
                            continue;
                        }

                        dateDic[row] = DateTime.Parse(value).ToShortDateString();
                    }
                    else
                    {
                        // 查找当前列的映射
                        if (stockMappings.TryGetValue(col, out var mapping))
                        {
                            // 如果值是跳过的值，则继续
                            if (value == mapping.skipValue)
                            {
                                continue;
                            }
                            // 获取日期的key
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
    }
}