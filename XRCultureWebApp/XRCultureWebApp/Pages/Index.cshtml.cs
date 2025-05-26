using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace XRCultureWebApp.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        private readonly IOperationSingleton _singletonOperation;
        public IndexModel(ILogger<LoginModel> logger, IOperationSingleton singletonOperation)
        {
            _logger = logger;
            _singletonOperation = singletonOperation;
        }

        public void OnGet()
        {
        }

        public string OperationId => _singletonOperation.OperationId.ToString("N");
        public bool Started => _singletonOperation.Started;
    }
}