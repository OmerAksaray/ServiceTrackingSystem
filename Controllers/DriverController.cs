using Microsoft.AspNetCore.Mvc;

namespace ServiceTrackingSystem.Controllers
{
    public class DriverController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
