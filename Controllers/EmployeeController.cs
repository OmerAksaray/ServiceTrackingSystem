using Microsoft.AspNetCore.Mvc;

namespace ServiceTrackingSystem.Controllers
{
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
