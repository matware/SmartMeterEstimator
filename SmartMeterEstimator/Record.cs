using CsvHelper.Configuration;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartMeterEstimator
{
    public static class Constants
    {
        public static readonly TimeSpan TimeFiddle = TimeSpan.FromMinutes(30);
    }

    public enum TarrifTypes
    {
        OnPeak,
        OffPeak
    }

    public class Record
    {      
        public int RecrodType { get; set; }
        public string DateString { get; set; }
        public DateTime Date { get { return DateTime.ParseExact(DateString, "yyyyMMdd", CultureInfo.InvariantCulture); } }
        public List<decimal> Readings { get; set; } = new List<decimal>();

        public TarrifTypes TarrifType = TarrifTypes.OnPeak;

        public Decimal EstimatedCost { get; set; }
    }

    public class RecordMap : ClassMap<Record>
    {
        const int RECORD_TYPE = 0;
        const int DATE_STRING = RECORD_TYPE + 1;
        const int FIRST_MEASUREMENT = DATE_STRING + 1;
        const int LAST_MEASUREMENT = FIRST_MEASUREMENT + 287;

        public RecordMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.RecrodType).Index(RECORD_TYPE);
            Map(m => m.DateString).Index(DATE_STRING);
            Map(m => m.Readings).Index(FIRST_MEASUREMENT,LAST_MEASUREMENT);
        }
    }

    public class PriceBand
    {
        private readonly TimeSpan indexIncrement = TimeSpan.FromMinutes(5);
        private readonly string name;

        public PriceBand(TimeSpan startTime, TimeSpan endTime, decimal pricePerKwH, TarrifTypes tarrifType,string name)
        {
            StartTime = startTime + Constants.TimeFiddle;
            EndTime = endTime + Constants.TimeFiddle;
            PricePerKwH = pricePerKwH;
            TarrifType = tarrifType;
            this.name = name;
            Console.WriteLine($"{name} --> StartTime:{StartTime} EndTime:{EndTime}");
        }

        public bool IsInBand(Record r, int index)
        {
            var t = index * indexIncrement;

            if (r.TarrifType != this.TarrifType)
                return false;

            return t >= StartTime && t < EndTime;
        }

        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public decimal PricePerKwH { get; }
        public TarrifTypes TarrifType { get; }

        public override string ToString()
        {
            return $"{name} : ${PricePerKwH}";
        }
    }

    public class CostBreakdown
    {
        private readonly List<PriceBand> prices;

        public CostBreakdown(List<PriceBand> prices)
        {
            this.prices = prices;
        }

        public Dictionary<PriceBand, decimal> Price(Record record)
        {
            var result = new Dictionary<PriceBand, decimal>();

            foreach (var b in prices)
            {
                int index = 0;
                result[b] = 0;
                foreach (var r in record.Readings)
                {
                    if (b.IsInBand(record, index))
                    {
                        result[b] += b.PricePerKwH * r;
                    }
                    index++;
                }
            }

            return result;
        }

    }

    public struct Summary
    {
        public DateTime Date;
        public decimal Total;

        public override string ToString()
        {
            return $"{Date} : ${Total}";
        }
    }

    public class DateRangeSummary
    {
        private readonly DateTime startDate;
        private readonly DateTime endDate;
        private readonly CostBreakdown costBreakdown;
        Dictionary<DateTime, decimal> totals = new Dictionary<DateTime, decimal>();
        Dictionary<DateTime, decimal> powerTotals = new Dictionary<DateTime, decimal>();

        public DateRangeSummary(CostBreakdown costBreakdown)
        {
            this.costBreakdown = costBreakdown;
        }

        public void Add(Record r)
        {
            if (costBreakdown != null)
            {
                var total = costBreakdown.Price(r).Values.Sum();
                AddTotal(r, total, totals);
            }

            var p = r.Readings.Sum();

            AddTotal(r, p, powerTotals);
        }

        private void AddTotal(Record r, decimal val, Dictionary<DateTime, decimal> kvp)
        {
            if (kvp.ContainsKey(r.Date))
                kvp[r.Date] += val;
            else
                kvp[r.Date] = val;
        }

        public decimal GetTotalCost(DateTime startDate, DateTime endDate)
        {
            return GetTotalImpl(startDate, endDate, totals);
        }

        public decimal GetTotalPower(DateTime startDate, DateTime endDate)
        {
            return GetTotalImpl(startDate, endDate, powerTotals);
        }

        public decimal GetTotalImpl(DateTime startDate, DateTime endDate, Dictionary<DateTime, decimal> kvp)
        {
            endDate = endDate.AddMicroseconds(1);
            var total = 0m;
            int count = 0;
            foreach (var date in kvp.Keys)
            {
                if (date.OutOfRange(startDate, endDate))
                    continue;

                total += kvp[date];
                count++;
            }
            return total;
        }

        private Summary[] GetTotalsImpl(Dictionary<DateTime, decimal> kvp)
        {
            var dates = kvp.Keys.Order().ToArray();

            var summaries = new List<Summary>(dates.Length);

            foreach (var date in dates)
            {
                summaries.Add(new Summary { Date = date, Total = kvp[date] });
            }

            return summaries.ToArray();
        }

        private Summary[] GetTotalsImpl(Dictionary<DateTime, decimal> kvp, DateTime start, DateTime end)
        {
            var dates = kvp.Keys.Order().ToArray();

            var summaries = new List<Summary>(dates.Length);

            foreach (var date in dates)
            {
                if (date.OutOfRange(start, end))
                    continue;

                summaries.Add(new Summary { Date = date, Total = kvp[date] });
            }

            return summaries.ToArray();
        }

        public Summary[] GetCosts()
        {
            return GetTotalsImpl(totals);
        }
        public Summary[] GetPower()
        {
            return GetTotalsImpl(powerTotals);
        }

        public Summary[] GetCosts(DateTime start, DateTime end)
        {
            return GetTotalsImpl(totals, start, end);
        }
        public Summary[] GetPower(DateTime start, DateTime end)
        {
            return GetTotalsImpl(powerTotals, start, end);
        }
    }

    public static class DateRangeHelpers
    {
        public static bool OutOfRange(this DateTime d, DateTime start, DateTime end)
        {
            return d < start || d >= end;

        }
    }

    public static class Optomatic
    {
        /// <summary>
        /// Converts the `Optional` value to `Some(value)` if the value is valid, or `None` if it is not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="opt">The `Optional` value to convert to a F# Option</param>
        /// <returns>opt converted to `Option`</returns>
        public static Microsoft.FSharp.Core.FSharpOption<T> ToOption<T>(this Optional<T> opt) => opt.IsSome ? new(opt.Value) : Microsoft.FSharp.Core.FSharpOption<T>.None;

        public static Optional<T> ToOptional<T>(this T o)
        {
            return new Optional<T>() { Value = o };
        }
    }
}


