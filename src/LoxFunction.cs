namespace cslox {
  class LoxFunction : LoxCallable {
    Stmt.Function declaration;
    Environment closure;
    bool isInitializer;

    public LoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer) {
      this.declaration = declaration;
      this.closure = closure;
      this.isInitializer = isInitializer;
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
        if(isInitializer) return closure.GetAt(0, "this");

        return returnValue.value;
      }
      
      if(isInitializer) return closure.GetAt(0, "this");
      return null;
    }

    public LoxFunction Bind(LoxInstance instance) {
      Environment environment = new Environment(closure);
      environment.Define("this", instance);
      return new LoxFunction(declaration, environment, isInitializer);
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