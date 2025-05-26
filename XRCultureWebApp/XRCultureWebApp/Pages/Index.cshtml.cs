using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace XRCultureWebApp.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;
        public IndexModel(ILogger<LoginModel> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet()
        {
        }
    }
}