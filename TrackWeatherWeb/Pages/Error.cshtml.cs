using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace TrackWeatherWeb.Pages
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int? Code { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId) && Code is null or >= 500;

        public void OnGet() => Prepare();

        // A failed POST is re-executed here with its original method
        public void OnPost() => Prepare();

        private void Prepare()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            if (Code is >= 400 and < 600)
            {
                Response.StatusCode = Code.Value;
            }
        }
    }
}
