using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeterEstimator
{
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
        public PriceBand(TimeSpan startTime, TimeSpan endTime, decimal pricePerKwH, TarrifTypes tarrifType)
        {
            StartTime = startTime;
            EndTime = endTime;
            PricePerKwH = pricePerKwH;
            TarrifType = tarrifType;
        }

        public bool IsInBand(Record r, int index)
        {
            var t = index * indexIncrement;
            if (r.TarrifType != this.TarrifType)
                return false;

            return t >= StartTime && t< EndTime;
        }

        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public decimal PricePerKwH { get; }
        public TarrifTypes TarrifType { get; }
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
        public Decimal Total;
    }

    public class DailySummary
    {
        Dictionary<DateTime, decimal> totals = new Dictionary<DateTime, decimal>();

        public DailySummary()
        {

        }

        public void Add(Record r, decimal total)
        {
            if (totals.ContainsKey(r.Date))
                totals[r.Date] += total;
            else
                totals[r.Date] = total;
        }

        public Summary[] GetTotals()
        {
            var dates = totals.Keys.Order().ToArray();

            var summaries = new List<Summary>(dates.Length);

            foreach (var date in dates)
            {
                summaries.Add(new Summary { Date = date, Total = totals[date] });
            }

            return summaries.ToArray();
        }
    }
}


