using System.Globalization;

namespace SmartMeterEstimator
{

    public class Record
    {      
        public int RecrodType { get; set; }
        public string DateString { get; set; }
        public DateTime Date { get { return DateTime.ParseExact(DateString, "yyyyMMdd", CultureInfo.InvariantCulture); } }
        public List<decimal> Readings { get; set; } = new List<decimal>();

        public TarrifTypes TarrifType = TarrifTypes.OnPeak;

        public Decimal EstimatedCost { get; set; }
    }
}


