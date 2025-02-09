﻿using CsvHelper;
using CsvHelper.Configuration;
using SmartMeterEstimator;
using System.Globalization;
using Plotly.NET;
using CS = Plotly.NET.CSharp;
using Plotly.NET.CSharp;
using CommandLine;
using System.Text.Json;
using System.Collections.Generic;

internal class Program
{

    public class Options
    {
        [Option('i',"input",Required =true,HelpText = "The etsa/power networks sa detailed file csv containing the power meausrements")]
        public string Input {  get; set; }

        [Option('b', "bands", Required = false, HelpText = "Price bands configuration", Default = "bands.json")]
        public string PriceBands { get;set; }

        [Option('s', "start", Required = true, HelpText = "Graph start date")]
        public DateOnly Start { get; set; } = new DateOnly(2024, 05, 19);

        [Option('e', "end", Required = true, HelpText = "Graph end date")]
        public DateOnly End { get; set; } = new DateOnly(2024, 06, 19);
    }
    public static List<PriceBand> GetDefaultBands()
    {
        var car = new PriceBand(0.07272728m, TarrifTypes.OnPeak, "car", TimeRange.FromHours(0, 6));
        var peak = new PriceBand(0.4561m, TarrifTypes.OnPeak, "peak", TimeRange.FromHours(6, 10), TimeRange.FromHours(15, 24), TimeRange.FromHours(-0.5f, 0));
        var sholder = new PriceBand(0.2673m, TarrifTypes.OnPeak, "sholder", TimeRange.FromHours(10, 15));

        var controlledMorning = new PriceBand(0.2155m, TarrifTypes.OffPeak, "controlled off peak", TimeRange.FromHours(0, 6.5), TimeRange.FromHours(23.5, 24), TimeRange.FromHours(-0.5, 0));
        var controlledSholder = new PriceBand(0.1725m, TarrifTypes.OffPeak, "controlled sholder", TimeRange.FromHours(9.5, 15.5));
        var controlledPeak1 = new PriceBand(0.4145m, TarrifTypes.OffPeak, "controlled peak", TimeRange.FromHours(6.5, 9.5), TimeRange.FromHours(15.5, 23.5));

        //var start = new DateTime(2024, 05, 19);
        //var end = new DateTime(2024, 06, 19);

        
        var bandList = new List<PriceBand> {
            car,
            sholder,
            peak,
            controlledMorning,
            controlledSholder,
            controlledPeak1,};

        return bandList;
    }
    private static void Main(string[] args)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            //NewLine = Environment.NewLine,
            HasHeaderRecord = false,
        };

        Options options = null;
        
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {            
            options = o;
        });

        if (options == null)
        {
            Console.WriteLine("oops");
            return;
        }

        var detailedView = new DateTime(2025, 01, 30);

        List < PriceBand > bandList = null;

        if (File.Exists(options.PriceBands))
        { 
            var bands = File.ReadAllText(options.PriceBands);
            bandList = JsonSerializer.Deserialize<List<PriceBand>>(bands);
        }
        else
        {
            bandList = GetDefaultBands();

            var json = JsonSerializer.Serialize(bandList, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(options.PriceBands, json);
        }

        var cb = new CostBreakdown(bandList);
        var cb2 = new RecordSummary(bandList,x=>x.PricePerKwH);
        var cb3 = new RecordSummary(bandList, x => 1);


        using (var sr = new StreamReader(options.Input))
        using (var csv = new CsvReader(sr, config))
        {
            var rows = new List<Record>();
            csv.Context.RegisterClassMap<RecordMap>();

            var currentTarrifType = TarrifTypes.OffPeak;

            var summarisers = new List<DateRangeSummary>();
            summarisers.Add(new DateRangeSummary(cb2,new Style() { Unit = "$", Title = "price", PostFix = false }));
            summarisers.Add(new DateRangeSummary(cb3,new Style() { Unit = "kWh", Title = "power", PostFix = true }));

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

                        foreach(var summariser in summarisers)
                            summariser.Add(r);

                        rows.Add(r);
                        break;
                }
            }

            var grid = CS.Chart.Grid(GetOverviewCharts(cb, options.Start.ToDateTime(TimeOnly.MinValue), options.End.ToDateTime(TimeOnly.MaxValue), summarisers.ToArray()), 4, 1);

            CS.GenericChartExtensions.Show(grid);

            var charts = GetDetailedCharts(cb,rows,detailedView);

            CS.GenericChartExtensions.Show(CS.Chart.Grid(charts, 2, 1));
        }
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
        var bandedChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
                      values: priceDetail.Select(r => r.Band == band ? r.Value : 0m),
                      Keys: xAxis, Name: $"{band.Name} - ${priceDetail.Where(x => x.Band == band).Sum(x => x.Value):F2}");

        bandedChart.WithXAxisStyle(title: Title.init(xAxisLabel));
        bandedChart.WithYAxisStyle(title: Title.init(yAxisLable));
        return bandedChart;
    }
}