using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;

namespace PixelWallE.Pages
{
    public class IndexModel : PageModel
    {
        private static DebugSession? _debugSession;

        [BindProperty]
        public string Code { get; set; } = "";

        [BindProperty]
        public string ConsoleOutput { get; set; } = "";

        [BindProperty]
        public int CanvasSize { get; set; } = 256;

        public void OnGet()
        {
            // Código de ejemplo
            Code = @"Spawn(128, 128)
Color(""Blue"")
Size(3)
DrawLine(1, 0, 50)
Color(""Red"")
DrawCircle(1, 1, 20)";

            ConsoleOutput = "Sistema inicializado. Listo para ejecutar código.";
        }

        /// <summary>
        /// Método para ejecutar el código con manejo mejorado de errores
        /// </summary>
        public IActionResult OnGetExecuteAjax(string? code, int canvasSize)
        {
            try
            {
                Console.WriteLine($"[AJAX] Ejecutando código: {code?.Length ?? 0} caracteres");
                Console.WriteLine($"[AJAX] Tamaño del canvas: {canvasSize}");

                var result = ExecuteCodeSafely(code, canvasSize);

                Console.WriteLine($"[AJAX] Éxito: {result.Success}");
                Console.WriteLine($"[AJAX] Píxeles generados: {result.PixelData?.Count ?? 0}");

                return new JsonResult(new
                {
                    success = result.Success,
                    error = result.Error,
                    pixelData = result.PixelData,
                    consoleOutput = result.ConsoleOutput,
                    wallePosition = result.WallePosition
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AJAX] Error inesperado: {ex.Message}");
                Console.WriteLine($"[AJAX] StackTrace: {ex.StackTrace}");

                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    pixelData = new List<PixelData>(),
                    consoleOutput = "Error inesperado: " + ex.Message,
                    wallePosition = new { x = 0, y = 0, color = "Transparent", size = 1 }
                });
            }
        }

        /// <summary>
        /// Ejecuta el código con manejo seguro de errores y timeouts
        /// </summary>
        private ExecutionResultResponse ExecuteCodeSafely(string code, int canvasSize)
        {
            var output = new StringBuilder("Iniciando compilación...\n");
            var result = new ExecutionResultResponse();

            try
            {
                // Validación inicial
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Error = "Código vacío";
                    result.ConsoleOutput = "Error: No se proporcionó código para ejecutar.";
                    return result;
                }

                if (canvasSize < 10 || canvasSize > 1000)
                {
                    result.Error = "Tamaño de canvas inválido";
                    result.ConsoleOutput = "Error: El tamaño del canvas debe estar entre 10 y 1000 píxeles.";
                    return result;
                }

                // 1. Análisis léxico
                output.AppendLine("Análisis léxico...");
                var lexer = new Lexer(code);
                var tokens = lexer.RunLexer();

                if (!string.IsNullOrEmpty(lexer.Error))
                {
                    result.Error = lexer.Error;
                    result.ConsoleOutput = output.ToString() + "Error léxico: " + lexer.Error;
                    return result;
                }

                // 2. Análisis sintáctico
                output.AppendLine("Análisis sintáctico...");
                var parser = new Parser(tokens);
                ProgramNode ast;

                try
                {
                    ast = parser.Parse();

                    if (!string.IsNullOrEmpty(parser.Error))
                    {
                        result.Error = parser.Error;
                        result.ConsoleOutput = output.ToString() + "Error sintáctico: " + parser.Error;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                    result.ConsoleOutput = output.ToString() + "Error sintáctico: " + ex.Message;
                    return result;
                }

                // 3. Análisis semántico
                output.AppendLine("Análisis semántico...");
                var semanticAnalyzer = new SemanticAnalyzer(ast, canvasSize);
                bool isSemanticValid = semanticAnalyzer.Analyze();

                if (!isSemanticValid)
                {
                    result.Error = semanticAnalyzer.Error;
                    result.ConsoleOutput = output.ToString() + "Error semántico: " + semanticAnalyzer.Error;
                    return result;
                }

                // 4. Ejecución con timeout
                output.AppendLine("Ejecutando código...");
                var interpreter = new Interpreter(ast, canvasSize);

                // Ejecutar con timeout de 30 segundos
                var executionTask = Task.Run(() => interpreter.Execute());

                if (executionTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    var executionResult = executionTask.Result;

                    result.Success = string.IsNullOrEmpty(executionResult.ErrorMessage);
                    result.Error = executionResult.ErrorMessage ?? "";
                    result.PixelData = executionResult.PixelData ?? new List<PixelData>();
                    result.ConsoleOutput = output.ToString() +
                                         $"Ejecución completada. Se generaron {result.PixelData.Count} píxeles.\n" +
                                         (executionResult.ConsoleOutput ?? "");

                    result.WallePosition = new
                    {
                        x = interpreter.CurrentX,
                        y = interpreter.CurrentY,
                        color = interpreter.CurrentColor,
                        size = interpreter.BrushSize
                    };
                }
                else
                {
                    result.Error = "Timeout: La ejecución tardó demasiado tiempo";
                    result.ConsoleOutput = output.ToString() + "Error: La ejecución fue cancelada por timeout.";
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                result.ConsoleOutput = output.ToString() + "Error de ejecución: " + ex.Message;
            }

            return result;
        }

        #region Métodos de depuración

        public IActionResult OnGetStartDebug(string code, int canvasSize)
        {
            try
            {
                _debugSession = new DebugSession(code, canvasSize);
                bool initialized = _debugSession.Initialize();

                if (!initialized)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        error = _debugSession.LastError,
                        nextLine = 0
                    });
                }

                var state = _debugSession.GetCurrentState();

                return new JsonResult(new
                {
                    success = true,
                    nextLine = state.CurrentLine,
                    wallePosition = new
                    {
                        x = state.WallePosition.X,
                        y = state.WallePosition.Y,
                        color = state.WallePosition.Color,
                        size = state.WallePosition.Size
                    },
                    pixelData = state.PixelData,
                    variables = state.Variables,
                    instructions = _debugSession.Instructions,
                    executableLines = _debugSession.GetExecutableLines()
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    nextLine = 0
                });
            }
        }

        public IActionResult OnGetStepDebug()
        {
            try
            {
                if (_debugSession == null)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        error = "No hay una sesión de depuración activa",
                        nextLine = 0
                    });
                }

                bool success = _debugSession.Step();
                var state = _debugSession.GetCurrentState();

                return new JsonResult(new
                {
                    success = success,
                    nextLine = state.CurrentLine,
                    wallePosition = new
                    {
                        x = state.WallePosition.X,
                        y = state.WallePosition.Y,
                        color = state.WallePosition.Color,
                        size = state.WallePosition.Size
                    },
                    pixelData = state.PixelData,
                    variables = state.Variables,
                    logMessage = _debugSession.LastLogMessage,
                    finished = state.IsFinished,
                    error = success ? "" : _debugSession.LastError,
                    hasError = state.HasError
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    nextLine = 0
                });
            }
        }

        public IActionResult OnGetContinueDebug()
        {
            try
            {
                if (_debugSession == null)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        error = "No hay una sesión de depuración activa",
                        nextLine = 0
                    });
                }

                bool success = _debugSession.Continue();
                var state = _debugSession.GetCurrentState();

                return new JsonResult(new
                {
                    success = success,
                    wallePosition = new
                    {
                        x = state.WallePosition.X,
                        y = state.WallePosition.Y,
                        color = state.WallePosition.Color,
                        size = state.WallePosition.Size
                    },
                    pixelData = state.PixelData,
                    variables = state.Variables,
                    finished = true,
                    error = success ? "" : _debugSession.LastError,
                    hasError = state.HasError
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    nextLine = 0
                });
            }
        }

        public IActionResult OnGetStopDebug()
        {
            _debugSession = null;
            return new JsonResult(new { success = true });
        }

        #endregion

        /// <summary>
        /// Extrae el número de línea de mensajes de error
        /// </summary>
        private int ExtractLineNumber(string errorMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(errorMessage)) return 0;

                var lineMatches = Regex.Matches(errorMessage, @"línea\s+(\d+)|line\s+(\d+)", RegexOptions.IgnoreCase);

                if (lineMatches.Count > 0)
                {
                    foreach (Match match in lineMatches)
                    {
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.Groups[i].Success && int.TryParse(match.Groups[i].Value, out int lineNumber))
                            {
                                return lineNumber;
                            }
                        }
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Clase mejorada para manejar la sesión de depuración
    /// </summary>
    public class DebugSession
    {
        public string Code { get; private set; }
        public int CanvasSize { get; private set; }
        public string LastError { get; private set; } = "";
        public string LastLogMessage { get; private set; } = "";
        public List<string> Instructions { get; private set; } = new List<string>();

        private DebugInterpreter? _debugInterpreter;
        private ProgramNode? _ast;
        private bool _isInitialized = false;

        public DebugSession(string code, int canvasSize)
        {
            Code = code ?? "";
            CanvasSize = canvasSize;
        }

        public bool Initialize()
        {
            try
            {
                // Validación inicial
                if (string.IsNullOrWhiteSpace(Code))
                {
                    LastError = "Código vacío";
                    return false;
                }

                // 1. Análisis léxico
                var lexer = new Lexer(Code);
                var tokens = lexer.RunLexer();

                if (!string.IsNullOrEmpty(lexer.Error))
                {
                    LastError = "Error léxico: " + lexer.Error;
                    return false;
                }

                // 2. Análisis sintáctico
                var parser = new Parser(tokens);

                try
                {
                    _ast = parser.Parse();

                    if (!string.IsNullOrEmpty(parser.Error))
                    {
                        LastError = "Error sintáctico: " + parser.Error;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LastError = "Error sintáctico: " + ex.Message;
                    return false;
                }

                // Verificar que tenemos un AST válido
                if (_ast?.Statements == null || _ast.Statements.Count == 0)
                {
                    LastError = "No se pudo generar un AST válido.";
                    return false;
                }

                // 3. Análisis semántico
                var semanticAnalyzer = new SemanticAnalyzer(_ast, CanvasSize);
                bool isValid = semanticAnalyzer.Analyze();

                if (!isValid)
                {
                    LastError = "Error semántico: " + semanticAnalyzer.Error;
                    // Continúar con advertencia, no fallar completamente
                }

                // 4. Crear el intérprete de depuración
                _debugInterpreter = new DebugInterpreter(_ast, CanvasSize);

                // Extraer instrucciones para mostrar
                Instructions = ExtractInstructions(Code);

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                LastError = "Error de inicialización: " + ex.Message;
                return false;
            }
        }

        public bool Step()
        {
            if (!_isInitialized || _debugInterpreter == null)
            {
                LastError = "Sesión de depuración no inicializada";
                return false;
            }

            try
            {
                var result = _debugInterpreter.StepExecute();
                LastLogMessage = result.LogMessage;

                if (result.HasError)
                {
                    LastError = result.Error;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public bool Continue()
        {
            if (!_isInitialized || _debugInterpreter == null)
            {
                LastError = "Sesión de depuración no inicializada";
                return false;
            }

            try
            {
                var result = _debugInterpreter.ContinueExecution();
                LastLogMessage = result.LogMessage;

                if (result.HasError)
                {
                    LastError = result.Error;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public DebugState GetCurrentState()
        {
            if (!_isInitialized || _debugInterpreter == null)
            {
                return new DebugState
                {
                    HasError = true,
                    ErrorMessage = "Sesión no inicializada"
                };
            }

            return _debugInterpreter.GetCurrentState();
        }

        public List<int> GetExecutableLines()
        {
            if (!_isInitialized || _debugInterpreter == null)
            {
                return new List<int>();
            }

            return _debugInterpreter.GetExecutableLines();
        }

        public void Reset()
        {
            if (_debugInterpreter != null)
            {
                _debugInterpreter.Reset();
            }
        }

        private List<string> ExtractInstructions(string code)
        {
            var result = new List<string>();
            string[] lines = code.Split('\n');

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Respuesta de la ejecución de código
    /// </summary>
    public class ExecutionResultResponse
    {
        public bool Success { get; set; } = false;
        public string Error { get; set; } = "";
        public List<PixelData> PixelData { get; set; } = new List<PixelData>();
        public string ConsoleOutput { get; set; } = "";
        public object WallePosition { get; set; } = new { x = 0, y = 0, color = "Transparent", size = 1 };
    }
}