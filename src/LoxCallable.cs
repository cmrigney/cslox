namespace cslox
{
  interface LoxCallable {
    int Arity();
    object? Call(Interpreter interpeter, List<object?> arguments);
  }
}