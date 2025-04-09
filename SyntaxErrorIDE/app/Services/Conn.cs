using DotNetEnv;
using MySql.Data.MySqlClient;

namespace SyntaxErrorIDE.app.Services
{
    public class Conn
    {
        private static readonly string ConnectionString;

        static Conn()
        {
            var host = Env.GetString("DB_HOST");
            var name = Env.GetString("DB_NAME");
            var user = Env.GetString("DB_USER");
            var pass = Env.GetString("DB_PASS");

            ConnectionString = $"Server={host};Database={name};Uid={user};Pwd={pass}";
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static MySqlDataReader GetReader(string query)
        {
            var con = GetConnection();
            con.Open();
            var cmd = new MySqlCommand(query, con);
            return cmd.ExecuteReader();
        }
    }
}