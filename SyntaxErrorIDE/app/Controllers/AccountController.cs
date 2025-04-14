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
            return Redirect("/");
        }
        
        ViewBag.Message = "Login failed";
        return View("login");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost]
    public IActionResult Register([FromForm] string email, [FromForm] string name, [FromForm] string password, [FromForm] string passwordRepeat)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordRepeat))
        {
            ViewBag.Message = "Please fill in all fields";
            return View();
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            ViewBag.Message = "Email is not valid";
            return View();
        }

        var result = _loginService.Register(name, email, password, passwordRepeat);

        if (result == "User registered successfully")
        {
            return Redirect("/");
        }

        ViewBag.Message = result;
        return View();
    }
}