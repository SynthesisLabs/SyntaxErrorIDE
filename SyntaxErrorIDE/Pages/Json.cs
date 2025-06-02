using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SyntaxErrorIDE.Pages
{
    public class Json
    {
        public static List<string> Keywords = new List<string>();
        public static List<string> Identifiers = new List<string>();
        public static List<string> PublicityIdentifiers = new List<string>();
        public static List<List<string>> ReadLanguageData()
        {
            string jsonString = GetJsonFile();
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;


            foreach (var item in root.GetProperty("keywords").EnumerateArray())
            {
                Keywords.Add(item.GetString());
            }

            foreach (var item in root.GetProperty("identifiers").EnumerateArray())
            {
                Identifiers.Add(item.GetString());
            }

            var publicityIdentifiers = new List<string>();
            foreach (var item in root.GetProperty("publicity-identifiers").EnumerateArray())
            {
                PublicityIdentifiers.Add(item.GetString());
            }

            return new List<List<string>>()
        {
            Keywords,
            Identifiers,
            PublicityIdentifiers
        };
        }


        private static string GetJsonFile()
        {
            try
            {
                return File.ReadAllText(Path.Combine(rawModel._env.WebRootPath, "js/c#.json"));
            }
            catch (Exception e)
            {
                File.CreateText(Path.Combine(rawModel._env.WebRootPath, "js/c#.json"));
                Console.WriteLine(e);
                throw;
            }
        }

        public static void WriteJson(List<TokenTypes> tokenTypes, List<string> code)
        {
            Dictionary<string, string> tokenTypesDictionary = new Dictionary<string, string>();
            for (int i = 0; i < Math.Min(tokenTypes.Count, code.Count); i++)
            {
                string key = tokenTypes[i].ToString();
                string value = code[i];
                int counter = 1;
                string originalKey = key;
                while (tokenTypesDictionary.ContainsKey(key))
                {
                    key = $"{originalKey}_{counter}";
                    counter++;
                }
                tokenTypesDictionary[key] = value;

            }
            string jsonString = JsonSerializer.Serialize(tokenTypesDictionary, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(Path.Combine(rawModel._env.WebRootPath, "js/data.json"), jsonString);
            Console.WriteLine("Sucessfuly writed to json");
        }
    }
}
