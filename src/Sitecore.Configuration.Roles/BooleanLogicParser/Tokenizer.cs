namespace Sitecore.Configuration.Roles.BooleanLogic
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;

  internal class Tokenizer
  {
    private readonly StringReader _reader;
    private string _text;
    private readonly string[] _trueAliases;

    internal Tokenizer(string text)
    {
      _text = text;
      _reader = new StringReader(text);
      _trueAliases = new string[0];
    }

    internal Tokenizer(string text, params string[] trueAliases)
    {
      _text = text;
      _reader = new StringReader(text);
      _trueAliases = trueAliases;
    }

    internal IEnumerable<Token> Tokenize()
    {
      var tokens = new List<Token>();
      while (_reader.Peek() != -1)
      {
        while (char.IsWhiteSpace((char)_reader.Peek()))
        {
          _reader.Read();
        }

        if (_reader.Peek() == -1)
        {
          break;
        }

        var c = (char)_reader.Peek();
        switch (c)
        {
          case '&':
            tokens.Add(new AndToken());
            _reader.Read();
            break;

          case '|':
            tokens.Add(new OrToken());
            _reader.Read();
            break;

          case '!':
            tokens.Add(new NegationToken());
            _reader.Read();
            break;

          case '(':
            tokens.Add(new OpenParenthesisToken());
            _reader.Read();
            break;

          case ')':
            tokens.Add(new ClosedParenthesisToken());
            _reader.Read();
            break;

          default:
            if (IsValidTokenCharacter(c))
            {
              var token = ParseKeyword();
              tokens.Add(token);
            }
            else
            {
              var remainingText = _reader.ReadToEnd() ?? string.Empty;
              throw new Exception(string.Format("Unknown grammar found at position {0} : '{1}'", _text.Length - remainingText.Length, remainingText));
            }
            break;
        }
      }

      return tokens;
    }

    private static bool IsValidTokenCharacter(char c)
    {
      return char.IsLetter(c) || char.IsDigit(c) || c == '-' || c == '_' || c == '.' || c == '/';
    }

    private Token ParseKeyword()
    {
      var text = new StringBuilder();
      while (IsValidTokenCharacter((char)_reader.Peek()))
      {
        text.Append((char)_reader.Read());
      }

      var potentialKeyword = text.ToString().ToLower();

      var aliases = _trueAliases.Length > 0;
      if (_trueAliases.Any(x => x.Equals(potentialKeyword, StringComparison.OrdinalIgnoreCase)))
      {
        return new TrueToken();
      }

      switch (potentialKeyword)
      {
        case "true":
          return new TrueToken();

        case "false":
          return new FalseToken();

        case "and":
          return new AndToken();

        case "or":
          return new OrToken();

        default:
          if (aliases)
          {
            return new FalseToken();
          }
          else
          {
            throw new Exception("Expected keyword (True, False, And, Or) but found " + potentialKeyword);
          }
      }
    }
  }
}