﻿using CsvHelper;
using CsvHelper.Configuration;
using SmartMeterEstimator;
using System.Globalization;
using Plotly.NET;
using CS = Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Plotly.NET.CSharp;
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    //NewLine = Environment.NewLine,
    HasHeaderRecord = false,    
};

var car = new PriceBand(TimeSpan.FromHours(0), TimeSpan.FromHours(6), 0.07272728m, TarrifTypes.OnPeak, "car");
var sholder = new PriceBand(TimeSpan.FromHours(10), TimeSpan.FromHours(15), 0.2673m, TarrifTypes.OnPeak, "sholder");
var peak1= new PriceBand(TimeSpan.FromHours(6), TimeSpan.FromHours(10), 0.4561m, TarrifTypes.OnPeak, "peak - Mornings");
var peak2 = new PriceBand(TimeSpan.FromHours(3), TimeSpan.FromHours(12), 0.4561m, TarrifTypes.OnPeak, "peak - Evening");

var controlledNight = new PriceBand(TimeSpan.FromHours(23.5), TimeSpan.FromHours(24), 0.2155m, TarrifTypes.OffPeak, "controledNight");
var controlledMorning = new PriceBand(TimeSpan.FromHours(0), TimeSpan.FromHours(6.5), 0.2155m, TarrifTypes.OffPeak, "controlledMorning");
var controlledSholder = new PriceBand(TimeSpan.FromHours(9.5), TimeSpan.FromHours(15.5), 0.1725m, TarrifTypes.OffPeak, "controlledSholder");
var controlledPeak1 = new PriceBand(TimeSpan.FromHours(6.5), TimeSpan.FromHours(9.5), 0.4145m, TarrifTypes.OffPeak, "controlledPeak");
var controlledPeak2 = new PriceBand(TimeSpan.FromHours(15.5), TimeSpan.FromHours(23.5), 0.4145m, TarrifTypes.OffPeak, "controlledPeak");


var cb = new CostBreakdown(new List<PriceBand> { 
    car, 
    sholder, 
    peak1, 
    peak2, 
    controlledNight, 
    controlledMorning, 
    controlledSholder, 
    controlledPeak1, 
    controlledPeak2 });

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

    var start = new DateTime(2024, 05, 19);
    var end = new DateTime(2024, 06, 19);

    Console.WriteLine($"Estimate for bill {priceSummary.GetTotalCost(start, end)}");
    var powerTotal = priceSummary.GetPower(start, end);
    var priceTotal = priceSummary.GetCosts(start, end);

    Plotly.NET.Defaults.DefaultWidth = 30 * powerTotal.Length;
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

    foreach (var band in cb.Prices)
    {
        var bandTotal = priceSummary.GetCosts(start, end, band);
        if(bandTotal.Sum(x=>x.Total) <= 0)
            continue;

        var bandedChart = CS.Chart.StackedColumn<decimal, DateTime, string>(
            values: bandTotal.Select(r => r.Total), 
            Keys: bandTotal.Select(r => r.Date).ToOptional(), Name: $"{band} - ${bandTotal.Sum(x=>x.Total):F2}");

        bandedChart.WithXAxisStyle(title: Title.init("Date"));
        bandedChart.WithYAxisStyle(title: Title.init("$"));
        priceCharts.Add(bandedChart);
    }

    var xxx = Plotly.NET.CSharp.Chart.Combine(priceCharts);
    var grid = CS.Chart.Grid(new[] { power, prices, xxx }, 3, 1);

    Plotly.NET.CSharp.GenericChartExtensions.Show(grid);

}