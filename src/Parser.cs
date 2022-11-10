namespace cslox
{
  public class Parser
  {
    List<Token> tokens;
    int current = 0;

    public Parser(List<Token> tokens)
    {
      this.tokens = tokens;
    }

    public List<Stmt> Parse()
    {
      List<Stmt> statements = new List<Stmt>();
      while (!IsAtEnd())
      {
        var decl = Declaration();
        if (decl != null) statements.Add(decl);
      }

      return statements;
    }

    /*
    program       -> declaration* EOF;
    declaration   -> varDecl | statement;
    statement     -> exprStmt | printStmt;
    varDecl       -> "var" IDENTIFIER ( "=" expression )? ";" ;
    */

    Stmt? Declaration()
    {
      try
      {
        if (Match(TokenType.VAR)) return VarDeclaration();
        return Statement();
      }
      catch (ParseException)
      {
        Synchronize();
        return null;
      }
    }

    Stmt VarDeclaration()
    {
      Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

      Expr? initializer = null;
      if (Match(TokenType.EQUAL))
      {
        initializer = Expression();
      }

      Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
      return new Stmt.Var(name, initializer);
    }

    Stmt Statement()
    {
      if (Match(TokenType.PRINT)) return PrintStatement();
      if (Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

      return ExpressionStatement();
    }

    List<Stmt> Block()
    {
      List<Stmt> statements = new List<Stmt>();

      while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
      {
        var decl = Declaration();
        if (decl != null)
        {
          statements.Add(decl);
        }
      }

      Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
      return statements;
    }

    Stmt PrintStatement()
    {
      Expr value = Expression();
      Consume(TokenType.SEMICOLON, "Expect ';' after value.");
      return new Stmt.Print(value);
    }

    Stmt ExpressionStatement()
    {
      Expr expr = Expression();
      Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
      return new Stmt.Expression(expr);
    }

    /*
    expression     → equality ;
    equality       → comparison ( ( "!=" | "==" ) comparison )* ;
    comparison     → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
    term           → factor ( ( "-" | "+" ) factor )* ;
    factor         → unary ( ( "/" | "*" ) unary )* ;
    unary          → ( "!" | "-" ) unary
                  | primary ;
    primary        → NUMBER | STRING | "true" | "false" | "nil"
                  | "(" expression ")"
                  | IDENTIFIER;
    */

    Expr Expression()
    {
      return Assignment();
    }

    Expr Assignment()
    {
      Expr expr = Equality();

      if (Match(TokenType.EQUAL))
      {
        Token equals = Previous();
        Expr value = Assignment();

        if (expr is Expr.Variable)
        {
          Token name = ((Expr.Variable)expr).name;
          return new Expr.Assign(name, value);
        }

        Program.Error(equals, "Invalid assignment target.");
      }

      return expr;
    }

    Expr Equality()
    {
      Expr expr = Comparison();

      while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
      {
        Token op = Previous();
        Expr right = Comparison();
        expr = new Expr.Binary(expr, op, right);
      }

      return expr;
    }

    Expr Comparison()
    {
      Expr expr = Term();

      while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
      {
        Token op = Previous();
        Expr right = Term();
        expr = new Expr.Binary(expr, op, right);
      }

      return expr;
    }

    Expr Term()
    {
      Expr expr = Factor();

      while (Match(TokenType.MINUS, TokenType.PLUS))
      {
        Token op = Previous();
        Expr right = Factor();
        expr = new Expr.Binary(expr, op, right);
      }

      return expr;
    }

    Expr Factor()
    {
      Expr expr = Unary();

      while (Match(TokenType.SLASH, TokenType.STAR))
      {
        Token op = Previous();
        Expr right = Unary();
        expr = new Expr.Binary(expr, op, right);
      }

      return expr;
    }

    Expr Unary()
    {
      if (Match(TokenType.BANG, TokenType.MINUS))
      {
        Token op = Previous();
        Expr right = Unary();
        return new Expr.Unary(op, right);
      }

      return Primary();
    }

    Expr Primary()
    {
      if (Match(TokenType.FALSE)) return new Expr.Literal(false);
      if (Match(TokenType.TRUE)) return new Expr.Literal(true);
      if (Match(TokenType.NIL)) return new Expr.Literal(null);

      if (Match(TokenType.NUMBER, TokenType.STRING))
      {
        return new Expr.Literal(Previous().literal);
      }

      if (Match(TokenType.IDENTIFIER))
      {
        return new Expr.Variable(Previous());
      }

      if (Match(TokenType.LEFT_PAREN))
      {
        Expr expr = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
        return new Expr.Grouping(expr);
      }

      throw Error(Peek(), "Expect expression.");
    }

    Token Consume(TokenType type, string message)
    {
      if (Check(type)) return Advance();

      throw Error(Peek(), message);
    }

    ParseException Error(Token token, string message)
    {
      Program.Error(token, message);
      return new ParseException();
    }

    class ParseException : Exception
    {

    }

    void Synchronize()
    {
      Advance();

      while (!IsAtEnd())
      {
        if (Previous().type == TokenType.SEMICOLON) return;

        switch (Peek().type)
        {
          case TokenType.CLASS:
          case TokenType.FUN:
          case TokenType.VAR:
          case TokenType.FOR:
          case TokenType.IF:
          case TokenType.WHILE:
          case TokenType.PRINT:
          case TokenType.RETURN:
            return;
        }

        Advance();
      }
    }

    bool Match(params TokenType[] types)
    {
      foreach (TokenType type in types)
      {
        if (Check(type))
        {
          Advance();
          return true;
        }
      }

      return false;
    }

    bool Check(TokenType type)
    {
      if (IsAtEnd()) return false;
      return Peek().type == type;
    }

    Token Advance()
    {
      if (!IsAtEnd()) current++;
      return Previous();
    }

    bool IsAtEnd()
    {
      return Peek().type == TokenType.EOF;
    }

    Token Peek()
    {
      return tokens[current];
    }

    Token Previous()
    {
      return tokens[current - 1];
    }

  }
}