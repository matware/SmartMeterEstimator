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

    public class CostDetails
    {
        public List<PriceBand> Bands { get; }

        public CostDetails(List<PriceBand> prices)
        {
            this.Bands = prices;
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
                        result.Add(new BandValue() { Band = b, Value = b.PricePerKwH * record.Readings[i]});
                        break;
                    }
                }
            }

            return result;
        }

        public List<BandValue> Power(Record record)
        {
            var result = new List<BandValue>();

            for (int i = 0; i < record.Readings.Count; i++)
            {
                foreach (var b in Bands)
                {
                    if (b.IsInBand(record, i))
                    {
                        var band = new BandValue() { Band = b, Value = record.Readings[i] };
                        result.Add(band);
                        break;
                    }

                }
            }

            return result;
        }
    }
}


