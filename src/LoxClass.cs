namespace cslox
{
  class LoxClass : LoxCallable
  {
    public string name;
    public Dictionary<string, LoxFunction> methods;
    public LoxClass? superclass;

    public LoxClass(string name, LoxClass? superclass, Dictionary<string, LoxFunction> methods)
    {
      this.superclass = superclass;
      this.name = name;
      this.methods = methods;
    }

    public int Arity()
    {
      LoxFunction? initializer = FindMethod("init");
      if(initializer == null) return 0;
      return initializer.Arity();
    }

    public object? Call(Interpreter interpeter, List<object?> arguments)
    {
      LoxInstance instance = new LoxInstance(this);
      LoxFunction? initializer = FindMethod("init");
      if(initializer != null) {
        initializer.Bind(instance).Call(interpeter, arguments);
      }
      return instance;
    }

    public LoxFunction? FindMethod(string name) {
      if(methods.ContainsKey(name)) {
        return methods[name];
      }

      if(superclass != null) {
        return superclass.FindMethod(name);
      }

      return null;
    }

    public override string ToString()
    {
      return name;
    }
  }
}