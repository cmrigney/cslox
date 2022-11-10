namespace cslox
{
  public class Interpreter : Expr.Visitor<object?>, Stmt.Visitor<object?>
  {
    Environment environment = new Environment();

    public void Interpret(List<Stmt> statements) {
      try {
        foreach(Stmt statement in statements) {
          Execute(statement);
        }
      } catch(RuntimeException error) {
        Program.RuntimeError(error);
      }
    }

    void Execute(Stmt stmt) {
      stmt.Accept(this);
    }

    object?  Evaluate(Expr expr) {
      return expr.Accept(this);
    }
    
    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
      Evaluate(stmt.expression);
      return null;
    }

    public object? VisitPrintStmt(Stmt.Print stmt)
    {
      object? value = Evaluate(stmt.expression);
      Console.WriteLine(Stringify(value));
      return null;
    }

    public object? VisitBlockStmt(Stmt.Block stmt)
    {
      ExecuteBlock(stmt.statements, new Environment(environment));
      return null;
    }

    public object? VisitVarStmt(Stmt.Var stmt)
    {
      object? value = null;
      if(stmt.initializer != null) {
        value = Evaluate(stmt.initializer);
      }

      environment.Define(stmt.name.lexeme, value);
      return null;
    }

    void ExecuteBlock(List<Stmt> statements, Environment environment) {
      Environment previous = this.environment;
      try {
        this.environment = environment;

        foreach(Stmt statement in statements) {
          Execute(statement);
        }
      } finally {
        this.environment = previous;
      }
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
      object? value = Evaluate(expr.value);
      environment.Assign(expr.name, value);
      return value;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
      object? left = Evaluate(expr.left);
      object? right = Evaluate(expr.right);

      switch(expr.op.type) {
        case TokenType.GREATER:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! > (double)right!;
        case TokenType.GREATER_EQUAL:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! >= (double)right!;
        case TokenType.LESS:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! < (double)right!;
        case TokenType.LESS_EQUAL:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! <= (double)right!;
        case TokenType.BANG_EQUAL:
          return !IsEqual(left, right);
        case TokenType.EQUAL_EQUAL:
          return IsEqual(left, right);
        case TokenType.MINUS:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! - (double)right!;
        case TokenType.PLUS:
          if(left is double && right is double) {
            return (double)left + (double)right;
          }

          if(left is string && right is string) {
            return (string)left + (string)right;
          }

          throw new RuntimeException(expr.op, "Operands must be two numbers or two strings.");
        case TokenType.SLASH:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! / (double)right!;
        case TokenType.STAR:
          CheckNumberOperands(expr.op, left, right);
          return (double)left! * (double)right!;
      }

      // Unreachable
      return null;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
      return Evaluate(expr.expression);
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
      return expr.value;
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
      object? right = Evaluate(expr.right);

      switch(expr.op.type) {
        case TokenType.BANG:
          return !IsTruthy(right);
        case TokenType.MINUS:
          CheckNumberOperand(expr.op, right);
          return -(double)right!;
      }

      // Unreachable
      return null;  
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
      return environment.Get(expr.name);
    }

    bool IsTruthy(object? obj) {
      if(obj == null) return false;
      if(obj is bool) return (bool)obj;
      return true;
    }

    bool IsEqual(object? a, object? b) {
      if(a == null && b == null) return true;
      if(a == null) return false;
      return a.Equals(b);
    }

    string Stringify(object? obj) {
      if(obj == null) return "nil";

      if(obj is double) {
        string text = ((double)obj).ToString();
        if(text.EndsWith(".0")) {
          text = text.Substring(0, text.Length - 2);
        }
        return text;
      }

      return obj.ToString() ?? "nil";
    }

    void CheckNumberOperands(Token op, object? left, object? right) {
      if(left is double && right is double) return;
      throw new RuntimeException(op, "Operands must be numbers.");
    }
    void CheckNumberOperand(Token op, object? operand) {
      if(operand is double) return;
      throw new RuntimeException(op, "Operand must be a number.");
    }

    public class RuntimeException : Exception {
      public Token token;
      public RuntimeException(Token token, string message) : base(message) {
        this.token = token;
      }
    }
  }
}