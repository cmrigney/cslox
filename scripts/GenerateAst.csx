#!/usr/bin/env dotnet script
using Internal;

Main();

void Main() {
  if (Args.Count != 1)
  {
    Console.WriteLine("Usage: dotnet script GenerateAst.cs <output directory>");
    System.Environment.Exit(64);
  }

  string outputDir = Args[0];
  DefineAst(outputDir, "Expr", new[] {
    "Assign     : Token name, Expr value",
    "Binary     : Expr left, Token op, Expr right",
    "Grouping   : Expr expression",
    "Literal    : object? value",
    "Logical    : Expr left, Token op, Expr right",
    "Variable   : Token name",
    "Unary      : Token op, Expr right" }.ToList());
  
  DefineAst(outputDir, "Stmt", new[] {
    "Block       : List<Stmt> statements",
    "Expression  : Expr expression",
    "If          : Expr condition, Stmt thenBranch, Stmt? elseBranch",
    "Print       : Expr expression",
    "Var         : Token name, Expr? initializer",
    "While       : Expr condition, Stmt body"
  }.ToList());
}



void DefineAst(string outputDir, string baseName, List<string> types) {
  string path = $"{outputDir}/{baseName}.cs";
  StringBuilder sb = new StringBuilder();

  sb.AppendLine("namespace cslox {");
  sb.AppendLine();
  sb.AppendLine($"  public abstract class {baseName} {{");

  DefineVisitor(sb, baseName, types);

  foreach(string type in types) {
    string className = type.Split(':')[0].Trim();
    string fields = type.Split(':')[1].Trim();
    DefineType(sb, baseName, className, fields);
  }

  sb.AppendLine();
  sb.AppendLine("    public abstract R Accept<R>(Visitor<R> visitor);");

  sb.AppendLine("  }");
  sb.AppendLine("}");

  string output = sb.ToString();
  File.WriteAllText(path, output);
}

void DefineType(StringBuilder sb, string baseName, string className, string fieldList) {
  sb.AppendLine($"    public class {className} : {baseName} {{");

  // constructor
  sb.AppendLine($"      public {className}({fieldList}) {{");
  
  // store parameters in fields
  string[] fields = fieldList.Split(", ");
  foreach(string field in fields) {
    string name = field.Split(" ")[1];
    sb.AppendLine($"        this.{name} = {name};");
  }

  sb.AppendLine("      }");

  // Visitor pattern
  sb.AppendLine();
  sb.AppendLine("      public override R Accept<R>(Visitor<R> visitor) {");
  sb.AppendLine($"        return  visitor.Visit{className}{baseName}(this);");
  sb.AppendLine("      }");

  // fields
  sb.AppendLine();
  foreach(string field in fields) {
    sb.AppendLine($"      public {field};");
  }

  sb.AppendLine("    }");
}

void DefineVisitor(StringBuilder sb, string baseName, List<String> types) {
  sb.AppendLine("    public interface Visitor<R> {");

  foreach(string type in types) {
    string typeName = type.Split(':')[0].Trim();
    sb.AppendLine($"      R Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
  }

  sb.AppendLine("    }");
  sb.AppendLine();
}
