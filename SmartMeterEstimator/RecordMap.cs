using CsvHelper.Configuration;
using System.Globalization;

namespace SmartMeterEstimator
{
    public class RecordMap : ClassMap<Record>
    {
        const int RECORD_TYPE = 0;
        const int DATE_STRING = RECORD_TYPE + 1;
        const int FIRST_MEASUREMENT = DATE_STRING + 1;
        const int LAST_MEASUREMENT = FIRST_MEASUREMENT + 287;

        public RecordMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.RecrodType).Index(RECORD_TYPE);
            Map(m => m.DateString).Index(DATE_STRING);
            Map(m => m.Readings).Index(FIRST_MEASUREMENT,LAST_MEASUREMENT);
        }
    }
}


