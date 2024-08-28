using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.DTOs;

namespace TrackWeatherWeb.Pages
{
    public class TransportRequestPageModel : PageModel
    {
        private readonly Repositories.ITransport _transport;

        // List of requests
        public List<RequestTransportDTO> Requests { get; set; } = new List<RequestTransportDTO>();
        public DeleteTransportRequestDTO Delete = new();

        public TransportRequestPageModel(Repositories.ITransport transport)
        {
            _transport = transport;
        }

        public async Task OnGetAsync()
        {
            TempData["SuccessMessage"] = null;
            TempData["ErrorMessage"] = null;
            Requests = await _transport.GetTransportRequestsAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Id is required.");
            }

            Delete.Id = id;

            var response = await _transport.DeleteTransportRequestAsync(Delete);
            if (response.Flag)
            {

                return RedirectToPage();
            }
            else
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Не удалось удалить запрос.");
                return Page();
            }
        }
    }
}
