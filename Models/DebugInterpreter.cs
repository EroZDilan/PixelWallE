using System;
using System.Collections.Generic;
using System.Linq;

public class DebugInterpreter : Interpreter
{
    private int currentStatementIndex = 0;
    private List<Statement> flattenedStatements;
    private Dictionary<int, int> statementToOriginalLine;
    private bool hasError = false;
    private string currentError = "";

    public DebugInterpreter(ProgramNode ast, int canvasSize) : base(ast, canvasSize)
    {
        // Aplanar todos los statements para ejecución paso a paso
        flattenedStatements = new List<Statement>();
        statementToOriginalLine = new Dictionary<int, int>();
        FlattenStatements(ast.Statements, 0);
    }

    private int FlattenStatements(List<Statement> statements, int startIndex)
    {
        int currentIndex = startIndex;

        foreach (var statement in statements)
        {
            flattenedStatements.Add(statement);
            statementToOriginalLine[flattenedStatements.Count - 1] = statement.Line;
            currentIndex++;

            // NO agregar los statements internos del label aquí
            // Se ejecutarán cuando se haga GoTo hacia el label
        }

        return currentIndex;
    }

    public StepResult StepExecute()
    {
        // Verificar si hemos terminado todos los statements
        if (currentStatementIndex >= flattenedStatements.Count)
        {
            return new StepResult
            {
                IsFinished = true,
                CurrentLine = GetLastExecutedLine(),
                PixelData = GeneratePixelData(),
                LogMessage = "Programa completado."
            };
        }

        var statement = flattenedStatements[currentStatementIndex];
        string logMessage = "";
        int originalLine = GetOriginalLineNumber(currentStatementIndex);

        try
        {
            // Obtener descripción antes de ejecutar (por si falla)
            logMessage = $"Línea {originalLine}: {GetStatementDescription(statement)}";

            // Ejecutar un solo statement
            ExecuteStatement(statement);
            currentStatementIndex++;

            // Generar los datos de píxeles actuales
            var pixelData = GeneratePixelData();

            return new StepResult
            {
                IsFinished = currentStatementIndex >= flattenedStatements.Count,
                LogMessage = logMessage + " ✓",
                CurrentLine = originalLine - 1, // Ajustar para índice base 0
                PixelData = pixelData,
                WallePosition = new WallePosition
                {
                    X = CurrentX,
                    Y = CurrentY,
                    Color = CurrentColor,
                    Size = BrushSize
                }
            };
        }
        catch (Exception ex)
        {
            // Si hay un error, capturarlo pero permitir continuar con la depuración
            hasError = true;
            currentError = ex.Message;

            // Generar los datos de píxeles actuales (los que se hayan generado hasta el error)
            var pixelData = GeneratePixelData();

            return new StepResult
            {
                IsFinished = true, // Marcamos como finalizado debido al error
                LogMessage = logMessage + $" ❌ Error: {ex.Message}",
                CurrentLine = originalLine - 1,
                PixelData = pixelData,
                Error = ex.Message,
                HasError = true,
                WallePosition = new WallePosition
                {
                    X = CurrentX,
                    Y = CurrentY,
                    Color = CurrentColor,
                    Size = BrushSize
                }
            };
        }
    }

    public StepResult ContinueExecution()
    {
        var lastResult = new StepResult();

        while (!lastResult.IsFinished && !hasError)
        {
            lastResult = StepExecute();

            // Si hay error, detener la ejecución
            if (lastResult.HasError)
            {
                break;
            }
        }

        return lastResult;
    }

    public void Reset()
    {
        currentStatementIndex = 0;
        hasError = false;
        currentError = "";

        // Reinicializar el estado del intérprete
        variables.Clear();
        CurrentX = 0;
        CurrentY = 0;
        CurrentColor = "Transparent";
        BrushSize = 1;
        consoleOutput = "";

        // Reinicializar el canvas
        for (int i = 0; i < canvasSize; i++)
        {
            for (int j = 0; j < canvasSize; j++)
            {
                canvas[i, j] = "White";
            }
        }
    }

    private int GetOriginalLineNumber(int statementIndex)
    {
        if (statementToOriginalLine.ContainsKey(statementIndex))
        {
            return statementToOriginalLine[statementIndex];
        }

        // Si no encontramos el mapeo, usar el número de línea del statement
        if (statementIndex < flattenedStatements.Count)
        {
            return flattenedStatements[statementIndex].Line;
        }

        return 1; // Valor por defecto
    }

    private int GetLastExecutedLine()
    {
        if (currentStatementIndex > 0 && currentStatementIndex <= flattenedStatements.Count)
        {
            return GetOriginalLineNumber(currentStatementIndex - 1);
        }
        return 1;
    }

    private string GetStatementDescription(Statement statement)
    {
        try
        {
            if (statement is AssigmentNode assignmentNode)
            {
                return $"{assignmentNode.VariableName} <- {GetExpressionDescription(assignmentNode.Value)}";
            }
            else if (statement is Instruction instruction)
            {
                string args = string.Join(", ", instruction.Arguments.Select(GetExpressionDescription));
                return $"{instruction.Name}({args})";
            }
            else if (statement is LabelNode labelNode)
            {
                return $"Etiqueta: {labelNode.Name}";
            }
            else if (statement is FunctionCallStatement functionCallStatement)
            {
                string args = string.Join(", ", functionCallStatement.FunctionCall.Arguments.Select(GetExpressionDescription));
                return $"{functionCallStatement.FunctionCall.Name}({args})";
            }

            return statement.GetType().Name;
        }
        catch (Exception)
        {
            return "Statement no reconocido";
        }
    }

    private string GetExpressionDescription(ExpressionNode expression)
    {
        try
        {
            if (expression is NumberNode numberNode)
            {
                return numberNode.Value.ToString();
            }
            else if (expression is VariableNode variableNode)
            {
                return variableNode.Name;
            }
            else if (expression is StringExpression stringExpr)
            {
                return $"\"{stringExpr.Value.Trim('"')}\"";
            }
            else if (expression is BoolLiteralNode boolNode)
            {
                return boolNode.Value ? "true" : "false";
            }
            else if (expression is FunctionCall functionCall)
            {
                string args = string.Join(", ", functionCall.Arguments.Select(GetExpressionDescription));
                return $"{functionCall.Name}({args})";
            }
            else if (expression is AdditiveExpression addExpr)
            {
                if (addExpr.Right != null && addExpr.Operator.HasValue)
                {
                    string op = addExpr.Operator.Value == Tipo.SUM ? "+" : "-";
                    return $"{GetExpressionDescription(addExpr.Left)} {op} {GetExpressionDescription(addExpr.Right)}";
                }
                return GetExpressionDescription(addExpr.Left);
            }
            else if (expression is MultiplicativeExpression multExpr)
            {
                if (multExpr.Right != null && multExpr.Operator.HasValue)
                {
                    string op = multExpr.Operator.Value == Tipo.MULT ? "*" :
                               multExpr.Operator.Value == Tipo.DIV ? "/" : "%";
                    return $"{GetExpressionDescription(multExpr.Left)} {op} {GetExpressionDescription(multExpr.Right)}";
                }
                return GetExpressionDescription(multExpr.Left);
            }
            else if (expression is PowerExpression powExpr)
            {
                if (powExpr.Exponent != null)
                {
                    return $"{GetExpressionDescription(powExpr.Base)} ** {GetExpressionDescription(powExpr.Exponent)}";
                }
                return GetExpressionDescription(powExpr.Base);
            }
            else if (expression is ComparisonExpression compExpr)
            {
                if (compExpr.Right != null && compExpr.Operator.HasValue)
                {
                    string op = compExpr.Operator.Value == Tipo.EQUALS ? "==" :
                               compExpr.Operator.Value == Tipo.GREATER ? ">" :
                               compExpr.Operator.Value == Tipo.LESSER ? "<" :
                               compExpr.Operator.Value == Tipo.GREATER_EQUAL ? ">=" : "<=";
                    return $"{GetExpressionDescription(compExpr.Left)} {op} {GetExpressionDescription(compExpr.Right)}";
                }
                return GetExpressionDescription(compExpr.Left);
            }

            return expression.GetType().Name;
        }
        catch (Exception)
        {
            return "expr";
        }
    }

    public DebugState GetCurrentState()
    {
        return new DebugState
        {
            CurrentStatementIndex = currentStatementIndex,
            TotalStatements = flattenedStatements.Count,
            CurrentLine = currentStatementIndex < flattenedStatements.Count ?
                         GetOriginalLineNumber(currentStatementIndex) : GetLastExecutedLine(),
            Variables = GetVariables(),
            WallePosition = new WallePosition
            {
                X = CurrentX,
                Y = CurrentY,
                Color = CurrentColor,
                Size = BrushSize
            },
            PixelData = GeneratePixelData(),
            HasError = hasError,
            ErrorMessage = currentError,
            IsFinished = currentStatementIndex >= flattenedStatements.Count || hasError
        };
    }

    public bool JumpToLine(int lineNumber)
    {
        for (int i = 0; i < flattenedStatements.Count; i++)
        {
            if (GetOriginalLineNumber(i) == lineNumber)
            {
                currentStatementIndex = i;
                return true;
            }
        }
        return false;
    }

    public List<int> GetExecutableLines()
    {
        var lines = new List<int>();
        for (int i = 0; i < flattenedStatements.Count; i++)
        {
            int lineNumber = GetOriginalLineNumber(i);
            if (!lines.Contains(lineNumber))
            {
                lines.Add(lineNumber);
            }
        }
        return lines.OrderBy(x => x).ToList();
    }
}

public class StepResult
{
    public bool IsFinished { get; set; }
    public string LogMessage { get; set; } = "";
    public int CurrentLine { get; set; }
    public List<PixelData> PixelData { get; set; } = new List<PixelData>();
    public string Error { get; set; } = "";
    public bool HasError { get; set; } = false;
    public WallePosition WallePosition { get; set; } = new WallePosition();
}

public class DebugState
{
    public int CurrentStatementIndex { get; set; }
    public int TotalStatements { get; set; }
    public int CurrentLine { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    public WallePosition WallePosition { get; set; } = new WallePosition();
    public List<PixelData> PixelData { get; set; } = new List<PixelData>();
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = "";
    public bool IsFinished { get; set; }
}

public class WallePosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; } = "Transparent";
    public int Size { get; set; } = 1;
}