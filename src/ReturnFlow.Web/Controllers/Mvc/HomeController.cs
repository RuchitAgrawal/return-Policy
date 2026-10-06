using Microsoft.AspNetCore.Mvc;

namespace ReturnFlow.Web.Controllers.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Submit", "Rma");
    }
}
