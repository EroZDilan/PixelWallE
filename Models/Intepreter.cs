using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

public class Interpreter
{
    protected readonly ProgramNode ast;
    protected readonly int canvasSize;

    protected List<Statement> flattenedStatements;
    protected Dictionary<string, int> labelToIndex;
    protected Dictionary<string, object> variables;
    protected Dictionary<string, LabelNode> labels;
    protected string[,] canvas;

    // Estado público de Wall-E
    public int CurrentX { get; protected set; }
    public int CurrentY { get; protected set; }
    public string CurrentColor { get; protected set; }
    public int BrushSize { get; protected set; }

    protected string consoleOutput;
    protected List<PixelData> pixelData;

    // Control de flujo para GoTo
    protected bool shouldJump;
    protected string jumpLabel;
    protected Stack<ExecutionContext> executionStack;
    protected Dictionary<string, int> labelIndexes = new Dictionary<string, int>();

    // Para mantener el estado parcial en caso de error
    protected bool hasError = false;
    protected string errorMessage = "";

    public Interpreter(ProgramNode ast, int canvasSize)
    {
        this.ast = ast;
        this.canvasSize = canvasSize;
        this.variables = new Dictionary<string, object>();
        this.labels = new Dictionary<string, LabelNode>();
        this.canvas = new string[canvasSize, canvasSize];
        this.CurrentX = 0;
        this.CurrentY = 0;
        this.CurrentColor = "Transparent";
        this.BrushSize = 1;
        this.consoleOutput = "";
        this.pixelData = new List<PixelData>();
        this.shouldJump = false;
        this.jumpLabel = "";


        // Inicializar el canvas con color blanco
        InitializeCanvas();

        this.flattenedStatements = new List<Statement>();
        this.labelToIndex = new Dictionary<string, int>();

        FlattenAndMapStatements(ast.Statements);

        DebugPrintInternalState();


    }



    private void InitializeCanvas()
    {
        for (int i = 0; i < canvasSize; i++)
        {
            for (int j = 0; j < canvasSize; j++)
            {
                canvas[i, j] = "White";
            }
        }
    }
    private void FlattenAndMapStatements(List<Statement> statements)
    {
        Console.WriteLine($"DEBUG: Procesando {statements?.Count ?? 0} statements");

        if (statements == null)
        {
            Console.WriteLine("DEBUG: Lista de statements es null");
            return;
        }

        foreach (var statement in statements)
        {
            Console.WriteLine($"DEBUG: Procesando {statement.GetType().Name} en línea {statement.Line}");

            if (statement is LabelNode labelNode)
            {
                Console.WriteLine($"DEBUG: Encontrada etiqueta '{labelNode.Name}' -> mapeando a índice {flattenedStatements.Count}");

                // Mapear la etiqueta al índice actual
                labelToIndex[labelNode.Name] = flattenedStatements.Count;

                // Procesar statements internos de la etiqueta
                if (labelNode.Programa?.Statements != null)
                {
                    Console.WriteLine($"DEBUG: Etiqueta '{labelNode.Name}' tiene {labelNode.Programa.Statements.Count} statements internos");
                    FlattenAndMapStatements(labelNode.Programa.Statements);
                }
                else
                {
                    Console.WriteLine($"DEBUG: Etiqueta '{labelNode.Name}' no tiene statements internos");
                }
            }
            else
            {
                Console.WriteLine($"DEBUG: Agregando statement {statement.GetType().Name} al índice {flattenedStatements.Count}");
                flattenedStatements.Add(statement);
            }
        }
    }

    private void DebugPrintInternalState()
    {
        Console.WriteLine("=== DEBUG: Estado Interno del Intérprete ===");

        Console.WriteLine($"\nTotal de statements aplanados: {flattenedStatements?.Count ?? 0}");

        if (flattenedStatements != null)
        {
            for (int i = 0; i < flattenedStatements.Count; i++)
            {
                var stmt = flattenedStatements[i];
                Console.WriteLine($"[{i}] {stmt.GetType().Name} (Línea {stmt.Line}, Columna {stmt.Column})");

                // Mostrar detalles adicionales para diferentes tipos de statements
                if (stmt is AssigmentNode assignment)
                {
                    Console.WriteLine($"    -> Variable: {assignment.VariableName}");
                }
                else if (stmt is Instruction instruction)
                {
                    Console.WriteLine($"    -> Instrucción: {instruction.Name}");
                }
            }
        }

        Console.WriteLine($"\nEtiquetas mapeadas: {labelToIndex?.Count ?? 0}");
        if (labelToIndex != null)
        {
            foreach (var kvp in labelToIndex)
            {
                Console.WriteLine($"  '{kvp.Key}' -> índice {kvp.Value}");
            }
        }

        Console.WriteLine("=== FIN DEBUG ===\n");
    }
    public Dictionary<string, object> GetVariables()
    {
        return new Dictionary<string, object>(variables);
    }




    public ExecutionResult Execute()
    {
        try
        {
            int currentIndex = 0;
            int maxExecutionSteps = 10000;
            int executionSteps = 0;

            // Usar la estructura ya aplanada que preparamos en el constructor
            while (currentIndex < flattenedStatements.Count && !hasError && executionSteps < maxExecutionSteps)
            {
                executionSteps++;
                Statement statement = flattenedStatements[currentIndex];

                try
                {
                    ExecuteStatement(statement);

                    // Manejar saltos usando nuestro diccionario pre-construido
                    if (shouldJump)
                    {
                        if (labelToIndex.ContainsKey(jumpLabel))
                        {
                            currentIndex = labelToIndex[jumpLabel];
                            shouldJump = false;
                            jumpLabel = "";
                            continue;
                        }
                        else
                        {
                            throw new Exception($"Etiqueta no encontrada: {jumpLabel}");
                        }
                    }

                    currentIndex++;
                }
                catch (Exception ex)
                {
                    hasError = true;
                    errorMessage = $"Error en línea {statement.Line}, columna {statement.Column}: {ex.Message}";
                    consoleOutput += $"\n{errorMessage}\n";
                }
            }

            // Verificar si se detuvo por límite de ejecución
            if (executionSteps >= maxExecutionSteps)
            {
                consoleOutput += "\nAdvertencia: Ejecución detenida por límite de seguridad. Posible loop infinito.\n";
            }

            var resultPixelData = GeneratePixelData();

            if (hasError)
            {
                consoleOutput += $"\nEjecución detenida por error. Se dibujaron {resultPixelData.Count} píxeles hasta el momento del error.\n";
            }
            else
            {
                consoleOutput += $"\nEjecución completa. Se generaron {resultPixelData.Count} píxeles.\n";
            }

            return new ExecutionResult
            {
                PixelData = resultPixelData,
                ConsoleOutput = consoleOutput,
                ErrorMessage = hasError ? errorMessage : ""
            };
        }
        catch (Exception ex)
        {
            consoleOutput += $"Error de ejecución: {ex.Message}\n";

            return new ExecutionResult
            {
                PixelData = GeneratePixelData(),
                ConsoleOutput = consoleOutput,
                ErrorMessage = ex.Message
            };
        }
    }
    // Método auxiliar para encontrar el índice de un label
    private int FindLabelIndex(string labelName, List<Statement> statements)
    {
        for (int i = 0; i < statements.Count; i++)
        {
            if (statements[i] is LabelNode label && label.Name == labelName)
            {
                return i;
            }
        }
        return -1; // No encontrado
    }

    public List<PixelData> GeneratePixelData()
    {
        var result = new List<PixelData>();

        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                if (canvas[y, x] != "White" && canvas[y, x] != "Transparent")
                {
                    result.Add(new PixelData
                    {
                        X = x,
                        Y = y,
                        Color = canvas[y, x]
                    });
                }
            }
        }

        return result;
    }

    protected void ExecuteStatement(Statement statement)
    {
        switch (statement)
        {
            case AssigmentNode assignmentNode:
                ExecuteAssignment(assignmentNode);
                break;
            case Instruction instruction:
                ExecuteInstruction(instruction);
                break;
            case LabelNode:
                // Las etiquetas no se ejecutan directamente en el flujo secuencial
                break;
            case FunctionCallStatement functionCallStatement:
                EvaluateExpression(functionCallStatement.FunctionCall);
                break;
            default:
                throw new Exception($"Tipo de statement no reconocido: {statement.GetType().Name}");
        }
    }
    protected void ExecuteAssignment(AssigmentNode assignment)
    {
        object value = EvaluateExpression(assignment.Value);
        variables[assignment.VariableName] = value;
    }

    protected void ExecuteInstruction(Instruction instruction)
    {
        switch (instruction.Name)
        {
            case "Spawn":
                ExecuteSpawn(instruction);
                break;
            case "Color":
                ExecuteColor(instruction);
                break;
            case "Size":
                ExecuteSize(instruction);
                break;
            case "DrawLine":
                ExecuteDrawLine(instruction);
                break;
            case "DrawCircle":
                ExecuteDrawCircle(instruction);
                break;
            case "DrawRectangle":
                ExecuteDrawRectangle(instruction);
                break;
            case "Fill":
                ExecuteFill(instruction);
                break;
            case "GoTo":
                ExecuteGoTo(instruction);
                break;
            default:
                throw new Exception($"Instrucción desconocida: {instruction.Name}");
        }
    }

    #region Instrucciones específicas

    protected void ExecuteSpawn(Instruction instruction)
    {
        if (instruction.Arguments.Count != 2)
        {
            throw new Exception("Spawn requiere exactamente 2 argumentos");
        }

        int x = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));
        int y = Convert.ToInt32(EvaluateExpression(instruction.Arguments[1]));

        if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize)
        {
            throw new Exception($"Coordenadas de Spawn ({x}, {y}) fuera de los límites del canvas");
        }

        CurrentX = x;
        CurrentY = y;
    }

    protected void ExecuteColor(Instruction instruction)
    {
        if (instruction.Arguments.Count != 1)
        {
            throw new Exception("Color requiere exactamente 1 argumento");
        }

        string color = EvaluateExpression(instruction.Arguments[0]).ToString().Trim('"');

        if (!IsValidColor(color))
        {
            throw new Exception($"Color no válido: {color}");
        }

        CurrentColor = color;
    }

    protected void ExecuteSize(Instruction instruction)
    {
        if (instruction.Arguments.Count != 1)
        {
            throw new Exception("Size requiere exactamente 1 argumento");
        }

        int size = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));

        if (size <= 0)
        {
            throw new Exception("El tamaño del pincel debe ser positivo");
        }

        // Si es par, usar el impar inmediatamente menor
        if (size % 2 == 0)
        {
            size--;
        }

        BrushSize = size;
    }

    protected void ExecuteDrawLine(Instruction instruction)
    {
        if (instruction.Arguments.Count != 3)
        {
            throw new Exception("DrawLine requiere exactamente 3 argumentos");
        }

        int dirX = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));
        int dirY = Convert.ToInt32(EvaluateExpression(instruction.Arguments[1]));
        int distance = Convert.ToInt32(EvaluateExpression(instruction.Arguments[2]));

        ValidateDirection(dirX, dirY, "DrawLine");

        if (distance <= 0)
        {
            throw new Exception("La distancia para DrawLine debe ser positiva");
        }

        DrawLine(dirX, dirY, distance);
    }



    protected void ExecuteDrawCircle(Instruction instruction)
    {
        if (instruction.Arguments.Count != 3)
        {
            throw new Exception("DrawCircle requiere exactamente 3 argumentos");
        }

        int dirX = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));
        int dirY = Convert.ToInt32(EvaluateExpression(instruction.Arguments[1]));
        int radius = Convert.ToInt32(EvaluateExpression(instruction.Arguments[2]));

        ValidateDirection(dirX, dirY, "DrawCircle");



        if (radius <= 0)
        {
            throw new Exception("El radio para DrawCircle debe ser positivo");
        }

        // Calcular el centro del círculo
        int centerX = CurrentX + dirX * radius;
        int centerY = CurrentY + dirY * radius;

        if (centerX < 0 || centerX >= canvasSize || centerY < 0 || centerY >= canvasSize)
        {
            throw new Exception($"El centro del círculo estaría fuera del canvas en ({centerX}, {centerY}). Canvas size: {canvasSize}x{canvasSize}");
        }


        DrawCircle(centerX, centerY, radius);

        // Actualizar la posición actual de Wall-E
        CurrentX = centerX;
        CurrentY = centerY;
    }

    protected void ExecuteDrawRectangle(Instruction instruction)
    {
        if (instruction.Arguments.Count != 5)
        {
            throw new Exception("DrawRectangle requiere exactamente 5 argumentos");
        }

        int dirX = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));
        int dirY = Convert.ToInt32(EvaluateExpression(instruction.Arguments[1]));
        int distance = Convert.ToInt32(EvaluateExpression(instruction.Arguments[2]));
        int width = Convert.ToInt32(EvaluateExpression(instruction.Arguments[3]));
        int height = Convert.ToInt32(EvaluateExpression(instruction.Arguments[4]));

        ValidateDirection(dirX, dirY, "DrawRectangle");

        if (distance <= 0 || width <= 0 || height <= 0)
        {
            throw new Exception("La distancia, ancho y alto para DrawRectangle deben ser positivos");
        }

        // Calcular el centro del rectángulo
        int centerX = CurrentX + dirX * distance;
        int centerY = CurrentY + dirY * distance;

        if (centerX < 0 || centerX >= canvasSize || centerY < 0 || centerY >= canvasSize)
        {
            throw new Exception($"El centro del círculo estaría fuera del canvas en ({centerX}, {centerY}). Canvas size: {canvasSize}x{canvasSize}");
        }


        DrawRectangle(centerX, centerY, width, height);

        // Actualizar la posición actual de Wall-E
        CurrentX = centerX;
        CurrentY = centerY;
    }

    protected void ExecuteFill(Instruction instruction)
    {
        if (instruction.Arguments.Count != 0)
        {
            throw new Exception("Fill no debe tener argumentos");
        }

        if (CurrentColor == "Transparent")
        {
            return;
        }

        // Validar posición actual
        if (CurrentX < 0 || CurrentX >= canvasSize || CurrentY < 0 || CurrentY >= canvasSize)
        {
            return;
        }

        string targetColor = canvas[CurrentY, CurrentX];

        // No hacer nada si el color objetivo es igual al color actual
        if (targetColor == CurrentColor)
        {
            return;
        }

        // Algoritmo de flood fill iterativo para evitar desbordamiento de pila
        FloodFillIterative(CurrentX, CurrentY, targetColor);
    }

    protected void ExecuteGoTo(Instruction instruction)
    {
        if (instruction.Arguments.Count != 2)
        {
            throw new Exception("GoTo requiere exactamente 2 argumentos");
        }

        string labelName = EvaluateExpression(instruction.Arguments[0]).ToString().Trim('"');
        bool condition = Convert.ToBoolean(EvaluateExpression(instruction.Arguments[1]));

        Console.WriteLine($"DEBUG GoTo: Evaluando salto a '{labelName}', condición = {condition}");
        Console.WriteLine($"DEBUG GoTo: Etiquetas disponibles: [{string.Join(", ", labelToIndex.Keys)}]");

        if (!labelToIndex.ContainsKey(labelName))
        {
            Console.WriteLine($"DEBUG GoTo: Etiqueta '{labelName}' NO encontrada en el diccionario");
            throw new Exception($"Etiqueta no encontrada: {labelName}");
        }

        if (condition)
        {
            Console.WriteLine($"DEBUG GoTo: Saltando a etiqueta '{labelName}' (índice {labelToIndex[labelName]})");
            shouldJump = true;
            jumpLabel = labelName;
        }
        else
        {
            Console.WriteLine($"DEBUG GoTo: Condición falsa, no saltando");
        }
    }
    #endregion

    #region Funciones de dibujo

    protected void DrawLine(int dirX, int dirY, int distance)
    {
        if (CurrentColor == "Transparent")
        {
            // Para movimiento transparente, Wall-E se mueve la distancia completa
            // MÁS un pixel adicional
            int newX = CurrentX + dirX * (distance + 1);
            int newY = CurrentY + dirY * (distance + 1);

            if (newX < 0 || newX >= canvasSize || newY < 0 || newY >= canvasSize)
            {
                throw new Exception($"Wall-E se movería fuera del canvas a ({newX}, {newY})");
            }

            CurrentX = newX;
            CurrentY = newY;
            return;
        }

        int startX = CurrentX;
        int startY = CurrentY;

        // Calcular dónde termina la línea
        int lineEndX = startX + dirX * distance;
        int lineEndY = startY + dirY * distance;

        // Wall-E se posiciona UN pixel más allá del final de la línea
        int walleNewX = lineEndX + dirX;
        int walleNewY = lineEndY + dirY;

        // Validar que la línea completa y la posición de Wall-E estén en el canvas
        if (lineEndX < 0 || lineEndX >= canvasSize || lineEndY < 0 || lineEndY >= canvasSize ||
            walleNewX < 0 || walleNewX >= canvasSize || walleNewY < 0 || walleNewY >= canvasSize)
        {
            throw new Exception("La línea o la posición final de Wall-E estarían fuera del canvas");
        }

        // Dibujar cada pixel de la línea
        for (int step = 0; step <= distance; step++)
        {
            int currentX = startX + dirX * step;
            int currentY = startY + dirY * step;
            DrawPixelWithBrush(currentX, currentY);
        }

        // Posicionar Wall-E un pixel después del final de la línea
        CurrentX = walleNewX;
        CurrentY = walleNewY;
    }
    protected void DrawCircle(int centerX, int centerY, int radius)
    {
        if (CurrentColor == "Transparent")
        {
            return;
        }

        // Algoritmo de Bresenham para círculos
        int x = 0;
        int y = radius;
        int d = 3 - 2 * radius;

        while (y >= x)
        {
            DrawCirclePoints(centerX, centerY, x, y);

            if (d > 0)
            {
                y--;
                d = d + 4 * (x - y) + 10;
            }
            else
            {
                d = d + 4 * x + 6;
            }
            x++;
        }
    }

    protected void DrawCirclePoints(int centerX, int centerY, int x, int y)
    {
        // Dibujar los 8 puntos simétricos del círculo
        DrawPixelWithBrush(centerX + x, centerY + y);
        DrawPixelWithBrush(centerX - x, centerY + y);
        DrawPixelWithBrush(centerX + x, centerY - y);
        DrawPixelWithBrush(centerX - x, centerY - y);
        DrawPixelWithBrush(centerX + y, centerY + x);
        DrawPixelWithBrush(centerX - y, centerY + x);
        DrawPixelWithBrush(centerX + y, centerY - x);
        DrawPixelWithBrush(centerX - y, centerY - x);
    }

    protected void DrawRectangle(int centerX, int centerY, int width, int height)
    {
        if (CurrentColor == "Transparent")
        {
            return;
        }

        int left = centerX - width / 2;
        int top = centerY - height / 2;
        int right = left + width - 1;
        int bottom = top + height - 1;

        // Dibujar los bordes horizontales
        for (int x = left; x <= right; x++)
        {
            DrawPixelWithBrush(x, top);
            DrawPixelWithBrush(x, bottom);
        }

        // Dibujar los bordes verticales (sin repetir las esquinas)
        for (int y = top + 1; y < bottom; y++)
        {
            DrawPixelWithBrush(left, y);
            DrawPixelWithBrush(right, y);
        }
    }

    protected void DrawPixelWithBrush(int centerX, int centerY)
    {
        // Validar que el centro esté dentro del canvas
        if (centerX < 0 || centerX >= canvasSize || centerY < 0 || centerY >= canvasSize)
        {
            return;
        }

        int halfBrush = BrushSize / 2;

        // Dibujar con el tamaño de brocha especificado
        // Cada iteración dibuja exactamente UN pixel
        for (int dy = -halfBrush; dy <= halfBrush; dy++)
        {
            for (int dx = -halfBrush; dx <= halfBrush; dx++)
            {
                int pixelX = centerX + dx;
                int pixelY = centerY + dy;

                // Verificar que este pixel individual esté dentro del canvas
                if (pixelX >= 0 && pixelX < canvasSize &&
                    pixelY >= 0 && pixelY < canvasSize)
                {
                    // Aquí es donde realmente pintamos UN pixel
                    canvas[pixelY, pixelX] = CurrentColor;
                }
            }
        }
    }
    protected void FloodFillIterative(int startX, int startY, string targetColor)
    {
        var stack = new Stack<(int x, int y)>();
        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            // Verificar límites del canvas
            if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize)
            {
                continue;
            }

            // Verificar si el pixel es del color objetivo
            if (canvas[y, x] != targetColor)
            {
                continue;
            }

            // Pintar el pixel
            canvas[y, x] = CurrentColor;

            // Agregar píxeles adyacentes a la pila
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }
    }

    #endregion

    #region Evaluación de expresiones

    protected object EvaluateExpression(ExpressionNode expression)
    {
        return expression switch
        {
            StringExpression stringExpr => stringExpr.Value,
            NumberNode numberNode => numberNode.Value,
            BoolLiteralNode boolNode => boolNode.Value,
            VariableNode variableNode => EvaluateVariable(variableNode),
            FunctionCall functionCall => EvaluateFunction(functionCall),
            AdditiveExpression addExpr => EvaluateAdditiveExpression(addExpr),
            MultiplicativeExpression multExpr => EvaluateMultiplicativeExpression(multExpr),
            PowerExpression powExpr => EvaluatePowerExpression(powExpr),
            UnaryExpressionNode unaryExpr => EvaluateUnaryExpression(unaryExpr),
            ParenthesizedExpression parenExpr => EvaluateExpression(parenExpr.Expression),
            OrExpression orExpr => EvaluateOrExpression(orExpr),
            AndExpression andExpr => EvaluateAndExpression(andExpr),
            ComparisonExpression compExpr => EvaluateComparisonExpression(compExpr),
            _ => throw new Exception($"Tipo de expresión no soportado: {expression.GetType().Name}")
        };
    }

    protected object EvaluateVariable(VariableNode variableNode)
    {
        if (!variables.ContainsKey(variableNode.Name))
        {
            throw new Exception($"Variable no definida: {variableNode.Name}");
        }
        return variables[variableNode.Name];
    }

    protected int EvaluateAdditiveExpression(AdditiveExpression addExpr)
    {
        int left = Convert.ToInt32(EvaluateExpression(addExpr.Left));

        if (addExpr.Right != null && addExpr.Operator.HasValue)
        {
            int right = Convert.ToInt32(EvaluateExpression(addExpr.Right));
            return addExpr.Operator.Value == Tipo.SUM ? left + right : left - right;
        }

        return left;
    }

    protected int EvaluateMultiplicativeExpression(MultiplicativeExpression multExpr)
    {
        int left = Convert.ToInt32(EvaluateExpression(multExpr.Left));

        if (multExpr.Right != null && multExpr.Operator.HasValue)
        {
            int right = Convert.ToInt32(EvaluateExpression(multExpr.Right));

            return multExpr.Operator.Value switch
            {
                Tipo.MULT => left * right,
                Tipo.DIV => right == 0 ? throw new DivideByZeroException("División por cero") : left / right,
                Tipo.MOD => right == 0 ? throw new DivideByZeroException("Módulo por cero") : left % right,
                _ => throw new Exception($"Operador multiplicativo no válido: {multExpr.Operator.Value}")
            };
        }

        return left;
    }

    protected int EvaluatePowerExpression(PowerExpression powExpr)
    {
        int baseValue = Convert.ToInt32(EvaluateExpression(powExpr.Base));

        if (powExpr.Exponent != null)
        {
            int exponent = Convert.ToInt32(EvaluateExpression(powExpr.Exponent));
            return (int)Math.Pow(baseValue, exponent);
        }

        return baseValue;
    }

    protected int EvaluateUnaryExpression(UnaryExpressionNode unaryExpr)
    {
        int value = Convert.ToInt32(EvaluateExpression(unaryExpr.Module));
        return unaryExpr.Sign ? -value : value;
    }

    protected bool EvaluateOrExpression(OrExpression orExpr)
    {
        bool left = Convert.ToBoolean(EvaluateExpression(orExpr.Left));

        if (orExpr.Right != null)
        {
            bool right = Convert.ToBoolean(EvaluateExpression(orExpr.Right));
            return left || right;
        }

        return left;
    }

    protected bool EvaluateAndExpression(AndExpression andExpr)
    {
        bool left = Convert.ToBoolean(EvaluateExpression(andExpr.Left));

        if (andExpr.Right != null)
        {
            bool right = Convert.ToBoolean(EvaluateExpression(andExpr.Right));
            return left && right;
        }

        return left;
    }

    protected bool EvaluateComparisonExpression(ComparisonExpression compExpr)
    {
        var left = EvaluateExpression(compExpr.Left);

        if (compExpr.Right != null && compExpr.Operator.HasValue)
        {
            var right = EvaluateExpression(compExpr.Right);

            if (left is int leftInt && right is int rightInt)
            {
                return compExpr.Operator.Value switch
                {
                    Tipo.EQUALS => leftInt == rightInt,
                    Tipo.GREATER => leftInt > rightInt,
                    Tipo.LESSER => leftInt < rightInt,
                    Tipo.GREATER_EQUAL => leftInt >= rightInt,
                    Tipo.LESSER_EQUAL => leftInt <= rightInt,
                    _ => throw new Exception($"Operador de comparación no válido: {compExpr.Operator.Value}")
                };
            }
            else if (left is bool leftBool && right is bool rightBool)
            {
                if (compExpr.Operator.Value == Tipo.EQUALS)
                {
                    return leftBool == rightBool;
                }
            }

            throw new Exception("Tipos incompatibles en comparación");
        }

        return Convert.ToBoolean(left);
    }

    #endregion

    #region Funciones del lenguaje

    protected object EvaluateFunction(FunctionCall functionCall)
    {
        return functionCall.Name switch
        {
            "GetActualX" => GetActualX(),
            "GetActualY" => GetActualY(),
            "GetCanvasSize" => GetCanvasSize(),
            "GetColorCount" => GetColorCount(functionCall),
            "IsBrushColor" => IsBrushColor(functionCall),
            "IsBrushSize" => IsBrushSize(functionCall),
            "IsCanvasColor" => IsCanvasColor(functionCall),
            _ => throw new Exception($"Función desconocida: {functionCall.Name}")
        };
    }

    protected int GetActualX() => CurrentX;
    protected int GetActualY() => CurrentY;
    protected int GetCanvasSize() => canvasSize;

    protected int GetColorCount(FunctionCall functionCall)
    {
        if (functionCall.Arguments.Count != 5)
        {
            throw new Exception("GetColorCount requiere exactamente 5 argumentos");
        }

        string color = EvaluateExpression(functionCall.Arguments[0]).ToString().Trim('"');
        int x1 = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[1]));
        int y1 = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[2]));
        int x2 = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[3]));
        int y2 = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[4]));

        if (!IsValidColor(color))
        {
            throw new Exception($"Color no válido: {color}");
        }

        // Verificar límites del canvas
        if (x1 < 0 || x1 >= canvasSize || y1 < 0 || y1 >= canvasSize ||
            x2 < 0 || x2 >= canvasSize || y2 < 0 || y2 >= canvasSize)
        {
            return 0;
        }

        // Asegurar que x1 <= x2 e y1 <= y2
        if (x1 > x2) (x1, x2) = (x2, x1);
        if (y1 > y2) (y1, y2) = (y2, y1);

        // Contar píxeles del color especificado
        int count = 0;
        for (int y = y1; y <= y2; y++)
        {
            for (int x = x1; x <= x2; x++)
            {
                if (canvas[y, x] == color)
                {
                    count++;
                }
            }
        }

        return count;
    }

    protected int IsBrushColor(FunctionCall functionCall)
    {
        if (functionCall.Arguments.Count != 1)
        {
            throw new Exception("IsBrushColor requiere exactamente 1 argumento");
        }

        string color = EvaluateExpression(functionCall.Arguments[0]).ToString().Trim('"');

        if (!IsValidColor(color))
        {
            throw new Exception($"Color no válido: {color}");
        }

        return CurrentColor == color ? 1 : 0;
    }

    protected int IsBrushSize(FunctionCall functionCall)
    {
        if (functionCall.Arguments.Count != 1)
        {
            throw new Exception("IsBrushSize requiere exactamente 1 argumento");
        }

        int size = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[0]));
        return BrushSize == size ? 1 : 0;
    }

    protected int IsCanvasColor(FunctionCall functionCall)
    {
        if (functionCall.Arguments.Count != 3)
        {
            throw new Exception("IsCanvasColor requiere exactamente 3 argumentos");
        }

        string color = EvaluateExpression(functionCall.Arguments[0]).ToString().Trim('"');
        int vertical = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[1]));
        int horizontal = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[2]));

        if (!IsValidColor(color))
        {
            throw new Exception($"Color no válido: {color}");
        }

        // Calcular la posición a verificar
        int x = CurrentX + horizontal;
        int y = CurrentY + vertical;

        // Verificar que esté dentro del canvas
        if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize)
        {
            return 0;
        }

        return canvas[y, x] == color ? 1 : 0;
    }

    #endregion

    #region Métodos auxiliares

    protected bool IsValidColor(string color)
    {
        string[] validColors = {
            "Red", "Blue", "Green", "Yellow",
            "Orange", "Purple", "Black", "White", "Transparent"
        };

        return Array.Exists(validColors, c => c == color);
    }

    protected void ValidateDirection(int dirX, int dirY, string instruction)
    {
        if (dirX < -1 || dirX > 1 || dirY < -1 || dirY > 1)
        {
            throw new Exception($"Las direcciones para {instruction} deben ser -1, 0 o 1");
        }

        if (dirX == 0 && dirY == 0)
        {
            throw new Exception($"La dirección para {instruction} no puede ser (0, 0)");
        }
    }

    #endregion
}

public class ExecutionContext
{
    public List<Statement> Statements { get; set; }
    public int CurrentIndex { get; set; }
    public string Label { get; set; }
    public Dictionary<string, int> LabelIndexes { get; set; } = new Dictionary<string, int>();
}


public class ExecutionResult
{
    public List<PixelData> PixelData { get; set; } = new List<PixelData>();
    public string ConsoleOutput { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}

public class PixelData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; }

    public override string ToString()
    {
        return $"Pixel({X}, {Y}, {Color})";
    }

    public override bool Equals(object obj)
    {
        if (obj is PixelData other)
        {
            return X == other.X && Y == other.Y && Color == other.Color;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Color);
    }
}