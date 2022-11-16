namespace cslox
{
  public class Interpreter : Expr.Visitor<object?>, Stmt.Visitor<object?>
  {
    public Environment globals = new Environment();
    Environment environment;
    Dictionary<Expr, int> locals = new Dictionary<Expr, int>();

    public Interpreter() {
      environment = globals;
      globals.Define("clock", new ClockFn());
    }

    public void Interpret(List<Stmt> statements) {
      try {
        foreach(Stmt statement in statements) {
          Execute(statement);
        }
      } catch(RuntimeException error) {
        Program.RuntimeError(error);
      }
    }

    public void Resolve(Expr expr, int depth) {
      locals[expr] = depth;
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

    public object? VisitWhileStmt(Stmt.While stmt)
    {
      while(IsTruthy(Evaluate(stmt.condition))) {
        Execute(stmt.body);
      }

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

    public object? VisitIfStmt(Stmt.If stmt)
    {
      if(IsTruthy(Evaluate(stmt.condition))) {
        Execute(stmt.thenBranch);
      } else if(stmt.elseBranch != null) {
        Execute(stmt.elseBranch);
      }

      return null;
    }

    public object? VisitFunctionStmt(Stmt.Function stmt)
    {
      LoxFunction function = new LoxFunction(stmt, environment, false);
      environment.Define(stmt.name.lexeme, function);
      return null;
    }

    public object? VisitReturnStmt(Stmt.Return stmt)
    {
      object? value = null;
      if(stmt.value != null) value = Evaluate(stmt.value);

      throw new Return(value);
    }

    public object? VisitClassStmt(Stmt.Class stmt)
    {
      object? superclass = null;
      if(stmt.superclass != null) {
        superclass = Evaluate(stmt.superclass);
        if(superclass is not LoxClass) {
          throw new RuntimeException(stmt.superclass.name, "Superclass must be a class.");
        }
      }

      environment.Define(stmt.name.lexeme, null);

      if(stmt.superclass != null) {
        environment = new Environment(environment);
        environment.Define("super", superclass);
      }

      Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();
      foreach(Stmt.Function method in stmt.methods) {
        LoxFunction function = new LoxFunction(method, environment, method.name.lexeme.Equals("init"));
        methods[method.name.lexeme] = function;
      }

      LoxClass klass = new LoxClass(stmt.name.lexeme, (LoxClass?)superclass, methods);

      if(superclass != null) {
        environment = environment.enclosing!;
      }
 
      environment.Assign(stmt.name, klass);
      return null;
    }

    public void ExecuteBlock(List<Stmt> statements, Environment environment) {
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
      int distance;
      if(locals.TryGetValue(expr, out distance)) {
        environment.AssignAt(distance, expr.name, value);
      } else {
        globals.Assign(expr.name, value);
      }
      return value;
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
      object? obj = Evaluate(expr.obj);

      if(obj is not LoxInstance) {
        throw new RuntimeException(expr.name, "Only instances have fields.");
      }

      object? value = Evaluate(expr.value);
      ((LoxInstance)obj).Set(expr.name, value);
      return value;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
      object? callee = Evaluate(expr.callee);

      List<object?> arguments = new List<object?>();
      foreach(Expr argument in expr.arguments) {
        arguments.Add(Evaluate(argument));
      }

      if(!(callee is LoxCallable)) {
        throw new RuntimeException(expr.paren, "Can only call functions and classes.");
      }

      LoxCallable function = (LoxCallable)callee;
      if(arguments.Count != function.Arity()) {
        throw new RuntimeException(expr.paren, $"Expected {function.Arity()} arguments but got {arguments.Count}.");
      }

      return function.Call(this, arguments);
    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
      int distance = locals[expr];
      LoxClass superclass = (LoxClass)environment.GetAt(distance, "super")!;

      LoxInstance obj = (LoxInstance)environment.GetAt(distance - 1, "this")!;

      LoxFunction? method = superclass.FindMethod(expr.method.lexeme);

      if (method == null) {
        throw new RuntimeException(expr.method, $"Undefined property '{expr.method.lexeme}'.");
      }

      return method.Bind(obj);
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

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
      object? left = Evaluate(expr.left);

      if(expr.op.type == TokenType.OR) {
        if(IsTruthy(left)) return left;
      } else {
        if(!IsTruthy(left)) return left;
      }

      return Evaluate(expr.right);
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

    public object? VisitGetExpr(Expr.Get expr)
    {
      object? obj = Evaluate(expr.obj);
      if(obj is LoxInstance) {
        return ((LoxInstance)obj).Get(expr.name);
      }

      throw new RuntimeException(expr.name, "Only instances have properties");
    }

    public object? VisitThisExpr(Expr.This expr)
    {
      return LookupVariable(expr.keyword, expr);
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
      return LookupVariable(expr.name, expr);
    }

    object? LookupVariable(Token name, Expr expr) {
      int distance;

      if(locals.TryGetValue(expr, out distance)) {
        return environment.GetAt(distance, name.lexeme);
      } else {
        return globals.Get(name);
      }
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