using Microsoft.AspNetCore.Mvc;
using TaskUp.ViewModels;

namespace TaskUp.Controllers;

public class DashboardController : Controller
{
    public IActionResult Access()
    {
        var model = new DashboardAccessVm();
        return View(model);
    }
    
   
}