namespace cslox
{
  public class Environment
  {
    Environment? enclosing;
    Dictionary<string, object?> values = new Dictionary<string, object?>();

    public Environment() {
      enclosing = null;
    }

    public Environment(Environment enclosing) {
      this.enclosing = enclosing;
    }

    public void Define(string name, object? value)
    {
      values[name] = value;
    }

    public void Assign(Token name, object? value)
    {
      if (values.ContainsKey(name.lexeme))
      {
        values[name.lexeme] = value;
        return;
      }

      if(enclosing != null) {
        enclosing.Assign(name, value);
        return;
      }

      throw new Interpreter.RuntimeException(name, $"Undefined variable '{name.lexeme}'.");
    }

    public object? Get(Token name)
    {
      if (values.ContainsKey(name.lexeme))
      {
        return values[name.lexeme];
      }

      if(enclosing != null) return enclosing.Get(name);

      throw new Interpreter.RuntimeException(name, $"Undefined variable '{name.lexeme}'.");
    }
  }
}