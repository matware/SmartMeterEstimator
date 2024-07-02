using Plotly.NET.CSharp;

namespace SmartMeterEstimator
{
    public static class Optomatic
    {
        /// <summary>
        /// Converts the `Optional` value to `Some(value)` if the value is valid, or `None` if it is not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="opt">The `Optional` value to convert to a F# Option</param>
        /// <returns>opt converted to `Option`</returns>
        public static Microsoft.FSharp.Core.FSharpOption<T> ToOption<T>(this Optional<T> opt) => opt.IsSome ? new(opt.Value) : Microsoft.FSharp.Core.FSharpOption<T>.None;

        public static Optional<T> ToOptional<T>(this T o)
        {
            return new Optional<T>() { Value = o };
        }
    }

    public static class StylingHelpers
    {
        public static string GetPrefix(this DateRangeSummary s)
        {
            return s.Style.PostFix ? "" : s.Style.Unit;
        }

        public static string GetPostfix(this DateRangeSummary s)
        {
            return s.Style.PostFix ?s.Style.Unit:"";
        }

    }

}


