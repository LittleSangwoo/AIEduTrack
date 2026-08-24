using Microsoft.AspNetCore.Mvc;

namespace AIEduTrack.Controllers
{
    public class HomeController : Controller
    {
        // Только экран выбора роли. Логика конструктора/загрузки переехала в MethodistController.
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}