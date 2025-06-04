using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SyntaxErrorIDE.Pages
{
    public class HighlightModel : PageModel
    {
        public static IWebHostEnvironment _env;
        Tokenizer tokenizer = new Tokenizer();
        public string HtmlContent { get; set; }
        [BindProperty(SupportsGet = true)]
        public string code { get; set; }

        public string Code = "";
        public HighlightModel(IWebHostEnvironment env)
        {
            _env = env;
        }
        public void OnGet()
        {
            if (Code == string.Empty)
            {
                Console.WriteLine("No code provided. Please enter some code to tokenize.");
            }

            foreach (var item in Request.Query)
            {

                if (item.Key == "code")
                {
                    Code = item.Value;
                }
            }
            Console.WriteLine("Coding: " + Code);
            tokenizer.runTokenizer(Code);
            var result = GetKeywords();
            HtmlContent = result.HtmlContent;
        }
        public void OnPost()
        {

        }

        private string GetJsonFile()
        {
            try
            {
                return System.IO.File.ReadAllText(Path.Combine(_env.WebRootPath, "js/data.json"));

            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldnt find file making it");
                System.IO.File.Create(Path.Combine(_env.WebRootPath, "js/data.json"));
                Console.WriteLine(ex);
                throw ex;
            }

        }

        private (List<string> Keywords, string HtmlContent) GetKeywords()
        {
            var keywords = new List<string>();
            var htmlBuilder = new StringBuilder();
            List<string> colors = new List<string>()
                {
                    "#d4021e",
                    "#aad402",
                    "#3f5270"
                };
            string colorToUse = "";
            try
            {
                var codeDictionary = ParseJson();
                foreach (var keyValuePair in codeDictionary)
                {
                    string baseKey = keyValuePair.Key.Split('_')[0];
                    Console.WriteLine("Basekey: " + baseKey);
                    switch (baseKey)
                    {
                        case "Keyword":
                            Console.WriteLine("Keyword");
                            colorToUse = colors[0];
                            break;
                        case "Identifier":
                            Console.WriteLine("Identifier");
                            colorToUse = colors[1];
                            break;
                        case "PublicityIdentifiers":
                            Console.WriteLine("Publicity");
                            colorToUse = colors[2];
                            break;
                        case "NewLine":
                            htmlBuilder.Append("<br>");
                            break;
                        default:
                            colorToUse = "ffff";
                            Console.WriteLine("No cases matched");
                            break;
                    }
                    htmlBuilder.Append($"<p style=\"color:{colorToUse}\">{keyValuePair.Value}</p>");
                }


            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }

            return (keywords, htmlBuilder.ToString());
        }

        public Dictionary<string, string> ParseJson()
        {
            string JsonFile = GetJsonFile();
            if (JsonFile == null)
            {
                Console.WriteLine("Cant find json file");
                return null;
            }
            Dictionary<string, string> Data = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonFile);
            return Data;

        }
    }
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
                return File.ReadAllText(Path.Combine(HighlightModel._env.WebRootPath, "js/c#.json"));
            }
            catch (Exception e)
            {
                File.CreateText(Path.Combine(HighlightModel._env.WebRootPath, "js/c#.json"));
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
            File.WriteAllText(Path.Combine(HighlightModel._env.WebRootPath, "js/data.json"), jsonString);
            Console.WriteLine("Sucessfuly writed to json");
        }
    }
    public class Token
    {
        public TokenTypes TokenType { get; }
        public string Value { get; }

        public Token(TokenTypes tokenType, string value)
        {
            this.TokenType = tokenType;
            this.Value = value;
        }
    }
    public enum TokenTypes
    {
        Keyword,
        Identifier,
        Integers,
        Operator,
        String,
        Comment,
        Whitespace,
        Unknown,
        Floats,
        Getter,
        Setter,
        Char,
        Boolean,
        NotSet,
        PublicityIdentifiers,
        NewLine,
    }
    public class Tokenizer
    {
        public static List<List<string>> Tokens = Json.ReadLanguageData();
        private static readonly List<string> Keywords = Tokens[0];
        private static readonly List<string> Identifiers = Tokens[1];
        private static readonly List<string> PublicityIndentifiers = Tokens[2];

        private static readonly Regex TokenRegex = new Regex(
            @"
        (\b\d+\b)|              # Matches integers (BigInteger)
        (\b(?:true|false)\b)|   # Matches Boolean values
        (\bchar\b)|             # Matches 'char' keyword
        (\b(?:get|set)\b)|      # Matches 'getter' and 'setter'
        (\b(?:decimal|double|BigInteger)\b)|  # Matches specific data types
        (\b\w+\b)|              # Matches identifiers/keywords
        ([+\-*/=<>])|           # Matches operators
        ([""'].*?[""'])|        # Matches strings
        (//.*)|                 # Matches comments
        (\s+)",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var matches = TokenRegex.Matches(input);

            foreach (Match match in matches)
            {
                string value = match.Value;
                TokenTypes type = TokenTypes.Unknown;
                if (Keywords.Contains(value))
                {
                    type = TokenTypes.Keyword;
                    tokens.Add(new Token(type, value));
                    continue;
                }

                if (Identifiers.Contains(value))
                {
                    type = TokenTypes.Identifier;
                    tokens.Add(new Token(type, value));
                    continue;
                }

                if (PublicityIndentifiers.Contains(value))
                {
                    type = TokenTypes.PublicityIdentifiers;
                    tokens.Add(new Token(type, value));
                    continue;
                }
                switch (value)
                {
                    case var v when Regex.IsMatch(v, @"^\r?\n$"):
                        type = TokenTypes.NewLine;
                        break;
                    case var v when Regex.IsMatch(v, @"^[+\-*/=<>]$"):
                        type = TokenTypes.Operator;
                        break;

                    case var v when Regex.IsMatch(v, @"^[""'].*[""']$"):
                        type = TokenTypes.String;
                        break;

                    case var v when Regex.IsMatch(v, @"^//.*$"):
                        type = TokenTypes.Comment;
                        break;

                    case var v when Regex.IsMatch(v, @"^\s+$"):
                        type = TokenTypes.Whitespace;
                        break;

                    case var v when Regex.IsMatch(v, @"(\b\d+\b)"):
                        type = TokenTypes.Integers;
                        break;
                    case var v when Regex.IsMatch(v, @":decimal|double"):
                        type = TokenTypes.Floats;
                        break;
                    case var v when Regex.IsMatch(v, @":true|false"):
                        type = TokenTypes.Boolean;
                        break;
                    case var v when Regex.IsMatch(v, @"\b function\s+get[A-Z]\w*\s*\("):
                        type = TokenTypes.Getter;
                        break;

                    case var v when Regex.IsMatch(v, @"\b function\s+set[A-Z]\w*\s*\("):
                        type = TokenTypes.Setter;
                        break;
                    default:
                        type = TokenTypes.Unknown;
                        break;
                }
                tokens.Add(new Token(type, value));
            }
            return tokens;
        }
        public void runTokenizer(string code)
        {
            var tokenizer = new Tokenizer();
            Console.WriteLine("Code:" + code);
            List<Token> tokens = tokenizer.Tokenize(code);
            foreach (Token token in tokens)
            {
                Console.WriteLine(token.TokenType + " value: " + token.Value);
            }
            List<TokenTypes> types = new List<TokenTypes>();
            List<string> codeList = new List<string>();
            foreach (Token token in tokens)
            {
                types.Add(token.TokenType);
                codeList.Add(token.Value);
            }
            Json.WriteJson(types, codeList);
        }
    }
}
