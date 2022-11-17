namespace cslox
{

  class Program
  {
    public static bool hadError = false;
    static bool hadRuntimeError = false;
    static Interpreter interpreter = new Interpreter();

    static void Main(string[] args)
    {
      if (args.Length > 1)
      {
        Console.WriteLine("Usage: cslox [script]");
        System.Environment.Exit(64);
      }
      else if (args.Length == 1)
      {
        RunFile(args[0]);
      }
      else
      {
        RunPrompt();
      }

    }

    static void RunFile(string path)
    {
      string content = File.ReadAllText(path);
      Run(content, Path.GetDirectoryName(path)!);

      if(hadError) System.Environment.Exit(65);
      if(hadRuntimeError) System.Environment.Exit(70);
    }

    static void RunPrompt()
    {
      while (true)
      {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line == null) break;
        Run(line, Directory.GetCurrentDirectory());
        hadError = false;
      }
    }

    static void Run(string source, string cwd)
    {
      Scanner scanner = new Scanner(source);
      List<Token> tokens = scanner.ScanTokens();
      Parser parser = new Parser(tokens);
      List<Stmt> statements = parser.Parse();

      new ImportResolver().ResolveImports(statements, cwd);

      // Stop if there was a syntax error
      if(hadError) return;

      Resolver resolver = new Resolver(interpreter);
      resolver.Resolve(statements);

      // Stop if there was a resolution error
      if(hadError) return;

      interpreter.Interpret(statements);
    }

    public static void Error(int line, string message)
    {
      Report(line, "", message);
    }

    public static void Error(Token token, string message) {
      if(token.type == TokenType.EOF) {
        Report(token.line, " at end", message);
      } else {
        Report(token.line, $" at '{token.lexeme}'", message);
      }
    }

    static void Report(int line, string where, string message)
    {
      Console.WriteLine($"[line {line.ToString()}] Error{where}: {message}");
      hadError = true;
    }

    public static void RuntimeError(Interpreter.RuntimeException error) {
      Console.WriteLine($"{error.Message}\n[line {error.token.line}]");
      hadRuntimeError = true;
    }
  }
}