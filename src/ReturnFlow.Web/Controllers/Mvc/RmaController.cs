using Microsoft.AspNetCore.Mvc;

namespace ReturnFlow.Web.Controllers.Mvc;

[Route("Rma")]
public class RmaController : Controller
{
    [HttpGet("")]
    [HttpGet("Submit")]
    public IActionResult Submit()
    {
        return View();
    }
}
