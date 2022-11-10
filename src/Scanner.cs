using System;

namespace cslox
{
    public class Scanner
    {
        string source;
        List<Token> tokens = new List<Token>();
        int start = 0;
        int current = 0;
        int line = 1;

        public Scanner(string source) {
          this.source = source;
        }

        public List<Token> ScanTokens() {
          while(!IsAtEnd()) {
            // at the beginning of the next lexeme
            start = current;
            ScanToken();
          }

          tokens.Add(new Token(TokenType.EOF, "", null, line));
          return tokens;
        }

        void ScanToken() {
          char c = Advance();
          switch(c) {
            case '(': AddToken(TokenType.LEFT_PAREN); break;
            case ')': AddToken(TokenType.RIGHT_PAREN); break;
            case '{': AddToken(TokenType.LEFT_BRACE); break;
            case '}': AddToken(TokenType.RIGHT_BRACE); break;
            case ',': AddToken(TokenType.COMMA); break;
            case '.': AddToken(TokenType.DOT); break;
            case '-': AddToken(TokenType.MINUS); break;
            case '+': AddToken(TokenType.PLUS); break;
            case ';': AddToken(TokenType.SEMICOLON); break;
            case '*': AddToken(TokenType.STAR); break;
            case '!':
              AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
              break;
            case '=':
              AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
              break;
            case '<':
              AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
              break;
            case '>':
              AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
              break;
            case '/':
              if(Match('/')) {
                // Eat the comment up
                while(Peek() != '\n' && !IsAtEnd()) Advance();
              } else {
                AddToken(TokenType.SLASH);
              }
              break;

            case ' ':
            case '\r':
            case '\t':
              // ignore whitespace
              break;
            
            case '\n':
              line++;
              break;
            
            case '"':
              String();
              break;

            default:
              if(Char.IsDigit(c)) {
                Number();
              } else if(Char.IsLetter(c) || c == '_') {
                Identifier();
              } else {
                Program.Error(line, "Unexpected character.");
              }
              break;
          }
        }

        char Advance() {
          return source[current++];
        }

        char Peek() {
          if(IsAtEnd()) return '\0';
          return source[current];
        }

        char PeekNext() {
          if(current + 1 >= source.Length) return '\0';
          return source[current + 1];
        }

        bool Match(char expected) {
          if(IsAtEnd()) return false;
          if(source[current] != expected) return false;

          current++;
          return true;
        }

        void String() {
          while(Peek() != '"' && !IsAtEnd()) {
            if(Peek() == '\n') line++;
            Advance();
          }

          if(IsAtEnd()) {
            Program.Error(line, "Unterminated string.");
            return;
          }

          Advance(); // closing "

          string value = source.Substring(start + 1, current - start - 2);
          AddToken(TokenType.STRING, value);
        }

        void Number() {
          while(Char.IsDigit(Peek())) Advance();

          // Fractional part
          if(Peek() == '.' && Char.IsDigit(PeekNext())) {
            // consume the dot
            Advance();

            while(Char.IsDigit(Peek())) Advance();
          }

          AddToken(TokenType.NUMBER, Double.Parse(source.Substring(start, current - start)));
        }

        void Identifier() {
          while(Char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();

          string text = source.Substring(start, current - start);
          TokenType type;
          if(Keywords.TryGetValue(text, out type)) {
            AddToken(type);
          } else {
            AddToken(TokenType.IDENTIFIER);
          }
        }

        void AddToken(TokenType type) {
          AddToken(type, null);
        }

        void AddToken(TokenType type, object? literal) {
          string text = source.Substring(start, current - start);
          tokens.Add(new Token(type, text, literal, line));
        }

        bool IsAtEnd() {
          return current >= source.Length;
        }
      static Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>()
      {
        { "and", TokenType.AND },
        { "class", TokenType.CLASS },
        { "else", TokenType.ELSE },
        { "false", TokenType.FALSE },
        { "for", TokenType.FOR },
        { "fun", TokenType.FUN },
        { "if", TokenType.IF },
        { "nil", TokenType.NIL },
        { "or", TokenType.OR },
        { "print", TokenType.PRINT },
        { "return", TokenType.RETURN },
        { "super", TokenType.SUPER },
        { "this", TokenType.THIS },
        { "true", TokenType.TRUE },
        { "var", TokenType.VAR },
        { "while", TokenType.WHILE },
      };
    }
}