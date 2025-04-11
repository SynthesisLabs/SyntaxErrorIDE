using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using SyntaxErrorIDE.app.Models;

namespace SyntaxErrorIDE.app.Services;

public class LoginService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public bool Login(string name, string password)
    {
        var users = User.GetAllUsers();
        foreach (var user in users)
        {
            if (user.name != name) continue;
            var reader = Conn.GetReader("SELECT * FROM users WHERE name = @name", new MySqlParameter("@name", name));
            while (reader.Read())
            {
                var savedPasswordHash = reader.GetString(reader.GetOrdinal("password"));
                if (!Password.Verify(password, savedPasswordHash)) continue;
                
                var userId = reader.GetInt32(reader.GetOrdinal("id"));
                _httpContextAccessor.HttpContext?.Session.SetInt32("UserId", userId);
                _httpContextAccessor.HttpContext?.Session.SetString("UserName", name);
                _httpContextAccessor.HttpContext?.Session.SetString("is_logged", "true");
                
                reader.Close();
                return true;
            }
        }

        return false;
    }
    
    public string Register(string? name,string? email, string? password, string? secondPassword)
    {
        if (string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || 
            string.IsNullOrEmpty(secondPassword))
        {
            return "Please fill in all fields";
        }
        
        if (new EmailAddressAttribute().IsValid(email)) return "Email is not valid";
        
        var reader = Conn.GetReader($"SELECT * FROM users WHERE email = @email", new MySqlParameter("@email", email));
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
        reader = Conn.GetReader("INSERT INTO users (name, email, password) VALUES (@name, @name, @password)", 
            new MySqlParameter("@name", name), 
            new MySqlParameter("@name", email), 
            new MySqlParameter("@password", hashedPassword));
        reader.Close();
        
        Login(email, password);
        return "User registered successfully";
    }
}