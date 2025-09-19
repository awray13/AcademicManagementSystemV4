using Microsoft.AspNetCore.Mvc;

namespace AcademicManagementSystemV4.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
