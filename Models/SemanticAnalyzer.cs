using System;
using System.Collections.Generic;

public class SemanticAnalyzer
{
    private readonly ProgramNode ast;
    private Dictionary<string, ExpressionNode> variables;
    private Dictionary<string, LabelNode> labels;
    private int canvasSize;

    public string Error { get; private set; }

    public SemanticAnalyzer(ProgramNode ast, int canvasSize)
    {
        this.ast = ast;
        this.canvasSize = canvasSize;
        this.variables = new Dictionary<string, ExpressionNode>();
        this.labels = new Dictionary<string, LabelNode>();
        this.Error = "";
    }

    public bool Analyze()
    {
        try
        {
            // Verificar que el AST no sea nulo
            if (ast == null)
            {
                Error = "Error semántico: El árbol de sintaxis es nulo.";
                return false;
            }

            // Verificar que el programa tenga statements
            if (ast.Statements == null || ast.Statements.Count == 0)
            {
                Error = "Error semántico: El programa está vacío.";
                return false;
            }

            // Primer paso: recolectar todas las etiquetas
            CollectLabels(ast.Statements);

            if (!string.IsNullOrEmpty(Error))
            {
                return false;
            }

            // Segundo paso: verificar que el programa empiece con una instrucción Spawn
            if (ast.Statements.Count > 0 && ast.Statements[0] is Instruction instruction && instruction.Name == "Spawn")
            {
                AnalyzeSpawnInstruction(instruction);
            }
            else
            {
                Error = "Error semántico: El programa debe comenzar con una instrucción Spawn.";
                return false;
            }

            if (!string.IsNullOrEmpty(Error))
            {
                return false;
            }

            // Tercer paso: verificar la semántica de cada statement
            for (int i = 1; i < ast.Statements.Count; i++)
            {
                if (ast.Statements[i] == null)
                {
                    Error = $"Error semántico: La instrucción {i} es nula.";
                    return false;
                }

                AnalyzeStatement(ast.Statements[i]);

                if (!string.IsNullOrEmpty(Error))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Error = $"Error semántico: {ex.Message}";
            return false;
        }
    }

    private void CollectLabels(List<Statement> statements)
    {
        if (statements == null)
        {
            Error = "Error semántico: Lista de declaraciones nula.";
            return;
        }

        foreach (var statement in statements)
        {
            if (statement == null)
            {
                Error = "Error semántico: Declaración nula encontrada.";
                return;
            }

            if (statement is LabelNode labelNode)
            {
                if (string.IsNullOrEmpty(labelNode.Name))
                {
                    Error = $"Error semántico: Etiqueta sin nombre en línea {labelNode.Line}, columna {labelNode.Column}.";
                    return;
                }

                if (labels.ContainsKey(labelNode.Name))
                {
                    Error += $"Error semántico: Etiqueta '{labelNode.Name}' ya definida en línea {labelNode.Line}, columna {labelNode.Column}.\n";
                }
                else
                {
                    labels[labelNode.Name] = labelNode;

                    // Verificar que el programa de la etiqueta no sea nulo
                    if (labelNode.Programa == null)
                    {
                        Error = $"Error semántico: Programa nulo en etiqueta '{labelNode.Name}'.";
                        return;
                    }

                    // Verificar que los statements del programa no sean nulos
                    if (labelNode.Programa.Statements == null)
                    {
                        Error = $"Error semántico: Statements nulos en etiqueta '{labelNode.Name}'.";
                        return;
                    }

                    // Recolectar etiquetas en los statements de la etiqueta
                    CollectLabels(labelNode.Programa.Statements);
                }
            }
        }
    }

    private void AnalyzeStatement(Statement statement)
    {
        if (statement == null)
        {
            Error = "Error semántico: Declaración nula.";
            return;
        }

        if (statement is AssigmentNode assignmentNode)
        {
            AnalyzeAssignment(assignmentNode);
        }
        else if (statement is Instruction instruction)
        {
            AnalyzeInstruction(instruction);
        }
        else if (statement is LabelNode labelNode)
        {
            // Las etiquetas ya fueron recolectadas, ahora analizamos sus statements
            if (labelNode.Programa == null || labelNode.Programa.Statements == null)
            {
                Error = $"Error semántico: Programa inválido en etiqueta '{labelNode.Name}'.";
                return;
            }

            foreach (var innerStatement in labelNode.Programa.Statements)
            {
                if (innerStatement == null)
                {
                    Error = $"Error semántico: Declaración nula en etiqueta '{labelNode.Name}'.";
                    return;
                }

                AnalyzeStatement(innerStatement);

                if (!string.IsNullOrEmpty(Error))
                {
                    return;
                }
            }
        }
        else if (statement is FunctionCallStatement functionCallStatement)
        {
            if (functionCallStatement.FunctionCall == null)
            {
                Error = "Error semántico: Llamada a función nula.";
                return;
            }

            AnalyzeFunctionCall(functionCallStatement.FunctionCall);
        }
        else
        {
            Error = $"Error semántico: Tipo de declaración desconocido: {statement.GetType().Name}.";
        }
    }

    private void AnalyzeAssignment(AssigmentNode assignment)
    {
        if (assignment == null)
        {
            Error = "Error semántico: Asignación nula.";
            return;
        }

        if (string.IsNullOrEmpty(assignment.VariableName))
        {
            Error = $"Error semántico: Nombre de variable vacío en línea {assignment.Line}, columna {assignment.Column}.";
            return;
        }

        if (assignment.Value == null)
        {
            Error = $"Error semántico: Valor nulo en asignación de variable '{assignment.VariableName}' en línea {assignment.Line}, columna {assignment.Column}.";
            return;
        }

        // Verificar que la expresión sea válida
        ExpressionNode expressionValue = AnalyzeExpression(assignment.Value);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        // Guardar el tipo de la variable
        variables[assignment.VariableName] = expressionValue;
    }

    private void AnalyzeInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción nula.";
            return;
        }

        if (string.IsNullOrEmpty(instruction.Name))
        {
            Error = $"Error semántico: Nombre de instrucción vacío en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción '{instruction.Name}' en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        string instructionName = instruction.Name;

        switch (instructionName)
        {
            case "Spawn":
                AnalyzeSpawnInstruction(instruction);
                break;

            case "Color":
                AnalyzeColorInstruction(instruction);
                break;

            case "Size":
                AnalyzeSizeInstruction(instruction);
                break;

            case "DrawLine":
                AnalyzeDrawLineInstruction(instruction);
                break;

            case "DrawCircle":
                AnalyzeDrawCircleInstruction(instruction);
                break;

            case "DrawRectangle":
                AnalyzeDrawRectangleInstruction(instruction);
                break;

            case "Fill":
                AnalyzeFillInstruction(instruction);
                break;

            case "GoTo":
                AnalyzeGoToInstruction(instruction);
                break;

            default:
                Error += $"Error semántico: Instrucción inválida '{instructionName}' en línea {instruction.Line}, columna {instruction.Column}.\n";
                break;
        }
    }

    private void AnalyzeSpawnInstruction(Instruction instruction)
    {
        // Verificaciones iniciales
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción Spawn nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción Spawn en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que tenga exactamente 2 argumentos
        if (instruction.Arguments.Count != 2)
        {
            Error += $"Error semántico: Instrucción Spawn debe tener exactamente 2 argumentos en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        if (instruction.Arguments[0] == null || instruction.Arguments[1] == null)
        {
            Error += $"Error semántico: Argumentos nulos en instrucción Spawn en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que los argumentos sean expresiones numéricas
        ExpressionNode x = AnalyzeExpression(instruction.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode y = AnalyzeExpression(instruction.Arguments[1]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(x is ArithmeticExpression) || !(y is ArithmeticExpression))
        {
            Error += $"Error semántico: Argumentos de Spawn deben ser expresiones numéricas en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Opcionalmente, verificar que las coordenadas estén dentro del canvas si son constantes
        if (x is NumberNode xNode && y is NumberNode yNode)
        {
            if (xNode.Value < 0 || xNode.Value >= canvasSize ||
                yNode.Value < 0 || yNode.Value >= canvasSize)
            {
                Error += $"Error semántico: Coordenadas de Spawn ({xNode.Value}, {yNode.Value}) fuera de los límites del canvas en línea {instruction.Line}, columna {instruction.Column}.\n";
            }
        }
    }

    private void AnalyzeColorInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción Color nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción Color en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que tenga exactamente 1 argumento
        if (instruction.Arguments.Count != 1)
        {
            Error += $"Error semántico: Instrucción Color debe tener exactamente 1 argumento en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que el argumento no sea nulo
        if (instruction.Arguments[0] == null)
        {
            Error += $"Error semántico: Argumento nulo en instrucción Color en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que el argumento sea una expresión de cadena
        ExpressionNode colorArg = AnalyzeExpression(instruction.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(colorArg is StringExpression))
        {
            Error += $"Error semántico: Argumento de Color debe ser una cadena en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que sea un color válido
        StringExpression colorExpr = (StringExpression)colorArg;
        if (colorExpr.Value == null)
        {
            Error += $"Error semántico: Valor de color nulo en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        string color = colorExpr.Value.Trim('"'); // Quitar comillas

        if (!IsValidColor(color))
        {
            Error += $"Error semántico: Color '{color}' no válido en línea {instruction.Line}, columna {instruction.Column}. Colores válidos: Red, Blue, Green, Yellow, Orange, Purple, Black, White, Transparent.\n";
        }
    }

    private bool IsValidColor(string color)
    {
        if (string.IsNullOrEmpty(color))
        {
            return false;
        }

        string[] validColors =
        {
            "Red", "Blue", "Green", "Yellow",
            "Orange", "Purple", "Black", "White", "Transparent"
        };

        return Array.Exists(validColors, c => c == color);
    }

    private void AnalyzeSizeInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción Size nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción Size en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que tenga exactamente 1 argumento
        if (instruction.Arguments.Count != 1)
        {
            Error += $"Error semántico: Instrucción Size debe tener exactamente 1 argumento en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que el argumento no sea nulo
        if (instruction.Arguments[0] == null)
        {
            Error += $"Error semántico: Argumento nulo en instrucción Size en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que el argumento sea una expresión numérica
        ExpressionNode sizeArg = AnalyzeExpression(instruction.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(sizeArg is ArithmeticExpression))
        {
            Error += $"Error semántico: Argumento de Size debe ser una expresión numérica en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que el tamaño sea positivo si es una constante
        if (sizeArg is NumberNode sizeNode && sizeNode.Value <= 0)
        {
            Error += $"Error semántico: Tamaño del pincel debe ser positivo en línea {instruction.Line}, columna {instruction.Column}.\n";
        }
    }

    private void AnalyzeDrawLineInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción DrawLine nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción DrawLine en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que tenga exactamente 3 argumentos
        if (instruction.Arguments.Count != 3)
        {
            Error += $"Error semántico: Instrucción DrawLine debe tener exactamente 3 argumentos en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        for (int i = 0; i < 3; i++)
        {
            if (instruction.Arguments[i] == null)
            {
                Error += $"Error semántico: Argumento {i + 1} nulo en instrucción DrawLine en línea {instruction.Line}, columna {instruction.Column}.\n";
                return;
            }
        }

        // Verificar que los argumentos sean expresiones numéricas
        ExpressionNode dirX = AnalyzeExpression(instruction.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode dirY = AnalyzeExpression(instruction.Arguments[1]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode distance = AnalyzeExpression(instruction.Arguments[2]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(dirX is ArithmeticExpression) || !(dirY is ArithmeticExpression) || !(distance is ArithmeticExpression))
        {
            Error += $"Error semántico: Argumentos de DrawLine deben ser expresiones numéricas en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que las direcciones sean -1, 0 o 1 si son constantes
        if (dirX is NumberNode dirXNode && dirY is NumberNode dirYNode)
        {
            if (dirXNode.Value < -1 || dirXNode.Value > 1 ||
                dirYNode.Value < -1 || dirYNode.Value > 1)
            {
                Error += $"Error semántico: Direcciones para DrawLine deben ser -1, 0 o 1 en línea {instruction.Line}, columna {instruction.Column}.\n";
            }

            // Verificar que no sea (0, 0)
            if (dirXNode.Value == 0 && dirYNode.Value == 0)
            {
                Error += $"Error semántico: Dirección para DrawLine no puede ser (0, 0) en línea {instruction.Line}, columna {instruction.Column}.\n";
            }
        }

        // Verificar que la distancia sea positiva si es una constante
        if (distance is NumberNode distanceNode && distanceNode.Value <= 0)
        {
            Error += $"Error semántico: Distancia para DrawLine debe ser positiva en línea {instruction.Line}, columna {instruction.Column}.\n";
        }
    }

    private void AnalyzeDrawCircleInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción DrawCircle nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción DrawCircle en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que tenga exactamente 3 argumentos
        if (instruction.Arguments.Count != 3)
        {
            Error += $"Error semántico: Instrucción DrawCircle debe tener exactamente 3 argumentos en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        for (int i = 0; i < 3; i++)
        {
            if (instruction.Arguments[i] == null)
            {
                Error += $"Error semántico: Argumento {i + 1} nulo en instrucción DrawCircle en línea {instruction.Line}, columna {instruction.Column}.\n";
                return;
            }
        }

        // Verificar que los argumentos sean expresiones numéricas
        ExpressionNode dirX = AnalyzeExpression(instruction.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode dirY = AnalyzeExpression(instruction.Arguments[1]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode radius = AnalyzeExpression(instruction.Arguments[2]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(dirX is ArithmeticExpression) || !(dirY is ArithmeticExpression) || !(radius is ArithmeticExpression))
        {
            Error += $"Error semántico: Argumentos de DrawCircle deben ser expresiones numéricas en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que las direcciones sean -1, 0 o 1 si son constantes
        if (dirX is NumberNode dirXNode && dirY is NumberNode dirYNode)
        {
            if (dirXNode.Value < -1 || dirXNode.Value > 1 ||
                dirYNode.Value < -1 || dirYNode.Value > 1)
            {
                Error += $"Error semántico: Direcciones para DrawCircle deben ser -1, 0 o 1 en línea {instruction.Line}, columna {instruction.Column}.\n";
            }

            // Verificar que no sea (0, 0)
            if (dirXNode.Value == 0 && dirYNode.Value == 0)
            {
                Error += $"Error semántico: Dirección para DrawCircle no puede ser (0, 0) en línea {instruction.Line}, columna {instruction.Column}.\n";
            }
        }

        // Verificar que el radio sea positivo si es una constante
        if (radius is NumberNode radiusNode && radiusNode.Value <= 0)
        {
            Error += $"Error semántico: Radio para DrawCircle debe ser positivo en línea {instruction.Line}, columna {instruction.Column}.\n";
        }
    }

    private void AnalyzeDrawRectangleInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción DrawRectangle nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción DrawRectangle en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que tenga exactamente 5 argumentos
        if (instruction.Arguments.Count != 5)
        {
            Error += $"Error semántico: Instrucción DrawRectangle debe tener exactamente 5 argumentos en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        for (int i = 0; i < 5; i++)
        {
            if (instruction.Arguments[i] == null)
            {
                Error += $"Error semántico: Argumento {i + 1} nulo en instrucción DrawRectangle en línea {instruction.Line}, columna {instruction.Column}.\n";
                return;
            }
        }

        // Verificar que los argumentos sean expresiones numéricas
        ExpressionNode dirX = AnalyzeExpression(instruction.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode dirY = AnalyzeExpression(instruction.Arguments[1]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode distance = AnalyzeExpression(instruction.Arguments[2]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode width = AnalyzeExpression(instruction.Arguments[3]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode height = AnalyzeExpression(instruction.Arguments[4]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(dirX is ArithmeticExpression) || !(dirY is ArithmeticExpression) ||
            !(distance is ArithmeticExpression) || !(width is ArithmeticExpression) ||
            !(height is ArithmeticExpression))
        {
            Error += $"Error semántico: Argumentos de DrawRectangle deben ser expresiones numéricas en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que las direcciones sean -1, 0 o 1 si son constantes
        if (dirX is NumberNode dirXNode && dirY is NumberNode dirYNode)
        {
            if (dirXNode.Value < -1 || dirXNode.Value > 1 ||
                dirYNode.Value < -1 || dirYNode.Value > 1)
            {
                Error += $"Error semántico: Direcciones para DrawRectangle deben ser -1, 0 o 1 en línea {instruction.Line}, columna {instruction.Column}.\n";
            }

            // Verificar que no sea (0, 0)
            if (dirXNode.Value == 0 && dirYNode.Value == 0)
            {
                Error += $"Error semántico: Dirección para DrawRectangle no puede ser (0, 0) en línea {instruction.Line}, columna {instruction.Column}.\n";
            }
        }

        // Verificar que distancia, ancho y alto sean positivos si son constantes
        if (distance is NumberNode distanceNode && distanceNode.Value <= 0)
        {
            Error += $"Error semántico: Distancia para DrawRectangle debe ser positiva en línea {instruction.Line}, columna {instruction.Column}.\n";
        }

        if (width is NumberNode widthNode && widthNode.Value <= 0)
        {
            Error += $"Error semántico: Ancho para DrawRectangle debe ser positivo en línea {instruction.Line}, columna {instruction.Column}.\n";
        }

        if (height is NumberNode heightNode && heightNode.Value <= 0)
        {
            Error += $"Error semántico: Alto para DrawRectangle debe ser positivo en línea {instruction.Line}, columna {instruction.Column}.\n";
        }
    }

    private void AnalyzeFillInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción Fill nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción Fill en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Verificar que no tenga argumentos
        if (instruction.Arguments.Count != 0)
        {
            Error += $"Error semántico: Instrucción Fill no debe tener argumentos en línea {instruction.Line}, columna {instruction.Column}.\n";
        }
    }

    private void AnalyzeGoToInstruction(Instruction instruction)
    {
        if (instruction == null)
        {
            Error = "Error semántico: Instrucción GoTo nula.";
            return;
        }

        if (instruction.Arguments == null)
        {
            Error = $"Error semántico: Argumentos nulos en instrucción GoTo en línea {instruction.Line}, columna {instruction.Column}.";
            return;
        }

        // Debe tener exactamente 2 argumentos: la etiqueta y la condición
        if (instruction.Arguments.Count != 2)
        {
            Error += $"Error semántico: Instrucción GoTo debe tener exactamente 2 argumentos en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        for (int i = 0; i < 2; i++)
        {
            if (instruction.Arguments[i] == null)
            {
                Error += $"Error semántico: Argumento {i + 1} nulo en instrucción GoTo en línea {instruction.Line}, columna {instruction.Column}.\n";
                return;
            }
        }

        // El primer argumento debe ser una cadena con el nombre de la etiqueta
        if (!(instruction.Arguments[0] is StringExpression labelExpr))
        {
            Error += $"Error semántico: El primer argumento de GoTo debe ser el nombre de la etiqueta en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        if (labelExpr.Value == null)
        {
            Error += $"Error semántico: Nombre de etiqueta nulo en GoTo en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        string labelName = labelExpr.Value.Trim('"'); // Quitar comillas

        // Verificar que la etiqueta exista
        if (!labels.ContainsKey(labelName))
        {
            Error += $"Error semántico: Etiqueta '{labelName}' no definida en línea {instruction.Line}, columna {instruction.Column}.\n";
            return;
        }

        // El segundo argumento debe ser una expresión booleana
        ExpressionNode condition = AnalyzeExpression(instruction.Arguments[1]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(condition is BooleanExpression))
        {
            Error += $"Error semántico: El segundo argumento de GoTo debe ser una expresión booleana en línea {instruction.Line}, columna {instruction.Column}.\n";
        }
    }

    private ExpressionNode AnalyzeExpression(ExpressionNode expression)
    {
        if (expression == null)
        {
            Error = "Error semántico: Expresión nula.";
            return null;
        }

        if (expression is StringExpression)
        {
            return expression; // Las expresiones de cadena son válidas tal cual
        }
        else if (expression is ArithmeticExpression)
        {
            return AnalyzeArithmeticExpression((ArithmeticExpression)expression);
        }
        else if (expression is BooleanExpression)
        {
            return AnalyzeBooleanExpression((BooleanExpression)expression);
        }
        else if (expression is BoolLiteralNode)
        {
            return expression; // Los literales booleanos son válidos tal cual
        }

        Error += $"Error semántico: Tipo de expresión no reconocido ({expression.GetType().Name}) en línea {expression.Line}, columna {expression.Column}.\n";
        return expression;
    }

    private ArithmeticExpression AnalyzeArithmeticExpression(ArithmeticExpression expression)
    {
        if (expression == null)
        {
            Error = "Error semántico: Expresión aritmética nula.";
            return null;
        }

        if (expression is NumberNode)
        {
            return expression; // Los números son válidos tal cual
        }
        else if (expression is VariableNode variableNode)
        {
            if (string.IsNullOrEmpty(variableNode.Name))
            {
                Error += $"Error semántico: Nombre de variable vacío en línea {variableNode.Line}, columna {variableNode.Column}.\n";
                return expression;
            }

            // Verificar que la variable esté definida
            if (!variables.ContainsKey(variableNode.Name))
            {
                Error += $"Error semántico: Variable '{variableNode.Name}' no definida en línea {variableNode.Line}, columna {variableNode.Column}.\n";
                return expression;
            }

            // Verificar que la variable sea de tipo numérico
            ExpressionNode varValue = variables[variableNode.Name];

            if (varValue == null)
            {
                Error += $"Error semántico: Valor nulo para variable '{variableNode.Name}' en línea {variableNode.Line}, columna {variableNode.Column}.\n";
                return expression;
            }

            if (!(varValue is ArithmeticExpression))
            {
                Error += $"Error semántico: Variable '{variableNode.Name}' no es de tipo numérico en línea {variableNode.Line}, columna {variableNode.Column}.\n";
            }

            return expression;
        }
        else if (expression is FunctionCall functionCall)
        {
            return AnalyzeFunctionCall(functionCall);
        }
        else if (expression is AdditiveExpression additiveExpr)
        {
            if (additiveExpr.Left == null)
            {
                Error += $"Error semántico: Operando izquierdo nulo en expresión aditiva en línea {additiveExpr.Line}, columna {additiveExpr.Column}.\n";
                return expression;
            }

            AnalyzeArithmeticExpression(additiveExpr.Left);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            if (additiveExpr.Right != null)
            {
                AnalyzeArithmeticExpression(additiveExpr.Right);

                if (!string.IsNullOrEmpty(Error))
                {
                    return expression;
                }
            }

            return expression;
        }
        else if (expression is MultiplicativeExpression multExpr)
        {
            if (multExpr.Left == null)
            {
                Error += $"Error semántico: Operando izquierdo nulo en expresión multiplicativa en línea {multExpr.Line}, columna {multExpr.Column}.\n";
                return expression;
            }

            AnalyzeArithmeticExpression(multExpr.Left);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            if (multExpr.Right != null)
            {
                AnalyzeArithmeticExpression(multExpr.Right);

                if (!string.IsNullOrEmpty(Error))
                {
                    return expression;
                }
            }

            return expression;
        }
        else if (expression is PowerExpression powerExpr)
        {
            if (powerExpr.Base == null)
            {
                Error += $"Error semántico: Base nula en expresión de potencia en línea {powerExpr.Line}, columna {powerExpr.Column}.\n";
                return expression;
            }

            AnalyzeArithmeticExpression(powerExpr.Base);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            if (powerExpr.Exponent != null)
            {
                AnalyzeArithmeticExpression(powerExpr.Exponent);

                if (!string.IsNullOrEmpty(Error))
                {
                    return expression;
                }
            }

            return expression;
        }
        else if (expression is UnaryExpressionNode unaryExpr)
        {
            if (unaryExpr.Module == null)
            {
                Error += $"Error semántico: Módulo nulo en expresión unaria en línea {unaryExpr.Line}, columna {unaryExpr.Column}.\n";
                return expression;
            }

            AnalyzeArithmeticExpression(unaryExpr.Module);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            return expression;
        }
        else if (expression is ParenthesizedExpression parenExpr)
        {
            if (parenExpr.Expression == null)
            {
                Error += $"Error semántico: Expresión nula dentro de paréntesis en línea {parenExpr.Line}, columna {parenExpr.Column}.\n";
                return expression;
            }

            AnalyzeExpression(parenExpr.Expression);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            return expression;
        }

        Error += $"Error semántico: Tipo de expresión aritmética no reconocido ({expression.GetType().Name}) en línea {expression.Line}, columna {expression.Column}.\n";
        return expression;
    }

    private BooleanExpression AnalyzeBooleanExpression(BooleanExpression expression)
    {
        if (expression == null)
        {
            Error = "Error semántico: Expresión booleana nula.";
            return null;
        }

        if (expression is OrExpression orExpr)
        {
            if (orExpr.Left == null)
            {
                Error += $"Error semántico: Operando izquierdo nulo en expresión OR en línea {orExpr.Line}, columna {orExpr.Column}.\n";
                return expression;
            }

            AnalyzeBooleanExpression(orExpr.Left);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            if (orExpr.Right != null)
            {
                AnalyzeBooleanExpression(orExpr.Right);

                if (!string.IsNullOrEmpty(Error))
                {
                    return expression;
                }
            }

            return expression;
        }
        else if (expression is AndExpression andExpr)
        {
            if (andExpr.Left == null)
            {
                Error += $"Error semántico: Operando izquierdo nulo en expresión AND en línea {andExpr.Line}, columna {andExpr.Column}.\n";
                return expression;
            }

            AnalyzeBooleanExpression(andExpr.Left);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            if (andExpr.Right != null)
            {
                AnalyzeBooleanExpression(andExpr.Right);

                if (!string.IsNullOrEmpty(Error))
                {
                    return expression;
                }
            }

            return expression;
        }
        else if (expression is ComparisonExpression compExpr)
        {
            if (compExpr.Left == null)
            {
                Error += $"Error semántico: Operando izquierdo nulo en expresión de comparación en línea {compExpr.Line}, columna {compExpr.Column}.\n";
                return expression;
            }

            AnalyzeExpression(compExpr.Left);

            if (!string.IsNullOrEmpty(Error))
            {
                return expression;
            }

            if (compExpr.Right != null && compExpr.Operator.HasValue)
            {
                AnalyzeExpression(compExpr.Right);

                if (!string.IsNullOrEmpty(Error))
                {
                    return expression;
                }
            }

            return expression;
        }
        else if (expression is BoolLiteralNode)
        {
            return expression; // Los literales booleanos son válidos tal cual
        }

        Error += $"Error semántico: Tipo de expresión booleana no reconocido ({expression.GetType().Name}) en línea {expression.Line}, columna {expression.Column}.\n";
        return expression;
    }

    private ArithmeticExpression AnalyzeFunctionCall(FunctionCall functionCall)
    {
        if (functionCall == null)
        {
            Error = "Error semántico: Llamada a función nula.";
            return null;
        }

        if (string.IsNullOrEmpty(functionCall.Name))
        {
            Error += $"Error semántico: Nombre de función vacío en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return functionCall;
        }

        if (functionCall.Arguments == null)
        {
            Error += $"Error semántico: Argumentos nulos en llamada a función '{functionCall.Name}' en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return functionCall;
        }

        string functionName = functionCall.Name;

        switch (functionName)
        {
            case "GetActualX":
                VerifyArgumentCount(functionCall, 0);
                break;

            case "GetActualY":
                VerifyArgumentCount(functionCall, 0);
                break;

            case "GetCanvasSize":
                VerifyArgumentCount(functionCall, 0);
                break;

            case "GetColorCount":
                AnalyzeGetColorCount(functionCall);
                break;

            case "IsBrushColor":
                AnalyzeIsBrushColor(functionCall);
                break;

            case "IsBrushSize":
                AnalyzeIsBrushSize(functionCall);
                break;

            case "IsCanvasColor":
                AnalyzeIsCanvasColor(functionCall);
                break;

            default:
                Error += $"Error semántico: Función desconocida '{functionName}' en línea {functionCall.Line}, columna {functionCall.Column}.\n";
                break;
        }

        return functionCall;
    }

    private void VerifyArgumentCount(FunctionCall functionCall, int expectedCount)
    {
        if (functionCall == null)
        {
            Error = "Error semántico: Llamada a función nula.";
            return;
        }

        if (functionCall.Arguments == null)
        {
            Error += $"Error semántico: Argumentos nulos en función {GetFunctionName(functionCall)} en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        if (functionCall.Arguments.Count != expectedCount)
        {
            Error += $"Error semántico: Función {GetFunctionName(functionCall)} debe tener exactamente {expectedCount} argumentos en línea {functionCall.Line}, columna {functionCall.Column}.\n";
        }
    }

    private string GetFunctionName(FunctionCall functionCall)
    {
        return functionCall?.Name ?? "desconocida";
    }

    private void AnalyzeGetColorCount(FunctionCall functionCall)
    {
        if (functionCall == null)
        {
            Error = "Error semántico: Llamada a función GetColorCount nula.";
            return;
        }

        if (functionCall.Arguments == null)
        {
            Error += $"Error semántico: Argumentos nulos en función GetColorCount en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que tenga exactamente 5 argumentos
        if (functionCall.Arguments.Count != 5)
        {
            Error += $"Error semántico: Función GetColorCount debe tener exactamente 5 argumentos en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        for (int i = 0; i < 5; i++)
        {
            if (functionCall.Arguments[i] == null)
            {
                Error += $"Error semántico: Argumento {i + 1} nulo en función GetColorCount en línea {functionCall.Line}, columna {functionCall.Column}.\n";
                return;
            }
        }

        // Verificar que el primer argumento sea una cadena (color)
        if (!(functionCall.Arguments[0] is StringExpression))
        {
            Error += $"Error semántico: El primer argumento de GetColorCount debe ser una cadena (color) en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el color sea válido
        StringExpression colorExpr = (StringExpression)functionCall.Arguments[0];

        if (colorExpr.Value == null)
        {
            Error += $"Error semántico: Valor de color nulo en función GetColorCount en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        string color = colorExpr.Value.Trim('"'); // Quitar comillas

        if (!IsValidColor(color))
        {
            Error += $"Error semántico: Color '{color}' no válido en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que los otros 4 argumentos sean expresiones numéricas
        for (int i = 1; i <= 4; i++)
        {
            ExpressionNode arg = AnalyzeExpression(functionCall.Arguments[i]);

            if (!string.IsNullOrEmpty(Error))
            {
                return;
            }

            if (!(arg is ArithmeticExpression))
            {
                Error += $"Error semántico: El argumento {i + 1} de GetColorCount debe ser una expresión numérica en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            }
        }
    }

    private void AnalyzeIsBrushColor(FunctionCall functionCall)
    {
        if (functionCall == null)
        {
            Error = "Error semántico: Llamada a función IsBrushColor nula.";
            return;
        }

        if (functionCall.Arguments == null)
        {
            Error += $"Error semántico: Argumentos nulos en función IsBrushColor en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que tenga exactamente 1 argumento
        if (functionCall.Arguments.Count != 1)
        {
            Error += $"Error semántico: Función IsBrushColor debe tener exactamente 1 argumento en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el argumento no sea nulo
        if (functionCall.Arguments[0] == null)
        {
            Error += $"Error semántico: Argumento nulo en función IsBrushColor en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el argumento sea una cadena (color)
        if (!(functionCall.Arguments[0] is StringExpression))
        {
            Error += $"Error semántico: El argumento de IsBrushColor debe ser una cadena (color) en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el color sea válido
        StringExpression colorExpr = (StringExpression)functionCall.Arguments[0];

        if (colorExpr.Value == null)
        {
            Error += $"Error semántico: Valor de color nulo en función IsBrushColor en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        string color = colorExpr.Value.Trim('"'); // Quitar comillas

        if (!IsValidColor(color))
        {
            Error += $"Error semántico: Color '{color}' no válido en línea {functionCall.Line}, columna {functionCall.Column}.\n";
        }
    }

    private void AnalyzeIsBrushSize(FunctionCall functionCall)
    {
        if (functionCall == null)
        {
            Error = "Error semántico: Llamada a función IsBrushSize nula.";
            return;
        }

        if (functionCall.Arguments == null)
        {
            Error += $"Error semántico: Argumentos nulos en función IsBrushSize en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que tenga exactamente 1 argumento
        if (functionCall.Arguments.Count != 1)
        {
            Error += $"Error semántico: Función IsBrushSize debe tener exactamente 1 argumento en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el argumento no sea nulo
        if (functionCall.Arguments[0] == null)
        {
            Error += $"Error semántico: Argumento nulo en función IsBrushSize en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el argumento sea una expresión numérica
        ExpressionNode sizeArg = AnalyzeExpression(functionCall.Arguments[0]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(sizeArg is ArithmeticExpression))
        {
            Error += $"Error semántico: El argumento de IsBrushSize debe ser una expresión numérica en línea {functionCall.Line}, columna {functionCall.Column}.\n";
        }
    }

    private void AnalyzeIsCanvasColor(FunctionCall functionCall)
    {
        if (functionCall == null)
        {
            Error = "Error semántico: Llamada a función IsCanvasColor nula.";
            return;
        }

        if (functionCall.Arguments == null)
        {
            Error += $"Error semántico: Argumentos nulos en función IsCanvasColor en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que tenga exactamente 3 argumentos
        if (functionCall.Arguments.Count != 3)
        {
            Error += $"Error semántico: Función IsCanvasColor debe tener exactamente 3 argumentos en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que los argumentos no sean nulos
        for (int i = 0; i < 3; i++)
        {
            if (functionCall.Arguments[i] == null)
            {
                Error += $"Error semántico: Argumento {i + 1} nulo en función IsCanvasColor en línea {functionCall.Line}, columna {functionCall.Column}.\n";
                return;
            }
        }

        // Verificar que el primer argumento sea una cadena (color)
        if (!(functionCall.Arguments[0] is StringExpression))
        {
            Error += $"Error semántico: El primer argumento de IsCanvasColor debe ser una cadena (color) en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que el color sea válido
        StringExpression colorExpr = (StringExpression)functionCall.Arguments[0];

        if (colorExpr.Value == null)
        {
            Error += $"Error semántico: Valor de color nulo en función IsCanvasColor en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        string color = colorExpr.Value.Trim('"'); // Quitar comillas

        if (!IsValidColor(color))
        {
            Error += $"Error semántico: Color '{color}' no válido en línea {functionCall.Line}, columna {functionCall.Column}.\n";
            return;
        }

        // Verificar que los otros 2 argumentos sean expresiones numéricas
        ExpressionNode verticalArg = AnalyzeExpression(functionCall.Arguments[1]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        ExpressionNode horizontalArg = AnalyzeExpression(functionCall.Arguments[2]);

        if (!string.IsNullOrEmpty(Error))
        {
            return;
        }

        if (!(verticalArg is ArithmeticExpression) || !(horizontalArg is ArithmeticExpression))
        {
            Error += $"Error semántico: Los argumentos 2 y 3 de IsCanvasColor deben ser expresiones numéricas en línea {functionCall.Line}, columna {functionCall.Column}.\n";
        }
    }
}