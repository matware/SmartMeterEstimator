namespace SmartMeterEstimator
{
    public class DateRangeSummary
    {
        private readonly DateTime startDate;
        private readonly DateTime endDate;
        private readonly CostBreakdown costBreakdown;
        Dictionary<DateTime, decimal> totals = new Dictionary<DateTime, decimal>();
        Dictionary<DateTime, Dictionary<PriceBand, decimal>> bandPriceTotals = new Dictionary<DateTime, Dictionary<PriceBand, decimal>>();
        Dictionary<DateTime, Dictionary<PriceBand, decimal>> bandPowerTotals = new Dictionary<DateTime, Dictionary<PriceBand, decimal>>();
        Dictionary<DateTime, decimal> powerTotals = new Dictionary<DateTime, decimal>();

        public DateRangeSummary(CostBreakdown costBreakdown)
        {
            this.costBreakdown = costBreakdown;
        }

        public void Add(Record r)
        {
            if (costBreakdown != null)
            {
                var bandedPrices = costBreakdown.Price(r);
                var total = bandedPrices.Values.Sum();

                AddTotal(r, total, totals);
                AddTotal(r, bandedPrices, bandPriceTotals);
                AddTotal(r, costBreakdown.Power(r), bandPowerTotals);
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

        private void AddTotal(Record r, Dictionary<PriceBand, decimal> val, Dictionary<DateTime, Dictionary<PriceBand, decimal>> kvp)
        {

            if (kvp.ContainsKey(r.Date))
            {
                var bands = kvp[r.Date];
                foreach (var band in val.Keys)
                {

                    if(!bands.ContainsKey(band))
                        bands[band] = val[band];
                    else
                        bands[band] += val[band];
                }
            }
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

        private Summary[] GetTotalsImpl(Dictionary<DateTime, Dictionary<PriceBand, decimal>> kvp, DateTime start, DateTime end, PriceBand bandFilter)
        {
            var dates = kvp.Keys.Order().ToArray();

            var summaries = new List<Summary>(dates.Length);

            foreach (var date in dates)
            {
                if (date.OutOfRange(start, end))
                    continue;

                if (kvp[date].ContainsKey(bandFilter))
                    summaries.Add(new Summary { Date = date, Total = kvp[date][bandFilter] });
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

        public Summary[] GetCosts(DateTime start, DateTime end, PriceBand? bandFilter = null)
        {
            if (bandFilter == null)
                return GetTotalsImpl(totals, start, end);
            else
                return GetTotalsImpl(bandPriceTotals, start, end, bandFilter);
        }

        public Summary[] GetPower(DateTime start, DateTime end, PriceBand? bandFilter = null)
        {
            if (bandFilter == null)
                return GetTotalsImpl(powerTotals, start, end);
            else
                return GetTotalsImpl(bandPowerTotals, start, end, bandFilter);
        }
    }
}


