using CsvHelper.Configuration;
using CsvHelper;
using SmartMeterEstimator;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace SmartMeterEstimator
{
    public class PriceBand
    {
        private readonly TimeSpan indexIncrement = TimeSpan.FromMinutes(5);
        public string Name { get; set; }

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

        public static List<PriceBand> LoadOrCreateBands(string bandsFile)
        {
            List<PriceBand> bandList = null;

            if (File.Exists(bandsFile))
            {
                var bands = File.ReadAllText(bandsFile);
                bandList = JsonSerializer.Deserialize<List<PriceBand>>(bands);
            }
            else
            {
                bandList = GetDefaultBands();

                var json = JsonSerializer.Serialize(bandList, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(bandsFile, json);
            }

            return bandList;
        }

        public PriceBand(decimal pricePerKwH, TarrifTypes tarrifType,string name, params TimeRange[] ranges)
        {
            foreach (var t in ranges)
            {
                Ranges.Add(t);
            }

            PricePerKwH = pricePerKwH;
            TarrifType = tarrifType;
            this.Name = name;
        }

        [JsonConstructor]
        public PriceBand(decimal pricePerKwH, TarrifTypes tarrifType, string name, List<TimeRange> ranges):
            this(pricePerKwH,tarrifType, name, ranges.ToArray()) 
        {
        }

        public bool IsInBand(Record r, int index)
        {
            var t = index * indexIncrement;

            if (r.TarrifType != this.TarrifType)
                return false;

            foreach (var range in Ranges)
            {
                if(t >= range.Start && t < range.End)
                    return true;
            }
            return false;
        }

        public List<TimeRange> Ranges { get; set; } = new List<TimeRange>();
        public decimal PricePerKwH { get; set; }
        public TarrifTypes TarrifType { get; set; }

        public override string ToString()
        {
            return $"{Name} : ${PricePerKwH:F2}";
        }
    }
}

public static class RecordLoader
{
    private static CsvConfiguration config;
    static RecordLoader()
    {
        config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            //NewLine = Environment.NewLine,
            HasHeaderRecord = false,
        };
    }

    public static List<Record> GetRecords(string input, List<PriceBand> priceBands)
    {
        var cb = new CostBreakdown(priceBands);
        var rows = new List<Record>();

        using (var sr = new StreamReader(input))

        using (var csv = new CsvReader(sr, config))
        {
            csv.Context.RegisterClassMap<RecordMap>();

            var currentTarrifType = TarrifTypes.OffPeak;

            var summarisers = new List<DateRangeSummary>();

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

                        foreach (var summariser in summarisers)
                            summariser.Add(r);

                        rows.Add(r);
                        break;
                }
            }
        }

        return rows;
    }

}

