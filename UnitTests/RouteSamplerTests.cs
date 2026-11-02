using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Services;

namespace UnitTests
{
	public class RouteSamplerTests
	{
		// A straight line along a meridian: one degree of latitude is about 111 km
		private static List<RoutePointDTO> Line(double degrees, int vertices = 200, double hours = 10) =>
			Enumerable.Range(0, vertices)
				.Select(i => new RoutePointDTO
				{
					Lat = 40 + degrees * i / (vertices - 1),
					Lon = 39,
					Seconds = hours * 3600 * i / (vertices - 1)
				})
				.ToList();

		[Fact]
		public void TotalDistance_IsAccurate()
		{
			var route = RouteSampler.Sample(Line(1));

			Assert.InRange(route.TotalKm, 110, 112);
		}

		[Fact]
		public void ShortRoute_GetsOneStretch_CoveringEverything()
		{
			var points = Line(0.1);

			var sample = Assert.Single(RouteSampler.Sample(points).Samples);

			Assert.Equal(0, sample.StartIndex);
			Assert.Equal(points.Count - 1, sample.EndIndex);
		}

		[Fact]
		public void MediumRoute_IsSplitIntoStretchesOfAtLeastTheMinimum()
		{
			var route = RouteSampler.Sample(Line(3));

			Assert.InRange(route.Samples.Count, 10, 12);
			Assert.All(route.Samples, s => Assert.InRange(s.Lat, 40, 43));
		}

		[Fact]
		public void VeryLongRoute_IsCapped_SoItCannotExhaustTheApiQuota()
		{
			var route = RouteSampler.Sample(Line(40, vertices: 2000));

			Assert.Equal(RouteSampler.MaxSamples, route.Samples.Count);
		}

		[Fact]
		public void StretchesCoverTheWholeLine_WithoutGaps()
		{
			var points = Line(5);

			var samples = RouteSampler.Sample(points).Samples;

			Assert.Equal(0, samples[0].StartIndex);
			Assert.Equal(points.Count - 1, samples[^1].EndIndex);
			for (var i = 1; i < samples.Count; i++)
			{
				Assert.True(samples[i].StartIndex <= samples[i - 1].EndIndex, $"gap before stretch {i}");
				Assert.True(samples[i].Seconds > samples[i - 1].Seconds);
				Assert.True(samples[i].DistanceKm > samples[i - 1].DistanceKm);
			}
		}

		[Fact]
		public void TravelTime_IsInterpolated()
		{
			var samples = RouteSampler.Sample(Line(0.1, hours: 2)).Samples;

			Assert.InRange(samples[0].Seconds, 3500, 3700);
		}

		[Fact]
		public void RepeatedPoints_DoNotBreakSampling()
		{
			var point = new RoutePointDTO { Lat = 51, Lon = 39 };

			var sample = Assert.Single(RouteSampler.Sample([point, point, point]).Samples);

			Assert.Equal(51, sample.Lat);
			Assert.Equal(0, sample.DistanceKm);
		}
	}
}
