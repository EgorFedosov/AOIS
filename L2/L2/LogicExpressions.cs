using System.Text;

namespace L2;

public abstract class BoolExpr
{
    public abstract bool Evaluate(Func<char, bool> variableResolver);
    public abstract void CollectVariables(ISet<char> variables);
}

public sealed class VariableExpr(char name) : BoolExpr
{
    private char Name { get; } = name;

    public override bool Evaluate(Func<char, bool> variableResolver) => variableResolver(Name);

    public override void CollectVariables(ISet<char> variables) => variables.Add(Name);
}

public sealed class NotExpr(BoolExpr operand) : BoolExpr
{
    private BoolExpr Operand { get; } = operand;

    public override bool Evaluate(Func<char, bool> variableResolver) => !Operand.Evaluate(variableResolver);

    public override void CollectVariables(ISet<char> variables) => Operand.CollectVariables(variables);
}

public sealed class AndExpr(BoolExpr left, BoolExpr right) : BoolExpr
{
    private BoolExpr Left { get; } = left;
    private BoolExpr Right { get; } = right;

    public override bool Evaluate(Func<char, bool> variableResolver) =>
        Left.Evaluate(variableResolver) && Right.Evaluate(variableResolver);

    public override void CollectVariables(ISet<char> variables)
    {
        Left.CollectVariables(variables);
        Right.CollectVariables(variables);
    }
}

public sealed class OrExpr(BoolExpr left, BoolExpr right) : BoolExpr
{
    private BoolExpr Left { get; } = left;
    private BoolExpr Right { get; } = right;

    public override bool Evaluate(Func<char, bool> variableResolver) =>
        Left.Evaluate(variableResolver) || Right.Evaluate(variableResolver);

    public override void CollectVariables(ISet<char> variables)
    {
        Left.CollectVariables(variables);
        Right.CollectVariables(variables);
    }
}

public sealed class ImpliesExpr(BoolExpr left, BoolExpr right) : BoolExpr
{
    private BoolExpr Left { get; } = left;
    private BoolExpr Right { get; } = right;

    public override bool Evaluate(Func<char, bool> variableResolver) =>
        !Left.Evaluate(variableResolver) || Right.Evaluate(variableResolver);

    public override void CollectVariables(ISet<char> variables)
    {
        Left.CollectVariables(variables);
        Right.CollectVariables(variables);
    }
}

public sealed class EquivalentExpr(BoolExpr left, BoolExpr right) : BoolExpr
{
    private BoolExpr Left { get; } = left;
    private BoolExpr Right { get; } = right;

    public override bool Evaluate(Func<char, bool> variableResolver) =>
        Left.Evaluate(variableResolver) == Right.Evaluate(variableResolver);

    public override void CollectVariables(ISet<char> variables)
    {
        Left.CollectVariables(variables);
        Right.CollectVariables(variables);
    }
}

public static class BooleanExpressionParser
{
    private const string AllowedVariables = "abcde";

    public static BoolExpr Parse(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Expression must not be empty.", nameof(source));
        }

        var normalized = Normalize(source);
        var tokenizer = new Tokenizer(normalized);
        var parser = new Parser(tokenizer.Tokenize());
        var expression = parser.ParseExpression();
        parser.EnsureEnd();
        return expression;
    }
    private static string Normalize(string input)
    {
        var compact = new string(input.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
        compact = compact
            .Replace("В¬", "!")
            .Replace("в€§", "&")
            .Replace("в€Ё", "|")
            .Replace("в†’", "->")
            .Replace("в†”", "~");

        var builder = new StringBuilder(compact.Length * 2);
        foreach (var ch in compact)
        {
            switch (ch)
            {
                case '\u00AC':
                    builder.Append('!');
                    break;
                case '\u2227':
                    builder.Append('&');
                    break;
                case '\u2228':
                    builder.Append('|');
                    break;
                case '\u2192':
                    builder.Append("->");
                    break;
                case '\u2194':
                    builder.Append('~');
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    private enum TokenType
    {
        Variable,
        Not,
        And,
        Or,
        Implies,
        Equivalent,
        LeftParenthesis,
        RightParenthesis,
        End
    }

    private readonly record struct Token(TokenType Type, char Variable = '\0');

    private sealed class Tokenizer(string input)
    {
        public IReadOnlyList<Token> Tokenize()
        {
            var tokens = new List<Token>();
            for (var i = 0; i < input.Length; i++)
            {
                var current = input[i];
                if (char.IsLetter(current))
                {
                    var variable = char.ToLowerInvariant(current);
                    if (!AllowedVariables.Contains(variable))
                    {
                        throw new ArgumentException($"Unsupported variable '{current}'. Allowed: {AllowedVariables}");
                    }

                    tokens.Add(new Token(TokenType.Variable, variable));
                    continue;
                }

                switch (current)
                {
                    case '!':
                        tokens.Add(new Token(TokenType.Not));
                        break;
                    case '&':
                        tokens.Add(new Token(TokenType.And));
                        break;
                    case '|':
                        tokens.Add(new Token(TokenType.Or));
                        break;
                    case '~':
                        tokens.Add(new Token(TokenType.Equivalent));
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParenthesis));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RightParenthesis));
                        break;
                    case '-' when i + 1 < input.Length && input[i + 1] == '>':
                        tokens.Add(new Token(TokenType.Implies));
                        i++;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported token near '{current}'.");
                }
            }

            tokens.Add(new Token(TokenType.End));
            return tokens;
        }
    }

    private sealed class Parser(IReadOnlyList<Token> tokens)
    {
        private int _position;

        public BoolExpr ParseExpression() => ParseEquivalent();

        public void EnsureEnd()
        {
            if (!Is(TokenType.End))
            {
                throw new ArgumentException("Unexpected trailing tokens.");
            }
        }

        private BoolExpr ParseEquivalent()
        {
            var left = ParseImplies();
            while (Match(TokenType.Equivalent))
            {
                var right = ParseImplies();
                left = new EquivalentExpr(left, right);
            }

            return left;
        }

        private BoolExpr ParseImplies()
        {
            var left = ParseOr();
            if (!Match(TokenType.Implies))
            {
                return left;
            }

            var right = ParseImplies();
            return new ImpliesExpr(left, right);
        }

        private BoolExpr ParseOr()
        {
            var left = ParseAnd();
            while (Match(TokenType.Or))
            {
                var right = ParseAnd();
                left = new OrExpr(left, right);
            }

            return left;
        }

        private BoolExpr ParseAnd()
        {
            var left = ParseUnary();
            while (Match(TokenType.And))
            {
                var right = ParseUnary();
                left = new AndExpr(left, right);
            }

            return left;
        }

        private BoolExpr ParseUnary()
        {
            return Match(TokenType.Not) ? new NotExpr(ParseUnary()) : ParsePrimary();
        }

        private BoolExpr ParsePrimary()
        {
            if (Match(TokenType.Variable, out var variable))
            {
                return new VariableExpr(variable);
            }

            if (!Match(TokenType.LeftParenthesis))
            {
                throw new ArgumentException("Expected variable, negation, or opening parenthesis.");
            }

            var inner = ParseExpression();
            return !Match(TokenType.RightParenthesis)
                ? throw new ArgumentException("Closing parenthesis is missing.")
                : inner;
        }

        private bool Is(TokenType tokenType) => tokens[_position].Type == tokenType;

        private bool Match(TokenType tokenType)
        {
            if (!Is(tokenType))
            {
                return false;
            }

            _position++;
            return true;
        }

        private bool Match(TokenType tokenType, out char variable)
        {
            if (!Is(tokenType))
            {
                variable = '\0';
                return false;
            }

            variable = tokens[_position].Variable;
            _position++;
            return true;
        }
    }
}
