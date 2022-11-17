namespace cslox {

  public abstract class Stmt {
    public interface Visitor<R> {
      R VisitBlockStmt(Block stmt);
      R VisitClassStmt(Class stmt);
      R VisitExpressionStmt(Expression stmt);
      R VisitFunctionStmt(Function stmt);
      R VisitIfStmt(If stmt);
      R VisitPrintStmt(Print stmt);
      R VisitImportStmt(Import stmt);
      R VisitReturnStmt(Return stmt);
      R VisitVarStmt(Var stmt);
      R VisitWhileStmt(While stmt);
    }

    public class Block : Stmt {
      public Block(List<Stmt> statements) {
        this.statements = statements;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitBlockStmt(this);
      }

      public List<Stmt> statements;
    }
    public class Class : Stmt {
      public Class(Token name, Expr.Variable? superclass, List<Stmt.Function> methods) {
        this.name = name;
        this.superclass = superclass;
        this.methods = methods;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitClassStmt(this);
      }

      public Token name;
      public Expr.Variable? superclass;
      public List<Stmt.Function> methods;
    }
    public class Expression : Stmt {
      public Expression(Expr expression) {
        this.expression = expression;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitExpressionStmt(this);
      }

      public Expr expression;
    }
    public class Function : Stmt {
      public Function(Token name, List<Token> parms, List<Stmt> body) {
        this.name = name;
        this.parms = parms;
        this.body = body;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitFunctionStmt(this);
      }

      public Token name;
      public List<Token> parms;
      public List<Stmt> body;
    }
    public class If : Stmt {
      public If(Expr condition, Stmt thenBranch, Stmt? elseBranch) {
        this.condition = condition;
        this.thenBranch = thenBranch;
        this.elseBranch = elseBranch;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitIfStmt(this);
      }

      public Expr condition;
      public Stmt thenBranch;
      public Stmt? elseBranch;
    }
    public class Print : Stmt {
      public Print(Expr expression) {
        this.expression = expression;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitPrintStmt(this);
      }

      public Expr expression;
    }
    public class Import : Stmt {
      public Import(Token filename) {
        this.filename = filename;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitImportStmt(this);
      }

      public Token filename;
    }
    public class Return : Stmt {
      public Return(Token keyword, Expr? value) {
        this.keyword = keyword;
        this.value = value;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitReturnStmt(this);
      }

      public Token keyword;
      public Expr? value;
    }
    public class Var : Stmt {
      public Var(Token name, Expr? initializer) {
        this.name = name;
        this.initializer = initializer;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitVarStmt(this);
      }

      public Token name;
      public Expr? initializer;
    }
    public class While : Stmt {
      public While(Expr condition, Stmt body) {
        this.condition = condition;
        this.body = body;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitWhileStmt(this);
      }

      public Expr condition;
      public Stmt body;
    }

    public abstract R Accept<R>(Visitor<R> visitor);
  }
}
