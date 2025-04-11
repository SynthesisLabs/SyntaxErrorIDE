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
    public IActionResult Login([FromForm] string name, [FromForm] string password)
    {
        var sucess = _loginService.Login(name, password);

        if (sucess)
        {
            return Redirect("/");
        }
        
        ViewBag.Message = "Login failed";
        return View("login");
    }
}