public class Parser
{
    private List<Token> tokens;
    private int currentPosition;
    private Token currentToken;
    public string Error {get; private set;}
    
    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
        this.currentPosition = 0;
        this.currentToken = tokens[0];
        Error = "";
    }
    
    private void Advance()
    {
        currentPosition++;
        if (currentPosition < tokens.Count)
        {
            currentToken = tokens[currentPosition];
        }
    }
    
    private Token Consume(Tipo expectedType)
    {
        if (currentToken.Type == expectedType)
        {
            Token token = currentToken;
            Advance();
            return token;
        }
        throw new Exception($"Syntax error: {expectedType} expected, found {currentToken.Type} at line {currentToken.Line}, column {currentToken.Column}");
    }
    
    // Método principal para iniciar el parseo
    public ProgramNode Parse()
    {
        List<Statement> statements = new List<Statement>();
        
        while (currentToken.Type != Tipo.EOF)
        {
            statements.Add(ParseStatement());
        }
        
        return new ProgramNode(statements, tokens[0].Line, tokens[0].Column);
    }

    private Statement ParseStatement()
    {
        if(currentToken.Type == Tipo.IDENTIFIER) 
        {
            if(LookAhead() == Tipo.ASSIGN) return ParseAssignment();
            else return ParseLabel();
        }
        else if(currentToken.Type == Tipo.INSTRUCTION) return ParseInstruction();
        else if(currentToken.Type == Tipo.FUNCTION) 
        {
            // Tratar una llamada a función como Statement
            FunctionCall functionCall = ParseFunctionCall();
            return new FunctionCallStatement(functionCall, functionCall.Line, functionCall.Column);
        }
        else
        {
            System.Console.WriteLine(currentToken.Type);
            throw new Exception($"Syntax error: Invalid statement '{currentToken.Lexeme}' at line {currentToken.Line}, column {currentToken.Column}");
        }
    }

    private Tipo LookAhead()
    {
        int position = currentPosition + 1;
        if(position < tokens.Count)
        {
            return tokens[position].Type;
        }
        return Tipo.EOF;
    }

    private LabelNode ParseLabel()
    {
        string name = currentToken.Lexeme;
        int line = currentToken.Line;
        int column = currentToken.Column;
        Consume(Tipo.IDENTIFIER);
        
        // Parsea los statements que siguen a la etiqueta
        List<Statement> statements = new List<Statement>();
        while (currentToken.Type != Tipo.EOF && 
               !(currentToken.Type == Tipo.IDENTIFIER && LookAhead() != Tipo.ASSIGN))
        {
            statements.Add(ParseStatement());
        }
        
        return new LabelNode(name, new ProgramNode(statements, line, column), line, column);
    }

    private AssigmentNode ParseAssignment()
    {
        string name = currentToken.Lexeme;
        int line = currentToken.Line;
        int column = currentToken.Column;
        Consume(Tipo.IDENTIFIER);
        Consume(Tipo.ASSIGN); // Consume "<-"
        
        ExpressionNode expression = ParseExpression();
        
        return new AssigmentNode(name, expression, line, column);
    }

    private Instruction ParseInstruction()
    {
        System.Console.WriteLine($"Instruction: {currentToken.Lexeme}");
        Token instructionToken = currentToken;
        Consume(Tipo.INSTRUCTION);
        
        if (instructionToken.Lexeme == "GoTo")
        {
            return ParseGoTo(instructionToken.Line, instructionToken.Column);
        }
        else
        {
            return ParseRegularInstruction(instructionToken);
        }
    }

    private Instruction ParseRegularInstruction(Token instructionToken)
    {
        Consume(Tipo.L_PAREN);
        List<ExpressionNode> arguments = ParseInstructionArguments();
        Consume(Tipo.R_PAREN);
        
        return new Instruction(
            instructionToken.Lexeme, 
            arguments, 
            instructionToken.Line, 
            instructionToken.Column
        );
    }

    private Instruction ParseGoTo(int line, int column)
    {
        Consume(Tipo.L_BRACKET);
        string label = currentToken.Lexeme;
        Consume(Tipo.IDENTIFIER);
        Consume(Tipo.R_BRACKET);
        
        Consume(Tipo.L_PAREN);
        BooleanExpression condition = ParseBooleanExpression();
        Consume(Tipo.R_PAREN);
        
        List<ExpressionNode> args = new List<ExpressionNode> { 
            new StringExpression(label, line, column), 
            condition 
        };
        
        return new Instruction("GoTo", args, line, column);
    }

    private List<ExpressionNode> ParseInstructionArguments()
    {
        return ParseArguments();
    }

    private List<ExpressionNode> ParseArguments()
    {
        List<ExpressionNode> arguments = new List<ExpressionNode>();
        
        while (currentToken.Type != Tipo.R_PAREN)
        {
            arguments.Add(ParseExpression());
            
            while (currentToken.Type == Tipo.COMMA)
            {
                Consume(Tipo.COMMA);
                arguments.Add(ParseExpression());
            }
        }
        
        return arguments;
    }

    private ExpressionNode ParseExpression()
    {
        if(currentToken.Type == Tipo.STRING) return ParseStringExpression();
        if(currentToken.Type == Tipo.L_PAREN)
        {
            Consume(Tipo.L_PAREN);
            ExpressionNode exp = ParseExpression();
            Consume(Tipo.R_PAREN);
            return exp;
        }

        Tipo op = LookAhead();
        if(currentToken.Type == Tipo.BOOL || Utils.LogicalOps.ContainsValue(op) || Utils.ComparativeOps.ContainsValue(op)) return ParseBooleanExpression();
        return ParseArithmeticExpression();
    }

    private StringExpression ParseStringExpression()
    {
        string value = currentToken.Lexeme;
        int line = currentToken.Line;
        int column = currentToken.Column;
        Consume(Tipo.STRING);
        return new StringExpression(value, line, column);
    }

    private ArithmeticExpression ParseArithmeticExpression()
    {
        return ParseAdditiveExpression();
    }
    
    private AdditiveExpression ParseAdditiveExpression()
    {
        MultiplicativeExpression left = ParseMultiplicativeExpression();
        int line = left.Line;
        int column = left.Column;
        
        while (currentToken.Type == Tipo.SUM || currentToken.Type == Tipo.SUB)
        {
            Tipo operatorType = currentToken.Type;
            Advance();
            MultiplicativeExpression right = ParseMultiplicativeExpression();
            return new AdditiveExpression(left, right, operatorType, line, column);
        }
        
        return new AdditiveExpression(left, line, column);
    }

    private MultiplicativeExpression ParseMultiplicativeExpression()
    {
        PowerExpression left = ParsePowerExpression();
        int line = left.Line;
        int column = left.Column;
        
        while (currentToken.Type == Tipo.MULT || currentToken.Type == Tipo.DIV || currentToken.Type == Tipo.MOD)
        {
            Tipo operatorType = currentToken.Type;
            Advance();
            PowerExpression right = ParsePowerExpression();
            return new MultiplicativeExpression(left, right, operatorType, line, column);
        }
        
        return new MultiplicativeExpression(left, line, column);
    }

    private PowerExpression ParsePowerExpression()
    {
        UnaryExpressionNode base_ = ParseUnaryExpression();
        int line = base_.Line;
        int column = base_.Column;
        
        if (currentToken.Type == Tipo.POW)
        {
            Advance();
            UnaryExpressionNode exponent = ParseUnaryExpression();
            return new PowerExpression(base_, exponent, line, column);
        }
        
        return new PowerExpression(base_, line, column);
    }
    
    private UnaryExpressionNode ParseUnaryExpression()
    {
        bool negative = false;
        int line = currentToken.Line;
        int column = currentToken.Column;
        
        while (currentToken.Type == Tipo.SUB)
        {
            negative = !negative; // Alternar el signo
            Advance();
        }
        
        PrimaryExpression expr = ParsePrimaryExpression();
        return new UnaryExpressionNode(negative, expr, line, column);
    }

    private PrimaryExpression ParsePrimaryExpression()
    {
        switch (currentToken.Type)
        {
            case Tipo.NUMBER:
                int value = int.Parse(currentToken.Lexeme);
                int line = currentToken.Line;
                int column = currentToken.Column;
                Consume(Tipo.NUMBER);
                return new NumberNode(value, line, column);
                
            case Tipo.IDENTIFIER:
                string name = currentToken.Lexeme;
                line = currentToken.Line;
                column = currentToken.Column;
                Consume(Tipo.IDENTIFIER);
                return new VariableNode(name, line, column);
                
            case Tipo.FUNCTION:
                return ParseFunctionCall();
                
            case Tipo.L_PAREN:
                line = currentToken.Line;
                column = currentToken.Column;
                Consume(Tipo.L_PAREN);
                ArithmeticExpression expr = ParseArithmeticExpression();
                Consume(Tipo.R_PAREN);
                return new ParenthesizedExpression(expr, line, column);
                
            default:
                throw new Exception($"Syntax Error: Invalid expression '{currentToken.Lexeme}' at line {currentToken.Line}, column {currentToken.Column}");
        }
    }

    private FunctionCall ParseFunctionCall()
    {
        Token functionToken = currentToken;
        Consume(Tipo.FUNCTION);
        Consume(Tipo.L_PAREN);
        List<ExpressionNode> arguments = ParseFunctionArguments();
        Consume(Tipo.R_PAREN);

        return new FunctionCall(functionToken.Lexeme, arguments, functionToken.Line, functionToken.Column);
    }

    private List<ExpressionNode> ParseFunctionArguments()
    {
        return ParseArguments();
    }

    private BooleanExpression ParseBooleanExpression()
    {
        if (currentToken.Type == Tipo.BOOL)
        {
            bool value = currentToken.Lexeme == "true";
            int line = currentToken.Line;
            int column = currentToken.Column;
            Consume(Tipo.BOOL);
            return new BoolLiteralNode(value, line, column);
        }
        return ParseOrExpression();
    }
    
    private OrExpression ParseOrExpression()
    {
        AndExpression left = ParseAndExpression();
        int line = left.Line;
        int column = left.Column;
        
        while (currentToken.Type == Tipo.OR)
        {
            Advance();
            AndExpression right = ParseAndExpression();
            return new OrExpression(left, right, line, column);
        }
        
        return new OrExpression(left, line, column);
    }
    
    private AndExpression ParseAndExpression()
    {
        ComparisonExpression left = ParseComparisonExpression();
        int line = left.Line;
        int column = left.Column;
        
        while (currentToken.Type == Tipo.AND)
        {
            Advance();
            ComparisonExpression right = ParseComparisonExpression();
            return new AndExpression(left, right, line, column);
        }
        
        return new AndExpression(left, line, column);
    }

    private ComparisonExpression ParseComparisonExpression()
    {
        ExpressionNode left;
        int line = currentToken.Line;
        int column = currentToken.Column;
        
        if (currentToken.Type == Tipo.BOOL)
        {
            left = ParseBooleanExpression();
        }

        else if (currentToken.Type == Tipo.L_PAREN)
        {
            Consume(Tipo.L_PAREN);
            left = ParseBooleanExpression();
            Consume(Tipo.R_PAREN);
        }

        else left = ParseArithmeticExpression();
        
        if (IsBooleanOperator(currentToken.Type))
        {
            Tipo operatorType = currentToken.Type;
            Advance();
            
            ExpressionNode right;
            if (currentToken.Type == Tipo.BOOL)
            {
                right = ParseBooleanExpression();
            }
            else
            {
                right = ParseArithmeticExpression();
            }
            
            return new ComparisonExpression(left, operatorType, right, line, column);
        }

        return new ComparisonExpression(left, line, column);
    }

    private bool IsBooleanOperator(Tipo type)
    {
        return type == Tipo.EQUALS || 
               type == Tipo.GREATER || 
               type == Tipo.LESSER || 
               type == Tipo.GREATER_EQUAL || 
               type == Tipo.LESSER_EQUAL;
    }
}