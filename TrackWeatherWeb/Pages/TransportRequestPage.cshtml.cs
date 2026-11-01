using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    [Authorize(Roles = Roles.Admin)]
    public class TransportRequestPageModel(ITransportRepo transport) : PageModel
    {
        // List of requests
        public List<RequestTransportDTO> Requests { get; set; } = [];

        public async Task OnGetAsync()
        {
            Requests = await transport.GetTransportRequestsAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var response = await transport.DeleteTransportRequestAsync(id);
            TempData[response.Flag ? "Success" : "Error"] = response.Message;
            return RedirectToPage();
        }
    }
}
