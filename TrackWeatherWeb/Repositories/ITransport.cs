using TrackWeatherWeb.DTOs;
using static TrackWeatherWeb.Responses.CustomResponses;
using TrackWeatherWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace TrackWeatherWeb.Repositories
{
    public interface ITransport
    {
        Task<IEnumerable<RequestTransportDTO>> GetTransportRequestsAsync();
        Task<TransportRequests?> GetTransportRequestAsync(int id);
        Task<BaseResponse> RequestTransportAsync(RequestTransportDTO model);
        Task<BaseResponse> DeleteTransportRequestAsync(DeleteTransportRequestDTO model);
        Task<ActionResult<BaseResponse>> UpdateTransportRequestAsync(int id, RequestTransportDTO model);
    }
}
