using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    public class MyRequestsModel(ITransportRepo transport) : PageModel
    {
        public List<RequestTransportDTO> Requests { get; set; } = [];

        public async Task OnGetAsync() => await LoadAsync();

        // The table alone: the page refreshes it when a live update arrives
        public async Task<IActionResult> OnGetTableAsync()
        {
            await LoadAsync();
            return Partial("_MyRequestsTable", this);
        }

        public async Task<IActionResult> OnPostCompleteAsync(int id) =>
            Finish(await transport.CompleteAsync(id, User.ToTransportActor()));

        public async Task<IActionResult> OnPostCancelAsync(int id) =>
            Finish(await transport.CancelAsync(id, User.ToTransportActor()));

        private async Task LoadAsync() =>
            Requests = await transport.GetRequestsForUserAsync(User.ToTransportActor().Email);

        private RedirectToPageResult Finish(Responses.CustomResponses.BaseResponse response)
        {
            TempData[response.Flag ? "Success" : "Error"] = response.Message;
            return RedirectToPage();
        }
    }
}
