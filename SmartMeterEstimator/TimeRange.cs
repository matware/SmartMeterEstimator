namespace SmartMeterEstimator
{
    public class TimeRange
    {
        public TimeRange(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }
        public TimeSpan Start { get; set; }
        public TimeSpan End {get; set; }

        public static TimeRange FromHours(double s, double e)
        {
            return new TimeRange(TimeSpan.FromHours(s), TimeSpan.FromHours(e));
        }
    }
}


