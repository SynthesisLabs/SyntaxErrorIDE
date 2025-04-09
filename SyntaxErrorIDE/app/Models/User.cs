using System.Collections.Generic;
using SyntaxErrorIDE.app.Services;

namespace SyntaxErrorIDE.app.Models;

public class User
{
    public string name { get; set; }
    public int id { get; set; }
    public string? email { get; set; }

    User(int id)
    {
        var reader = Conn.GetReader($"SELECT * FROM users WHERE id = {id}");
        while (reader.Read())
        {
            this.id = id;
            email = reader.GetString(reader.GetOrdinal("email"));
            name = reader.GetString(reader.GetOrdinal("name"));
        }
        reader.Close();
    }

    public static User Get(int id)
    {
        return new User(id);
    }

    public static List<User> GetAllUsers()
    {
        var reader = Conn.GetReader("SELECT * FROM users");
        var users = new List<User>();
        while (reader.Read())
        {
            users.Add(new User(reader.GetInt32(reader.GetOrdinal("id"))));
        }
        reader.Close();
        return users;
    }
}