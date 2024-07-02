namespace SmartMeterEstimator
{
    public static class DateRangeHelpers
    {
        public static bool OutOfRange(this DateTime d, DateTime start, DateTime end)
        {
            return d < start || d >= end;

        }
    }
}


