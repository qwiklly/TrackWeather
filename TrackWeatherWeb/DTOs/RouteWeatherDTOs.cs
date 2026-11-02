using System.ComponentModel.DataAnnotations;
using TrackWeatherWeb.Services;

namespace TrackWeatherWeb.DTOs
{
    public class RoutePointDTO
    {
        [Range(-90, 90)]
        public double Lat { get; set; }

        [Range(-180, 180)]
        public double Lon { get; set; }

        // Planned travel time from the start of the route to this point
        [Range(0, 10_000_000)]
        public double Seconds { get; set; }
    }

    public class RouteWeatherRequestDTO
    {
        [Required, Length(2, 5000)]
        public List<RoutePointDTO> Points { get; set; } = [];

        // Departure time; empty or in the past means "now"
        public DateTime? Departure { get; set; }
    }

    public class RouteHazardDTO
    {
        public Hazard Code { get; set; }
        public RiskLevel Level { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class RouteWeatherPointDTO
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double DistanceKm { get; set; }
        public DateTime EtaUtc { get; set; }
        // The stretch of the submitted points this forecast describes
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public double? Temp { get; set; }
        public string? Summary { get; set; }
        public RiskLevel Risk { get; set; }
        public List<RouteHazardDTO> Hazards { get; set; } = [];
    }

    public class RouteWeatherDTO
    {
        public RiskLevel OverallRisk { get; set; }
        public DateTime DepartureUtc { get; set; }
        public double TotalKm { get; set; }
        public List<RouteWeatherPointDTO> Points { get; set; } = [];
    }
}
