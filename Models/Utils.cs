//BORRAR

public static class Utils
{
    public static readonly Dictionary<string, Tipo> ArithmeticOps = new Dictionary<string, Tipo>{
      
        {"+",Tipo.SUM},
        {"-",Tipo.SUB},
        {"/",Tipo.DIV},
        {"*",Tipo.MULT},
        {"**",Tipo.POW},
        {"%",Tipo.MOD}

    };
    public static readonly Dictionary<string, Tipo> LogicalOps = new Dictionary<string, Tipo>{
       
        {"&&",Tipo.AND},
        {"||",Tipo.OR}
    };
    public static readonly Dictionary<string, Tipo> ComparativeOps = new Dictionary<string, Tipo>{
       
        {"==",Tipo.EQUALS},
        {">=",Tipo.GREATER_EQUAL},
        {"<=", Tipo.LESSER_EQUAL},
        {">", Tipo.GREATER},
        {"<", Tipo.LESSER}
    };

    public static readonly Dictionary<string, Tipo> Assignation = new Dictionary<string, Tipo>{
        {"<-",Tipo.ASSIGN}
    };

    public static readonly Dictionary<string, Tipo> Group = new Dictionary<string, Tipo>{

        {"(", Tipo.L_PAREN},
        {")",Tipo.R_PAREN},
        {"[", Tipo.L_BRACKET},
        {"]",Tipo.R_BRACKET}
    };
}