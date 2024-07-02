namespace SmartMeterEstimator
{
    public struct TimeRange
    {
        public TimeRange(TimeSpan start, TimeSpan end)
        {
            StartTime = start;
            EndTime = end;
        }
        public TimeSpan StartTime;
        public TimeSpan EndTime;

        public static TimeRange FromHours(double s, double e)
        {
            return new TimeRange(TimeSpan.FromHours(s), TimeSpan.FromHours(e));
        }
    }
}


