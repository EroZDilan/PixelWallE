using System.Linq.Expressions;

public abstract class ExpressionNode : ASTNode
{
    public ExpressionNode(int line, int column) : base(line, column) {}

    
}

public abstract class ArithmeticExpression : ExpressionNode
{
    public ArithmeticExpression(int line, int column) : base(line, column) {}
}

public class AdditiveExpression : ArithmeticExpression
{
    public MultiplicativeExpression Left {get; }
    public Tipo? Operator {get; }
    public MultiplicativeExpression? Right {get; }

    public AdditiveExpression(MultiplicativeExpression left, MultiplicativeExpression right, Tipo op, int line, int column) : base(line, column)
    {
        Left = left;
        Right = right;
        Operator = op;
    }

    public AdditiveExpression(MultiplicativeExpression left, int line, int column) : base(line, column)
    {
        Left = left;
    }
}

public class MultiplicativeExpression : ArithmeticExpression
{
    public PowerExpression Left {get; }
    public Tipo? Operator {get; }
    public PowerExpression? Right {get; }

    public MultiplicativeExpression(PowerExpression left, PowerExpression right, Tipo op, int line, int column) : base(line, column)
    {
        Left = left;
        Right = right;
        Operator = op;
    }

    public MultiplicativeExpression(PowerExpression left, int line, int column) : base(line, column)
    {
        Left = left;
    }
}

public class PowerExpression : ArithmeticExpression
{
    public UnaryExpressionNode Base {get; }
    
    public UnaryExpressionNode? Exponent {get; }

    public PowerExpression(UnaryExpressionNode base_, UnaryExpressionNode exponent,  int line, int column) : base(line, column)
    {
        Base = base_;
        Exponent = exponent;
    }

    public PowerExpression(UnaryExpressionNode base_, int line, int column) : base(line, column)
    {
        Base = base_;
    }
}

public class UnaryExpressionNode : ArithmeticExpression
{
    public bool  Sign {get; }
    public PrimaryExpression Module {get; }

    public UnaryExpressionNode(bool sign, PrimaryExpression module, int line, int column) : base(line, column)
    {
        Sign = sign;
        Module = module;

    }
}

public abstract class PrimaryExpression : ArithmeticExpression
{
    public PrimaryExpression(int line, int column) : base(line, column) {}
    public PrimaryExpression(Expression exp, int line, int column) : base(line, column) {}
}

public class ParenthesizedExpression : PrimaryExpression
{
    public ExpressionNode Expression { get; }
    public ParenthesizedExpression(ExpressionNode expr, int line, int column) : base(line, column)
    {
        Expression = expr;
    }
}

public class NumberNode: PrimaryExpression
{
    public int Value {get; }
    public NumberNode (int value, int line, int column) : base(line, column)
    {
        Value = value;
    }
}

public class VariableNode : PrimaryExpression
{
    public string Name {get; }

    public VariableNode(string name, int line, int column) : base(line, column)
    {
        Name = name;
    }
}

public class FunctionCall : PrimaryExpression
{
    public string Name {get; }
    public List<ExpressionNode> Arguments {get; }

    public FunctionCall(string name, List<ExpressionNode> arguments, int line, int column) : base(line, column)
    {
        Name = name;
        Arguments = arguments;
    }
}

public abstract class BooleanExpression : ExpressionNode
{
    public BooleanExpression(int line, int column) : base(line, column) {}
}

public class OrExpression : BooleanExpression
{
    public AndExpression Left {get; }
    public AndExpression? Right {get; }

    public OrExpression(AndExpression left, AndExpression right, int line, int column ) : base(line, column)
    {
        Left = left;
        Right = right;
    }
    public OrExpression(AndExpression left, int line, int column ) : base(line, column)
    {
        Left = left;
    }
}

public class AndExpression : BooleanExpression
{
    public ComparisonExpression Left {get; }
    public ComparisonExpression? Right {get; }

    public AndExpression(ComparisonExpression left, ComparisonExpression right, int line, int column ) : base(line, column)
    {
        Left = left;
        Right = right;
    }
    public AndExpression(ComparisonExpression left, int line, int column ) : base(line, column)
    {
        Left = left;
    }
}

public class ComparisonExpression : BooleanExpression
{
    public ExpressionNode Left { get; }
    public Tipo? Operator { get; }
    public ExpressionNode? Right { get; }

    public ComparisonExpression(ExpressionNode left, Tipo op, ExpressionNode right, int line, int column) : base(line, column)
    {
        Left = left;
        Right = right;
        Operator = op;
    }

    public ComparisonExpression(ExpressionNode left, int line, int column) : base(line, column)
    {
        Left = left;
    }
}

public class BoolLiteralNode : BooleanExpression
{
    public bool Value { get; }

    public BoolLiteralNode(bool value, int line, int column) : base(line, column)
    {
        Value = value;
    }
}

public class StringExpression :ExpressionNode
{
    public string Value {get; }

    public StringExpression(string value, int line, int column) : base(line, column)
    {
        Value = value;
    }
}