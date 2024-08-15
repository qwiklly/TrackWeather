namespace TrackWeatherWeb.Models
{
    public class TransportRequests
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public decimal? Coordinate_x { get; set; }
        public decimal? Coordinate_y { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

    }
}
