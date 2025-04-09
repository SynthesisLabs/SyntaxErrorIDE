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
        return Content(_loginService.Login(name, password) ? "Login successful" : "Login unsuccessful");
    }
}