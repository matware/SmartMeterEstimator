namespace SmartMeterEstimator
{
    public class CostBreakdown
    {
        public List<PriceBand> Bands { get; }

        public CostBreakdown(List<PriceBand> prices)
        {
            this.Bands = prices;
        }

        public Dictionary<PriceBand, decimal> Price(Record record)
        {
            var result = new Dictionary<PriceBand, decimal>();
            int index = 0;
            bool used = false;
            foreach (var r in record.Readings)
            {
                foreach (var b in Bands)
                {
                    if (b.IsInBand(record, index))
                    {
                        if (!result.ContainsKey(b))
                            result[b] = 0;

                        result[b] += b.PricePerKwH * r;
                        used = true;
                        break;
                    }
                }
                
                index++;
            }
            

            return result;
        }

        public Dictionary<PriceBand, decimal> Power(Record record)
        {
            var result = new Dictionary<PriceBand, decimal>();

            foreach (var b in Bands)
            {
                int index = 0;
                result[b] = 0;
                foreach (var r in record.Readings)
                {
                    if (b.IsInBand(record, index))
                    {
                        result[b] += r;
                    }
                    index++;
                }
            }

            return result;
        }
    }

    public struct BandValue
    {
        public PriceBand Band { get; set; }
        public decimal Value { get; set; }
    }

    public class CostDetail
    {
        private readonly Func<PriceBand, decimal> scale;

        public List<PriceBand> Bands { get; }
        public string Unit { get; }

        public CostDetail(List<PriceBand> prices, Func<PriceBand,decimal> scale, string unit)
        {
            this.Bands = prices;
            this.scale = scale;
            Unit = unit;
        }

        public List<BandValue> Price(Record record)
        {
            var result = new List<BandValue>();
            
            for (int i = 0; i < record.Readings.Count; i++)
            {
                foreach (var b in Bands)
                {
                    if (b.IsInBand(record, i))
                    {   
                        result.Add(new BandValue() { Band = b, Value = scale(b) * record.Readings[i]});
                        break;
                    }
                }
            }

            return result;
        }
    }
}


