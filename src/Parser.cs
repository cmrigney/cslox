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
    Example syntax:
    program       -> declaration* EOF;
    declaration   -> varDecl | statement;
    statement     -> exprStmt | printStmt;
    varDecl       -> "var" IDENTIFIER ( "=" expression )? ";" ;
    */

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

    Stmt? Declaration()
    {
      try
      {
        if (Match(TokenType.CLASS)) return ClassDeclaration();
        if (Match(TokenType.FUN)) return Function("function");
        if (Match(TokenType.VAR)) return VarDeclaration();
        return Statement();
      }
      catch (ParseException)
      {
        Synchronize();
        return null;
      }
    }

    Stmt Function(string kind)
    {
      Token name = Consume(TokenType.IDENTIFIER, $"Expect {kind} name.");
      Consume(TokenType.LEFT_PAREN, $"Expect '(' after {kind} name.");
      List<Token> parameters = new List<Token>();
      if (!Check(TokenType.RIGHT_PAREN))
      {
        do
        {
          if (parameters.Count >= 255)
          {
            Program.Error(Peek(), "Can't have more than 255 parameters.");
          }

          parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
        } while (Match(TokenType.COMMA));
      }
      Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");

      Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {kind} body.");
      List<Stmt> body = Block();
      return new Stmt.Function(name, parameters, body);
    }

    Stmt ClassDeclaration()
    {
      Token name = Consume(TokenType.IDENTIFIER, "Expect class name.");

      Expr.Variable? superclass = null;
      if (Match(TokenType.LESS))
      {
        Consume(TokenType.IDENTIFIER, "Expect superclass name.");
        superclass = new Expr.Variable(Previous());
      }

      Consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");

      List<Stmt.Function> methods = new List<Stmt.Function>();
      while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
      {
        methods.Add((Stmt.Function)Function("method"));
      }

      Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
      return new Stmt.Class(name, superclass, methods);
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
      if (Match(TokenType.FOR)) return ForStatement();
      if (Match(TokenType.IF)) return IfStatement();
      if (Match(TokenType.PRINT)) return PrintStatement();
      if (Match(TokenType.IMPORT)) return ImportStatement();
      if (Match(TokenType.RETURN)) return ReturnStatement();
      if (Match(TokenType.WHILE)) return WhileStatement();
      if (Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

      return ExpressionStatement();
    }

    Stmt ImportStatement()
    {
      // Token keyword = Previous();
      Token filename = Consume(TokenType.STRING, "Import must be followed by string.");
      Consume(TokenType.SEMICOLON, "Expect ';' after import.");
      return new Stmt.Import(filename);
    }

    Stmt ReturnStatement()
    {
      Token keyword = Previous();
      Expr? value = null;
      if (!Check(TokenType.SEMICOLON))
      {
        value = Expression();
      }

      Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
      return new Stmt.Return(keyword, value);
    }

    Stmt ForStatement()
    {
      Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

      Stmt? initializer = null;
      if (Match(TokenType.SEMICOLON))
      {
        initializer = null;
      }
      else if (Match(TokenType.VAR))
      {
        initializer = VarDeclaration();
      }
      else
      {
        initializer = ExpressionStatement();
      }

      Expr? condition = null;
      if (!Check(TokenType.SEMICOLON))
      {
        condition = Expression();
      }
      Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

      Expr? increment = null;
      if (!Check(TokenType.RIGHT_PAREN))
      {
        increment = Expression();
      }

      Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");
      Stmt body = Statement();

      // Syntax sugar for while loop
      if (increment != null)
      {
        body = new Stmt.Block(new[] { body, new Stmt.Expression(increment) }.ToList());
      }

      if (condition == null) condition = new Expr.Literal(true);
      body = new Stmt.While(condition, body);

      if (initializer != null)
      {
        body = new Stmt.Block(new[] { initializer, body }.ToList());
      }

      return body;
    }

    Stmt WhileStatement()
    {
      Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
      Expr condition = Expression();
      Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
      Stmt body = Statement();

      return new Stmt.While(condition, body);
    }

    Stmt IfStatement()
    {
      Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
      Expr condition = Expression();
      Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

      Stmt thenBranch = Statement();
      Stmt? elseBranch = null;
      if (Match(TokenType.ELSE))
      {
        elseBranch = Statement();
      }

      return new Stmt.If(condition, thenBranch, elseBranch);
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

    Expr Expression()
    {
      return Assignment();
    }

    Expr Assignment()
    {
      Expr expr = Or();

      if (Match(TokenType.EQUAL))
      {
        Token equals = Previous();
        Expr value = Assignment();

        if (expr is Expr.Variable)
        {
          Token name = ((Expr.Variable)expr).name;
          return new Expr.Assign(name, value);
        }
        else if (expr is Expr.Get)
        {
          Expr.Get get = (Expr.Get)expr;
          return new Expr.Set(get.obj, get.name, value);
        }

        Program.Error(equals, "Invalid assignment target.");
      }

      return expr;
    }

    Expr Or()
    {
      Expr expr = And();

      while (Match(TokenType.OR))
      {
        Token op = Previous();
        Expr right = And();
        expr = new Expr.Logical(expr, op, right);
      }

      return expr;
    }

    Expr And()
    {
      Expr expr = Equality();

      while (Match(TokenType.AND))
      {
        Token op = Previous();
        Expr right = Equality();
        expr = new Expr.Logical(expr, op, right);
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

      return Call();
    }

    Expr Call()
    {
      Expr expr = Primary();

      while (true)
      {
        if (Match(TokenType.LEFT_PAREN))
        {
          expr = FinishCall(expr);
        }
        else if (Match(TokenType.DOT))
        {
          Token name = Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
          expr = new Expr.Get(expr, name);
        }
        else
        {
          break;
        }
      }

      return expr;
    }

    Expr FinishCall(Expr callee)
    {
      List<Expr> arguments = new List<Expr>();
      if (!Check(TokenType.RIGHT_PAREN))
      {
        do
        {
          if (arguments.Count >= 255)
          {
            Program.Error(Peek(), "Can't have more than 255 arguments.");
          }
          arguments.Add(Expression());
        } while (Match(TokenType.COMMA));
      }

      Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");

      return new Expr.Call(callee, paren, arguments);
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

      if (Match(TokenType.SUPER))
      {
        Token keyword = Previous();
        Consume(TokenType.DOT, "Expect '.' after 'super'.");
        Token method = Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
        return new Expr.Super(keyword, method);
      }

      if (Match(TokenType.THIS)) return new Expr.This(Previous());

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