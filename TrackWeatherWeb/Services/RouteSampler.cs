using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Services
{
    public sealed record RouteSample(double Lat, double Lon, double DistanceKm, double Seconds, int StartIndex, int EndIndex);

    public sealed record SampledRoute(double TotalKm, IReadOnlyList<RouteSample> Samples);

    // Splits a route into equal stretches and picks the middle of each one as the place to check the forecast
    public static class RouteSampler
    {
        public const int MaxSamples = 20;
        public const double MinStretchKm = 30;

        private const double EarthRadiusKm = 6371;

        public static SampledRoute Sample(IReadOnlyList<RoutePointDTO> points)
        {
            var cumulative = new double[points.Count];
            for (var i = 1; i < points.Count; i++)
            {
                cumulative[i] = cumulative[i - 1] + DistanceKm(points[i - 1], points[i]);
            }

            var totalKm = cumulative[^1];
            var stretchKm = Math.Max(MinStretchKm, totalKm / MaxSamples);
            var count = Math.Max(1, (int)Math.Ceiling(totalKm / stretchKm));
            var length = totalKm / count;

            var samples = new List<RouteSample>(count);
            for (var k = 0; k < count; k++)
            {
                var (lat, lon, seconds) = Interpolate(points, cumulative, (k + 0.5) * length);
                samples.Add(new RouteSample(
                    lat,
                    lon,
                    (k + 0.5) * length,
                    seconds,
                    k == 0 ? 0 : LastIndexAtOrBefore(cumulative, k * length),
                    k == count - 1 ? points.Count - 1 : FirstIndexAtOrAfter(cumulative, (k + 1) * length)));
            }

            return new SampledRoute(totalKm, samples);
        }

        public static double DistanceKm(RoutePointDTO a, RoutePointDTO b)
        {
            var dLat = ToRadians(b.Lat - a.Lat);
            var dLon = ToRadians(b.Lon - a.Lon);
            var h = Math.Pow(Math.Sin(dLat / 2), 2)
                + Math.Cos(ToRadians(a.Lat)) * Math.Cos(ToRadians(b.Lat)) * Math.Pow(Math.Sin(dLon / 2), 2);
            return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        private static (double Lat, double Lon, double Seconds) Interpolate(IReadOnlyList<RoutePointDTO> points, double[] cumulative, double distance)
        {
            var end = FirstIndexAtOrAfter(cumulative, distance);
            if (end == 0)
            {
                return (points[0].Lat, points[0].Lon, points[0].Seconds);
            }

            var (a, b) = (points[end - 1], points[end]);
            var span = cumulative[end] - cumulative[end - 1];
            var t = span <= 0 ? 0 : (distance - cumulative[end - 1]) / span;
            return (a.Lat + (b.Lat - a.Lat) * t, a.Lon + (b.Lon - a.Lon) * t, a.Seconds + (b.Seconds - a.Seconds) * t);
        }

        private static int FirstIndexAtOrAfter(double[] cumulative, double distance)
        {
            var index = Array.BinarySearch(cumulative, distance);
            return Math.Min(index >= 0 ? index : ~index, cumulative.Length - 1);
        }

        private static int LastIndexAtOrBefore(double[] cumulative, double distance)
        {
            var index = Array.BinarySearch(cumulative, distance);
            return Math.Max(index >= 0 ? index : ~index - 1, 0);
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
