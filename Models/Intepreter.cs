using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class Interpreter
{
    private readonly ProgramNode ast;
    private readonly int canvasSize;
    private Dictionary<string, object> variables;
    private Dictionary<string, LabelNode> labels;
    private string[,] canvas;
    private int currentX;
    private int currentY;
    private string currentColor;
    private int brushSize;
    private string consoleOutput;
    private List<PixelData> pixelData;

    public Interpreter(ProgramNode ast, int canvasSize)
    {
        this.ast = ast;
        this.canvasSize = canvasSize;
        this.variables = new Dictionary<string, object>();
        this.labels = new Dictionary<string, LabelNode>();
        this.canvas = new string[canvasSize, canvasSize];
        this.currentX = 0;
        this.currentY = 0;
        this.currentColor = "Transparent";
        this.brushSize = 1;
        this.consoleOutput = "";
        this.pixelData = new List<PixelData>();

        // Inicializar el canvas con color blanco
        for (int i = 0; i < canvasSize; i++)
        {
            for (int j = 0; j < canvasSize; j++)
            {
                canvas[i, j] = "White";
            }
        }

        // Recolectar las etiquetas del programa
        CollectLabels(ast.Statements);
    }

    private void CollectLabels(List<Statement> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is LabelNode labelNode)
            {
                labels[labelNode.Name] = labelNode;
                CollectLabels(labelNode.Programa.Statements);
            }
        }
    }

    // Agrega estas líneas de depuración al método Execute() del intérprete
    public ExecutionResult Execute()
    {
        try
        {
            ExecuteStatements(ast.Statements);

            // Depuración: Mostrar cuántos píxeles se generaron
            var pixelData = GeneratePixelData();
            consoleOutput += $"\nDebug: Se generaron {pixelData.Count} píxeles.";

            if (pixelData.Count > 0)
            {
                // Mostrar el primer píxel para verificar su formato
                var firstPixel = pixelData[0];
                consoleOutput += $"\nDebug: Primer píxel en ({firstPixel.X}, {firstPixel.Y}) con color {firstPixel.Color}";
            }

            return new ExecutionResult
            {
                PixelData = pixelData,
                ConsoleOutput = consoleOutput
            };
        }
        catch (Exception ex)
        {
            consoleOutput += $"Error de ejecución: {ex.Message}\n";
            return new ExecutionResult
            {
                PixelData = new List<PixelData>(),
                ConsoleOutput = consoleOutput
            };
        }
    }
    private List<PixelData> GeneratePixelData()
    {
        var result = new List<PixelData>();

        // Recorrer toda la matriz del canvas
        for (int y = 0; y < canvasSize; y++)
        {
            for (int x = 0; x < canvasSize; x++)
            {
                // TEMPORAL PARA DEPURACIÓN: Incluir todos los píxeles incluyendo blancos 
                // para ver qué está pasando
                result.Add(new PixelData
                {
                    X = x,
                    Y = y,
                    Color = canvas[y, x]
                });

                // Después puedes volver a la condición original:
                // Solo agregar píxeles que no sean blancos
                /*
                if (canvas[y, x] != "White" && canvas[y, x] != "Transparent")
                {
                    result.Add(new PixelData
                    {
                        X = x,
                        Y = y,
                        Color = canvas[y, x]
                    });
                }
                */
            }
        }

        // Depuración: Imprimir algunos datos
        Console.WriteLine($"GeneratePixelData: Generados {result.Count} píxeles totales.");

        if (result.Count > 0)
        {
            Console.WriteLine($"Primer píxel: ({result[0].X}, {result[0].Y}) - {result[0].Color}");
        }
        else
        {
            Console.WriteLine("¡No se generaron píxeles!");
        }

        return result;
    }
    private void ExecuteStatements(List<Statement> statements, int startIndex = 0)
    {
        for (int i = startIndex; i < statements.Count; i++)
        {
            ExecuteStatement(statements[i]);
        }
    }

    private void ExecuteStatement(Statement statement)
    {
        if (statement is AssigmentNode assignmentNode)
        {
            ExecuteAssignment(assignmentNode);
        }
        else if (statement is Instruction instruction)
        {
            ExecuteInstruction(instruction);
        }
        else if (statement is LabelNode)
        {
            // Las etiquetas no se ejecutan directamente
        }
        else if (statement is FunctionCallStatement functionCallStatement)
        {
            EvaluateExpression(functionCallStatement.FunctionCall);
        }
    }

    private void ExecuteAssignment(AssigmentNode assignment)
    {
        object value = EvaluateExpression(assignment.Value);
        variables[assignment.VariableName] = value;
    }

    private void ExecuteInstruction(Instruction instruction)
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

    private void ExecuteSpawn(Instruction instruction)
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

        currentX = x;
        currentY = y;
    }

    private void ExecuteColor(Instruction instruction)
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

        currentColor = color;
    }

    private bool IsValidColor(string color)
    {
        string[] validColors =
        {
            "Red", "Blue", "Green", "Yellow",
            "Orange", "Purple", "Black", "White", "Transparent"
        };

        return Array.Exists(validColors, c => c == color);
    }

    private void ExecuteSize(Instruction instruction)
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

        brushSize = size;
    }

    private void ExecuteDrawLine(Instruction instruction)
    {
        if (instruction.Arguments.Count != 3)
        {
            throw new Exception("DrawLine requiere exactamente 3 argumentos");
        }

        int dirX = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));
        int dirY = Convert.ToInt32(EvaluateExpression(instruction.Arguments[1]));
        int distance = Convert.ToInt32(EvaluateExpression(instruction.Arguments[2]));

        if (dirX < -1 || dirX > 1 || dirY < -1 || dirY > 1)
        {
            throw new Exception("Las direcciones para DrawLine deben ser -1, 0 o 1");
        }

        if (dirX == 0 && dirY == 0)
        {
            throw new Exception("La dirección para DrawLine no puede ser (0, 0)");
        }

        if (distance <= 0)
        {
            throw new Exception("La distancia para DrawLine debe ser positiva");
        }

        // Dibujar la línea
        DrawLine(dirX, dirY, distance);
    }

    private void DrawLine(int dirX, int dirY, int distance)
    {
        if (currentColor == "Transparent")
        {
            // Si el color es transparente, solo mover la posición
            currentX += dirX * distance;
            currentY += dirY * distance;
            return;
        }

        int startX = currentX;
        int startY = currentY;

        // Calculamos el desplazamiento para cada paso
        int offsetSize = brushSize / 2;

        for (int i = 0; i <= distance; i++)
        {
            int posX = startX + dirX * i;
            int posY = startY + dirY * i;

            // Dibujar el punto y sus adyacentes según el tamaño del pincel
            for (int dy = -offsetSize; dy <= offsetSize; dy++)
            {
                for (int dx = -offsetSize; dx <= offsetSize; dx++)
                {
                    int x = posX + dx;
                    int y = posY + dy;

                    // Verificar que esté dentro del canvas
                    if (x >= 0 && x < canvasSize && y >= 0 && y < canvasSize)
                    {
                        canvas[y, x] = currentColor;
                    }
                }
            }
        }

        // Actualizar la posición actual de Wall-E
        currentX = startX + dirX * distance;
        currentY = startY + dirY * distance;
    }

    private void ExecuteDrawCircle(Instruction instruction)
    {
        if (instruction.Arguments.Count != 3)
        {
            throw new Exception("DrawCircle requiere exactamente 3 argumentos");
        }

        int dirX = Convert.ToInt32(EvaluateExpression(instruction.Arguments[0]));
        int dirY = Convert.ToInt32(EvaluateExpression(instruction.Arguments[1]));
        int radius = Convert.ToInt32(EvaluateExpression(instruction.Arguments[2]));

        if (dirX < -1 || dirX > 1 || dirY < -1 || dirY > 1)
        {
            throw new Exception("Las direcciones para DrawCircle deben ser -1, 0 o 1");
        }

        if (dirX == 0 && dirY == 0)
        {
            throw new Exception("La dirección para DrawCircle no puede ser (0, 0)");
        }

        if (radius <= 0)
        {
            throw new Exception("El radio para DrawCircle debe ser positivo");
        }

        // Calcular el centro del círculo
        int centerX = currentX + dirX * radius;
        int centerY = currentY + dirY * radius;

        // Dibujar el círculo
        DrawCircle(centerX, centerY, radius);

        // Actualizar la posición actual de Wall-E
        currentX = centerX;
        currentY = centerY;
    }

    private void DrawCircle(int centerX, int centerY, int radius)
    {
        if (currentColor == "Transparent")
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

    private void DrawCirclePoints(int centerX, int centerY, int x, int y)
    {
        int offsetSize = brushSize / 2;

        // Dibujar los 8 puntos simétricos del círculo
        DrawPixel(centerX + x, centerY + y);
        DrawPixel(centerX - x, centerY + y);
        DrawPixel(centerX + x, centerY - y);
        DrawPixel(centerX - x, centerY - y);
        DrawPixel(centerX + y, centerY + x);
        DrawPixel(centerX - y, centerY + x);
        DrawPixel(centerX + y, centerY - x);
        DrawPixel(centerX - y, centerY - x);
    }

    private void DrawPixel(int x, int y)
    {
        int offsetSize = brushSize / 2;

        // Dibujar el punto y sus adyacentes según el tamaño del pincel
        for (int dy = -offsetSize; dy <= offsetSize; dy++)
        {
            for (int dx = -offsetSize; dx <= offsetSize; dx++)
            {
                int px = x + dx;
                int py = y + dy;

                // Verificar que esté dentro del canvas
                if (px >= 0 && px < canvasSize && py >= 0 && py < canvasSize)
                {
                    canvas[py, px] = currentColor;
                }
            }
        }
    }

    private void ExecuteDrawRectangle(Instruction instruction)
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

        if (dirX < -1 || dirX > 1 || dirY < -1 || dirY > 1)
        {
            throw new Exception("Las direcciones para DrawRectangle deben ser -1, 0 o 1");
        }

        if (dirX == 0 && dirY == 0)
        {
            throw new Exception("La dirección para DrawRectangle no puede ser (0, 0)");
        }

        if (distance <= 0 || width <= 0 || height <= 0)
        {
            throw new Exception("La distancia, ancho y alto para DrawRectangle deben ser positivos");
        }

        // Calcular el centro del rectángulo
        int centerX = currentX + dirX * distance;
        int centerY = currentY + dirY * distance;

        // Dibujar el rectángulo
        DrawRectangle(centerX, centerY, width, height);

        // Actualizar la posición actual de Wall-E
        currentX = centerX;
        currentY = centerY;
    }

    private void DrawRectangle(int centerX, int centerY, int width, int height)
    {
        if (currentColor == "Transparent")
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
            DrawPixel(x, top);
            DrawPixel(x, bottom);
        }

        // Dibujar los bordes verticales
        for (int y = top + 1; y < bottom; y++)
        {
            DrawPixel(left, y);
            DrawPixel(right, y);
        }
    }

    private void ExecuteFill(Instruction instruction)
    {
        if (instruction.Arguments.Count != 0)
        {
            throw new Exception("Fill no debe tener argumentos");
        }

        if (currentColor == "Transparent")
        {
            return;
        }

        // Obtener el color a reemplazar
        string targetColor = canvas[currentY, currentX];

        // No hacer nada si el color objetivo es igual al color actual
        if (targetColor == currentColor)
        {
            return;
        }

        // Algoritmo de flood fill (implementación recursiva)
        FloodFill(currentX, currentY, targetColor);
    }

    private void FloodFill(int x, int y, string targetColor)
    {
        // Verificar límites del canvas
        if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize)
        {
            return;
        }

        // Verificar si el pixel es del color objetivo
        if (canvas[y, x] != targetColor)
        {
            return;
        }

        // Pintar el pixel
        canvas[y, x] = currentColor;

        // Continuar con los píxeles adyacentes
        FloodFill(x + 1, y, targetColor);
        FloodFill(x - 1, y, targetColor);
        FloodFill(x, y + 1, targetColor);
        FloodFill(x, y - 1, targetColor);
    }

    private void ExecuteGoTo(Instruction instruction)
    {
        if (instruction.Arguments.Count != 2)
        {
            throw new Exception("GoTo requiere exactamente 2 argumentos");
        }

        string labelName = EvaluateExpression(instruction.Arguments[0]).ToString().Trim('"');
        bool condition = Convert.ToBoolean(EvaluateExpression(instruction.Arguments[1]));

        if (!labels.ContainsKey(labelName))
        {
            throw new Exception($"Etiqueta no encontrada: {labelName}");
        }

        if (condition)
        {
            // Ejecutar los statements de la etiqueta
            ExecuteStatements(labels[labelName].Programa.Statements);
        }
    }

    private object EvaluateExpression(ExpressionNode expression)
    {
        if (expression is StringExpression stringExpr)
        {
            return stringExpr.Value;
        }
        else if (expression is NumberNode numberNode)
        {
            return numberNode.Value;
        }
        else if (expression is BoolLiteralNode boolNode)
        {
            return boolNode.Value;
        }
        else if (expression is VariableNode variableNode)
        {
            if (!variables.ContainsKey(variableNode.Name))
            {
                throw new Exception($"Variable no definida: {variableNode.Name}");
            }
            return variables[variableNode.Name];
        }
        else if (expression is FunctionCall functionCall)
        {
            return EvaluateFunction(functionCall);
        }
        else if (expression is AdditiveExpression addExpr)
        {
            var left = Convert.ToInt32(EvaluateExpression(addExpr.Left));

            if (addExpr.Right != null && addExpr.Operator.HasValue)
            {
                var right = Convert.ToInt32(EvaluateExpression(addExpr.Right));

                if (addExpr.Operator.Value == Tipo.SUM)
                {
                    return left + right;
                }
                else if (addExpr.Operator.Value == Tipo.SUB)
                {
                    return left - right;
                }
            }

            return left;
        }
        else if (expression is MultiplicativeExpression multExpr)
        {
            var left = Convert.ToInt32(EvaluateExpression(multExpr.Left));

            if (multExpr.Right != null && multExpr.Operator.HasValue)
            {
                var right = Convert.ToInt32(EvaluateExpression(multExpr.Right));

                if (multExpr.Operator.Value == Tipo.MULT)
                {
                    return left * right;
                }
                else if (multExpr.Operator.Value == Tipo.DIV)
                {
                    if (right == 0)
                    {
                        throw new DivideByZeroException("División por cero");
                    }
                    return left / right;
                }
                else if (multExpr.Operator.Value == Tipo.MOD)
                {
                    if (right == 0)
                    {
                        throw new DivideByZeroException("Módulo por cero");
                    }
                    return left % right;
                }
            }

            return left;
        }
        else if (expression is PowerExpression powExpr)
        {
            var baseValue = Convert.ToInt32(EvaluateExpression(powExpr.Base));

            if (powExpr.Exponent != null)
            {
                var exponent = Convert.ToInt32(EvaluateExpression(powExpr.Exponent));
                return (int)Math.Pow(baseValue, exponent);
            }

            return baseValue;
        }
        else if (expression is UnaryExpressionNode unaryExpr)
        {
            var value = Convert.ToInt32(EvaluateExpression(unaryExpr.Module));
            return unaryExpr.Sign ? -value : value;
        }
        else if (expression is ParenthesizedExpression parenExpr)
        {
            return EvaluateExpression(parenExpr.Expression);
        }
        else if (expression is OrExpression orExpr)
        {
            var left = Convert.ToBoolean(EvaluateExpression(orExpr.Left));

            if (orExpr.Right != null)
            {
                var right = Convert.ToBoolean(EvaluateExpression(orExpr.Right));
                return left || right;
            }

            return left;
        }
        else if (expression is AndExpression andExpr)
        {
            var left = Convert.ToBoolean(EvaluateExpression(andExpr.Left));

            if (andExpr.Right != null)
            {
                var right = Convert.ToBoolean(EvaluateExpression(andExpr.Right));
                return left && right;
            }

            return left;
        }
        else if (expression is ComparisonExpression compExpr)
        {
            var left = EvaluateExpression(compExpr.Left);

            if (compExpr.Right != null && compExpr.Operator.HasValue)
            {
                var right = EvaluateExpression(compExpr.Right);

                if (left is int leftInt && right is int rightInt)
                {
                    switch (compExpr.Operator.Value)
                    {
                        case Tipo.EQUALS:
                            return leftInt == rightInt;
                        case Tipo.GREATER:
                            return leftInt > rightInt;
                        case Tipo.LESSER:
                            return leftInt < rightInt;
                        case Tipo.GREATER_EQUAL:
                            return leftInt >= rightInt;
                        case Tipo.LESSER_EQUAL:
                            return leftInt <= rightInt;
                    }
                }
                else if (left is bool leftBool && right is bool rightBool)
                {
                    if (compExpr.Operator.Value == Tipo.EQUALS)
                    {
                        return leftBool == rightBool;
                    }
                }
            }

            return Convert.ToBoolean(left);
        }

        throw new Exception($"Tipo de expresión no soportado: {expression.GetType().Name}");
    }

    private object EvaluateFunction(FunctionCall functionCall)
    {
        switch (functionCall.Name)
        {
            case "GetActualX":
                return GetActualX();
            case "GetActualY":
                return GetActualY();
            case "GetCanvasSize":
                return GetCanvasSize();
            case "GetColorCount":
                return GetColorCount(functionCall);
            case "IsBrushColor":
                return IsBrushColor(functionCall);
            case "IsBrushSize":
                return IsBrushSize(functionCall);
            case "IsCanvasColor":
                return IsCanvasColor(functionCall);
            default:
                throw new Exception($"Función desconocida: {functionCall.Name}");
        }
    }

    private int GetActualX()
    {
        return currentX;
    }

    private int GetActualY()
    {
        return currentY;
    }

    private int GetCanvasSize()
    {
        return canvasSize;
    }

    private int GetColorCount(FunctionCall functionCall)
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

        // Verificar que el color sea válido
        if (!IsValidColor(color))
        {
            throw new Exception($"Color no válido: {color}");
        }

        // Verificar que las coordenadas estén dentro del canvas
        if (x1 < 0 || x1 >= canvasSize || y1 < 0 || y1 >= canvasSize ||
            x2 < 0 || x2 >= canvasSize || y2 < 0 || y2 >= canvasSize)
        {
            return 0;
        }

        // Asegurar que x1 <= x2 e y1 <= y2
        if (x1 > x2)
        {
            int temp = x1;
            x1 = x2;
            x2 = temp;
        }

        if (y1 > y2)
        {
            int temp = y1;
            y1 = y2;
            y2 = temp;
        }

        // Contar los píxeles del color especificado
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

    private int IsBrushColor(FunctionCall functionCall)
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

        return currentColor == color ? 1 : 0;
    }

    private int IsBrushSize(FunctionCall functionCall)
    {
        if (functionCall.Arguments.Count != 1)
        {
            throw new Exception("IsBrushSize requiere exactamente 1 argumento");
        }

        int size = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[0]));

        return brushSize == size ? 1 : 0;
    }

    private int IsCanvasColor(FunctionCall functionCall)
    {
        if (functionCall.Arguments.Count != 3)
        {
            throw new Exception("IsCanvasColor requiere exactamente 3 argumentos");
        }

        string color = EvaluateExpression(functionCall.Arguments[0]).ToString().Trim('"');
        int vertical = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[1]));
        int horizontal = Convert.ToInt32(EvaluateExpression(functionCall.Arguments[2]));

        // Verificar que el color sea válido
        if (!IsValidColor(color))
        {
            throw new Exception($"Color no válido: {color}");
        }

        // Calcular la posición a verificar
        int x = currentX + horizontal;
        int y = currentY + vertical;

        // Verificar que esté dentro del canvas
        if (x < 0 || x >= canvasSize || y < 0 || y >= canvasSize)
        {
            return 0;
        }

        return canvas[y, x] == color ? 1 : 0;
    }
}

public class ExecutionResult
{
    public List<PixelData> PixelData { get; set; }
    public string ConsoleOutput { get; set; }
}

public class PixelData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; }
}