using Microsoft.AspNetCore.Mvc;
using SyntaxErrorIDE.app.Services;

namespace SyntaxErrorIDE.app.Controllers;

public class AccountController : Controller
{
    private readonly LoginService _loginService;

    public AccountController(LoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    public IActionResult Login(string name, string password)
    {
        if (_loginService.Login(name, password))
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Error = "Invalid login attempt";
        return View();
    }
}