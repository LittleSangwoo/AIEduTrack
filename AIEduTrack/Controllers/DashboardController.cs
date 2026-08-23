using Microsoft.AspNetCore.Mvc;

namespace AIEduTrack.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Analytics()
        {
            return View();
        }
    }
}
