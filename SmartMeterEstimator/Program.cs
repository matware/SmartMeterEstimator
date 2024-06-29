using CsvHelper;
using CsvHelper.Configuration;
using SmartMeterEstimator;
using System.Globalization;
using System.Linq;
using Plotly.NET.CSharp;
using System.Collections.Specialized;

var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    //NewLine = Environment.NewLine,
    HasHeaderRecord = false,    
};

var car = new PriceBand(TimeSpan.FromHours(0), TimeSpan.FromHours(6), 0.07272728m, TarrifTypes.OnPeak);
var sholder = new PriceBand(TimeSpan.FromHours(10), TimeSpan.FromHours(15), 0.2673m, TarrifTypes.OnPeak);
var peak1= new PriceBand(TimeSpan.FromHours(6), TimeSpan.FromHours(10), 0.4561m, TarrifTypes.OnPeak);
var peak2 = new PriceBand(TimeSpan.FromHours(3), TimeSpan.FromHours(12), 0.4561m, TarrifTypes.OnPeak);

var controlledNight = new PriceBand(TimeSpan.FromHours(23.5), TimeSpan.FromHours(24), 0.4561m, TarrifTypes.OffPeak);
var controlledMorning = new PriceBand(TimeSpan.FromHours(0), TimeSpan.FromHours(6.5), 0.4561m, TarrifTypes.OffPeak);
var controlledSholder = new PriceBand(TimeSpan.FromHours(9.5), TimeSpan.FromHours(15.5), 0.4561m, TarrifTypes.OffPeak);
var controlledPeak = new PriceBand(TimeSpan.FromHours(9.5), TimeSpan.FromHours(15.5), 0.4561m, TarrifTypes.OffPeak);

var cb = new CostBreakdown(new List<PriceBand> { car, sholder, peak1, peak2, controlledNight, controlledMorning, controlledSholder, controlledPeak });

using (var sr = new StreamReader("20015323531_20221222_20240625_20240626210349_SAPN_DETAILED.csv"))
using (var csv = new CsvReader(sr,config))
{
    var  rows = new List<Record>();
    csv.Context.RegisterClassMap<RecordMap>();

    var currentTarrifType = TarrifTypes.OnPeak;


    var priceSummary = new DailySummary();
    var powerSummary = new DailySummary();
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
                priceSummary.Add(r,r.EstimatedCost);
                powerSummary.Add(r,r.Readings.Sum());
                rows.Add(r);
                break;
        }
    }

    foreach(var row in rows)
        Console.WriteLine($"{row.Date.ToShortDateString()}, {row.Readings.Sum()}khw");

    Plotly.NET.Defaults.DefaultWidth = 2400;
    var powerTotal = powerSummary.GetTotals();
    var priceTotal = priceSummary.GetTotals();
    Chart.Point<DateTime, decimal, string>(y: powerTotal.Select(r => r.Total),x:powerTotal.Select(r=>r.Date)).Show();
    Chart.Point<DateTime, decimal, string>(y: priceTotal.Select(r => r.Total), x: priceTotal.Select(r => r.Date)).Show();
}