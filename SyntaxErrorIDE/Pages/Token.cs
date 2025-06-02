namespace SyntaxErrorIDE.Pages
{
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
}
