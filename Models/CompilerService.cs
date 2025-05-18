using System;
using System.Collections.Generic;
using System.Text;

namespace PixelWallE.Services
{
    public class CompilerService
    {
        public CompilerResult CompileAndExecute(string code, int canvasSize)
        {
            var result = new CompilerResult();
            var consoleOutput = new StringBuilder();

            try
            {
                // 1. Análisis léxico
                consoleOutput.AppendLine("Iniciando análisis léxico...");
                var lexer = new Lexer(code);
                var tokens = lexer.RunLexer();

                if (!string.IsNullOrEmpty(lexer.Error))
                {
                    consoleOutput.AppendLine("Error en análisis léxico:");
                    consoleOutput.AppendLine(lexer.Error);
                    result.IsSuccess = false;
                    result.Error = lexer.Error;
                    result.ConsoleOutput = consoleOutput.ToString();
                    return result;
                }

                consoleOutput.AppendLine("Análisis léxico completado correctamente.");

                // 2. Análisis sintáctico
                consoleOutput.AppendLine("Iniciando análisis sintáctico...");
                var parser = new Parser(tokens);
                ProgramNode ast;

                try
                {
                    ast = parser.Parse();

                    if (!string.IsNullOrEmpty(parser.Error))
                    {
                        consoleOutput.AppendLine("Error en análisis sintáctico:");
                        consoleOutput.AppendLine(parser.Error);
                        result.IsSuccess = false;
                        result.Error = parser.Error;
                        result.ConsoleOutput = consoleOutput.ToString();
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    consoleOutput.AppendLine("Error en análisis sintáctico:");
                    consoleOutput.AppendLine(ex.Message);
                    result.IsSuccess = false;
                    result.Error = ex.Message;
                    result.ConsoleOutput = consoleOutput.ToString();
                    return result;
                }

                consoleOutput.AppendLine("Análisis sintáctico completado correctamente.");

                // 3. Análisis semántico
                consoleOutput.AppendLine("Iniciando análisis semántico...");
                var semanticAnalyzer = new SemanticAnalyzer(ast, canvasSize);
                bool isValid = semanticAnalyzer.Analyze();

                if (!isValid)
                {
                    consoleOutput.AppendLine("Error en análisis semántico:");
                    consoleOutput.AppendLine(semanticAnalyzer.Error);
                    result.IsSuccess = false;
                    result.Error = semanticAnalyzer.Error;
                    result.ConsoleOutput = consoleOutput.ToString();
                    return result;
                }

                consoleOutput.AppendLine("Análisis semántico completado correctamente.");

                // 4. Interpretación y ejecución
                consoleOutput.AppendLine("Iniciando ejecución del programa...");
                var interpreter = new Interpreter(ast, canvasSize);
                var executionResult = interpreter.Execute();

                consoleOutput.AppendLine("Ejecución completada correctamente.");
                consoleOutput.AppendLine($"Se generaron {executionResult.PixelData.Count} píxeles.");

                if (!string.IsNullOrEmpty(executionResult.ConsoleOutput))
                {
                    consoleOutput.AppendLine("Salida del programa:");
                    consoleOutput.AppendLine(executionResult.ConsoleOutput);
                }

                result.IsSuccess = true;
                result.PixelData = executionResult.PixelData;
                result.ConsoleOutput = consoleOutput.ToString();
                return result;
            }
            catch (Exception ex)
            {
                consoleOutput.AppendLine($"Error de ejecución: {ex.Message}");
                if (ex.InnerException != null)
                {
                    consoleOutput.AppendLine($"Detalles: {ex.InnerException.Message}");
                }

                result.IsSuccess = false;
                result.Error = ex.Message;
                result.ConsoleOutput = consoleOutput.ToString();
                return result;
            }
        }
    }

    public class CompilerResult
    {
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public List<PixelData> PixelData { get; set; } = new List<PixelData>();
        public string ConsoleOutput { get; set; }
    }
}