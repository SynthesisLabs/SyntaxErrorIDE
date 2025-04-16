using Microsoft.AspNetCore.Mvc;
using SyntaxErrorIDE.app.Services;
using System.ComponentModel.DataAnnotations;

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
        var successLogin = _loginService.Login(name, password);

        if (successLogin)
        {
            TempData["Message"] = "Login successful";
            return Redirect("/");
        }
        
        TempData["Message"] = "Login failed";
        return View("login");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return RedirectToPage("/Register");
    }

    [HttpPost]
    public IActionResult Register([FromForm] string name, [FromForm] string email, [FromForm] string password, [FromForm] string passwordRepeat)
    {
        var result = _loginService.Register(name, email, password, passwordRepeat);

        if (result == "User registered successfully")
        {
            return Redirect("/");
        }

        TempData["Message"] = result;
        return RedirectToPage("/Register");
    }
}