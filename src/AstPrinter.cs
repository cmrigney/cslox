using System.Text;

namespace cslox
{
  public class AstPrinter : Expr.Visitor<string>
  {
    public string print(Expr expr)
    {
      return expr.Accept(this);
    }

    public string VisitAssignExpr(Expr.Assign expr)
    {
      return Parenthesize(expr.name.lexeme, expr.value);
    }

    public string VisitBinaryExpr(Expr.Binary expr)
    {
      return Parenthesize(expr.op.lexeme, expr.left, expr.right);
    }

    public string VisitCallExpr(Expr.Call expr)
    {
      throw new NotImplementedException();
    }

    public string VisitGetExpr(Expr.Get expr)
    {
      throw new NotImplementedException();
    }

    public string VisitGroupingExpr(Expr.Grouping expr)
    {
      return Parenthesize("group", expr.expression);
    }

    public string VisitLiteralExpr(Expr.Literal expr)
    {
      if(expr.value == null) return "nil";
      return expr.value?.ToString() ?? throw new ArgumentNullException("expr.value");
    }

    public string VisitLogicalExpr(Expr.Logical expr)
    {
      throw new NotImplementedException();
    }

    public string VisitSetExpr(Expr.Set expr)
    {
      throw new NotImplementedException();
    }

    public string VisitThisExpr(Expr.This expr)
    {
      throw new NotImplementedException();
    }

    public string VisitUnaryExpr(Expr.Unary expr)
    {
      return Parenthesize(expr.op.lexeme, expr.right);
    }

    public string VisitVariableExpr(Expr.Variable expr)
    {
      return Parenthesize(expr.name.lexeme);
    }

    string Parenthesize(string name, params Expr[] exprs) {
      StringBuilder sb = new StringBuilder();

      sb.Append("(").Append(name);
      foreach(Expr expr in exprs) {
        sb.Append(" ");
        sb.Append(expr.Accept(this));
      }
      sb.Append(")");

      return sb.ToString();
    }
  }
}