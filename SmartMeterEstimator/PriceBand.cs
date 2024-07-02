namespace SmartMeterEstimator
{
    public class PriceBand
    {
        private readonly TimeSpan indexIncrement = TimeSpan.FromMinutes(5);
        public string Name { get; }

        public PriceBand(decimal pricePerKwH, TarrifTypes tarrifType,string name, params TimeRange[] r)
        {
            foreach (var t in r)
            {
                ranges.Add(new TimeRange() { StartTime = t.StartTime+Constants.TimeFiddle, EndTime = t.EndTime + Constants.TimeFiddle });
            }

            PricePerKwH = pricePerKwH;
            TarrifType = tarrifType;
            this.Name = name;
            Console.WriteLine($"{name} --> StartTime:{StartTime} EndTime:{EndTime}");
        }

        public bool IsInBand(Record r, int index)
        {
            var t = index * indexIncrement;

            if (r.TarrifType != this.TarrifType)
                return false;

            foreach (var range in ranges)
            {
                if(t >= range.StartTime && t < range.EndTime)
                    return true;
            }
            return false;
        }

        private List<TimeRange> ranges = new List<TimeRange>();
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public decimal PricePerKwH { get; }
        public TarrifTypes TarrifType { get; }

        public override string ToString()
        {
            return $"{Name} : ${PricePerKwH:F2}";
        }
    }
}


