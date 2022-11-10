namespace cslox {

  public abstract class Stmt {
    public interface Visitor<R> {
      R VisitBlockStmt(Block stmt);
      R VisitExpressionStmt(Expression stmt);
      R VisitPrintStmt(Print stmt);
      R VisitVarStmt(Var stmt);
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
    public class Expression : Stmt {
      public Expression(Expr expression) {
        this.expression = expression;
      }

      public override R Accept<R>(Visitor<R> visitor) {
        return  visitor.VisitExpressionStmt(this);
      }

      public Expr expression;
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

    public abstract R Accept<R>(Visitor<R> visitor);
  }
}
