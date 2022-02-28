using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insolaris.Calculation
{
    public class CalculationResult
    {
        public double TotalPeriod { get; private set; }
        public TimeSpan TotalTime { get; private set; }
        public List<Tuple<double, double>> SegmentPeriods { get; }
        public List<TimeSpan> SegmentTimes { get; }
        public double BiggestSegment { get; }

        double TotalShadowPeriod => throw new NotImplementedException();
        TimeSpan TotalShadowTime => throw new NotImplementedException();
        List<Tuple<double, double>> ShadowSegmentPeriods => throw new NotImplementedException();
        List<TimeSpan> ShadowSegmentTimes => throw new NotImplementedException();
        double BiggestShadowSegment => throw new NotImplementedException();

        private CalculationResult(double totalPeriod, TimeSpan totalTime, List<Tuple<double, double>> segmentPeriods, List<TimeSpan> segmentTimes, double bigSegment)
        {
            TotalPeriod = totalPeriod;
            TotalTime = totalTime;
            SegmentPeriods = segmentPeriods;
            SegmentTimes = segmentTimes;
            BiggestSegment = bigSegment;
        }

        public static CalculationResult CreateFromShadowSegments(List<Tuple<double, double>> shadowSegments, double startParam, double endParam)
        {
            var distinctedShadows = DistinctSegments(shadowSegments);
            var segmentPeriods = ConvertShadowToInsolactionSegments(distinctedShadows, startParam, endParam);
            var periods = segmentPeriods.Select(x => x.Item2 - x.Item1);
            var biggestSegment = periods.OrderBy(x => x).Last();
            var segmentTimes = segmentPeriods.Select(x => ConvertPeriodToTime(x)).ToList();
            var totalPeriod = periods.Sum();
            var totalTime = TimeSpan.FromSeconds(totalPeriod / Utils.InsolationCalculationUtils.RadianSecondsRatio);

            return new CalculationResult(totalPeriod, totalTime, segmentPeriods, segmentTimes, biggestSegment);
        }

        public static CalculationResult CreateFromInsolationSegments(List<Tuple<double, double>> insolationSegments)
        {
            var segmentPeriods = DistinctSegments(insolationSegments);
            var periods = insolationSegments.Select(x => x.Item2 - x.Item1);
            var biggestSegment = periods.OrderBy(x => x).Last();
            var segmentTimes = insolationSegments.Select(x => ConvertPeriodToTime(x)).ToList();
            var totalPeriod = periods.Sum();
            var totalTime = TimeSpan.FromSeconds(totalPeriod / Utils.InsolationCalculationUtils.RadianSecondsRatio);

            return new CalculationResult(totalPeriod, totalTime, segmentPeriods, segmentTimes, biggestSegment);
        }

        static List<Tuple<double, double>> ConvertShadowToInsolactionSegments(List<Tuple<double, double>> shadows, double startParam, double endParam)
        {
            const double tolerance = 0.004363323;
            List<Tuple<double,double>> insolationSegments = new List<Tuple<double,double>>();
            double lastSegmentEnd = startParam;
            foreach (var segment in shadows)
            {
                if (segment.Item2 > endParam) break;

                double ds = segment.Item1 - lastSegmentEnd;
                if (ds > tolerance)
                    insolationSegments.Add(new Tuple<double, double>(lastSegmentEnd, segment.Item1));

                lastSegmentEnd = segment.Item2;
            }

            double lastDs = endParam - lastSegmentEnd;
            if (lastDs > tolerance)
                insolationSegments.Add(new Tuple<double, double>(lastSegmentEnd, endParam));

            return insolationSegments;
        }

        static TimeSpan ConvertPeriodToTime(Tuple<double, double> period)
        {
            return TimeSpan.FromSeconds((period.Item2 - period.Item1)/Utils.InsolationCalculationUtils.RadianSecondsRatio);
        }

        static List<Tuple<double, double>> DistinctSegments(List<Tuple<double, double>> segments)
        {
            var ordered = segments.OrderBy(x => x.Item1).ToList();
            List<Tuple<double, double>> distinctSegments = new List<Tuple<double, double>> { ordered.First() };

            for (int i = 1; i < ordered.Count; i++)
            {
                var currentSegment = ordered[i];
                var lastDistinct = distinctSegments.Last();
                if (SegmentsIntersect(lastDistinct, currentSegment))
                {
                    currentSegment = SegmentsUnion(lastDistinct, currentSegment);
                    distinctSegments[distinctSegments.Count - 1] = currentSegment;
                }
                else distinctSegments.Add(currentSegment);
            }

            return distinctSegments;
        }

        static Tuple<double, double> SegmentsUnion(Tuple<double,double> segment1, Tuple<double, double> segment2)
        {
            return new Tuple<double, double>(Math.Min(segment1.Item1, segment2.Item1), Math.Max(segment1.Item2, segment2.Item2));
        }

        static bool SegmentsIntersect(Tuple<double, double> segment1, Tuple<double, double> segment2)
        {
            if (segment1.Item1 >= segment2.Item1 && segment1.Item1 <= segment2.Item2 ||
                segment2.Item1 >= segment1.Item1 && segment2.Item1 <= segment1.Item2)
                return true;

            return false;
        }
    }
}
