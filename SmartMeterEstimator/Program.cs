using CsvHelper;
using CsvHelper.Configuration;
using SmartMeterEstimator;
using System.Globalization;
using Plotly.NET;
using CS = Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Plotly.NET.CSharp;
using System;
using System.Collections.Generic;

internal class Program
{
    private static void Main(string[] args)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            //NewLine = Environment.NewLine,
            HasHeaderRecord = false,
        };

        var car = new PriceBand(0.07272728m, TarrifTypes.OnPeak, "car", TimeRange.FromHours(0, 6));
        var peak = new PriceBand(0.4561m, TarrifTypes.OnPeak, "peak", TimeRange.FromHours(6, 10), TimeRange.FromHours(15, 24), TimeRange.FromHours(-0.5f, 0));
        var sholder = new PriceBand(0.2673m, TarrifTypes.OnPeak, "sholder", TimeRange.FromHours(10, 15));

        var controlledMorning = new PriceBand(0.2155m, TarrifTypes.OffPeak, "controlled off peak", TimeRange.FromHours(0, 6.5), TimeRange.FromHours(23.5, 24), TimeRange.FromHours(-0.5, 0));
        var controlledSholder = new PriceBand(0.1725m, TarrifTypes.OffPeak, "controlled sholder", TimeRange.FromHours(9.5, 15.5));
        var controlledPeak1 = new PriceBand(0.4145m, TarrifTypes.OffPeak, "controlled peak", TimeRange.FromHours(6.5, 9.5), TimeRange.FromHours(15.5, 23.5));

        var bandList = new List<PriceBand> {
    car,
    sholder,
    peak,
    controlledMorning,
    controlledSholder,
    controlledPeak1,
    };
        DateTime detailedView = new DateTime(2024, 06, 20);

        var cb = new CostBreakdown(bandList);


        using (var sr = new StreamReader("20015323531_20221222_20240625_20240626210349_SAPN_DETAILED.csv"))
        using (var csv = new CsvReader(sr, config))
        {
            var rows = new List<Record>();
            csv.Context.RegisterClassMap<RecordMap>();

            var currentTarrifType = TarrifTypes.OffPeak;


            var priceSummary = new DateRangeSummary(cb);

            while (csv.Read())
            {
                switch (csv.GetField(0))
                {
                    case "200":
                        if (currentTarrifType == TarrifTypes.OnPeak)
                            currentTarrifType = TarrifTypes.OffPeak;
                        else
                            currentTarrifType = TarrifTypes.OnPeak;
                        continue; // filter 200s
                    case "400": continue; // filter 200s
                    case "300":
                        var r = csv.GetRecord<Record>();
                        r.TarrifType = currentTarrifType;
                        r.EstimatedCost = cb.Price(r).Values.Sum();
                        priceSummary.Add(r);

                        rows.Add(r);
                        break;
                }
            }

            var start = new DateTime(2024, 03, 19);
            var end = new DateTime(2024, 04, 19);

            var c = priceSummary.GetTotalCost(start, end);

            Console.WriteLine($"Estimate for bill {c:F2} + ${28.82:F2} = {c + 28.82m:F2}, {priceSummary.GetTotalPower(start, end):F1}kWh");

            var powerTotal = priceSummary.GetPower(start, end);
            var priceTotal = priceSummary.GetCosts(start, end);

            Defaults.DefaultWidth = 30 * powerTotal.Length;
            //////////////////////////
            // This is not working yet
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var aa = new LinearAxis();
            LinearAxis.style<IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible>(TickFormat: "kw".ToOptional().ToOption()).Invoke(aa);

            var yAxisTicks = LinearAxis.init<IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible>(TickFormat: "kw".ToOptional().ToOption());
            var xAxisTicks = LinearAxis.init<IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible, IConvertible>(TickFormat: "doy".ToOptional().ToOption());
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //var power = CS.Chart.Point<DateTime, decimal, string>(y: powerTotal.Select(r => r.Total), x: powerTotal.Select(r => r.Date), Name: "power");
            var power = CS.Chart.Column<decimal, DateTime, string>(values: powerTotal.Select(r => r.Total), Keys: powerTotal.Select(r => r.Date).ToOptional(), Name: "power");

            power.WithTitle("Power")
                .WithXAxisStyle(title: Title.init("Date"))
                .WithYAxisStyle(title: Title.init("KW")).WithYAxis(aa).WithYAxis(xAxisTicks);


            //var prices = CS.Chart.Point<DateTime, decimal, string>(y: priceTotal.Select(r => r.Total), x: priceTotal.Select(r => r.Date),Name:"price")
            var prices = CS.Chart.StackedColumn<decimal, DateTime, string>(values: priceTotal.Select(r => r.Total), Keys: priceTotal.Select(r => r.Date).ToOptional(), Name: "price");
            prices.WithTitle("Daily Costs");
            prices.WithXAxisStyle(title: Title.init("Date"));
            prices.WithYAxisStyle(title: Title.init("$"));


            var priceCharts = new List<GenericChart>();
            var powerCharts = new List<GenericChart>();

            foreach (var band in cb.Bands)
            {
                var bandTotal = priceSummary.GetCosts(start, end, band);
                if (bandTotal.Sum(x => x.Total) <= 0)
                    continue;

                var bandedChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
                    values: bandTotal.Select(r => r.Total),
                    Keys: bandTotal.Select(r => r.Date).ToOptional(), Name: $"{band} - ${bandTotal.Sum(x => x.Total):F2}");

                bandedChart.WithXAxisStyle(title: Title.init("Date"));
                bandedChart.WithYAxisStyle(title: Title.init("$"));
                priceCharts.Add(bandedChart);

                var bandPowerTotal = priceSummary.GetPower(start, end, band);
                if (bandTotal.Sum(x => x.Total) <= 0)
                    continue;

                var bandedPowerChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
                    values: bandPowerTotal.Select(r => r.Total),
                    Keys: bandPowerTotal.Select(r => r.Date).ToOptional(), Name: $"{band.Name} - {bandPowerTotal.Sum(x => x.Total):F2}kWh");

                bandedPowerChart.WithXAxisStyle(title: Title.init("Date"));
                bandedPowerChart.WithYAxisStyle(title: Title.init("kW"));
                powerCharts.Add(bandedPowerChart);
            }

            var priceChart = CS.Chart.Combine(priceCharts);
            var powerChart = CS.Chart.Combine(powerCharts);

            var grid = CS.Chart.Grid(new[] { power, prices, priceChart, powerChart }, 4, 1);

            CS.GenericChartExtensions.Show(grid);

            var charts = Carty2(cb,rows,detailedView);

            CS.GenericChartExtensions.Show(CS.Chart.Grid(charts, 2, 1));
        }
    }



    public static List<GenericChart> Carty2(CostBreakdown cb, List<Record> rows, DateTime detailedView)
    {
        var result = new List<GenericChart>();
        List<CostDetail> costDetails = new List<CostDetail>() { new CostDetail(cb.Bands, (b) => b.PricePerKwH,"$"), new CostDetail(cb.Bands, (b) => 1, "kWh") };
        Dictionary<CostDetail,List<BandValue>> detailValues = new Dictionary<CostDetail, List<BandValue>>();
        Record? selectedRecord = null;
        
        foreach (var row in rows)
        {
            if (row.Date == detailedView)
            {
                foreach(var d in costDetails)
                {
                    var values = d.Price(row);
                    if (values.Sum(v => v.Value) == 0)
                        continue;
                    detailValues[d] = d.Price(row);
                }
                selectedRecord = row;
                break;
            }
        }
        if (selectedRecord == null)
            return result;

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
        var bandedChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
                      values: priceDetail.Select(r => r.Band == band ? r.Value : 0m),
                      Keys: xAxis, Name: $"{band.Name} - ${priceDetail.Where(x => x.Band == band).Sum(x => x.Value):F2}");

        bandedChart.WithXAxisStyle(title: Title.init(xAxisLabel));
        bandedChart.WithYAxisStyle(title: Title.init(yAxisLable));
        return bandedChart;
    }
}