using System.Text.Json.Serialization;

namespace SmartMeterEstimator
{
    public class PriceBand
    {
        private readonly TimeSpan indexIncrement = TimeSpan.FromMinutes(5);
        public string Name { get; set; }

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


