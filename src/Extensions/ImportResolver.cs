namespace cslox {
  class ImportResolver
  {
    HashSet<string> resolvedPaths = new HashSet<string>();
    public void ResolveImports(List<Stmt> statements, string cwd) {
      ResolveImports(statements, statements, cwd);
      statements.RemoveAll(stmt => stmt is Stmt.Import); // clear imports
    }

    void ResolveImports(List<Stmt> destStatements, List<Stmt> statements, string cwd) {
      List<Stmt.Import> importStatements = statements.TakeWhile(stmt => stmt is Stmt.Import).Cast<Stmt.Import>().ToList();
      List<Stmt.Import> invalidImports = statements.FindAll(stmt => stmt is Stmt.Import).Cast<Stmt.Import>().Except(importStatements).ToList();
      if(invalidImports.Count > 0) {
        Program.Error(invalidImports.First().filename, "Import must occur at the top of the file.");
        return;
      }
      // Reverse to get right ordering of imports
      importStatements.Reverse();
      foreach(Stmt.Import statement in importStatements) {
        ResolveImport(destStatements, statement, cwd);
      }
    }

    void ResolveImport(List<Stmt> destStatements, Stmt.Import statement, string cwd) {
      string path = Path.GetFullPath(Path.Join(cwd, (string)statement.filename.literal!));

      string lowerPath = path.ToLowerInvariant();
      if(resolvedPaths.Contains(lowerPath)) {
        return; // skip if we already resolved it
      }
      resolvedPaths.Add(lowerPath);

      string source;
      try {
        source = File.ReadAllText(path);
      }
      catch {
        Program.Error(statement.filename, $"Unable to import '{statement.filename.literal!.ToString()}'.");
        return;
      }

      Scanner scanner = new Scanner(source);
      List<Token> tokens = scanner.ScanTokens();
      Parser parser = new Parser(tokens);
      List<Stmt> statements = parser.Parse();

      if (Program.hadError) return;

      destStatements.InsertRange(0, statements);

      ResolveImports(destStatements, statements, Path.GetDirectoryName(path)!);
    }
  }
}