using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Eee.Utility.Scripting.Lisp
{
  static public class LispParser
  {

    public static LispList Parse(string lispString)
    {
      int line = 1;
      int position = 0;
      List<LispToken> tokens = new List<LispToken>();


      // tokenize the string
      for (int i = 0; i < lispString.Length; i++)
      {
        char c = lispString[i];

        position++;

        if (c == '\n')
        {
          line++;
          position = 1;
        }

        if (char.IsWhiteSpace(c))
          continue;

        switch (c)
        {
          case '#':
          case ';':
            // eat the comment comment until we find a new line.
            int newIndex = lispString.IndexOf('\n', i);
            if (newIndex > 0)
              i = newIndex - 1;
            else
              i = lispString.Length;
            break;
          case '(':
            tokens.Add(new LispToken() { type = TokenType.ParamLeft, line = line, position = position });
            break;
          case ')':
            tokens.Add(new LispToken() { type = TokenType.ParamRight, line = line, position = position });
            break;
          case '"':
            {
              int index;
              bool isClosed = false;
              for (index = i + 1; index < lispString.Length && !isClosed; index++)
              {
                char cc = lispString[index];

                switch (cc)
                {
                  case '"':
                    isClosed = true;
                    index--;
                    break;
                  case '\n':
                    throw new LispParseException($"String literal cannot contain a carriage return. Line {line}:{position}");
                }
              }

              if (isClosed)
              {
                tokens.Add(new LispToken() { type = TokenType.String, value = lispString.Substring(i + 1, (index - 1) - i), line = line, position = position });
                i = index;
              }
              else
                throw new LispParseException($"String not closed on line {line}:{position}");
            }
            break;
          default:
            {
              if (char.IsLetter(c))
              {
                // it's a symbol
                int index;
                for (index = i; index < lispString.Length; index++)
                {
                  char cc = lispString[index];
                  if (!char.IsLetterOrDigit(cc))
                    break;
                }
                tokens.Add(new LispToken() { type = TokenType.Symbol, value = lispString.Substring(i, index - i), line = line, position = position });
                position += (index - 1) - i;
                i = index - 1;
              }
              else if (char.IsDigit(c) || c == '.')
              {
                // it's a number
                int index;
                for (index = i; index < lispString.Length; index++)
                {
                  char cc = lispString[index];
                  if (!(char.IsLetterOrDigit(cc) || cc == '.'))
                    break;
                }

                char typeValue = lispString[index - 1];
                string strValue = lispString.Substring(i, index - i - 1);
                switch (typeValue)
                {
                  case 'd':
                    {
                      double lValue;
                      if (double.TryParse(strValue, out lValue))
                        tokens.Add(new LispToken() { type = TokenType.Double, value = strValue, lValue = lValue, line = line, position = position });
                      else
                        throw new LispParseException($"Not a valid double. Line: {line}:{position}");
                      break;
                    }
                  case 'i':
                    {
                      int lValue;
                      if (int.TryParse(strValue, out lValue))
                        tokens.Add(new LispToken() { type = TokenType.Int, value = strValue, lValue = lValue, line = line, position = position });
                      else
                        throw new LispParseException($"Not a valid int. Line: {line}:{position}");
                    }
                    break;
                  default:
                    throw new LispParseException($"Not a valid number type. Line:{line},{position}");
                }

                position += (index - 1) - i;
                i = index - 1;
              }
            }
            break;
        }
      }

      // was there any tokens?
      if (tokens.Count == 0)
        throw new Exception("Nothing parsable in the file!");

      // perform balance check
      if (0 != tokens.Sum(t => ((t.type == TokenType.ParamLeft) ? 1 : (t.type == TokenType.ParamRight) ? -1 : 0)))
        throw new LispParseException("Parathensies aren't balanced.");

      int token_index = 0;
      LispList root = new LispList(tokens, ref token_index);
      return root;
    }

  }

  internal enum TokenType { ParamLeft, ParamRight, Symbol, String, Double, Int };

  internal class LispToken
  {
    public TokenType type;
    public string value; // string value
    public object lValue; // literal value
    public int line;
    public int position;

    public override string ToString()
    {
      switch (type)
      {
        case TokenType.ParamLeft:
        case TokenType.ParamRight:
          return type.ToString();
        case TokenType.String:
          return string.Format("STR: '{0}'", value);
        case TokenType.Symbol:
          return string.Format("SYM: '{0}'", value);
        case TokenType.Double:
          return string.Format("DBL: '{0}'", value);
        case TokenType.Int:
          return string.Format("INT: '{0}'", value);
      }

      return base.ToString();
    }
  }

  public class LispItem
  {
    public int line;
    public int position;
  }

  public class LispSymbol : LispItem
  {
    public string name;
    public override string ToString()
    {
      return string.Format("Symbol: '{0}'", name);
    }

  }

  public class LispInt : LispItem
  {
    public int value;
    public override string ToString()
    {
      return $"Int: '{value}'";
    }
  }

  public class LispDouble : LispItem
  {
    public double value;
    public override string ToString()
    {
      return string.Format("Double: '{0}'", value);
    }
  }

  public class LispString : LispItem
  {
    public string value;
    public override string ToString()
    {
      return string.Format("Value: '{0}'", value);
    }
  }

  public class LispList : LispItem
  {
    public List<LispItem> items;

    public LispList()
    {
      Init();
    }

    internal LispList(List<LispToken> tokens, ref int index)
    {
      Init();
      if (index >= tokens.Count)
        throw new LispParseException("Consturctor called but no tokens to parse!");

      // check for left paranthesies
      LispToken token = tokens[index];
      if (token.type != TokenType.ParamLeft)
        throw new LispParseException($"Lisp List must start with a left parantheses. Line {token.line}:{token.position}");

      bool notFoundEnd = true;
      index++;

      for (; index < tokens.Count && notFoundEnd; index++)
      {
        token = tokens[index];
        switch (token.type)
        {
          case TokenType.ParamLeft:
            LispList lst = new LispList(tokens, ref index);
            lst.line = token.line;
            lst.position = token.position;
            items.Add(lst);
            break;
          case TokenType.ParamRight:
            notFoundEnd = false;
            index--;
            break;
          case TokenType.Double:
            items.Add(new LispDouble() { value = (double)token.lValue, line = token.line, position = token.position });
            break;
          case TokenType.Int:
            items.Add(new LispInt() { value = (int)token.lValue, line = token.line, position = token.position });
            break;
          case TokenType.String:
            items.Add(new LispString() { value = token.value, line = token.line, position = token.position });
            break;
          case TokenType.Symbol:
            items.Add(new LispSymbol() { name = token.value, line = token.line, position = token.position });
            break;
        }
      }
    }

    private void Init()
    {
      items = new List<LispItem>();
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      StringBuildList(sb, 0);
      return sb.ToString();
    }

    public void StringBuildList(StringBuilder sb, int indent)
    {
      // do the indent
      sb.Append(BuildIndent(indent));
      sb.AppendLine("LST:");

      foreach (LispItem item in items)
      {
        if (item is LispList)
          (item as LispList).StringBuildList(sb, indent + 1);
        else
        {
          sb.AppendFormat("{0}{1}", BuildIndent(indent + 1), item);
          sb.AppendLine();
        }
      }
    }

    private string BuildIndent(int indent)
    {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < indent; i++)
        sb.Append("  |");
      if (indent > 0)
        sb.Append("-");
      return sb.ToString();
    }
  }

  [Serializable]
  public class LispParseException : Exception
  {
    public LispParseException(string message) : base(message)
    {

    }

    public LispParseException(string message, Exception innerException)
      :base(message, innerException)
    {

    }
  }
}
