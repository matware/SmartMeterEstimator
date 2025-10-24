using SmartMeterEstimator;
using Plotly.NET;
using CS = Plotly.NET.CSharp;
using Plotly.NET.CSharp;
using CommandLine;

internal partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

       

        Options options = null;
        
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {            
            options = o;
        });

        if (options == null)
        {
            Console.WriteLine("oops");
            return;
        }

        var inputCsvFile = GetLatestCsv(options);
        var bands = PriceBand.LoadOrCreateBands(options.PriceBands);
        var rows = RecordLoader.GetRecords(inputCsvFile, bands);

        app.MapGet("/day", (HttpContext context, DateOnly? date = null) => {
            context.Response.ContentType = "text/html";
            
            var firstDate = DateOnly.FromDateTime(rows.First().Date);
            var lastDate = DateOnly.FromDateTime(rows.Last().Date);
            if (date == null)
                date = lastDate;

            var prev = DateOnly.FromDateTime(date.Value.ToDateTime(TimeOnly.MinValue) - TimeSpan.FromDays(1));
            var next = DateOnly.FromDateTime(date.Value.ToDateTime(TimeOnly.MinValue) + TimeSpan.FromDays(1));
            var ss = GenericChart.toChartHTML(GetDay(rows, bands, date));
            var nextLink = next <= lastDate ? $"<a href=\"/day?date={next.Year}-{next.Month}-{next.Day}\">Next</a>":"[end of data]";
            var prevLink = prev >= firstDate ? $"<a href=\"/day?date={prev.Year}-{prev.Month}-{prev.Day}\">Prev</a>" : "[start of data]";
            var responseBody = $"""
<!DOCTYPE html>
<html><head>
<script src="https://cdn.plot.ly/plotly-2.27.1.min.js" charset="utf-8"></script>
<title>Plotly.NET Datavisualization</title><meta charset="UTF-8"><meta name="description" content="A plotly.js graph generated with Plotly.NET">
<link id="favicon" rel="shortcut icon" type="image/png" href="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAMAAACdt4HsAAAA1VBMVEVHcEwQnv+gCXURnf+gCXURnf8Rnf+gCXURnf+gCXWgCXURnf+gCHURnf+gCXURnf+gCXURnf+gCXUwke5YVbykBXEijO+gCXURnf8Rnf8Rnf8Rnf8Rnf8Rnf+gCXWIIoygCXUohekRnf8Rnf8Qn/+gCXUQnf8SoP////8ijO+PG4agAnGQLY6gEnrP7f94yP8aof8YwP/DY6jJcrDuz+RlwP/owt0Urv8k/v4e4v9Nr9F1XaSxMoyx3/9rc7Ayq/98UZ3gr9L8+v05rv9Fv9rF5/+7T52h9OprAAAAJHRSTlMAINTUgPmA+gYGNbu7NR9PR/xP/hoh/o74f471R3x8uie60TS1lKLVAAABzUlEQVRYw83X2XKCMBQGYOyK3RdL9x0ChVCkVAHFfXn/RyphKSIBE85Mp8woV/8HOUByIgj/+mg2yb8o1s4/nZHTw2NNobmzf0HOp/d7Ys18Apzv1hHCvJICqIZA8hnAL0T5FYBXiPOrAJ+Q5HMAj5Dm8wC78JtfA1iFLK8oeYBNWM1vvQitltB4QxxCLn8gXD2/NoTjbXZhLX9ypH8c8giFvKJLiEMo5gnALlDyEcAq0PIxwCZQ8wnAItDzKbBZKObNBJDlMCFvEor5YQ8buDfUJdt3kevb1QLl+j2vb4y9OZZ8z0a251feA238uG8qZh/rkmurSLXdqjrQ62eQn5EWsaqS9Dweh3ewDOI7aHdG5ULJ8yM1WE67cQ0604FaJqx/v0leGc6x8aV94+gpWNqiTR3FrShcU68fHqYSA3J47Qwgwnsm3NxtBtR2NVA2BKcbxIC1mFUOoaSIZldzIuDyU+tkAPtjoAMcLwIV4HkVaQDXx0ABOD9HZxIYwcTRJWswQrOBxT8hpBMKIi+xWmdK4pvS4JMqfFqHLyzwpQ2+uMKXd3iDAW9x4E0WvM2DN5rwVhfebMPbffiGA77lgW+64Ns++MYTvvX9m+MHc8vmMWg2fMUAAAAASUVORK5CYII=">
</head><body>
<!-- Plotly chart will be drawn inside this DIV --></div>
<span style="margin:auto; display:table;">{date}</span>
<span style="margin:auto; display:table;">{prevLink}&nbsp;
{nextLink}</span>
<div style="margin:auto; display:table;">
{ss}
</div>       
</body>
</html>
""";
            return responseBody;
            });

        app.Map("/period/", (HttpContext context) => {
            context.Response.ContentType = "text/html";
            return GenericChart.toEmbeddedHTML(GetPeriod(rows, bands,options));
        });

        app.MapGet("/", (HttpContext context) => {
            context.Response.ContentType = "text/html";
            return
"""
<!DOCTYPE html><html><head></head>
<body>
<a href="/day/">Day</a><br/>
<a href="/period/">Period</a>
</body>
</html>
"""; });

        app.Run();
    }

    private static string GetLatestCsv(Options options)
    {
        if(options.Input != null)
        {
            if (File.Exists(options.Input))
                return options.Input;
            else
                throw new FileNotFoundException($"{options.Input} not found");
        }

        var dir = @".\";

        if (Directory.Exists(dir))
        {
            var di = new DirectoryInfo(dir);
            return di.GetFiles("*.csv").OrderBy(f => f.CreationTime).Last().FullName;
            
        }
        throw new FileNotFoundException($"no input csv found");
    }

    private static (DateOnly start,  DateOnly end) GetDataDateRange(List<Record> rows)
    {
        return (DateOnly.FromDateTime(rows.First().Date), DateOnly.FromDateTime(rows.Last().Date));
    }

    private static GenericChart GetPeriod(List<Record> rows, List<PriceBand> bandList, Options options)
    {        
        var cb = new CostBreakdown(bandList);
        var cb2 = new RecordSummary(bandList, x => x.PricePerKwH);
        var cb3 = new RecordSummary(bandList, x => 1);

        var summarisers = new List<DateRangeSummary>
        {
            new DateRangeSummary(cb2, new Style() { Unit = "$", Title = "price", PostFix = false }),
            new DateRangeSummary(cb3, new Style() { Unit = "kWh", Title = "power", PostFix = true })
        };

        foreach (var row in rows)
        {
            foreach (var summariser in summarisers)
                summariser.Add(row);
        }

        var startDate = options.Start.HasValue ? options.Start.Value.ToDateTime(TimeOnly.MinValue) : rows.First().Date;
        var endDate = options.End.HasValue ? options.End.Value.ToDateTime(TimeOnly.MinValue) : rows.Last().Date;
        return CS.Chart.Grid(GetOverviewCharts(cb, startDate, endDate, summarisers.ToArray()), 4, 1);
    }

    
    private static GenericChart GetDay(List<Record> rows, List<PriceBand> bandList, DateOnly? d)
    {
        var cb = new CostBreakdown(bandList);
        var cb2 = new RecordSummary(bandList, x => x.PricePerKwH);
        var cb3 = new RecordSummary(bandList, x => 1);

        DateOnly date = d == null ? DateOnly.FromDateTime( rows.Last().Date): d.Value;

        var start = date.ToDateTime(TimeOnly.MinValue);
        var charts = GetDetailedCharts(cb, rows, start);
        return CS.Chart.Grid(charts, 2, 1);
    }

    public static List<GenericChart> GetOverviewCharts(CostBreakdown cb, DateTime start, DateTime end, params DateRangeSummary[] summarisers)
    {
        var result = new List<GenericChart>();

        foreach (var summary in summarisers)
        {
            var powerTotal = summary.GetValues(start, end);

            Defaults.DefaultWidth = 30 * powerTotal.Length;
            var summaryChart = CS.Chart.Column<decimal, DateTime, string>(values: powerTotal.Select(r => r.Total).ToArray(), Keys: powerTotal.Select(r => r.Date).ToArray(), Name: summary.Style.Title);

            summaryChart.WithTitle(summary.Style.Title)
                .WithXAxisStyle(title: Title.init("Date"))
                .WithYAxisStyle(title: Title.init(summary.Style.Unit));
            
            result.Add(summaryChart);

            var stackedCharts = new List<GenericChart>();

            foreach (var band in cb.Bands)
            {
                var bandTotal = summary.GetValues(start, end, band);
                if (bandTotal.Sum(x => x.Total) <= 0)
                    continue;

                var bandedChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
                    values: bandTotal.Select(r => r.Total),
                    Keys: bandTotal.Select(r => r.Date).ToArray(), Name: $"{band.Name} - {summary.GetPrefix()}{bandTotal.Sum(x => x.Total):F2}{summary.GetPostfix()}");

                bandedChart.WithXAxisStyle(title: Title.init("Date"));
                bandedChart.WithYAxisStyle(title: Title.init(summary.Style.Unit));
                stackedCharts.Add(bandedChart);
            }

            result.Add(CS.Chart.Combine(stackedCharts));
        }

        return result;
    }

 
    private static Record? GetSelectedRecord(List<Record> rows, DateTime detailedView)
    {
        foreach (var row in rows)
        {
            if (row.Date == detailedView)
                return row;
        }
        return null;
    }

    public static List<GenericChart> GetDetailedCharts(CostBreakdown cb, List<Record> rows, DateTime detailedView)
    {
        var result = new List<GenericChart>();
        List<DetailCalculator> costDetails = new List<DetailCalculator>() { new DetailCalculator(cb.Bands, (b) => b.PricePerKwH,"$"), new DetailCalculator(cb.Bands, (b) => 1, "kWh") };
        Dictionary<DetailCalculator,List<BandValue>> detailValues = new Dictionary<DetailCalculator, List<BandValue>>();

        var selectedRecord  = GetSelectedRecord(rows, detailedView);
        if (selectedRecord == null)
            return result;


        foreach (var d in costDetails)
        {
            var values = d.Calculate(selectedRecord);
            if (values.Sum(v => v.Value) == 0)
                continue;
            detailValues[d] = d.Calculate(selectedRecord);
        }

        var priceDetailCharts = new List<GenericChart>();

        var xAxis = GetXAxisValues(selectedRecord);

        foreach (var detail in detailValues.Keys)
        {
            priceDetailCharts.Clear();
            var bands = detailValues[detail].Select(x => x.Band).Distinct();

            foreach(var band in bands)
                priceDetailCharts.Add(GetDetailsChart(detailValues[detail], band, xAxis, detail.Unit, "Time"));

            result.Add(CS.Chart.Combine(priceDetailCharts));
        }
       
        return result;
    }

    public static List<DateTime> GetXAxisValues(Record selectedRecord)
    {
        List<DateTime> xAxis = new List<DateTime>();

        for (int i = 0; i < selectedRecord.Readings.Count; i++)
        {
            xAxis.Add(selectedRecord.Date + TimeSpan.FromMinutes(5) * i);
        }
        return xAxis;
    }

    private static GenericChart GetDetailsChart(List<BandValue> priceDetail, PriceBand band, List<DateTime> xAxis,string yAxisLable, string xAxisLabel) 
    {
        var prefix = yAxisLable == "$";
        var bandedChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
                      values: priceDetail.Select(r => r.Band == band ? r.Value : 0m),
                      Keys: xAxis, Name: $"{band.Name} - {(prefix?yAxisLable:string.Empty)}{priceDetail.Where(x => x.Band == band).Sum(x => x.Value):F2}{(!prefix?yAxisLable:string.Empty)}");

        bandedChart.WithXAxisStyle(title: Title.init(xAxisLabel));
        bandedChart.WithYAxisStyle(title: Title.init(yAxisLable));
        return bandedChart;
    }
}