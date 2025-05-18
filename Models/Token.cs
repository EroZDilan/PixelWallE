public class Token
{
    public Tipo Type{get; private set;}
    public string Lexeme{get; private set;}
    public int Line{get; private set;}
    public int Column {get; private set;}

    public Token (Tipo type, string lexeme, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Line = line;
        Column = column;
    }
}

public enum Tipo
{
    NUMBER,
    STRING,
    IDENTIFIER,
    ASSIGN,
    L_PAREN,
    R_PAREN,
    L_BRACKET,
    R_BRACKET,
    COMMA,
    SUM,
    SUB,
    MULT,
    DIV,
    POW,
    MOD,
    AND,
    OR,
    EQUALS,
    GREATER_EQUAL,
    LESSER_EQUAL,
    GREATER,
    LESSER,
    EOF,
    INVALID ,
    INSTRUCTION,
    FUNCTION,
    BOOL
    }