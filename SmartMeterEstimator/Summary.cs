namespace SmartMeterEstimator
{
    public struct Summary
    {
        public DateTime Date;
        public decimal Total;

        public override string ToString()
        {
            return $"{Date} : ${Total}";
        }
    }
}


