using System.Text.RegularExpressions;

public class Lexer
{
    private readonly string SourceCode;
    private int Line;
    private int Column;
    public string Error {get; private set;}
    

    private readonly Dictionary<string, Tipo> ArithmeticOps = new Dictionary<string, Tipo>{
      
        {"+",Tipo.SUM},
        {"-",Tipo.SUB},
        {"/",Tipo.DIV},
        {"*",Tipo.MULT},
        {"**",Tipo.POW},
        {"%",Tipo.MOD}

    };
    private readonly Dictionary<string, Tipo> LogicalOps = new Dictionary<string, Tipo>{
       
        {"&&",Tipo.AND},
        {"||",Tipo.OR}
    };
    private readonly Dictionary<string, Tipo> ComparativeOps = new Dictionary<string, Tipo>{
       
        {"==",Tipo.EQUALS},
        {">=",Tipo.GREATER_EQUAL},
        {"<=", Tipo.LESSER_EQUAL},
        {">", Tipo.GREATER},
        {"<", Tipo.LESSER}
    };

    private readonly Dictionary<string, Tipo> Assignation = new Dictionary<string, Tipo>{
        {"<-",Tipo.ASSIGN}
    };

    private readonly Dictionary<string, Tipo> Group = new Dictionary<string, Tipo>{

        {"(", Tipo.L_PAREN},
        {")",Tipo.R_PAREN},
        {"[", Tipo.L_BRACKET},
        {"]",Tipo.R_BRACKET}
    };

    public Lexer(string sourceCode)
    {
        SourceCode = sourceCode;   
        Line =1;
        Column = 1;
        Error = "";
    }

    public List<Token> RunLexer ()
    {
        string[] codeLines = SourceCode.Split('\n');
        List<Token> tokens = new List<Token>();
        Token? currentToken;

        for(int i = 0; i < codeLines.Length; i++)
        {
            while(codeLines[i].Length > 0)
            {
                currentToken = GetNextToken(ref codeLines[i]);
                if(currentToken != null && currentToken.Type != Tipo.INVALID) tokens.Add(currentToken);
            }
            Column = 1;
            Line ++;
        }

        tokens.Add(new Token(Tipo.EOF, "EOF", Line,Column));

        return tokens;
    }

    private Token? GetNextToken (ref string line)
    {
        if (line.Length == 0) return null;
        
        Match spaceSymbol = Regex.Match (line, @"^\s\s*");
        if(spaceSymbol.Success)
        {
            Column += spaceSymbol.Length;
            line = line.Substring(spaceSymbol.Length);
            return GetNextToken(ref line);
        }

        Match groupSymbol = Regex.Match(line, @"^(\(|\)|\[|\])");
        if(groupSymbol.Success)
        {
            Token token = new Token(Group[groupSymbol.Value],groupSymbol.Value,Line,Column);
            Column += groupSymbol.Length;
            line = line.Substring(groupSymbol.Length);
            return token;
        }
                        
        Match assignSymbol = Regex.Match(line,@"^<-");
        if(assignSymbol.Success)
        {
            Token token = new Token(Tipo.ASSIGN, assignSymbol.Value, Line, Column);
            Column += assignSymbol.Length;
            line = line.Substring(assignSymbol.Length);
            return token;
        }

        Match comparativeSymbol = Regex.Match(line, @"^(==|>=|<=|>|<)");
        if(comparativeSymbol.Success)
        {
            Token token = new Token(ComparativeOps[comparativeSymbol.Value], comparativeSymbol.Value, Line, Column);
            Column += comparativeSymbol.Length;
            line = line.Substring(comparativeSymbol.Length);
            return token;
        }

        Match logicSymbol = Regex.Match(line,@"^(&&|\|\|)");
        if(logicSymbol.Success)
        {
            Token token = new Token(LogicalOps[logicSymbol.Value], logicSymbol.Value, Line, Column);
            Column += logicSymbol.Length;
            line = line.Substring(logicSymbol.Length);
            return token;
        }

        Match arithmeticSymbol = Regex.Match(line,@"^(\+|\-|\*\*|\*|%|/)");
        if(arithmeticSymbol.Success)
        {
            Token token = new Token(ArithmeticOps[arithmeticSymbol.Value], arithmeticSymbol.Value, Line, Column);
            Column += arithmeticSymbol.Length;
            line = line.Substring(arithmeticSymbol.Length);
            return token;
        }

        Match numberSymbol = Regex.Match(line,@"^\d\d*\b");
        if(numberSymbol.Success)
        {
            Token token = new Token(Tipo.NUMBER, numberSymbol.Value, Line, Column);
            Column += numberSymbol.Length;
            line = line.Substring(numberSymbol.Length);
            return token;
        }

        Match commaSymbol = Regex.Match(line,@"^,");
        if(commaSymbol.Success)
        {
            Token token = new Token(Tipo.COMMA, commaSymbol.Value, Line, Column);
            Column += commaSymbol.Length;
            line = line.Substring(commaSymbol.Length);
            return token;
        }

        Match quotMark = Regex.Match(line,@"^\""");
        if(quotMark.Success)
        {
            Match endMark = Regex.Match(line.Substring(1),@"\""");
            if(endMark.Success)
            {
                Token token = new Token(Tipo.STRING, line.Substring(quotMark.Index,endMark.Index + 2), Line, Column);
                Column += quotMark.Length;
                line = line.Substring(endMark.Index + 2);
                return token;
            }
            else
            {
                Match end = Regex.Match (line, @"\b");
                Error += $"Grammar error: Missing '\"' at line {Line}, column {end.Index}\n";
                return new Token(Tipo.INVALID, "", Line, Column);
            }
        }

        Match instruction = Regex.Match(line, @"^Spawn|^GoTo|^Fill|^DrawLine|^DrawCircle|^DrawRectangle|^Size|^Color");
        if(instruction.Success && instruction.Index == 0)
        {
            Token token = new Token(Tipo.INSTRUCTION, instruction.Value, Line, Column);
            Column += instruction.Length;
            line = line.Substring(instruction.Length);
            return token;
        }

        Match function = Regex.Match(line, @"^GetActualX|^GetActualY|^GetCanvasSize|^IsBrushColor|^IsBrushSize|^IsCanvasSize|^GetColorCount");
        if(function.Success && function.Index == 0)
        {
            Token token = new Token(Tipo.FUNCTION, function.Value, Line, Column);
            Column += function.Length;
            line = line.Substring(function.Length);
            return token;
        }

        Match identifier = Regex.Match(line, @"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ][a-zA-ZáéíóúÁÉÍÓÚüÜñÑ0-9\-]*");
        if(identifier.Success && identifier.Index == 0)
        {
            Token token = new Token(Tipo.IDENTIFIER, identifier.Value, Line, Column);
            Column += identifier.Length;
            line = line.Substring(identifier.Length);
            return token;
        }


        Match boolean = Regex.Match(line, @"^true|^false");
        if(boolean.Success && boolean.Index == 0)
        {
            Token token = new Token(Tipo.BOOL, boolean.Value, Line, Column);
            Column += boolean.Length;
            line = line.Substring(boolean.Length);
            return token;
        }

        Match endOfInvalid = Regex.Match (line, @"\s|\(|\)|\[|\]|<-|==|>=|<=|>|<|&&|\|\||\+|\-|\*\*|\*|%|/");
        string invalidLexeme;
        if(endOfInvalid.Success) invalidLexeme = line.Substring(0,endOfInvalid.Index);
        else invalidLexeme = line;
        Error += $"Grammar error: Invalid token '{invalidLexeme}' at line {Line}, column {Column}\n";
        line = line.Substring(invalidLexeme.Length);
        return new Token(Tipo.INVALID, invalidLexeme, Line, Column);
    }
}         