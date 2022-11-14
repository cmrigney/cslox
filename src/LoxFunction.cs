namespace cslox {
  class LoxFunction : LoxCallable {
    Stmt.Function declaration;
    Environment closure;
    public LoxFunction(Stmt.Function declaration, Environment closure) {
      this.declaration = declaration;
      this.closure = closure;
    }

    public int Arity()
    {
      return declaration.parms.Count;
    }

    public object? Call(Interpreter interpeter, List<object?> arguments)
    {
      Environment environment = new Environment(closure);
      for(int i = 0; i < declaration.parms.Count; i++) {
        environment.Define(declaration.parms[i].lexeme, arguments[i]);
      }

      try {
        interpeter.ExecuteBlock(declaration.body, environment);
      } catch(Return returnValue) {
        return returnValue.value;
      }
      return null;
    }

    public override string ToString()
    {
      return $"<fn {declaration.name.lexeme}>";
    }
  }

  class Return : Exception {
    public object? value;

    public Return(object? value) {
      this.value = value;
    }
  }
}