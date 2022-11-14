namespace cslox {
  class ClockFn : LoxCallable
  {
    public int Arity()
    {
      return 0;
    }

    public object? Call(Interpreter interpet, List<object?> arguments)
    {
      TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
      return t.TotalSeconds;
    }

    public override string ToString()
    {
      return "<native fn clock>";
    }
  }
}