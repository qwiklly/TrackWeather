using TrackWeatherWeb.Services;
using TrackWeatherWeb.WeatherModels;

namespace UnitTests
{
	public class RouteRiskEvaluatorTests
	{
		private static ForecastItem Item(
			double temp = 15, int humidity = 50, int conditionId = 800, double? rain = null, double? snow = null,
			double wind = 2, double gust = 3, int visibility = 10000, double pop = 0.9) => new()
		{
			Main = new ForecastMain { Temp = temp, Humidity = humidity },
			Weather = [new WeatherDescription { Id = conditionId }],
			Rain = rain == null ? null : new ForecastPrecipitation { ThreeHours = rain },
			Snow = snow == null ? null : new ForecastPrecipitation { ThreeHours = snow },
			Wind = new ForecastWind { Speed = wind, Gust = gust },
			Visibility = visibility,
			Pop = pop
		};

		private static void AssertHazard(ForecastItem item, Hazard hazard, RiskLevel level)
		{
			var assessment = RouteRiskEvaluator.Evaluate(item);
			Assert.Contains(new HazardFinding(hazard, level), assessment.Hazards);
			Assert.True(assessment.Level >= level);
		}

		[Fact]
		public void ClearWeather_HasNoHazards()
		{
			var assessment = RouteRiskEvaluator.Evaluate(Item());

			Assert.Equal(RiskLevel.Low, assessment.Level);
			Assert.Empty(assessment.Hazards);
		}

		[Fact]
		public void RainOnFrozenRoad_IsIce() => AssertHazard(Item(temp: 0, rain: 0.5, conditionId: 500), Hazard.Ice, RiskLevel.High);

		[Fact]
		public void FreezingRain_IsIce_EvenAboveZero() => AssertHazard(Item(temp: 2, conditionId: 511), Hazard.Ice, RiskLevel.High);

		[Fact]
		public void DampnessAroundZero_IsPossibleIce() => AssertHazard(Item(temp: 1, humidity: 95), Hazard.Ice, RiskLevel.Moderate);

		[Fact]
		public void DryCold_IsNotIce()
		{
			var assessment = RouteRiskEvaluator.Evaluate(Item(temp: -15, humidity: 60));

			Assert.DoesNotContain(assessment.Hazards, h => h.Hazard == Hazard.Ice);
		}

		[Fact]
		public void WarmRain_IsNotIce()
		{
			var assessment = RouteRiskEvaluator.Evaluate(Item(temp: 12, rain: 1, conditionId: 500));

			Assert.Empty(assessment.Hazards);
		}

		[Theory]
		[InlineData(0.5, RiskLevel.Moderate)]
		[InlineData(3, RiskLevel.High)]
		public void Snow_DependsOnAmount(double amount, RiskLevel level) =>
			AssertHazard(Item(temp: -5, snow: amount, conditionId: 600), Hazard.Snow, level);

		[Theory]
		[InlineData(6, RiskLevel.Moderate)]
		[InlineData(20, RiskLevel.High)]
		public void Rain_DependsOnAmount(double amount, RiskLevel level) =>
			AssertHazard(Item(rain: amount, conditionId: 501), Hazard.HeavyRain, level);

		[Theory]
		[InlineData(11, 12, RiskLevel.Moderate)]
		[InlineData(5, 18, RiskLevel.Moderate)]
		[InlineData(16, 18, RiskLevel.High)]
		[InlineData(8, 26, RiskLevel.High)]
		public void Wind_ConsidersSpeedAndGusts(double speed, double gust, RiskLevel level) =>
			AssertHazard(Item(wind: speed, gust: gust), Hazard.Wind, level);

		[Theory]
		[InlineData(800, RiskLevel.Moderate)]
		[InlineData(100, RiskLevel.High)]
		public void Visibility_DependsOnDistance(int metres, RiskLevel level) =>
			AssertHazard(Item(visibility: metres), Hazard.PoorVisibility, level);

		[Fact]
		public void Thunderstorm_IsHigh() => AssertHazard(Item(conditionId: 211), Hazard.Thunderstorm, RiskLevel.High);

		[Fact]
		public void UnlikelyPrecipitation_IsIgnored()
		{
			var assessment = RouteRiskEvaluator.Evaluate(Item(temp: -3, humidity: 50, snow: 5, conditionId: 600, pop: 0.1));

			Assert.Empty(assessment.Hazards);
		}

		[Fact]
		public void OverallLevel_IsTheWorstFinding()
		{
			var assessment = RouteRiskEvaluator.Evaluate(Item(wind: 11, gust: 12, conditionId: 211));

			Assert.Equal(RiskLevel.High, assessment.Level);
			Assert.Equal(2, assessment.Hazards.Count);
		}
	}
}
