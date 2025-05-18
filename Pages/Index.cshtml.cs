using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace PixelWallE.Pages
{
    public class IndexModel : PageModel
    {
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
DrawLine(1, 0, 50)";

            ConsoleOutput = "Sistema inicializado. Listo para ejecutar código.";
        }

        // Método POST normal sin AJAX para probar
        public IActionResult OnPost()
        {
            try
            {
                // Obtener código del formulario
                string codeInput = Request.Form["code"];
                int canvasSize = int.Parse(Request.Form["canvasSize"]);

                // Ejecutar el código y obtener resultados
                var (success, error, pixelData, output) = CompileAndExecute(codeInput, canvasSize);

                // Actualizar propiedades
                Code = codeInput;
                CanvasSize = canvasSize;
                ConsoleOutput = output;

                // Guardar los resultados en TempData para mostrarlos
                TempData["Success"] = success;
                TempData["PixelData"] = JsonSerializer.Serialize(pixelData);

                return Page();
            }
            catch (Exception ex)
            {
                ConsoleOutput = "Error: " + ex.Message;
                return Page();
            }
        }

        // Método para AJAX - CORREGIDO
        public IActionResult OnGetExecuteAjax(string code, int canvasSize)
        {
            try
            {
                // Añadir logs para depuración 
                Console.WriteLine($"[AJAX] Recibido código: {code?.Length ?? 0} caracteres");
                Console.WriteLine($"[AJAX] Tamaño del canvas: {canvasSize}");

                // Ejecutar el código y obtener resultados
                var (success, error, pixelData, output) = CompileAndExecute(code, canvasSize);

                // Más logs para depuración
                Console.WriteLine($"[AJAX] Éxito: {success}");
                Console.WriteLine($"[AJAX] Píxeles generados: {pixelData?.Count ?? 0}");

                if (pixelData?.Count > 0)
                {
                    var pixel = pixelData[0];
                    Console.WriteLine($"[AJAX] Primer píxel: X={pixel.X}, Y={pixel.Y}, Color={pixel.Color}");
                }

                // Devolver resultados JSON
                return new JsonResult(new
                {
                    success = success,
                    error = error,
                    pixelData = pixelData,
                    consoleOutput = output
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AJAX] Error: {ex.Message}");
                Console.WriteLine($"[AJAX] StackTrace: {ex.StackTrace}");

                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    pixelData = new List<PixelData>(),
                    consoleOutput = "Error: " + ex.Message
                });
            }
        }

        // Método privado que maneja la compilación y ejecución
        private (bool success, string error, List<PixelData> pixelData, string output)
            CompileAndExecute(string code, int canvasSize)
        {
            var output = "Iniciando compilación...\n";

            try
            {
                // 1. Análisis léxico
                output += "Análisis léxico...\n";
                var lexer = new Lexer(code);
                var tokens = lexer.RunLexer();

                if (!string.IsNullOrEmpty(lexer.Error))
                {
                    output += "Error léxico: " + lexer.Error + "\n";
                    return (false, lexer.Error, new List<PixelData>(), output);
                }

                // 2. Análisis sintáctico
                output += "Análisis sintáctico...\n";
                var parser = new Parser(tokens);
                ProgramNode ast;

                try
                {
                    ast = parser.Parse();

                    if (!string.IsNullOrEmpty(parser.Error))
                    {
                        output += "Error sintáctico: " + parser.Error + "\n";
                        return (false, parser.Error, new List<PixelData>(), output);
                    }
                }
                catch (Exception ex)
                {
                    output += "Error sintáctico: " + ex.Message + "\n";
                    return (false, ex.Message, new List<PixelData>(), output);
                }

                // 3. Análisis semántico
                output += "Análisis semántico...\n";
                var semanticAnalyzer = new SemanticAnalyzer(ast, canvasSize);
                bool isValid = semanticAnalyzer.Analyze();

                if (!isValid)
                {
                    output += "Error semántico: " + semanticAnalyzer.Error + "\n";
                    return (false, semanticAnalyzer.Error, new List<PixelData>(), output);
                }

                // 4. Interpretación y ejecución
                output += "Ejecutando código...\n";
                var interpreter = new Interpreter(ast, canvasSize);
                var result = interpreter.Execute();

                output += "Ejecución completada con éxito.\n";
                output += $"Se generaron {result.PixelData.Count} píxeles.\n";

                if (!string.IsNullOrEmpty(result.ConsoleOutput))
                {
                    output += "Salida del programa:\n" + result.ConsoleOutput + "\n";
                }

                return (true, "", result.PixelData, output);
            }
            catch (Exception ex)
            {
                output += "Error: " + ex.Message + "\n";
                if (ex.InnerException != null)
                {
                    output += "Detalles: " + ex.InnerException.Message + "\n";
                }

                return (false, ex.Message, new List<PixelData>(), output);
            }
        }
    }
}