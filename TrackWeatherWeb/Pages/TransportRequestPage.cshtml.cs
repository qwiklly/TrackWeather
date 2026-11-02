using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TrackWeatherWeb.Auth;
using TrackWeatherWeb.DTOs;
using TrackWeatherWeb.Models;
using TrackWeatherWeb.Repositories.Interfaces;

namespace TrackWeatherWeb.Pages
{
    [Authorize(Roles = Roles.Admin)]
    public class TransportRequestPageModel(ITransportRepo transport, IAccountRepo accounts) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public TransportStatus? Status { get; set; }

        // List of requests
        public List<RequestTransportDTO> Requests { get; set; } = [];

        public List<GetUsersDTO> Assignees { get; set; } = [];

        public async Task OnGetAsync() => await LoadAsync();

        // The table alone: the page refreshes it when a live update arrives
        public async Task<IActionResult> OnGetTableAsync()
        {
            await LoadAsync();
            return Partial("_TransportTable", this);
        }

        public async Task<IActionResult> OnPostAssignAsync(int id, string email) =>
            Finish(await transport.AssignAsync(id, email ?? string.Empty, User.ToTransportActor()));

        public async Task<IActionResult> OnPostCompleteAsync(int id) =>
            Finish(await transport.CompleteAsync(id, User.ToTransportActor()));

        public async Task<IActionResult> OnPostCancelAsync(int id) =>
            Finish(await transport.CancelAsync(id, User.ToTransportActor()));

        public async Task<IActionResult> OnPostDeleteAsync(int id) =>
            Finish(await transport.DeleteTransportRequestAsync(id));

        private async Task LoadAsync()
        {
            Requests = await transport.GetTransportRequestsAsync(Status);
            Assignees = await accounts.GetUsersAsync();
        }

        private RedirectToPageResult Finish(Responses.CustomResponses.BaseResponse response)
        {
            TempData[response.Flag ? "Success" : "Error"] = response.Message;
            return RedirectToPage(new { status = Status });
        }
    }
}
