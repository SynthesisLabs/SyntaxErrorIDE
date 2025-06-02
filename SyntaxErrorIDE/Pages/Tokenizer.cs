using System.Text.RegularExpressions;

namespace SyntaxErrorIDE.Pages
{
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
