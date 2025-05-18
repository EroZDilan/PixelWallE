using System.Linq.Expressions;

public abstract class ASTNode
{
    public int Line {get ; }
    public int Column { get ; }

    public ASTNode(int line, int column)
    {
        Line = line;
        Column = column;
    }
}
public class ProgramNode : ASTNode
{
    public List<Statement> Statements {get; }
    public ProgramNode(List<Statement> statements, int line, int column): base(line, column)
    {
        Statements = statements;
    } 
}
