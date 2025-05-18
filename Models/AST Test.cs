public static class ASTVisualizer
{
    public static void Visualize(ASTNode node)
    {
        VisualizeNode(node, 0);
    }

    private static void VisualizeNode(ASTNode node, int depth)
    {
        string indent = new string(' ', depth * 4);

        if (node == null)
        {
            Console.WriteLine($"{indent}[NULL]");
            return;
        }

        // Mostrar información básica del nodo
        Console.Write($"{indent}{node.GetType().Name} (Line: {node.Line}, Col: {node.Column})");

        // Información específica según el tipo de nodo
        if (node is ProgramNode programNode)
        {
            Console.WriteLine(" {");
            foreach (var statement in programNode.Statements)
            {
                VisualizeNode(statement, depth + 1);
            }
            Console.WriteLine($"{indent}}}");
        }
        else if (node is AssigmentNode assignmentNode)
        {
            Console.WriteLine($" | Variable: {assignmentNode.VariableName} <- ");
            VisualizeNode(assignmentNode.Value, depth + 1);
        }
        else if (node is LabelNode labelNode)
        {
            Console.WriteLine($" | Label: {labelNode.Name} {{");
            VisualizeNode(labelNode.Programa, depth + 1);
            Console.WriteLine($"{indent}}}");
        }
        else if (node is Instruction instruction)
        {
            Console.WriteLine($" | Instruction: {instruction.Name}");
            foreach (var arg in instruction.Arguments)
            {
                VisualizeNode(arg, depth + 1);
            }
        }
        else if (node is FunctionCallStatement funcCallStmt)
        {
            VisualizeNode(funcCallStmt.FunctionCall, depth);
        }
        else if (node is FunctionCall functionCall)
        {
            Console.WriteLine($" | Function: {functionCall.Name}(");
            foreach (var arg in functionCall.Arguments)
            {
                VisualizeNode(arg, depth + 1);
            }
            Console.WriteLine($"{indent})");
        }
        else if (node is AdditiveExpression addExpr)
        {
            if (addExpr.Right != null)
            {
                Console.WriteLine($" | Operator: {addExpr.Operator}");
                Console.WriteLine($"{indent}Left:");
                VisualizeNode(addExpr.Left, depth + 1);
                Console.WriteLine($"{indent}Right:");
                VisualizeNode(addExpr.Right, depth + 1);
            }
            else
            {
                Console.WriteLine(" (Single operand)");
                VisualizeNode(addExpr.Left, depth + 1);
            }
        }
        else if (node is MultiplicativeExpression multExpr)
        {
            Console.WriteLine($" | Operator: {multExpr.Operator}");
            Console.WriteLine($"{indent}Left:");
            VisualizeNode(multExpr.Left, depth + 1);
            Console.WriteLine($"{indent}Right:");
            VisualizeNode(multExpr.Right, depth + 1);
        }
        else if (node is PowerExpression powExpr)
        {
            Console.WriteLine(" | Power expression");
            Console.WriteLine($"{indent}Base:");
            VisualizeNode(powExpr.Base, depth + 1);
            Console.WriteLine($"{indent}Exponent:");
            VisualizeNode(powExpr.Exponent, depth + 1);
        }
        else if (node is UnaryExpressionNode unaryExpr)
        {
            Console.WriteLine($" | Sign: {(unaryExpr.Sign ? "-" : "+")}");
            VisualizeNode(unaryExpr.Module, depth + 1);
        }
        else if (node is NumberNode numberNode)
        {
            Console.WriteLine($" | Value: {numberNode.Value}");
        }
        else if (node is VariableNode variableNode)
        {
            Console.WriteLine($" | Name: {variableNode.Name}");
        }
        else if (node is StringExpression stringExpr)
        {
            Console.WriteLine($" | Value: \"{stringExpr.Value}\"");
        }
        else if (node is OrExpression orExpr)
        {
            Console.WriteLine(" | OR");
            Console.WriteLine($"{indent}Left:");
            VisualizeNode(orExpr.Left, depth + 1);
            Console.WriteLine($"{indent}Right:");
            VisualizeNode(orExpr.Right, depth + 1);
        }
        else if (node is AndExpression andExpr)
        {
            Console.WriteLine(" | AND");
            Console.WriteLine($"{indent}Left:");
            VisualizeNode(andExpr.Left, depth + 1);
            Console.WriteLine($"{indent}Right:");
            VisualizeNode(andExpr.Right, depth + 1);
        }
        else if (node is ComparisonExpression compExpr)
        {
            Console.WriteLine($" | Comparison: {compExpr.Operator}");
            Console.WriteLine($"{indent}Left:");
            VisualizeNode(compExpr.Left, depth + 1);
            Console.WriteLine($"{indent}Right:");
            VisualizeNode(compExpr.Right, depth + 1);
        }
        else if (node is BoolLiteralNode boolLiteral)
        {
            Console.WriteLine($" | Value: {boolLiteral.Value}");
        }
        else
        {
            // Para cualquier otro tipo de nodo
            Console.WriteLine();
        }
    }
}
namespace PixelWallE.Models.Compiler
{
}
