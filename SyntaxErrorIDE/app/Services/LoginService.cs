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
        var reader = Conn.GetReader("SELECT id, password FROM users WHERE name = @name", new MySqlParameter("@name", name));

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

        reader.Close();
        return false;
    }

    
    public string Register(string? name,string? email, string? password, string? passwordRepeat)
    {
        if (string.IsNullOrEmpty(name) ||
            string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || 
            string.IsNullOrEmpty(passwordRepeat)) 
            return "Please fill in all fields";
        
        if (!new EmailAddressAttribute().IsValid(email)) return "Email is not valid";
        
        var emailReader = Conn.GetReader("SELECT * FROM users WHERE email = @email", new MySqlParameter("@email", email));
        while (emailReader.Read())
        {
            if (emailReader.GetString(emailReader.GetOrdinal("email")) != email) continue;
            emailReader.Close();
            return "There is already an account with this email";
        }
        emailReader.Close();
        
        var nameReader = Conn.GetReader("SELECT * FROM users WHERE name = @name", new MySqlParameter("@name", name));
        while (nameReader.Read())
        {
            if (nameReader.GetString(nameReader.GetOrdinal("name")) != name) continue;
            nameReader.Close();
            return "There is already an account with this name";
        }
        
        if (password != passwordRepeat)
        {
            nameReader.Close();
            return "Passwords do not match";
        }
        
        var hashedPassword = Password.Hash(password);
        nameReader = Conn.GetReader("INSERT INTO users (name, email, password) VALUES (@name, @email, @password)", 
            new MySqlParameter("@name", name), 
            new MySqlParameter("@email", email), 
            new MySqlParameter("@password", hashedPassword));
        nameReader.Close();
        
        Login(email, password);
        return "User registered successfully";
    }
}