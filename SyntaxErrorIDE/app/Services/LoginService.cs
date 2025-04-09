using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SyntaxErrorIDE.app.Models;

namespace SyntaxErrorIDE.app.Services;

public class LoginService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public bool Login(string email, string password)
    {
        var users = User.GetAllUsers();
        foreach (var user in users)
        {
            if (user.email != email) continue;
            var reader = Conn.GetReader($"SELECT * FROM users WHERE email = '{email}'");
            while (reader.Read())
            {
                var savedPasswordHash = reader.GetString(reader.GetOrdinal("password"));
                if (!Password.Verify(password, savedPasswordHash)) continue;
                
                var userId = reader.GetInt32(reader.GetOrdinal("id"));
                _httpContextAccessor.HttpContext?.Session.SetInt32("UserId", userId);
                _httpContextAccessor.HttpContext?.Session.SetString("UserEmail", email);
                
                reader.Close();
                return true;
            }
        }

        return false;
    }
    
    public string Register(string? email, string? password, string? secondPassword)
    {
        if (string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || 
            string.IsNullOrEmpty(secondPassword))
        {
            return "Please fill in all fields";
        }
        
        if (new EmailAddressAttribute().IsValid(email)) return "Email is not valid";
        
        var reader = Conn.GetReader($"SELECT * FROM users WHERE email = '{email}'");
        while (reader.Read())
        {
            if (reader.GetString(reader.GetOrdinal("email")) != email) continue;
            reader.Close();
            return "Email already exists";
        }
        
        if (password != secondPassword)
        {
            reader.Close();
            return "Passwords do not match";
        }
        
        var hashedPassword = Password.Hash(password);
        reader = Conn.GetReader($"INSERT INTO users (email, password) VALUES ('{email}', '{hashedPassword}')");
        reader.Close();
        
        Login(email, password);
        return "User registered successfully";
    }
}