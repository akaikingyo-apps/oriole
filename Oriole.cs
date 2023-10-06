using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Oriole
{
    class Program
    {
        public const string VERSION = "Version 0.83 Build 13 Nov 2013";

        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Program.ShowUsage();
                    return;
                }

                Dictionary<string, string[]> arguments = Program.ParseArguments(args);

                foreach (string action in arguments.Keys)
                {
                    string[] parameters = arguments[action];

                    switch (action)
                    {
                        case "run":
                        case "profile":
                            if (parameters.Length == 0)
                            {
                                Program.ShowUsage();
                                return;
                            }
                            int index = parameters[0].LastIndexOf('.');
                            if (index == -1)
                            {
                                Program.ShowUsage();
                                return;
                            }

                            string module = parameters[0].Substring(0, index);

                            object[] data = new object[parameters.Length - 1];
                            for (int i = 1; i < parameters.Length; i++)
                            {
                                data[i - 1] = parameters[i];
                            }
                            RuntimeEngine engine = new RuntimeEngine();
                            engine.ShowStatistics = action == "profile";
                            engine.Execute(module, parameters[0], data);
                            break;

                        case "compile":
                            string moduleToCompile = parameters[0];
                            if (moduleToCompile.EndsWith(".s"))
                            {
                                moduleToCompile = moduleToCompile.Substring(0, moduleToCompile.Length - 2);
                            }
                            bool generateAssembly = arguments.ContainsKey("asm");
                            bool optimize = arguments.ContainsKey("o");

                            Compiler parser = new Compiler();
                            Code code = parser.Compile(File.ReadAllText(moduleToCompile + ".s"));                            
                            if (optimize)
                            {
                                Optimizer optimizer = new Optimizer();
                                code = optimizer.Optimize(code);
                            }
                            if (generateAssembly)
                            {
                                File.WriteAllText(moduleToCompile + ".a", code.ToString());
                            }
                            code.Write(moduleToCompile + ".m");
                            break;
                        case "asm":
                        case "o":
                            break;
                        case "version":
                            Console.WriteLine(Program.VERSION);
                            break;
                        case "usage":
                        case "help":
                        default:
                            Program.ShowUsage();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
                Console.ReadKey();
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("[Oriole] Compiler, Optimizer and Runtime Engine, {0}", Program.VERSION);
            Console.WriteLine("Usage: Oriole /<command> [parameters..]");
            Console.WriteLine("Commands:");
            Console.WriteLine("/run <module>.<class> [<parameters>..]");
            Console.WriteLine("/profile <module>.<class> [<parameters>..]");
            Console.WriteLine("/compile <source>[.s] [/asm] [/o]");
        }

        private static Dictionary<string, string[]> ParseArguments(string[] arguments)
        {
            Dictionary<string, string[]> parameters = new Dictionary<string, string[]>();
            string key = null;
            List<string> values = new List<string>();
            foreach (string argument in arguments)
            {
                if (argument.StartsWith("/") && key != "run")
                {
                    if (key != null)
                    {
                        parameters.Add(key.Substring(1), values.ToArray());
                    }
                    key = argument;
                    values.Clear();
                }
                else
                {
                    values.Add(argument);
                }
            }
            if (key != null)
            {
                parameters.Add(key.Substring(1), values.ToArray());
            }
            return parameters;
        }
    }    

    public interface IFacadeClass
    {
        string[] GetStaticFields();
        string[] GetInstanceFields();
        string[] GetStaticMethodSignatures();
        string[] GetInstanceMethodSignatures();
        object GetField(string fieldName);
        void SetField(string fieldName, object value);
        object CallMethod(string methodSignature, object[] arguments);
    }

    public class Constant
    {
        public object Value;
        public Constant(object value)
        {
            this.Value = value;
        }
    }

    public class Identifier
    {
        public string Name;
        public Identifier(string name)
        {
            this.Name = name;
        }
    }

    public class ArrayReference
    {
    }

    public class ClassReference
    {
    }

    public class Expression
    {
    }

    public class MethodCall
    {
    }

    public class FieldReference
    {
        public string FieldName;
        public int FieldNumber;
    }

    public class Variable
    {
        public string VariableName;
        public int VariableNumber;        
    }

    public enum TokenType
    {
        Delimiter, Literal, Identifier, Operator, EOF
    }

    public class Token
    {
        private TokenType _tokenType;
        private object _value;
        private int _line;
        private int _column;

        public Token(TokenType tokenType, object value, int line, int column)
        {
            this._tokenType = tokenType;
            this._value = value;
            this._line = line;
            this._column = column;
        }

        public TokenType TokenType
        {
            get
            {
                return this._tokenType;
            }
        }

        public object Value
        {
            get
            {
                return this._value;
            }
        }
        
        public bool IsOperator(string oper)
        {
            return this._tokenType == TokenType.Operator && (string)this._value == oper;
        }

        public bool IsDelimiter(string delimiter)
        {
            return this._tokenType == TokenType.Delimiter && (string)this._value == delimiter;
        }

        public bool IsKeyword(string keyword)
        {
            return this._tokenType == TokenType.Identifier && (string)this._value == keyword;
        }

        public override string ToString()
        {
            return string.Format("<{0}:{1}>", this._tokenType, this._value);
        }
    }

    public class LexicalAnalyzer
    {
        private StreamReader _reader;
        private StringBuilder _buffer = new StringBuilder(100);
        private List<Token> _tokens = new List<Token>();
        private int _position = 0;
        private int _line = 1;
        private int _column = 1;

        public LexicalAnalyzer(Stream stream)
        {
            this._reader = new StreamReader(stream);
        }

        public string GetLine(int startLine, int endColumn, int linesToShow)
        {
            startLine -= linesToShow - 1;
            if (startLine < 1)
            {
                startLine = 1;
            }
            long savePosition = this._reader.BaseStream.Position;
            this._reader.BaseStream.Seek(0, SeekOrigin.Begin);
            this._reader.DiscardBufferedData();
            int line = 1;
            int column = 1;
            while (true)
            {
                if (startLine == line)
                {
                    break;
                }
                string q = this._reader.ReadLine();
                if (q == null)
                {
                    break;
                }
                line++;
                if (startLine == line)
                {
                    break;
                }                
            }
            string s = "";
            for (int i = 0; i < linesToShow; i++)
            {
                if (i != linesToShow - 1)
                {
                    s += this._reader.ReadLine().Trim() + "\n";
                }
                else
                {
                    s += this._reader.ReadLine().Substring(0, column).Trim();
                }
            }
            this._reader.BaseStream.Seek(savePosition, SeekOrigin.Begin);
            return s;
        }

        public string GetExceptionMessage(string message, params object[] parameters)
        {
            return string.Format("{0} at line {1} column {2} ==>\n{3}", string.Format(message, parameters), this._line, this._column, this.GetLine(this._line, this._column, 3));
        }

        private Token FetchToken()
        {
            this._buffer.Length = 0;
            int n, m;

            while (true)
            {
                while (char.IsWhiteSpace((char)this._reader.Peek()))
                {
                    if ((char)this._reader.Read() == '\n')
                    {
                        this._line++;
                        this._column = 1;
                    }
                    else
                    {
                        this._column++;
                    }
                }

                char ch = (char)this._reader.Peek();

                if (this._reader.Peek() == -1)
                {
                    return new Token(TokenType.EOF, null, this._line, this._column);
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    while (char.IsLetterOrDigit((char)this._reader.Peek()) || this._reader.Peek() == '_')
                    {
                        this._buffer.Append((char)this._reader.Read());
                    }
                    return new Token(TokenType.Identifier, this._buffer.ToString(), this._line, this._column);
                }
                else if (char.IsDigit(ch))
                {
                    int period = 0;
                    while (char.IsDigit((char)this._reader.Peek()) || this._reader.Peek() == '.')
                    {
                        if (this._reader.Peek() == '.')
                        {
                            period++;
                        }
                        this._buffer.Append((char)this._reader.Read());
                    }
                    bool isLong = false;
                    if (this._reader.Peek() == 'L')
                    {
                        isLong = true;
                        this._reader.Read();
                    }
                    if (period > 1)
                    {
                        throw new Exception(string.Format("Syntax error at: {0}", this._reader.ReadLine()));
                    }
                    else if (period == 1)
                    {
                        return new Token(TokenType.Literal, double.Parse(this._buffer.ToString()), this._line, this._column);
                    }
                    else if (isLong)
                    {
                        return new Token(TokenType.Literal, long.Parse(this._buffer.ToString()), this._line, this._column);
                    }
                    else
                    {
                        try
                        {
                            return new Token(TokenType.Literal, int.Parse(this._buffer.ToString()), this._line, this._column);
                        }
                        catch
                        {
                            return new Token(TokenType.Literal, long.Parse(this._buffer.ToString()), this._line, this._column);
                        }
                    }
                }
                else if (ch == '\"')
                {
                    this._reader.Read();
                    while (this._reader.Peek() != -1)
                    {
                        ch = (char)this._reader.Peek();
                        if (ch == '\\')
                        {
                            this._reader.Read();
                            n = this._reader.Read();
                            if (n == -1)
                            {
                                throw new Exception(string.Format("Syntax error at: {0}", this._reader.ReadLine()));
                            }
                            ch = (char)n;
                            if (ch == 'n')
                            {
                                ch = '\n';
                            }
                            else if (ch == 'r')
                            {
                                ch = '\r';
                            }
                            else if (ch == '\'')
                            {
                                ch = '\'';
                            }
                            else if (ch == '\"')
                            {
                                ch = '\"';
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (ch == '\"')
                        {
                            this._reader.Read();
                            return new Token(TokenType.Literal, this._buffer.ToString(), this._line, this._column);
                        }
                        else
                        {
                            ch = (char)this._reader.Read();
                        }
                        this._buffer.Append(ch);
                    }
                    throw new Exception(string.Format("Syntax error at: {0}", this._reader.ReadLine()));
                }
                else if (ch == '\'')
                {
                    this._reader.Read();
                    n = this._reader.Read();
                    if ((char)n == '\\')
                    {
                        switch (this._reader.Read())
                        {
                            case 'n':
                                n = '\n';
                                break;
                            case 'r':
                                n = '\r';
                                break;
                            case 't':
                                n = '\t';
                                break;
                            default:
                                throw new Exception("Unknown escape sequence: \\" + (char)n);
                        }                        
                    }
                    if (n == -1)
                    {
                        throw new Exception("Missing \"'\"");
                    }
                    if ((char)this._reader.Read() != '\'')
                    {
                        throw new Exception("Missing \"'\"");
                    }

                    return new Token(TokenType.Literal, (char)n, this._line, this._column);
                }
                else if (ch == '=' || ch == '&' || ch == '|' || ch == '<' || ch == '>')
                {
                    this._buffer.Append((char)this._reader.Read());
                    n = this._reader.Peek();
                    if (n != -1 && (ch == (char)n || (char)n == '='))
                    {
                        this._buffer.Append(ch = (char)this._reader.Read());
                        n = this._reader.Peek();
                        if (n != -1 && (char)n == '=' && ch != '=')
                        {
                            this._buffer.Append((char)this._reader.Read());
                        }
                    }
                    return new Token(TokenType.Operator, this._buffer.ToString(), this._line, this._column);
                }
                else if (ch == '+' || ch == '-' || ch == '*' || ch == '%' || ch == '^' || ch == '!' || ch == '~' || ch == '/')
                {
                    if (ch == '/')
                    {
                        this._reader.Read();
                        n = this._reader.Peek();

                        if (n != -1)
                        {
                            ch = (char)n;
                            if (ch == '/')
                            {
                                do
                                {
                                    n = this._reader.Read();
                                    this._column ++;
                                }
                                while (n != -1 && (char)n != '\n');
                                if ((char)n == '\n')
                                {
                                    this._line++;
                                    this._column = 1;
                                }
                                continue;
                            }
                            else if (ch == '*')
                            {
                                n = m = this._reader.Read();
                                
                                while (true)
                                {
                                    n = this._reader.Read();
                                    this._column ++;
                                    if (n == -1)
                                    {
                                        break;
                                    }
                                    else if (n == '*')
                                    {
                                        m = this._reader.Read();
                                        if (m == -1 || m == '/')
                                        {
                                            break;
                                        }
                                    }
                                    else if (n == '\n')
                                    {
                                        this._line++;
                                        this._column = 1;
                                    }
                                }
                                if (n == -1 || m == -1)
                                {
                                    throw new Exception(string.Format("Syntax error at: {0}", this._reader.ReadLine()));
                                }
                                continue;
                            }
                            else
                            {
                                this._buffer.Append("/");
                            }
                        }
                        ch = '/';
                    }
                    else
                    {
                        this._buffer.Append((char)this._reader.Read());
                    }

                    n = this._reader.Peek();
                    if (n != -1)
                    {
                        if ((char)n == '=')
                        {
                            this._buffer.Append((char)this._reader.Read());
                        }
                        else if ((ch == '+' || ch == '-') && (char)n == ch)
                        {
                            this._buffer.Append((char)this._reader.Read());
                        }
                    }
                    return new Token(TokenType.Operator, this._buffer.ToString(), this._line, this._column);
                }
                else if (ch == '[' || ch == ']' || ch == '(' || ch == ')' || ch == '.' || ch == '?' || ch == ':')
                {
                    this._buffer.Append((char)this._reader.Read());
                    if (ch == ':' && (char)this._reader.Peek() == ':')
                    {
                        this._buffer.Append((char)this._reader.Read());
                    }
                    return new Token(TokenType.Operator, this._buffer.ToString(), this._line, this._column);
                }
                else if (ch == ';' || ch == ',' || ch == '{' || ch == '}')
                {
                    this._buffer.Append((char)this._reader.Read());
                    return new Token(TokenType.Delimiter, this._buffer.ToString(), this._line, this._column);
                }
                else
                {
                    throw new Exception(string.Format("Syntax error at: {0}", this._reader.ReadLine()));
                }
            }
        }

        public Token NextToken()
        {
            Token token = this.PeekToken();
            this._position++;
            return token;
        }

        public Token PeekToken()
        {
            while (this._tokens.Count < this._position + 1)
            {
                this._tokens.Add(this.FetchToken());
            }
            return this._tokens[this._position];
        }

        public void Putback(Token token)
        {
            this._position--;
        }

        public int Position
        {
            get
            {
                return this._position;
            }
            set
            {
                this._position = value;
            }
        }

        public void NeedOperator(string oper)
        {
            if (!this.NextToken().IsOperator(oper))
            {
                throw new Exception(this.GetExceptionMessage("Expects '{0}'", oper));
            }
        }

        public void NeedDelimiter(string delimiter)
        {
            if (!this.NextToken().IsDelimiter(delimiter))
            {
                throw new Exception(this.GetExceptionMessage("Expects '{0}'", delimiter));
            }
        }

        public string NeedIdentifier()
        {
            Token token = this.NextToken();
            if (token.TokenType != TokenType.Identifier)
            {
                throw new Exception(this.GetExceptionMessage("Expects an identifier"));
            }
            return (string)token.Value;
        }

        public string NeedStringLiteral()
        {
            Token token = this.PeekToken();
            if (token.TokenType == TokenType.Literal && token.Value is string)
            {
                this.NextToken();
                return (string)token.Value;
            }
            throw new Exception(this.GetExceptionMessage("Expects a string literal"));
        }

        public int NeedIntegerLiteral()
        {
            Token token = this.PeekToken();
            if (token.TokenType == TokenType.Literal && token.Value is int)
            {
                this.NextToken();
                return (int)token.Value;
            }
            throw new Exception(this.GetExceptionMessage("Expects a integer literal"));
        }

        public bool MatchKeyword(string keyword)
        {
            if (this.PeekToken().IsKeyword(keyword))
            {
                this.NextToken();
                return true;
            }
            return false;
        }

        public bool MatchDelimiter(string delimiter)
        {
            if (this.PeekToken().IsDelimiter(delimiter))
            {
                this.NextToken();
                return true;
            }
            return false;
        }

        public bool MatchOperator(string oper)
        {
            if (this.PeekToken().IsOperator(oper))
            {
                this.NextToken();
                return true;
            }
            return false;
        }

        public string GetDotSeparatedIdentifier(string errorMessage)
        {
            StringBuilder s = new StringBuilder();
            while (true)
            {
                Token token = this.NextToken();
                if (token.TokenType != TokenType.Identifier)
                {
                    throw new Exception(errorMessage);
                }
                if (s.Length > 0)
                {
                    s.Append(".");
                }
                s.Append((string)token.Value);
                if (!this.MatchOperator("."))
                {
                    break;
                }
            }
            return s.ToString();
        }
    }

    public class StringInfo
    {
        public string Value;
        public int StringNumber;
    }

    public class ClassInfo
    {
        public string ClassName;
        public int ClassNumber;
        public bool IsFacade;
        public bool IsImplicitClass;
        public string TypeName;
        public string AssemblyFile;
        public string ParentClassName;
        public string ImplicitDataType;
    }

    public class FieldInfo
    {
        public string FieldName;
        public int FieldNumber;
        public bool IsStatic;
    }

    public class MethodInfo
    {
        public string MethodName;
        public int MethodNumber;
        public string ReturnType;
        public int Arguments;
        public int Variables;
        public int CodeAddress;
        public int Module;
        public bool IsStatic;
    }

    public class EnumerationInfo
    {
        public string Name;
        public Dictionary<string, int> Fields;
    }

    public class ClassReferenceInfo
    {
        public string ClassName;
        public int ClassNumber;
    }

    public class FieldAccessInfo
    {
        public string FieldName;
        public int FieldAccessNumber;
    }

    public class MethodCallInfo
    {
        public string MethodSignature;
        public int MethodCallNumber;
    }

    public class Assembler
    {
        private Dictionary<string, int> _labels = new Dictionary<string, int>();
        private List<object[]> _labelsToResolve = new List<object[]>();

        public void Assemble(LexicalAnalyzer lex, Code code, Compiler compiler, Dictionary<string, ClassReferenceInfo> classes, Dictionary<string,List<string>> classNameMap, Dictionary<string, FieldAccessInfo> fields, Dictionary<string, MethodCallInfo> methods, Dictionary<string, Variable> variables, Dictionary<string, StringInfo> literals)
        {
            while (true)
            {
                Token token = lex.PeekToken();
                if (token.TokenType == TokenType.Identifier)
                {
                    lex.NextToken();
                    string oper = (string)token.Value;

                    if (oper == "nop")
                    {
                        code.Nop();
                    }
                    else if (oper == "dup")
                    {
                        code.Dup();
                    }
                    else if (oper == "add")
                    {
                        code.Add();
                    }
                    else if (oper == "sub")
                    {
                        code.Sub();
                    }
                    else if (oper == "mul")
                    {
                        code.Mul();
                    }
                    else if (oper == "div")
                    {
                        code.Div();
                    }
                    else if (oper == "mod")
                    {
                        code.Mod();
                    }
                    else if (oper == "neg")
                    {
                        code.Neg();
                    }
                    else if (oper == "and")
                    {
                        code.And();
                    }
                    else if (oper == "or")
                    {
                        code.Or();
                    }
                    else if (oper == "xor")
                    {
                        code.Xor();
                    }
                    else if (oper == "not")
                    {
                        code.Not();
                    }
                    else if (oper == "lshift")
                    {
                        code.LShift();
                    }
                    else if (oper == "rshift")
                    {
                        code.RShift();
                    }
                    else if (oper == "inc")
                    {
                        string variableName = lex.NeedIdentifier();
                        if (!variables.ContainsKey(variableName))
                        {
                            throw new Exception(string.Format("Unresolved variable '{0}'", variableName));
                        }
                        code.Inc(variables[variableName].VariableNumber);
                    }
                    else if (oper == "dec")
                    {
                        string variableName = lex.NeedIdentifier();
                        if (!variables.ContainsKey(variableName))
                        {
                            throw new Exception(string.Format("Unresolved variable '{0}'", variableName));
                        }
                        code.Dec(variables[variableName].VariableNumber);
                    }
                    else if (oper == "ce")
                    {
                        code.Ce();
                    }
                    else if (oper == "cne")
                    {
                        code.Cne();
                    }
                    else if (oper == "cl")
                    {
                        code.Cl();
                    }
                    else if (oper == "cle")
                    {
                        code.Cle();
                    }
                    else if (oper == "cg")
                    {
                        code.Cg();
                    }
                    else if (oper == "cge")
                    {
                        code.Cge();
                    }
                    else if (oper == "br")
                    {
                        string label = lex.NeedIdentifier();

                        if (this._labels.ContainsKey(label))
                        {
                            code.Br(this._labels[label] - code.Position - 5);
                        }
                        else
                        {
                            this._labelsToResolve.Add(new object[] { label, code.Br(0) });
                        }
                    }
                    else if (oper == "btrue")
                    {
                        string label = lex.NeedIdentifier();

                        if (this._labels.ContainsKey(label))
                        {
                            code.BTrue(this._labels[label] - code.Position - 5);
                        }
                        else
                        {
                            this._labelsToResolve.Add(new object[] { label, code.BTrue(0) });
                        }
                    }
                    else if (oper == "bfalse")
                    {
                        string label = lex.NeedIdentifier();

                        if (this._labels.ContainsKey(label))
                        {
                            code.BFalse(this._labels[label] - code.Position - 5);
                        }
                        else
                        {
                            this._labelsToResolve.Add(new object[] { label, code.BFalse(0) });
                        }
                    }
                    else if (oper == "loadnull")
                    {
                        code.LoadC(new Constant(null));
                    }
                    else if (oper == "loadbyte")
                    {
                        token = lex.NextToken();
                        if (token.TokenType != TokenType.Literal || !(token.Value is char))
                        {
                            throw new Exception("Expects character for loadbyte");
                        }
                        code.LoadC(new Constant(token.Value));
                    }
                    else if (oper == "loadint")
                    {
                        token = lex.NextToken();
                        if (token.TokenType != TokenType.Literal || !(token.Value is int))
                        {
                            throw new Exception("Expects integer for loadint");
                        }
                        code.LoadC(new Constant(token.Value));
                    }
                    else if (oper == "loadfloat")
                    {
                        token = lex.NextToken();
                        if (token.TokenType != TokenType.Literal || !(token.Value is float || token.Value is int))
                        {
                            throw new Exception("Expects numeric for float");
                        }
                        code.LoadC(new Constant((float)token.Value));
                    }
                    else if (oper == "loadstring")
                    {
                        string literal = lex.NeedStringLiteral();
                        if (!literals.ContainsKey(literal))
                        {
                            StringInfo info = new StringInfo();
                            info.Value = literal;
                            info.StringNumber = literals.Count + 1;
                            literals.Add(literal, info);
                        }
                        code.LoadS(literals[literal].StringNumber);
                    }
                    else if (oper == "loadarray")
                    {
                        code.LoadA();
                    }
                    else if (oper == "storearray")
                    {
                        code.StoreA();
                    }
                    else if (oper == "loadfield" || oper == "storefield")
                    {
                        string fieldName = lex.NeedIdentifier();
                        if (!fields.ContainsKey(fieldName))
                        {
                            FieldAccessInfo info = new FieldAccessInfo();
                            info.FieldName = fieldName;
                            info.FieldAccessNumber = fields.Count + 1;
                            fields.Add(fieldName, info);
                        }
                        if (oper == "loadfield")
                        {
                            code.LoadF(fields[fieldName].FieldAccessNumber);
                        }
                        else
                        {
                            code.StoreF(fields[fieldName].FieldAccessNumber);
                        }
                    }
                    else if (oper == "loadclass" || oper == "newinstance")
                    {
                        string className = compiler.ResolveFullClassName(lex.GetDotSeparatedIdentifier("Expects class after loadclass/newinstance."));
                        string shortClassName = compiler.GetShortClassName(className);
                        if (!classes.ContainsKey(className))
                        {
                            ClassReferenceInfo info = new ClassReferenceInfo();
                            info.ClassName = className;
                            info.ClassNumber = classes.Count + 1;
                            classes.Add(className, info);
                            if (classNameMap.ContainsKey(shortClassName))
                            {
                                classNameMap[shortClassName].Add(className);
                            }
                            else
                            {
                                List<string> list = new List<string>();
                                list.Add(className);
                                classNameMap.Add(shortClassName, list);
                            }
                        }
                        if (oper == "loadclass")
                        {
                            code.LoadClass(classes[className].ClassNumber);
                        }
                        else
                        {
                            code.NewInstance(classes[className].ClassNumber);
                        }
                    }
                    else if (oper == "call" || oper == "callc")
                    {
                        string methodSignature = lex.NeedStringLiteral();
                        if (!methods.ContainsKey(methodSignature))
                        {
                            MethodCallInfo info = new MethodCallInfo();
                            info.MethodSignature = methodSignature;
                            info.MethodCallNumber = methods.Count + 1;
                            methods.Add(methodSignature, info);
                        }
                        if (oper == "call")
                        {
                            code.Call(methods[methodSignature].MethodCallNumber);
                        }
                        else
                        {
                            code.CallC(methods[methodSignature].MethodCallNumber);
                        }
                    }
                    else if (oper == "loadvar" || oper == "storevar")
                    {
                        string variableName = lex.NeedIdentifier();
                        if (!variables.ContainsKey(variableName))
                        {
                            throw new Exception(string.Format("Unresolved variable '{0}'", variableName));
                        }
                        if (oper == "loadvar")
                        {
                            code.LoadV(variables[variableName].VariableNumber);
                        }
                        else
                        {
                            code.StoreV(variables[variableName].VariableNumber);
                        }
                    }
                    else if (oper == "loadzero")
                    {
                        code.LoadC(new Constant(0));
                    }
                    else if (oper == "super")
                    {
                        code.Super();
                    }
                    else if (oper == "castbyte")
                    {
                        code.CastByte();
                    }
                    else if (oper == "castchar")
                    {
                        code.CastByte();
                    }
                    else if (oper == "castint")
                    {
                        code.CastInt();
                    }
                    else if (oper == "castlong")
                    {
                        code.CastLong();
                    }
                    else if (oper == "castfloat")
                    {
                        code.CastFloat();
                    }
                    else if (oper == "caststring")
                    {
                        code.CastString();
                    }
                    else if (oper == "ret")
                    {
                        code.Ret();
                    }
                    else if (oper == "retc")
                    {
                        code.RetC();
                    }
                    else if (oper == "retv")
                    {
                        code.RetV();
                    }
                    else if (oper == "pop")
                    {
                        code.Pop();
                    }
                    else if (oper == "ret")
                    {
                        code.Ret();
                    }
                    else if (oper == "newarray")
                    {
                        code.NewArray();
                    }
                    else if (oper == "size")
                    {
                        code.Size();
                    }
                    else if (oper == "hash")
                    {
                        code.Hash();
                    }
                    else if (oper == "time")
                    {
                        code.Time();
                    }
                    else if (oper == "loadmodule")
                    {
                        code.LoadModule();
                    }
                    else if (oper == "classname")
                    {
                        code.ClassName();
                    }
                    else if (oper == "hierarchy")
                    {
                        code.Hierarchy();
                    }
                    else if (oper == "staticfields")
                    {
                        code.StaticFields();
                    }
                    else if (oper == "fields")
                    {
                        code.Fields();
                    }
                    else if (oper == "staticmethods")
                    {
                        code.StaticMethods();
                    }
                    else if (oper == "methods")
                    {
                        code.Methods();
                    }
                    else if (oper == "createinstance")
                    {
                        code.CreateInstance();
                    }
                    else if (oper == "getfield")
                    {
                        code.GetField();
                    }
                    else if (oper == "setfield")
                    {
                        code.SetField();
                    }
                    else if (oper == "invokemethod")
                    {
                        code.InvokeMethod();
                    }
                    else if (oper == "invokeconstructor")
                    {
                        code.InvokeConstructor();
                    }
                    else if (oper == "esc")
                    {
                        string literal = lex.NeedStringLiteral();
                        if (!literals.ContainsKey(literal))
                        {
                            StringInfo info = new StringInfo();
                            info.Value = literal;
                            info.StringNumber = literals.Count + 1;
                            literals.Add(literal, info);
                        }
                        code.Esc(literals[literal].StringNumber);
                    }
                    else if (oper == "version")
                    {
                        code.Version();
                    }
                    else if (oper == "fork")
                    {
                        code.Fork();
                    }
                    else if (oper == "join")
                    {
                        code.Join();
                    }
                    else if (oper == "sleep")
                    {
                        code.Sleep();
                    }
                    else if (oper == "nice")
                    {
                        code.Nice();
                    }
                    else if (oper == "wait")
                    {
                        code.Wait();
                    }
                    else if (oper == "signal")
                    {
                        code.Signal();
                    }
                    else if (oper == "exit")
                    {
                        code.Exit();
                    }
                    else if (oper == "halt")
                    {
                        code.Halt();
                    }
                    else if (oper == "curthread")
                    {
                        code.CurThread();
                    }
                    else // label
                    {
                        lex.NeedOperator(":");
                        if (this._labels.ContainsKey(oper))
                        {
                            throw new Exception("Label defined more than once: " + oper);
                        }
                        this._labels.Add(oper, code.Position);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void ResolveLabels(Code code)
        {
            foreach (object[] item in this._labelsToResolve)
            {
                string label = (string)item[0];
                int position = (int)item[1];
                if (!this._labels.ContainsKey(label))
                {
                    throw new Exception("Unresolve label: " + label);
                }
                code.Seek(this._labels[label]);
                code.AnchorLabel(position);
            }
        }
    }

    public class Compiler
    {
        private LexicalAnalyzer _lex;
        private Code _code;
        private Assembler _asm;
        
        private Dictionary<string, ClassReferenceInfo> _allClasses = new Dictionary<string, ClassReferenceInfo>();
        private Dictionary<string, FieldAccessInfo> _allFields = new Dictionary<string, FieldAccessInfo>();
        private Dictionary<string, MethodCallInfo> _allMethods = new Dictionary<string, MethodCallInfo>();
        private Dictionary<string, EnumerationInfo> _allEnums = new Dictionary<string, EnumerationInfo>();        
        private string _currentClass;
        private string _currentMethod;

        private List<string> _dependentModules = new List<string>();
        private string _module;

        private Dictionary<string, List<string>> _classNameMap = new Dictionary<string, List<string>>();
        private Dictionary<string, ClassInfo> _classes = new Dictionary<string, ClassInfo>();
        private Dictionary<string, Dictionary<string, MethodInfo>> _classMethods = new Dictionary<string, Dictionary<string, MethodInfo>>();
        private Dictionary<string, Dictionary<string, FieldInfo>> _classStaticFields = new Dictionary<string, Dictionary<string, FieldInfo>>();
        private Dictionary<string, Dictionary<string, FieldInfo>> _classInstanceFields = new Dictionary<string, Dictionary<string, FieldInfo>>();
        private Dictionary<string, Dictionary<string, Dictionary<string, Variable>>> _localVariables = new Dictionary<string, Dictionary<string, Dictionary<string, Variable>>>();
        private Dictionary<string, StringInfo> _stringLiterals = new Dictionary<string, StringInfo>();
        private Dictionary<string, EnumerationInfo> _enums = new Dictionary<string, EnumerationInfo>();

        // looping labels
        private List<int> _continueLabels = null;
        private List<int>_breakLabels = null;

        // return statement detection
        private bool _returnStatementExists = false;

        // global parameters

        private int _mappingTableSizeThreshold = 100;
        private int _mappingTableUsageThreshold = 10;
        private int _minimumCaseThreshold = 3;

        #region Utilities

        private void CheckComparisionOperands(object left, object right, bool equalityOnly)
        {
            if (left is Constant && right is Constant)
            {
                if (((Constant)left).Value.GetType() != ((Constant)right).Value.GetType())
                {
                    throw new Exception("Operand must of the same type for comparison");
                }
                else if (!equalityOnly && !(((Constant)left).Value is int || ((Constant)left).Value is double))
                {
                    throw new Exception("Operand must of the numerical type for comparison");
                }
            }
        }

        private void CheckNumericalOperands(object left, object right)
        {
            bool isLeftNumeric = !(left is Constant) || ((Constant)left).Value is int || ((Constant)left).Value is double;
            bool isRightNumeric = !(right is Constant) || ((Constant)right).Value is int || ((Constant)right).Value is double;
            if (!isLeftNumeric && !isRightNumeric)
            {
                throw new Exception("Expects numerical operands");
            }
        }

        private void CheckIntegralOperands(object left, object right)
        {
            bool isLeftInteger = !(left is Constant) || ((Constant)left).Value is int;
            bool isRightInteger = !(right is Constant) || ((Constant)right).Value is int;
            if (!isLeftInteger && !isRightInteger)
            {
                throw new Exception("Expects integral operands");
            }
        }

        private void CheckBooleanOperands(object left, object right)
        {
            bool isLeftBoolean = !(left is Constant) || ((Constant)left).Value is bool;
            bool isRightBoolean = !(right is Constant) || ((Constant)right).Value is bool;
            if (!isLeftBoolean && !isRightBoolean)
            {
                throw new Exception("Expects boolean operands");
            }
        }

        private int ComputeArguments(string methodSignature)
        {
            int start = methodSignature.IndexOf("(");
            int end = methodSignature.IndexOf(")");
            return int.Parse(methodSignature.Substring(start + 1, end - start - 1));
        }

        protected internal string ResolveFullClassName(string className)
        {
            if (className.Contains("."))
            {
                return className;
            }
            else
            {
                if (this._classNameMap.ContainsKey(className))
                {
                    if (this._classNameMap[className].Count > 1)
                    {
                        throw new Exception(string.Format("Ambiguous class: {0}", className));
                    }
                    else
                    {
                        return this._classNameMap[className][0];
                    }
                }
                else
                {
                    return this._module + "." + className;
                }
            }
        }

        protected internal string GetShortClassName(string fullClassName)
        {
            return fullClassName.Substring(fullClassName.LastIndexOf('.') + 1);
        }

        #endregion

        #region Top-down recursive parsing

        private object ParseAtom()
        {
            Token token = this._lex.NextToken();
            if (token.TokenType == TokenType.Literal)
            {
                return new Constant(token.Value);
            }
            else if (token.TokenType == TokenType.Identifier)
            {
                string identifier = (string)token.Value;

                switch (identifier)
                {
                    case "true":
                        return new Constant(true);
                    case "false":
                        return new Constant(false);
                    case "null":
                        return new Constant(null);
                    case "empty":
                        return new Constant("");
                    case "super":
                        this._code.LoadV(1); // this
                        this._code.Super();
                        return new ClassReference();
                    default:
                        return new Identifier(identifier);
                }
            }
            else if (token.IsOperator("<")) // full class name
            {
                string fullClassName = this._lex.GetDotSeparatedIdentifier("Expects identifier in '<>'");
                this._lex.NeedOperator(">");
                return new Identifier(fullClassName);
            }
            else if (token.IsOperator("("))
            {
                object result = this.ParseAssignment();
                if (!this._lex.NextToken().IsOperator(")"))
                {
                    throw new Exception("Expects ')'");
                }
                return result;
            }
            else
            {
                throw new Exception("Expects identifier");
            }
        }

        private object ParsePrimary()
        {
            object left = null;

            Token token = this._lex.PeekToken();

            if (token.TokenType == TokenType.Identifier && (string)token.Value == "new")
            {
                this._lex.NextToken();

                #region new
                if (this._lex.MatchOperator("["))
                {
                    object result = this.ParseAssignment();
                    this._lex.NeedOperator("]");
                    this.Load(result, this._code);
                    this._code.NewArray();
                    left = new Expression(); // temp
                }
                else
                {
                    string className = this.ResolveFullClassName(this._lex.GetDotSeparatedIdentifier("Expects identifier after 'new'"));
                    if (!this._allClasses.ContainsKey(className))
                    {
                        ClassReferenceInfo info = new ClassReferenceInfo();
                        info.ClassName = className;
                        info.ClassNumber = this._allClasses.Count + 1;
                        this._allClasses.Add(className, info);
                        string shortClassName = this.GetShortClassName(className);
                        if (this._classNameMap.ContainsKey(shortClassName))
                        {
                            this._classNameMap[shortClassName].Add(className);
                        }
                        else
                        {
                            List<string> list = new List<string>();
                            list.Add(className);
                            this._classNameMap.Add(shortClassName, list);
                        }
                    }
                    this._code.NewInstance(this._allClasses[className].ClassNumber);
                    this._lex.NeedOperator("("); // constructor invocation
                    int arguments = 0;
                    if (!this._lex.MatchOperator(")"))
                    {
                        do
                        {
                            object result = this.ParseAssignment();
                            this.Load(result, this._code);
                            arguments++;
                        }
                        while (this._lex.MatchDelimiter(","));
                        this._lex.NeedOperator(")");
                    }

                    string methodSignature = string.Format("__constructor({0})", arguments);

                    if (!this._allMethods.ContainsKey(methodSignature))
                    {
                        MethodCallInfo info = new MethodCallInfo();
                        info.MethodSignature = methodSignature;
                        info.MethodCallNumber = this._allMethods.Count + 1;
                        this._allMethods.Add(methodSignature, info);
                    }

                    this._code.CallC(this._allMethods[methodSignature].MethodCallNumber);
                    left = new ClassReference();
                }
                #endregion
            }
            else
            {
                left = this.ParseAtom();

                #region method
                if (this._lex.MatchOperator("(")) // method invocation
                {
                    #region method

                    if (!(left is Identifier))
                    {
                        throw new Exception("Expects a identifier before '()'");
                    }

                    this.Load(new Identifier("this"), this._code); // default to current object instance

                    int arguments = 0;

                    if (!this._lex.MatchOperator(")"))
                    {
                        do
                        {
                            object result = this.ParseConditional();
                            this.Load(result, this._code);
                            arguments++;
                        }
                        while (this._lex.MatchDelimiter(","));
                        this._lex.NeedOperator(")");
                    }

                    string methodSignature = string.Format("{0}({1}):1", ((Identifier)left).Name, arguments);

                    if (!this._allMethods.ContainsKey(methodSignature))
                    {
                        MethodCallInfo info = new MethodCallInfo();
                        info.MethodSignature = methodSignature;
                        info.MethodCallNumber = this._allMethods.Count + 1;
                        this._allMethods.Add(methodSignature, info);
                    }

                    this._code.Call(this._allMethods[methodSignature].MethodCallNumber);
                    // assuming all methods must return a value
                    left = new MethodCall();
                    #endregion
                }
                #endregion
            }

            while (true)
            {                
                if (this._lex.MatchOperator("[")) // array indexing
                {
                    #region array

                    if (!(left is Identifier || left is ArrayReference || left is MethodCall || left is FieldReference))
                    {
                        throw new Exception("Needs identifier/array/field reference/method call before '[]'");
                    }

                    if (left is Identifier || left is FieldReference || left is ArrayReference)
                    {
                        this.Load(left, this._code);
                    }

                    object result = this.ParseAssignment();
                    this.Load(result, this._code);
                    this._lex.NeedOperator("]");                    
                    left = new ArrayReference();

                    #endregion
                }
                else if (this._lex.MatchOperator(".")) // period operator
                {
                    #region period

                    object right = this.ParseAtom();
                    if (!(left is Identifier || left is ClassReference || left is FieldReference || left is MethodCall || left is ArrayReference || left is Constant))
                    {
                        throw new Exception(this._lex.GetExceptionMessage("Expects identifier/reference before '.'"));
                    }
                    if (!(right is Identifier))
                    {
                        throw new Exception("Expects a identifier after '.''");
                    }

                    if (this._lex.MatchOperator("(")) // method invocation
                    {
                        #region method

                        this.Load(left, this._code); // load object instance

                        int arguments = 0;

                        if (!this._lex.MatchOperator(")"))
                        {
                            do
                            {
                                object result = this.ParseConditional();
                                this.Load(result, this._code);
                                arguments++;
                            }
                            while (this._lex.MatchDelimiter(","));
                            this._lex.NeedOperator(")");
                        }

                        string methodSignature = string.Format("{0}({1}):1", ((Identifier)right).Name, arguments);

                        if (!this._allMethods.ContainsKey(methodSignature))
                        {
                            MethodCallInfo info = new MethodCallInfo();
                            info.MethodSignature = methodSignature;
                            info.MethodCallNumber = this._allMethods.Count + 1;
                            this._allMethods.Add(methodSignature, info);
                        }

                        this._code.Call(this._allMethods[methodSignature].MethodCallNumber);
                        // assuming all methods must return a value
                        left = new MethodCall();
                        #endregion
                    }
                    else
                    {
                        #region field

                        string fieldName = ((Identifier)right).Name;

                        if (!this._allFields.ContainsKey(fieldName))
                        {
                            FieldAccessInfo info = new FieldAccessInfo();
                            info.FieldName = fieldName;
                            info.FieldAccessNumber = this._allFields.Count + 1;
                            this._allFields.Add(fieldName, info);
                        }

                        this.Load(left, this._code);
                        FieldReference fieldReference = new FieldReference();
                        fieldReference.FieldName = fieldName;                        
                        fieldReference.FieldNumber = this._allFields[fieldName].FieldAccessNumber;
                        left = fieldReference;
                        #endregion
                    }

                    #endregion
                }
                else if (this._lex.MatchOperator("::")) // class reference
                {
                    #region class or enum scope
                    if (!(left is Identifier))
                    {
                        throw new Exception("Expects class before '::'");
                    }
                    string className = this.ResolveFullClassName(((Identifier)left).Name);
                    string shortClassName = this.GetShortClassName(className);
                    if (this._allEnums.ContainsKey(className)) // is enum
                    {
                        string enumField = this._lex.NeedIdentifier();
                        if (!this._allEnums[className].Fields.ContainsKey(enumField))
                        {
                            throw new Exception(string.Format("Field '{0}' not defined in enum '{1}'.", enumField, className));
                        }                        
                        return new Constant(this._allEnums[className].Fields[enumField]);
                    }
                    else // class scope
                    {
                        if (!this._allClasses.ContainsKey(className))
                        {
                            ClassReferenceInfo info = new ClassReferenceInfo();
                            info.ClassName = className;
                            info.ClassNumber = this._allClasses.Count + 1;
                            this._allClasses.Add(className, info);
                            if (this._classNameMap.ContainsKey(shortClassName))
                            {
                                this._classNameMap[shortClassName].Add(className);
                            }
                            else
                            {
                                List<string> list = new List<string>();
                                list.Add(className);
                                this._classNameMap.Add(shortClassName, list);
                            }
                        }

                        this._code.LoadClass(this._allClasses[className].ClassNumber); // load class

                        object right = this.ParseAtom();
                        if (!(right is Identifier))
                        {
                            throw new Exception("Expects identifier after '::'");
                        }

                        if (this._lex.MatchOperator("(")) // method invocation
                        {
                            #region method

                            int arguments = 0;

                            if (!this._lex.MatchOperator(")"))
                            {
                                do
                                {
                                    object result = this.ParseConditional();
                                    this.Load(result, this._code);
                                    arguments++;
                                }
                                while (this._lex.MatchDelimiter(","));
                                this._lex.NeedOperator(")");
                            }

                            string methodSignature = string.Format("{0}({1}):1", ((Identifier)right).Name, arguments);

                            if (!this._allMethods.ContainsKey(methodSignature))
                            {
                                MethodCallInfo info = new MethodCallInfo();
                                info.MethodSignature = methodSignature;
                                info.MethodCallNumber = this._allMethods.Count + 1;
                                this._allMethods.Add(methodSignature, info);
                            }

                            this._code.Call(this._allMethods[methodSignature].MethodCallNumber);
                            // assuming all methods must return a value
                            left = new MethodCall();
                            #endregion
                        }
                        else
                        {
                            #region field

                            string fieldName = ((Identifier)right).Name;

                            if (!this._allFields.ContainsKey(fieldName))
                            {
                                FieldAccessInfo info = new FieldAccessInfo();
                                info.FieldName = fieldName;
                                info.FieldAccessNumber = this._allFields.Count + 1;
                                this._allFields.Add(fieldName, info);
                            }

                            FieldReference fieldReference = new FieldReference();
                            fieldReference.FieldName = fieldName;
                            fieldReference.FieldNumber = this._allFields[fieldName].FieldAccessNumber;
                            left = fieldReference;
                            #endregion
                        }
                    }
                    #endregion

                    return left;
                }
                else
                {
                    return left;
                }
            }             
        }

        private object ParseUnary()
        {
            Token token = this._lex.PeekToken();
            if (token.IsOperator("("))
            {
                this._lex.NextToken();
                if (this._lex.MatchKeyword("byte"))
                {
                    this._lex.NeedOperator(")");
                    object right = this.ParsePrimary();
                    this.Load(right, this._code);
                    this._code.CastByte();
                    return new Expression();
                }
                else if (this._lex.MatchKeyword("char"))
                {
                    this._lex.NeedOperator(")");
                    object right = this.ParsePrimary();
                    this.Load(right, this._code);
                    this._code.CastChar();
                    return new Expression();
                }
                else if (this._lex.MatchKeyword("int"))
                {
                    this._lex.NeedOperator(")");
                    object right = this.ParsePrimary();
                    this.Load(right, this._code);
                    this._code.CastInt();
                    return new Expression();
                }
                else if (this._lex.MatchKeyword("long"))
                {
                    this._lex.NeedOperator(")");
                    object right = this.ParsePrimary();
                    this.Load(right, this._code);
                    this._code.CastLong();
                    return new Expression();
                }
                else if (this._lex.MatchKeyword("double"))
                {
                    this._lex.NeedOperator(")");
                    object right = this.ParsePrimary();
                    this.Load(right, this._code);
                    this._code.CastFloat();
                    return new Expression();
                }
                else if (this._lex.MatchKeyword("string"))
                {
                    this._lex.NeedOperator(")");
                    object right = this.ParsePrimary();
                    this.Load(right, this._code);
                    this._code.CastString();
                    return new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                }
            }

            if (token.IsOperator("-") || token.IsOperator("+") || token.IsOperator("!") || token.IsOperator("~") || token.IsOperator("++") || token.IsOperator("--"))
            {
                this._lex.NextToken();
                object right = this.ParsePrimary();
                if (token.IsOperator("-"))
                {
                    this.Load(right, this._code);
                    this._code.Neg();
                    return new Expression();
                }
                else if (token.IsOperator("+"))
                {
                    return right;
                }
                else if (token.IsOperator("!") || token.IsOperator("~"))
                {
                    this.Load(right, this._code);
                    this._code.Not();
                    return new Expression();
                }
                else
                {
                    if (!(right is Identifier || right is ArrayReference || right is ClassReference))
                    {
                        throw new Exception("Expect a lvalue for '++' or '--'");
                    }
                    this.Load(right, this._code);
                    this._code.LoadC(new Constant(1));
                    if (token.IsOperator("++"))
                    {
                        this._code.Add();
                    }
                    else
                    {
                        this._code.Sub();
                    }
                    this.Store(right, this._code);
                    this.Load(right, this._code);
                    return new Expression();
                }                
            }
            else
            {
                object left = this.ParsePrimary();
                token = this._lex.PeekToken();
                if (token.IsOperator("++") || token.IsOperator("--"))
                {
                    this._lex.NextToken();
                    if (!(left is Identifier || left is ArrayReference || left is ClassReference || left is FieldReference))
                    {
                        throw new Exception("Expect a lvalue for '++' or '--'");
                    }
                    this.Load(left, this._code);
                    this.Load(left, this._code);
                    this._code.LoadC(new Constant(1));
                    if (token.IsOperator("++"))
                    {
                        this._code.Add();
                    }
                    else
                    {
                        this._code.Sub();
                    }
                    this.Store(left, this._code);
                    return new Expression();
                }
                else
                {
                    return left;
                }
            }
        }

        private object ParseSizeOf()
        {
            Token token = this._lex.PeekToken();
            if (token.IsKeyword("sizeof"))
            {
                this._lex.NextToken();
                object result = this.ParseUnary();
                if (!(result is Identifier || result is MethodCall || result is ArrayReference || result is FieldReference))
                {
                    throw new Exception("Expects variable/method call/array/field reference for sizeof.");
                }
                this.Load(result, this._code);
                this._code.Size();
                return new Expression();
            }
            else
            {
                return this.ParseUnary();
            }
        }

        private object ParseHashOf()
        {
            Token token = this._lex.PeekToken();
            if (token.IsKeyword("hashof"))
            {
                this._lex.NextToken();
                object result = this.ParseSizeOf();                
                this.Load(result, this._code);
                this._code.Hash();
                return new Expression();
            }
            else
            {
                return this.ParseSizeOf();
            }
        }

        private object ParseMultiplicationDivision()
        {
            object left = this.ParseHashOf();
            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("*") || token.IsOperator("/") || token.IsOperator("%"))
                {
                    this.Load(left, this._code);
                    object right = this.ParseHashOf();                    
                    this.Load(right, this._code);
                    
                    if (token.IsOperator("*"))
                    {
                        this.CheckNumericalOperands(left, right);
                        this._code.Mul();
                    }
                    else if (token.IsOperator("/"))
                    {
                        this.CheckNumericalOperands(left, right);
                        this._code.Div();
                    }
                    else if (token.IsOperator("%"))
                    {
                        this.CheckIntegralOperands(left, right);
                        this._code.Mod();
                    }
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }
        }

        private object ParseAdditionSubtraction()
        {
            object left = this.ParseMultiplicationDivision();
            
            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("+") || token.IsOperator("-"))
                {
                    this.Load(left, this._code);
                    object right = this.ParseMultiplicationDivision();                    
                    this.Load(right, this._code);

                    if (token.IsOperator("+"))
                    {
                        if (!(left is string || right is string))
                        {
                            this.CheckNumericalOperands(left, right);                            
                        }
                        this._code.Add();
                    }
                    else if (token.IsOperator("-"))
                    {
                        this.CheckNumericalOperands(left, right);
                        this._code.Sub();
                    }
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }            
        }

        private object ParseShift()
        {
            object left = this.ParseAdditionSubtraction();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("<<") || token.IsOperator(">>"))
                {
                    this.Load(left, this._code);
                    object right = this.ParseAdditionSubtraction();
                    this.Load(right, this._code);

                    if (token.IsOperator("<<"))
                    {
                        this._code.LShift();
                    }
                    else
                    {
                        this._code.RShift();
                    }
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }    
        }

        private object ParseComparsions()
        {
            object left = this.ParseShift();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsKeyword("is"))
                {
                    string className = this.ResolveFullClassName(this._lex.GetDotSeparatedIdentifier("Expects class name after 'is'."));
                    string shortClassName = this.GetShortClassName(className);
                    if (!this._allClasses.ContainsKey(className))
                    {
                        ClassReferenceInfo info = new ClassReferenceInfo();
                        info.ClassName = className;
                        info.ClassNumber = this._allClasses.Count + 1;
                        this._allClasses.Add(className, info);
                        if (this._classNameMap.ContainsKey(shortClassName))
                        {
                            this._classNameMap[shortClassName].Add(className);
                        }
                        else
                        {
                            List<string> list = new List<string>();
                            list.Add(className);
                            this._classNameMap.Add(shortClassName, list);
                        }
                    }
                    this.Load(left, this._code);
                    this._code.LoadClass(this._allClasses[className].ClassNumber);
                    this._code.IsInstance();
                    left = new Expression();
                }
                else if (token.IsOperator("<") || token.IsOperator("<=") || token.IsOperator(">") || token.IsOperator(">="))
                {                    
                    this.Load(left, this._code);
                    object right = this.ParseShift();                    
                    this.Load(right, this._code);

                    this.CheckComparisionOperands(left, right, false);

                    if (token.IsOperator("<"))
                    {
                        this._code.Cl();
                    }
                    else if (token.IsOperator("<="))
                    {
                        this._code.Cle();
                    }
                    else if (token.IsOperator(">"))
                    {
                        this._code.Cg();
                    }
                    else if (token.IsOperator(">="))
                    {
                        this._code.Cge();
                    }

                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }  
        }

        private object ParseEquality()
        {
            object left = this.ParseComparsions();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("==") || token.IsOperator("!="))
                {
                    this.Load(left, this._code);
                    object right = this.ParseComparsions();
                    this.Load(right, this._code);

                    this.CheckComparisionOperands(left, right, true);

                    if (token.IsOperator("=="))
                    {
                        this._code.Ce();
                    }
                    else
                    {
                        this._code.Cne();
                    }

                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }
        }

        private object ParseLogicalAnd()
        {
            object left = this.ParseEquality();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("&"))
                {
                    this.Load(left, this._code);
                    object right = this.ParseEquality();
                    this.CheckBooleanOperands(left, right);
                    this.Load(right, this._code);
                    this._code.And();
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }
        }

        private object ParseLogicalXor()
        {
            object left = this.ParseLogicalAnd();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("^"))
                {
                    this.Load(left, this._code);
                    object right = this.ParseLogicalAnd();
                    this.CheckBooleanOperands(left, right);
                    this.Load(right, this._code);
                    this._code.Xor();
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }
        }

        private object ParseLogicalOr()
        {
            object left = this.ParseLogicalXor();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("|"))
                {
                    this.Load(left, this._code);
                    object right = this.ParseRelationalAnd();
                    this.CheckBooleanOperands(left, right);
                    this.Load(right, this._code);
                    this._code.Or();
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }
        }

        private object ParseRelationalAnd()
        {
            object left = this.ParseLogicalOr();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("&&"))
                {
                    this.Load(left, this._code);
                    int falseLabel1 = this._code.BFalse(0);
                    object right = this.ParseLogicalOr();
                    this.CheckBooleanOperands(left, right);
                    this.Load(right, this._code);
                    int falseLabel2 = this._code.BFalse(0);
                    this._code.LoadC(new Constant(true));
                    int endLabel = this._code.Br(0);
                    this._code.AnchorLabel(falseLabel1);
                    this._code.AnchorLabel(falseLabel2);
                    this._code.LoadC(new Constant(false));
                    this._code.AnchorLabel(endLabel);
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            }
        }

        private object ParseRelationalOr()
        {
            object left = this.ParseRelationalAnd();

            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.IsOperator("||"))
                {
                    this.Load(left, this._code);
                    int trueLabel1 = this._code.BTrue(0);
                    object right = this.ParseLogicalXor();
                    this.CheckBooleanOperands(left, right);
                    this.Load(right, this._code);
                    int trueLabel2 = this._code.BTrue(0);
                    this._code.LoadC(new Constant(false));
                    int endLabel = this._code.Br(0);
                    this._code.AnchorLabel(trueLabel1);
                    this._code.AnchorLabel(trueLabel2);
                    this._code.LoadC(new Constant(true));
                    this._code.AnchorLabel(endLabel);
                    left = new Expression();
                }
                else
                {
                    this._lex.Putback(token);
                    return left;
                }
            } 
        }

        private object ParseConditional()
        {
            object condition = this.ParseRelationalOr();
            if (this._lex.MatchOperator("?"))
            {
                int falseLabel = this._code.BFalse(0);
                object left = this.ParseConditional();
                if (!this._lex.MatchOperator(":"))
                {
                    throw new Exception("Missing ':' in '?' statement.");
                }
                this.Load(left, this._code);
                int endLabel = this._code.Br(0);
                this._code.AnchorLabel(falseLabel);
                object right = this.ParseConditional();
                this.Load(right, this._code);
                this._code.AnchorLabel(endLabel);
                return new Expression();
            }
            else
            {
                return condition;
            }
        }

        private object ParseAssignment()
        {
            object left = this.ParseConditional();
            
            Token token = this._lex.NextToken();
            if (token.IsOperator("="))
            {
                object right = this.ParseAssignment();

                if (!(left is Identifier || left is ArrayReference || left is FieldReference))
                {
                    throw new Exception(this._lex.GetExceptionMessage("Expects left operand to be a variable for assignment."));
                }                
                this.Load(right, this._code);
                this.Store(left, this._code);
                return left;
            }
            else if (token.IsOperator("+=") || token.IsOperator("+=") || token.IsOperator("-=") || token.IsOperator("*=") || token.IsOperator("/=") || token.IsOperator("%="))
            {
                this.Load(left, this._code);
                object right = this.ParseAssignment();
                if (!(left is Identifier || left is ArrayReference || left is FieldReference))
                {
                    throw new Exception(string.Format("Expects left operand to be a variable for assignment: {0}", left.GetType().Name));
                }                
                this.Load(right, this._code);
                if (token.IsOperator("+="))
                {
                    this._code.Add();
                }
                else if (token.IsOperator("-="))
                {
                    this._code.Sub();
                }
                else if (token.IsOperator("*="))
                {
                    this._code.Mul();
                }
                else if (token.IsOperator("/="))
                {
                    this._code.Div();
                }
                else if (token.IsOperator("%="))
                {
                    this._code.Mod();
                }
                this.Store(left, this._code);
                return left;
            }
            else
            {
                this._lex.Putback(token);
                return left;
            }
        }

        private object ParseExpression(bool expectsReturnValue)
        {
            while (true)
            {
                object result = this.ParseAssignment();
                if (expectsReturnValue)
                {
                    this.Load(result, this._code);
                }
                else
                {
                    if (result is Expression || result is MethodCall) // stack stores result but no body using it, pop it
                    {
                        this._code.Pop();
                    }
                }
                Token token = this._lex.NextToken();
                if (!token.IsDelimiter(","))
                {
                    this._lex.Putback(token);
                    return result;
                }
            }
        }

        private void ParseStatement()
        {
            Token token = this._lex.PeekToken();
           
            if (this._lex.MatchKeyword("if"))
            {
                this.ParseIfStatement();
            }
            else if (this._lex.MatchKeyword("switch"))
            {
                this.ParseSwitchCaseStatement();
            }
            else if (this._lex.MatchKeyword("while"))
            {
                this.ParseWhileStatement();
            }
            else if (this._lex.MatchKeyword("do"))
            {
                this.ParseDoWhileStatement();
            }
            else if (this._lex.MatchKeyword("for"))
            {
                this.ParseForStatement();
            }
            else if (this._lex.MatchKeyword("foreach"))
            {
                this.ParseForEachStatement();
            }
            else if (this._lex.MatchKeyword("loop"))
            {
                this.ParseLoopStatement();
            }
            else if (this._lex.MatchKeyword("continue"))
            {
                this.ParseContinueStatement();
            }
            else if (this._lex.MatchKeyword("break"))
            {
                this.ParseBreakStatement();
            }
            else if (this._lex.MatchKeyword("return"))
            {
                this.ParseReturnStatement();
            }
            else if (this._lex.MatchKeyword("throw"))
            {
                this.ParseThrowStatement();
            }
            else if (this._lex.MatchKeyword("rethrow"))
            {
                this.ParseReThrowStatement();
            }
            else if (this._lex.MatchKeyword("try"))
            {
                this.ParseTryCatchFinallyBlock();
            }
            else if (this._lex.MatchKeyword("synchronized"))
            {
                this.ParseSynchronizedBlock();
            }
            else if (this._lex.MatchKeyword("asm"))
            {
                this.ParseAssemblyBlock();
            }
            else
            {
                this.ParseExpression(false);
                this._lex.NeedDelimiter(";");
            }
        }

        private void ParseIfStatement()
        {
            bool returnStatmentExistsBefore = this._returnStatementExists;           
            this._lex.NeedOperator("(");
            object result = this.ParseExpression(true);
            this._lex.NeedOperator(")");            
            if (!(result is Identifier || result is Expression || (result is Constant && ((Constant)result).Value is bool) || result is MethodCall || result is ArrayReference || result is FieldReference))
            {
                throw new Exception("Expects boolean expression in IF statement.");
            }
            int falseLabel = this._code.BFalse(0);
            this.ParseStatementBlock();                        
            bool ifBlockReturnStatementExists = this._returnStatementExists;
            bool elseBlockReturnStatementExists = false;
            if (this._lex.MatchKeyword("else"))
            {
                int trueLabel = this._code.Br(0);
                this._code.AnchorLabel(falseLabel);
                this.ParseStatementBlock();
                this._code.AnchorLabel(trueLabel);
                elseBlockReturnStatementExists = this._returnStatementExists;
            }
            else
            {
                this._code.AnchorLabel(falseLabel);
            }
            this._returnStatementExists = returnStatmentExistsBefore || (ifBlockReturnStatementExists && elseBlockReturnStatementExists);
        }

        private void ParseSwitchCaseStatement()
        {
            bool returnStatmentExistsBefore = this._returnStatementExists;
            bool allCasesReturnExists = true;
            List<int> outerBreakLabels = this._breakLabels;
            this._breakLabels = new List<int>();
            Dictionary<int, int> caseLabels = new Dictionary<int, int>();

            this._lex.NeedOperator("(");
            
            object result = this.ParseExpression(true);
            int compareLabel = this._code.Br(0);
            int defaultPosition = -1;
            bool defaultDefined = false;

            this._lex.NeedOperator(")");

            if (!(result is Identifier || result is Expression || (result is Constant && (((Constant)result).Value is int || ((Constant)result).Value is long)) || result is MethodCall || result is ArrayReference))
            {
                throw new Exception("Expects integral expression in SWITCH-CASE statement.");
            }

            this._lex.NeedDelimiter("{");            

            #region case executions
            while (true)
            {
                if (this._lex.MatchKeyword("case"))
                {
                    object caseValue = this.ParsePrimary();
                    if (!(caseValue is Constant) || !(((Constant)caseValue).Value is int || ((Constant)caseValue).Value is long))
                    {
                        throw new Exception(this._lex.GetExceptionMessage("Expects integral value for 'case'"));
                    }
                    this._lex.NeedOperator(":");
                    int value = ((Constant)caseValue).Value is int ? (int)((Constant)caseValue).Value : (int)(long)((Constant)caseValue).Value;
                    if (!caseLabels.ContainsKey(value))
                    {
                        caseLabels.Add(value, this._code.Position);
                    }
                    else
                    {
                        throw new Exception(this._lex.GetExceptionMessage("Case '{0}' already defined", value));
                    }
                    this._returnStatementExists = false;
                    if (this._lex.MatchDelimiter("{"))
                    {
                        while (this._lex.PeekToken().TokenType != TokenType.EOF && !this._lex.PeekToken().IsDelimiter("}"))
                        {
                            this.ParseStatement();
                        }
                        this._lex.NeedDelimiter("}");
                    }
                    else
                    {
                        while (this._lex.PeekToken().TokenType != TokenType.EOF && !this._lex.PeekToken().IsDelimiter("}") && !this._lex.PeekToken().IsKeyword("case") && !this._lex.PeekToken().IsKeyword("default"))
                        {
                            this.ParseStatement();
                        }
                    }
                    allCasesReturnExists = allCasesReturnExists && this._returnStatementExists;
                }
                else if (this._lex.MatchKeyword("default"))
                {
                    this._lex.NeedOperator(":");
                    if (defaultDefined)
                    {
                        throw new Exception("'default' already defined");
                    }
                    else
                    {
                        defaultDefined = true;
                    }
                    defaultPosition = this._code.Position;
                    this._returnStatementExists = false;
                    if (this._lex.MatchDelimiter("{"))
                    {
                        while (this._lex.PeekToken().TokenType != TokenType.EOF && !this._lex.PeekToken().IsDelimiter("}"))
                        {
                            this.ParseStatement();
                        }
                        this._lex.NeedDelimiter("}");
                    }
                    else
                    {
                        while (this._lex.PeekToken().TokenType != TokenType.EOF && !this._lex.PeekToken().IsDelimiter("}") && !this._lex.PeekToken().IsKeyword("case") && !this._lex.PeekToken().IsKeyword("default"))
                        {
                            this.ParseStatement();
                        }
                    }
                    allCasesReturnExists = allCasesReturnExists && this._returnStatementExists;
                }
                else if (this._lex.MatchDelimiter("}"))
                {
                    break;
                }
                else
                {
                    throw new Exception(this._lex.GetExceptionMessage("Expects 'case' or '}'"));
                }
            }
            #endregion

            List<int> endLabels = new List<int>();
            endLabels.Add(this._code.Br(0));
            this._code.AnchorLabel(compareLabel);

            #region comparison
            if (caseLabels.Count < 1)
            {
                throw new Exception("No case statements in SWITCH-CASE");
            }
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;
            foreach (int value in caseLabels.Keys)
            {
                if (minValue > value)
                {
                    minValue = value;
                }
                if (maxValue < value)
                {
                    maxValue = value;
                }
            }
            List<int> entryExitLabels = new List<int>();
            int mappingTableSize = Math.Abs(maxValue - minValue) + 1;
            int tableOffset = 0;
            bool useMappingTable = caseLabels.Keys.Count >= this._minimumCaseThreshold  && mappingTableSize < this._mappingTableSizeThreshold && (double)caseLabels.Keys.Count * 100 / (double)mappingTableSize >= (double)this._mappingTableUsageThreshold;
            if (useMappingTable) // use mapping table
            {
                // value of switch is on stack
                this._code.LoadC(new Constant(minValue));
                this._code.Sub();
                this._code.Dup();
                this._code.LoadC(new Constant(0));
                if (defaultDefined)
                {
                    this._code.Bl(defaultPosition - this._code.Position - 5);                    
                }
                else
                {
                    endLabels.Add(this._code.Bl(0));
                }
                this._code.Dup();
                this._code.LoadC(new Constant(mappingTableSize - 1));
                if (defaultDefined)
                {
                    this._code.Bg(defaultPosition - this._code.Position - 5);
                }
                else
                {
                    endLabels.Add(this._code.Bg(0));
                }
                this._code.Dup();
                this._code.BMap(mappingTableSize);
                tableOffset = this._code.Position;
                for (int i = 0; i < mappingTableSize; i++)
                {
                    int offset = caseLabels.ContainsKey(minValue + i) ? caseLabels[minValue + i] : 0;
                    if (offset == 0)
                    {
                        if (defaultDefined)
                        {
                            this._code.MapEntry(defaultPosition - tableOffset);
                        }
                        else
                        {
                            entryExitLabels.Add(this._code.MapEntry(0));
                        }
                    }
                    else
                    {
                        this._code.MapEntry(offset - tableOffset);
                    }                    
                }
            }
            else // use if-else
            {
                // value of switch is on stack

                foreach (int value in caseLabels.Keys)
                {
                    this._code.Dup();                    
                    this._code.LoadC(new Constant(value));
                    this._code.Be(caseLabels[value] - this._code.Position - 5);
                }
                if (defaultDefined)
                {
                    this._code.Br(defaultPosition - this._code.Position - 5);
                }
            }
            #endregion

            foreach (int label in endLabels)
            {
                this._code.AnchorLabel(label);
            }   
            
            foreach (int label in entryExitLabels)
            {
                this._code.AnchorCaseLabel(tableOffset, label);
            }
            foreach (int label in this._breakLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._code.Pop();
            this._breakLabels = outerBreakLabels;
            this._returnStatementExists = returnStatmentExistsBefore || allCasesReturnExists;
        }

        private void ParseWhileStatement()
        {
            this._lex.NeedOperator("(");
            int truePosition = this._code.Position;
            object result = this.ParseExpression(true);
            this._lex.NeedOperator(")");
            if (!(result is Identifier || result is Expression || (result is Constant && ((Constant)result).Value is bool) || result is MethodCall || result is ArrayReference || result is FieldReference))
            {
                throw new Exception("Expects boolean expression in WHILE statement.");
            }
            int falseLabel = this._code.BFalse(0);
            List<int> outerContinueLabels = this._continueLabels;
            List<int> outerBreakLabels = this._breakLabels;
            this._continueLabels = new List<int>();
            this._breakLabels = new List<int>();            
            this.ParseStatementBlock();
            foreach (int label in this._continueLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._code.Br(truePosition - this._code.Position - 5);
            this._code.AnchorLabel(falseLabel);
            foreach (int label in this._breakLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._continueLabels = outerContinueLabels;
            this._breakLabels = outerBreakLabels;
        }

        private void ParseDoWhileStatement()
        {
            int loopBegin = this._code.Position;
            List<int> outerContinueLabels = this._continueLabels;
            List<int> outerBreakLabels = this._breakLabels;
            this._continueLabels = new List<int>();
            this._breakLabels = new List<int>();            
            this.ParseStatementBlock();
            foreach (int label in this._continueLabels)
            {
                this._code.AnchorLabel(label);
            }
            if (!this._lex.MatchKeyword("while"))
            {
                throw new Exception("Expects 'while' keyword for DO-WHILE statement.");
            }
            this._lex.NeedOperator("(");
            object result = this.ParseExpression(true);
            this._lex.NeedOperator(")");
            this._lex.NeedDelimiter(";");
            if (!(result is Identifier || result is Expression || (result is Constant && ((Constant)result).Value is bool) || result is MethodCall || result is ArrayReference || result is FieldReference))
            {
                throw new Exception("Expects boolean expression in WHILE statement.");
            }
            this._code.BTrue(loopBegin - this._code.Position - 5);
            foreach (int label in this._breakLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._continueLabels = outerContinueLabels;
            this._breakLabels = outerBreakLabels;
        }

        private void ParseForStatement()
        {   
            this._lex.NeedOperator("(");            
            this.ParseExpression(false);
            this._lex.NeedDelimiter(";");
            int comparePosition = this._code.Position;            
            object result = this.ParseExpression(true);
            if (!(result is Identifier || result is Expression || (result is Constant && ((Constant)result).Value is bool) || result is MethodCall || result is ArrayReference || result is FieldReference))
            {
                throw new Exception("Expects boolean expression in WHILE statement.");
            }
            int falseLabel = this._code.BFalse(0);
            int loopLabel = this._code.Br(0);
            this._lex.NeedDelimiter(";");
            int incrementPosition = this._code.Position;
            this.ParseExpression(false);
            this._code.Br(comparePosition - this._code.Position - 5);
            this._lex.NeedOperator(")");
            this._code.AnchorLabel(loopLabel);
            List<int> outerContinueLabels = this._continueLabels;
            List<int> outerBreakLabels = this._breakLabels;
            this._continueLabels = new List<int>();
            this._breakLabels = new List<int>();
            this.ParseStatementBlock();
            foreach (int label in this._continueLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._code.Br(incrementPosition - this._code.Position - 5);
            this._code.AnchorLabel(falseLabel);
            foreach (int label in this._breakLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._continueLabels = outerContinueLabels;
            this._breakLabels = outerBreakLabels;
        }

        private void ParseForEachStatement()
        {
            this._lex.NeedOperator("(");
            string identifier = this._lex.NeedIdentifier();
            if (!this._localVariables[this._currentClass][this._currentMethod].ContainsKey(identifier))
            {
                Variable variable = new Variable();
                variable.VariableNumber = this._localVariables[this._currentClass][this._currentMethod].Count + 1;
                variable.VariableName = identifier;
                this._localVariables[this._currentClass][this._currentMethod].Add(identifier, variable);
            }
            int variableNumber = this._localVariables[this._currentClass][this._currentMethod][identifier].VariableNumber;
            int temporaryVariableNumber = this.CreateTemporaryVariable();
            if (!this._lex.MatchKeyword("in"))
            {
                throw new Exception("Expects 'in' in foreach");
            }
            object collection = this.ParseExpression(true);            
            this.Call("getEnumerator(0):1", this._code);
            this._code.StoreV(temporaryVariableNumber);
            int loopPosition = this._code.Position;
            this._code.LoadV(temporaryVariableNumber);
            this.Call("moveNext(0):1", this._code);
            int endLabel = this._code.BFalse(0);
            this._code.LoadV(temporaryVariableNumber);
            this.Call("getElement(0):1", this._code);
            this._code.StoreV(variableNumber);
            this._lex.NeedOperator(")");
            List<int> outerContinueLabels = this._continueLabels;
            List<int> outerBreakLabels = this._breakLabels;
            this._continueLabels = new List<int>();
            this._breakLabels = new List<int>();
            this.ParseStatementBlock();
            foreach (int label in this._continueLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._code.Br(loopPosition - this._code.Position - 5);
            foreach (int label in this._breakLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._continueLabels = outerContinueLabels;
            this._breakLabels = outerBreakLabels;            
            this._code.AnchorLabel(endLabel);
        }

        private void ParseLoopStatement()
        {
            this._lex.NeedOperator("(");
            string identifier = this._lex.NeedIdentifier();
            if (!this._localVariables[this._currentClass][this._currentMethod].ContainsKey(identifier))
            {
                Variable variable = new Variable();
                variable.VariableNumber = this._localVariables[this._currentClass][this._currentMethod].Count + 1;
                variable.VariableName = identifier;
                this._localVariables[this._currentClass][this._currentMethod].Add(identifier, variable);
            }
            int variableNumber = this._localVariables[this._currentClass][this._currentMethod][identifier].VariableNumber;
            int temporaryVariableNumber = this.CreateTemporaryVariable();
            this._lex.NeedOperator("=");
            this.ParseExpression(true);
            this._code.StoreV(variableNumber);
            bool increment = !this._lex.MatchKeyword("down");
            if (!this._lex.MatchKeyword("to"))
            {
                throw new Exception("Expects 'to' in loop statement");
            }

            int loopPosition = this._code.Position;
            this._code.LoadV(variableNumber);
            this.ParseExpression(true);
            bool exclusiveOfEndValue = this._lex.MatchKeyword("exclusive");
            this._lex.NeedOperator(")");
            int endLabel = increment ? exclusiveOfEndValue ? this._code.Bge(0) : this._code.Bg(0) : exclusiveOfEndValue ? this._code.Ble(0) : this._code.Bl(0);
            List<int> outerContinueLabels = this._continueLabels;
            List<int> outerBreakLabels = this._breakLabels;
            this._continueLabels = new List<int>();
            this._breakLabels = new List<int>();
            this.ParseStatementBlock();
            foreach (int label in this._continueLabels)
            {
                this._code.AnchorLabel(label);
            }
            if (increment)
            {
                this._code.Inc(variableNumber);
            }
            else
            {
                this._code.Dec(variableNumber);
            }
            this._code.Br(loopPosition - this._code.Position - 5);
            this._code.AnchorLabel(endLabel);
            foreach (int label in this._breakLabels)
            {
                this._code.AnchorLabel(label);
            }
            this._continueLabels = outerContinueLabels;
            this._breakLabels = outerBreakLabels;
        }

        private void ParseContinueStatement()
        {
            if (this._continueLabels == null)
            {
                throw new Exception("'continue' statement must be inside a loop");
            }
            this._continueLabels.Add(this._code.Br(0));
            this._lex.NeedDelimiter(";");
        }

        private void ParseBreakStatement()
        {
            if (this._breakLabels == null)
            {
                throw new Exception("'break' statement must be inside a loop");
            }
            this._breakLabels.Add(this._code.Br(0));
            this._lex.NeedDelimiter(";");
        }    

        private void ParseReturnStatement()
        {
            object result = this.ParseConditional();
            this._lex.NeedDelimiter(";");
            this.Load(result, this._code);
            this._code.RetV();
            this._returnStatementExists = true;
        }

        private void ParseThrowStatement()
        {
            object result = this.ParseConditional();
            this._lex.NeedDelimiter(";");
            this.Load(result, this._code);
            this._code.Throw();
        }

        private void ParseReThrowStatement()
        {
            this._lex.NeedDelimiter(";");
            this._code.ReThrow();
        }

        private void ParseStatementBlock()
        {
            object result = false;
            if (this._lex.MatchDelimiter("{"))
            {
                while (this._lex.PeekToken().TokenType != TokenType.EOF && !this._lex.PeekToken().IsDelimiter("}"))
                {
                    this.ParseStatement();
                }
                this._lex.NeedDelimiter("}");
            }
            else
            {
                this.ParseStatement();
            }
        }

        private void ParseAssemblyBlock()
        {
            this._lex.NeedDelimiter("{");
            this._asm.Assemble(this._lex, this._code, this, this._allClasses, this._classNameMap, this._allFields, this._allMethods, this._localVariables[this._currentClass][this._currentMethod], this._stringLiterals);
            this._lex.NeedDelimiter("}");
        }

        private void ParseTryCatchFinallyBlock()
        {
            int offset = this._code.EnterTry(0, 0);
            this.ParseStatementBlock();
            this._code.LeaveTry();
            this._code.AnchorCatch(offset);
            if (this._lex.MatchKeyword("catch"))
            {
                this._lex.NeedOperator("(");
                string variableName = this._lex.NeedIdentifier();
                if (!this._localVariables[this._currentClass][this._currentMethod].ContainsKey(variableName))
                {
                    Variable variable = new Variable();
                    variable.VariableName = variableName;
                    variable.VariableNumber = this._localVariables[this._currentClass][this._currentMethod].Count + 1;
                    this._localVariables[this._currentClass][this._currentMethod].Add(variableName, variable);
                }
                this._lex.NeedOperator(")");
                this._code.StoreV(this._localVariables[this._currentClass][this._currentMethod][variableName].VariableNumber);
                this.ParseStatementBlock();
            }
            this._code.LeaveCatch();
            this._code.AnchorFinally(offset);
            if (this._lex.MatchKeyword("finally"))
            {
                this.ParseStatementBlock();
            }
            this._code.LeaveFinally();
        }

        private void ParseSynchronizedBlock()
        {
            int temporaryVariableNumber = this.CreateTemporaryVariable();
            this._lex.NeedOperator("(");
            this.ParseExpression(true);
            this._code.StoreV(temporaryVariableNumber);
            this._lex.NeedOperator(")");
            int offset = this._code.EnterTry(0, 0);
            this._code.LoadV(temporaryVariableNumber);
            this._code.Wait();
            this.ParseStatementBlock();
            this._code.LeaveTry();
            this._code.AnchorCatch(offset);
            this._code.LeaveCatch();
            this._code.AnchorFinally(offset);
            this._code.LoadV(temporaryVariableNumber);
            this._code.Signal();            
            this._code.LeaveFinally();
        }

        private void ParseField(bool isStaticField)
        {
            Token token = this._lex.NextToken();
            if (token.TokenType != TokenType.Identifier)
            {
                throw new Exception("Expects identifier after 'field'");
            }

            string fieldName = (string)token.Value;
            if (this._classStaticFields[this._currentClass].ContainsKey(fieldName) || this._classInstanceFields[this._currentClass].ContainsKey(fieldName))
            {
                throw new Exception(string.Format("Field '{0}' already defined in class", fieldName));
            }            

            FieldInfo fieldInfo = new FieldInfo();
            fieldInfo.FieldName = fieldName;
            fieldInfo.FieldNumber = (isStaticField ? this._classStaticFields : this._classInstanceFields)[this._currentClass].Count + 1;
            fieldInfo.IsStatic = isStaticField;
            (isStaticField ? this._classStaticFields : this._classInstanceFields)[this._currentClass].Add(fieldName, fieldInfo);
            if (!this._allFields.ContainsKey(fieldName))
            {
                FieldAccessInfo info = new FieldAccessInfo();
                info.FieldName = fieldName;
                info.FieldAccessNumber = this._allFields.Count + 1;
                this._allFields.Add(fieldName, info);
            }
            this._lex.NeedDelimiter(";");
        }

        private void ParseMethod(bool isStaticMethod, bool isSynchronized)
        {            
            // reset local variables
            Dictionary<string, Variable> localVariables = new Dictionary<string, Variable>();

            Token identifier = this._lex.NextToken();
            if (identifier.TokenType != TokenType.Identifier)
            {
                throw new Exception("Expects identifier after 'method'");
            }
            this._lex.NeedOperator("(");

            // implicit instance variable
            Variable variable = new Variable();
            variable.VariableName = "this";
            variable.VariableNumber = localVariables.Count + 1;
            localVariables.Add("this", variable);

            int arguments = 0;
            while (true)
            {
                Token token = this._lex.PeekToken();
                if (token.TokenType == TokenType.Identifier)
                {
                    string argumentName = (string)token.Value;
                    if (localVariables.ContainsKey(argumentName))
                    {
                        throw new Exception(string.Format("Duplicated argument name: '{0}'", argumentName));
                    }
                    variable = new Variable();
                    variable.VariableName = argumentName;
                    variable.VariableNumber = localVariables.Count + 1;
                    localVariables.Add(argumentName, variable);
                    arguments++;
                    this._lex.NextToken();
                }
                else if (this._lex.MatchDelimiter(","))
                {
                    continue;
                }
                else if (this._lex.MatchOperator(")"))
                {
                    break;
                }
                else
                {
                    throw new Exception("Expects ')'");
                }
            }

            string methodSignature = string.Format("{0}({1}):1", (string)identifier.Value, arguments);
            if (this._classMethods[this._currentClass].ContainsKey(methodSignature))
            {
                throw new Exception(string.Format("Method '{0}' already defined in class", methodSignature));
            }

            this._localVariables[this._currentClass].Add(methodSignature, localVariables);
            this._currentMethod = methodSignature;
            this._returnStatementExists = false;
            int position = this._code.Position;

            if (isSynchronized)
            {
                int offset = this._code.EnterTry(0, 0);
                this._code.LoadV(localVariables["this"].VariableNumber);
                this._code.Wait();
                this.ParseStatementBlock();
                this._code.LeaveTry();
                this._code.AnchorCatch(offset);
                this._code.LeaveCatch();
                this._code.AnchorFinally(offset);
                this._code.LoadV(localVariables["this"].VariableNumber);
                this._code.Signal();
                this._code.LeaveFinally();
                if (!this._returnStatementExists)
                {
                    this._code.LoadC(new Constant(0));
                    this._code.RetV();
                }
            }
            else
            {
                this.ParseStatementBlock();
                if (!this._returnStatementExists)
                {
                    this._code.LoadC(new Constant(0));
                    this._code.RetV();
                }
            }

            MethodInfo methodInfo = new MethodInfo();
            methodInfo.MethodName = methodSignature;
            methodInfo.MethodNumber = this._classMethods[this._currentClass].Count + 1;
            methodInfo.CodeAddress = position;
            methodInfo.Arguments = arguments;
            methodInfo.Variables = this._localVariables[this._currentClass][this._currentMethod].Count;
            methodInfo.IsStatic = isStaticMethod;
            this._classMethods[this._currentClass].Add(methodSignature, methodInfo);
            if (!this._allMethods.ContainsKey(methodSignature))
            {
                MethodCallInfo info = new MethodCallInfo();
                info.MethodSignature = methodSignature;
                info.MethodCallNumber = this._allMethods.Count + 1;
                this._allMethods.Add(methodSignature, info);
            }
        }

        private void ParseConstructor()
        {
            // reset local variables
            Dictionary<string, Variable> localVariables = new Dictionary<string, Variable>();

            this._lex.NeedOperator("(");

            // implicit instance variable
            Variable variable = new Variable();
            variable.VariableName = "this";
            variable.VariableNumber = localVariables.Count + 1;
            localVariables.Add("this", variable);

            int arguments = 0;
            while (true)
            {
                Token token = this._lex.PeekToken();
                if (token.TokenType == TokenType.Identifier)
                {
                    string argumentName = (string)token.Value;
                    if (localVariables.ContainsKey(argumentName))
                    {
                        throw new Exception(string.Format("Duplicated argument name: '{0}'", argumentName));
                    }
                    variable = new Variable();
                    variable.VariableName = argumentName;
                    variable.VariableNumber = localVariables.Count + 1;
                    localVariables.Add(argumentName, variable);
                    arguments++;
                    this._lex.NextToken();
                }
                else if (this._lex.MatchDelimiter(","))
                {
                    continue;
                }
                else if (this._lex.MatchOperator(")"))
                {
                    break;
                }
                else
                {
                    throw new Exception("Expects ')'");
                }
            }

            string methodSignature = string.Format("__constructor({0})", arguments);
            if (this._classMethods[this._currentClass].ContainsKey(methodSignature))
            {
                throw new Exception(string.Format("Constructor '{0}' already defined in class", methodSignature));
            }
            
            this._localVariables[this._currentClass].Add(methodSignature, localVariables);
            this._currentMethod = methodSignature;
            int position = this._code.Position;

            if (this._lex.MatchOperator(":"))
            {
                if (!this._lex.MatchKeyword("super"))
                {
                    throw new Exception("Expects 'super()' after ':'.");
                }                
                this._lex.NeedOperator("(");
                int parentConstructorArguments = 0;
                if (!this._lex.MatchOperator(")"))
                {
                    do
                    {
                        object result = this.ParseConditional();
                        this.Load(result, this._code);
                        parentConstructorArguments++;
                    }
                    while (this._lex.MatchDelimiter(","));
                    this._lex.NeedOperator(")");
                }

                string parentConstructorSignature = string.Format("__constructor({0})", parentConstructorArguments);
                if (!this._allMethods.ContainsKey(parentConstructorSignature))
                {
                    MethodCallInfo info = new MethodCallInfo();
                    info.MethodSignature = parentConstructorSignature;
                    info.MethodCallNumber = this._allMethods.Count + 1;
                    this._allMethods.Add(parentConstructorSignature, info);
                }
                this._code.LoadV(1); // this
                this._code.Super();
                this._code.CallC(this._allMethods[parentConstructorSignature].MethodCallNumber);
                this._code.Pop();
            }
            
            this.ParseStatementBlock();
            this._code.RetC(); // no return value for constructor and preserves object instance in stack upon return;

            MethodInfo methodInfo = new MethodInfo();
            methodInfo.MethodName = methodSignature;
            methodInfo.MethodNumber = this._classMethods[this._currentClass].Count + 1;
            methodInfo.CodeAddress = position;
            methodInfo.Arguments = arguments;
            methodInfo.Variables = this._localVariables[this._currentClass][this._currentMethod].Count;
            this._classMethods[this._currentClass].Add(methodSignature, methodInfo);
            if (!this._allMethods.ContainsKey(methodSignature))
            {
                MethodCallInfo info = new MethodCallInfo();
                info.MethodSignature = methodSignature;
                info.MethodCallNumber = this._allMethods.Count + 1;
                this._allMethods.Add(methodSignature, info);
            }
        }

        private void ParseStaticConstructor()
        {
            // reset local variables
            Dictionary<string, Variable> localVariables = new Dictionary<string, Variable>();

            // implicit instance variable
            Variable variable = new Variable();
            variable.VariableName = "this";
            variable.VariableNumber = localVariables.Count + 1;
            localVariables.Add("this", variable);

            string methodSignature = "__static(0)";
            if (this._classMethods[this._currentClass].ContainsKey(methodSignature))
            {
                throw new Exception("Static block already defined in class.");
            }

            if (!this._allMethods.ContainsKey(methodSignature))
            {
                MethodCallInfo info = new MethodCallInfo();
                info.MethodSignature = methodSignature;
                info.MethodCallNumber = this._allMethods.Count + 1;
                this._allMethods.Add(methodSignature, info);
            }

            this._localVariables[this._currentClass].Add(methodSignature, localVariables);
            this._currentMethod = methodSignature;
            int position = this._code.Position;

            /*
            if (this._currentClass != "oriole.core.Object")
            {
                this._code.LoadV(1); // this
                this._code.Super();
                this._code.CallC(this._allMethods["__static(0)"].MethodCallNumber);
                this._code.Pop();
            }
            */

            this.ParseStatementBlock();
            this._code.RetC(); // no return value for constructor and preserves object instance in stack upon return;

            MethodInfo methodInfo = new MethodInfo();
            methodInfo.MethodName = methodSignature;
            methodInfo.MethodNumber = this._classMethods[this._currentClass].Count + 1;
            methodInfo.CodeAddress = position;
            methodInfo.Arguments = 0;
            methodInfo.Variables = this._localVariables[this._currentClass][this._currentMethod].Count;
            this._classMethods[this._currentClass].Add(methodSignature, methodInfo);            
        }

        private void ParseClass()
        {
            Token identifier = this._lex.NextToken();
            if (identifier.TokenType != TokenType.Identifier)
            {
                throw new Exception("Expects identifier after 'class'");
            }

            string className = this._module + "." + (String)identifier.Value;
            string shortClassName = this.GetShortClassName(className);
            string parentClassName = null;

            if (this._classes.ContainsKey(className))
            {
                throw new Exception(string.Format("Class '{0}' already defined", _classes));
            }

            if (this._lex.MatchKeyword("inherits"))
            {
                parentClassName = this.ResolveFullClassName(this._lex.GetDotSeparatedIdentifier("Expects identifier after 'inherits'"));                
            }
            else if (className != "oriole.core.Object")
            {
                parentClassName = "oriole.core.Object"; // default inherits from Object
            }

            if (parentClassName != null)
            {
                string shortParentClassName = this.GetShortClassName(parentClassName);
                if (!this._allClasses.ContainsKey(parentClassName))
                {
                    ClassReferenceInfo info = new ClassReferenceInfo();
                    info.ClassName = parentClassName;
                    info.ClassNumber = this._allClasses.Count + 1;
                    this._allClasses.Add(parentClassName, info);
                    if (this._classNameMap.ContainsKey(shortParentClassName))
                    {
                        this._classNameMap[shortParentClassName].Add(parentClassName);
                    }
                    else
                    {
                        List<string> list = new List<string>();
                        list.Add(parentClassName);
                        this._classNameMap.Add(shortParentClassName, list);
                    }
                }
            }

            ClassInfo classInfo = new ClassInfo();
            classInfo.ClassName = className;
            classInfo.IsFacade = false;
            classInfo.IsImplicitClass = false;
            classInfo.TypeName = null;
            classInfo.AssemblyFile = null;
            classInfo.ParentClassName = parentClassName;
            classInfo.ImplicitDataType = null;
            this._classes.Add(className, classInfo);
            this._classStaticFields.Add(className, new Dictionary<string, FieldInfo>());
            this._classInstanceFields.Add(className, new Dictionary<string, FieldInfo>());
            this._classMethods.Add(className, new Dictionary<string, MethodInfo>());
            this._localVariables.Add(className, new Dictionary<string, Dictionary<string, Variable>>());
            if (!this._allClasses.ContainsKey(className))
            {
                ClassReferenceInfo info = new ClassReferenceInfo();
                info.ClassName = className;
                info.ClassNumber = this._allClasses.Count + 1;
                this._allClasses.Add(className, info);
                if (this._classNameMap.ContainsKey(shortClassName))
                {
                    this._classNameMap[shortClassName].Add(className);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(className);
                    this._classNameMap.Add(shortClassName, list);
                }
            }
            this._currentClass = className;
            bool constructorDefined = false;
            //bool staticConstructorDefined = false;
            this._lex.NeedDelimiter("{");
            while (this._lex.PeekToken().TokenType != TokenType.EOF)
            {
                if (this._lex.MatchKeyword("static"))
                {
                    if (this._lex.MatchKeyword("method"))
                    {
                        this.ParseMethod(true, false);
                    }
                    else if (this._lex.MatchKeyword("field"))
                    {
                        this.ParseField(true);
                    }
                    else
                    {
                        Token token = this._lex.PeekToken();
                        if (token.IsDelimiter("{"))
                        {
                            this.ParseStaticConstructor();
                            //staticConstructorDefined = true;
                        }
                        else
                        {
                            throw new Exception("Expects 'field' or 'method' after 'static'.");
                        }
                    }                    
                }
                else if (this._lex.MatchKeyword("synchronized"))
                {
                    if (this._lex.MatchKeyword("static"))
                    {
                        if (this._lex.MatchKeyword("method"))
                        {
                            this.ParseMethod(true, true);
                        }
                        else
                        {
                            throw new Exception("Expects 'field' or 'method' after 'static'.");
                        }
                    }
                    else if (this._lex.MatchKeyword("method"))
                    {
                        this.ParseMethod(false, true);
                    }
                    else
                    {
                        throw new Exception("Expects 'static' or 'method' after 'synchronized'.");
                    }
                }
                else if (this._lex.MatchKeyword("method"))
                {
                    this.ParseMethod(false, false);
                }
                else if (this._lex.MatchKeyword("field"))
                {
                    this.ParseField(false);
                }
                else if (this._lex.MatchKeyword("constructor"))
                {
                    this.ParseConstructor();
                    constructorDefined = true;
                }
                else if (this._lex.MatchDelimiter("}"))
                {
                    /*if (!staticConstructorDefined)
                    {
                        this.CreateDefaultStaticConstructor(); // define a default static constructor if user did not define it
                    }*/
                    if (!constructorDefined) // define a default constructor if user did not define it
                    {
                        this.CreateDefaultConstructor();
                    }
                    break;
                }
                else
                {
                    throw new Exception("Expects 'method' or 'field'.");
                }
            }
        }

        private void ParseFacadeClass()
        {
            if (!this._lex.MatchKeyword("class"))
            {
                throw new Exception("Expects 'class' after 'facade'");
            }

            Token identifier = this._lex.NextToken();
            if (identifier.TokenType != TokenType.Identifier)
            {
                throw new Exception("Expects identifier after 'class'");
            }

            string className = this._module + "." + (String)identifier.Value;
            string shortClassName = this.GetShortClassName(className);
            if (this._classes.ContainsKey(className))
            {
                throw new Exception(string.Format("Class '{0}' already defined", _classes));
            }

            this._lex.NeedOperator("(");
            string typeName = this._lex.NeedStringLiteral();
            this._lex.NeedDelimiter(",");
            string assemblyFile = this._lex.NeedStringLiteral();
            this._lex.NeedOperator(")");
            this._lex.NeedDelimiter(";");

            // load the assembly to extract the class information

            Assembly assembly = Assembly.LoadFrom(assemblyFile);
            Type type = assembly.GetType(typeName);
            IFacadeClass facadeClass = (IFacadeClass)Activator.CreateInstance(type);

            // create the meta data

            ClassInfo classInfo = new ClassInfo();
            classInfo.ClassName = className;
            classInfo.IsFacade = true;
            classInfo.IsImplicitClass = false;
            classInfo.TypeName = typeName;
            classInfo.AssemblyFile = assemblyFile;
            classInfo.ParentClassName = null;
            classInfo.ImplicitDataType = null;
            this._classes.Add(className, classInfo);
            this._classStaticFields.Add(className, new Dictionary<string, FieldInfo>());
            this._classInstanceFields.Add(className, new Dictionary<string, FieldInfo>());
            this._classMethods.Add(className, new Dictionary<string, MethodInfo>());
            this._localVariables.Add(className, new Dictionary<string, Dictionary<string, Variable>>());
            if (!this._allClasses.ContainsKey(className))
            {
                ClassReferenceInfo info = new ClassReferenceInfo();
                info.ClassName = className;
                info.ClassNumber = this._allClasses.Count + 1;
                this._allClasses.Add(className, info);
                if (this._classNameMap.ContainsKey(shortClassName))
                {
                    this._classNameMap[shortClassName].Add(className);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(className);
                    this._classNameMap.Add(shortClassName, list);
                }
            }
            this._currentClass = className;

            foreach (string fieldName in facadeClass.GetStaticFields())
            {
                FieldInfo field = new FieldInfo();
                field.FieldName = fieldName;
                field.FieldNumber = this._classStaticFields[className].Count + 1;
                field.IsStatic = true;
                this._classStaticFields[className].Add(fieldName, field);
                if (!this._allFields.ContainsKey(fieldName))
                {
                    FieldAccessInfo info = new FieldAccessInfo();
                    info.FieldName = fieldName;
                    info.FieldAccessNumber = this._allFields.Count + 1;
                    this._allFields.Add(fieldName, info);
                }
            }

            foreach (string fieldName in facadeClass.GetInstanceFields())
            {
                FieldInfo field = new FieldInfo();
                field.FieldName = fieldName;
                field.FieldNumber = this._classInstanceFields[className].Count + 1;
                field.IsStatic = false;
                this._classInstanceFields[className].Add(fieldName, field);
                if (!this._allFields.ContainsKey(fieldName))
                {
                    FieldAccessInfo info = new FieldAccessInfo();
                    info.FieldName = fieldName;
                    info.FieldAccessNumber = this._allFields.Count + 1;
                    this._allFields.Add(fieldName, info);
                }
            }

            foreach (string methodSignature in facadeClass.GetStaticMethodSignatures())
            {
                MethodInfo method = new MethodInfo();
                method.MethodName = methodSignature;
                method.MethodNumber = this._classMethods[className].Count + 1;
                method.Variables = 0;
                method.Arguments = this.ComputeArguments(methodSignature);
                method.CodeAddress = -1;
                method.IsStatic = true;
                this._classMethods[className].Add(methodSignature, method);
                if (!this._allMethods.ContainsKey(methodSignature))
                {
                    MethodCallInfo info = new MethodCallInfo();
                    info.MethodSignature = methodSignature;
                    info.MethodCallNumber = this._allMethods.Count + 1;
                    this._allMethods.Add(methodSignature, info);
                }
            }

            foreach (string methodSignature in facadeClass.GetInstanceMethodSignatures())
            {
                MethodInfo method = new MethodInfo();
                method.MethodName = methodSignature;
                method.MethodNumber = this._classMethods[className].Count + 1;
                method.Variables = 0;
                method.Arguments = this.ComputeArguments(methodSignature);
                method.CodeAddress = -1;
                method.IsStatic = false;
                this._classMethods[className].Add(methodSignature, method);                
                if (!this._allMethods.ContainsKey(methodSignature))
                {
                    MethodCallInfo info = new MethodCallInfo();
                    info.MethodSignature = methodSignature;
                    info.MethodCallNumber = this._allMethods.Count + 1;
                    this._allMethods.Add(methodSignature, info);
                }
            }
        }

        private void ParseImplicitClass()
        {
            Token identifier = this._lex.NextToken();
            if (identifier.TokenType != TokenType.Identifier)
            {
                throw new Exception("Expects identifier after 'class'");
            }

            string className = this._module + "." + (String)identifier.Value;
            string shortClassName = this.GetShortClassName(className);

            if (!this._lex.MatchKeyword("for"))
            {
                throw new Exception("Expect 'for' after implicit class.");
            }

            string dataType = this._lex.NeedStringLiteral();

            if (this._classes.ContainsKey(className))
            {
                throw new Exception(string.Format("Class '{0}' already defined", _classes));
            }

            ClassInfo classInfo = new ClassInfo();
            classInfo.ClassName = className;
            classInfo.IsFacade = false;
            classInfo.IsImplicitClass = true;
            classInfo.TypeName = null;
            classInfo.AssemblyFile = null;
            classInfo.ParentClassName = null;
            classInfo.ImplicitDataType = dataType;
            this._classes.Add(className, classInfo);
            this._classStaticFields.Add(className, new Dictionary<string, FieldInfo>());
            this._classInstanceFields.Add(className, new Dictionary<string, FieldInfo>());
            this._classMethods.Add(className, new Dictionary<string, MethodInfo>());
            this._localVariables.Add(className, new Dictionary<string, Dictionary<string, Variable>>());
            if (!this._allClasses.ContainsKey(className))
            {
                ClassReferenceInfo info = new ClassReferenceInfo();
                info.ClassName = className;
                info.ClassNumber = this._allClasses.Count + 1;
                this._allClasses.Add(className, info);
                if (this._classNameMap.ContainsKey(shortClassName))
                {
                    this._classNameMap[shortClassName].Add(className);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(className);
                    this._classNameMap.Add(shortClassName, list);
                }
            }
            this._currentClass = className;

            this._lex.NeedDelimiter("{");
            while (this._lex.PeekToken().TokenType != TokenType.EOF)
            {
                if (this._lex.MatchKeyword("static"))
                {
                    if (this._lex.MatchKeyword("method"))
                    {
                        this.ParseMethod(true, false);
                    }
                    else if (this._lex.MatchKeyword("field"))
                    {
                        this.ParseField(true);
                    }
                    else
                    {
                        throw new Exception("Expects 'field' or 'method' after 'static'.");
                    }
                }
                else if (this._lex.MatchKeyword("method"))
                {
                    this.ParseMethod(false, false);
                }
                else if (this._lex.MatchKeyword("field"))
                {
                    this.ParseField(false);
                }
                else if (this._lex.MatchKeyword("constructor"))
                {
                    this.ParseConstructor();
                }
                else if (this._lex.MatchDelimiter("}"))
                {
                    break;
                }
                else
                {
                    throw new Exception("Expects 'method' or 'field'.");
                }
            }
        }

        private void ParseEnumeration()
        {
            Token identifier = this._lex.NextToken();
            if (identifier.TokenType != TokenType.Identifier)
            {
                throw new Exception("Expects identifier after 'enum'");
            }

            string enumType = this._module + "." + (String)identifier.Value;

            EnumerationInfo enumInfo = new EnumerationInfo();
            enumInfo.Name = enumType;
            enumInfo.Fields = new Dictionary<string, int>();

            this._lex.NeedDelimiter("{");
            int enumValue = 1;
            while (true)
            {
                Token token = this._lex.NextToken();
                if (token.TokenType == TokenType.Identifier)
                {
                    string enumField = (string)token.Value;
                    if (this._lex.MatchOperator("="))
                    {
                        Token enumValueToken = this._lex.NextToken();
                        if (enumValueToken.TokenType != TokenType.Literal || !(enumValueToken.Value is int))
                        {
                            throw new Exception("Expects integer for enumeration field value.");
                        }
                        enumValue = (int)enumValueToken.Value;
                    }
                    enumInfo.Fields.Add(enumField, enumValue);
                    enumValue++;
                    if (this._lex.MatchDelimiter(","))
                    {
                        continue;
                    }
                    else if (this._lex.MatchDelimiter("}"))
                    {
                        if (this._allEnums.ContainsKey(enumInfo.Name))
                        {
                            throw new Exception(string.Format("enum '{0}' already defined", enumInfo.Name));
                        }
                        this._allEnums.Add(enumInfo.Name, enumInfo);
                        this._enums.Add(enumInfo.Name, enumInfo);
                        break;
                    }
                }                
                
                throw new Exception("Expects enumeration field in enumeration type.");                
            }
        }

        private void ParseUse()
        {
            string moduleName = this._lex.GetDotSeparatedIdentifier("Expects identifier after 'using'");
            // load module information
            this.LoadModuleMeta(moduleName);
            if (!this._dependentModules.Contains(moduleName))
            {
                this._dependentModules.Add(moduleName);
            }
            this._lex.NeedDelimiter(";");
        }

        private void ParseModule()
        {
            if (this._lex.MatchKeyword("module"))
            {   
                this._module = this._lex.GetDotSeparatedIdentifier("Expects identifier after 'module'");
                
                this._lex.NeedDelimiter("{"); 
                while (this._lex.PeekToken().TokenType != TokenType.EOF)
                {
                    if (this._lex.MatchKeyword("class"))
                    {
                        this.ParseClass();
                    }
                    else if (this._lex.MatchKeyword("facade"))
                    {
                        this.ParseFacadeClass();
                    }
                    else if (this._lex.MatchKeyword("implicit"))
                    {
                        if (!this._lex.MatchKeyword("class"))
                        {
                            throw new Exception("Expects 'class' after 'implicit'.");
                        }
                        this.ParseImplicitClass();
                    }
                    else if (this._lex.MatchKeyword("enum"))
                    {
                        this.ParseEnumeration();
                    }
                    else if (this._lex.MatchKeyword("uses"))
                    {
                        this.ParseUse();
                    }
                    else if (this._lex.MatchDelimiter("}"))
                    {
                        break;
                    }
                    else
                    {
                        throw new Exception("Expects 'class', 'facade' or 'uses'.");
                    }
                }
            }
            else
            {
                throw new Exception("Expects 'module'");
            }  
        }

        private void CreateDefaultStaticConstructor()
        {
            string methodSignature = "__static(0)";

            if (this._classMethods[this._currentClass].ContainsKey(methodSignature))
            {
                throw new Exception(string.Format("Constructor '{0}' already defined in class", methodSignature));
            }

            Dictionary<string, Variable> localVariables = new Dictionary<string, Variable>();
            Variable variable = new Variable();
            variable.VariableName = "this";
            variable.VariableNumber = 1;
            localVariables.Add("this", variable);
            this._localVariables[this._currentClass].Add(methodSignature, localVariables);

            if (!this._allMethods.ContainsKey(methodSignature))
            {
                MethodCallInfo info = new MethodCallInfo();
                info.MethodSignature = methodSignature;
                info.MethodCallNumber = this._allMethods.Count + 1;
                this._allMethods.Add(methodSignature, info);
            }

            int position = this._code.Position;
            if (_currentClass != "oriole.core.Object")
            {
                this._code.LoadV(1); // this
                this._code.Super();
                this._code.CallC(this._allMethods[methodSignature].MethodCallNumber);
                this._code.Pop();
            }
            this._code.RetC();

            MethodInfo methodInfo = new MethodInfo();
            methodInfo.MethodName = methodSignature;
            methodInfo.MethodNumber = this._classMethods[this._currentClass].Count + 1;
            methodInfo.CodeAddress = position;
            methodInfo.Arguments = 0;
            methodInfo.Variables = 1;
            this._classMethods[this._currentClass].Add(methodSignature, methodInfo);
        }
        
        private void CreateDefaultConstructor()
        {
            string methodSignature = "__constructor(0)";

            if (this._classMethods[this._currentClass].ContainsKey(methodSignature))
            {
                throw new Exception(string.Format("Constructor '{0}' already defined in class", methodSignature));
            }

            Dictionary<string, Variable> localVariables = new Dictionary<string, Variable>();
            Variable variable = new Variable();
            variable.VariableName = "this";
            variable.VariableNumber = 1;
            localVariables.Add("this", variable);
            this._localVariables[this._currentClass].Add(methodSignature, localVariables);
            
            if (!this._allMethods.ContainsKey(methodSignature))
            {
                MethodCallInfo info = new MethodCallInfo();
                info.MethodSignature = methodSignature;
                info.MethodCallNumber = this._allMethods.Count + 1;
                this._allMethods.Add(methodSignature, info);
            }

            int position = this._code.Position;
            if (_currentClass != "oriole.core.Object")
            {
                this._code.LoadV(1); // this
                this._code.Super();
                this._code.CallC(this._allMethods[methodSignature].MethodCallNumber);
                this._code.Pop();
            }
            this._code.RetC();

            MethodInfo methodInfo = new MethodInfo();
            methodInfo.MethodName = methodSignature;
            methodInfo.MethodNumber = this._classMethods[this._currentClass].Count + 1;
            methodInfo.CodeAddress = position;
            methodInfo.Arguments = 0;
            methodInfo.Variables = 1;
            this._classMethods[this._currentClass].Add(methodSignature, methodInfo);
        }

        private void LoadModuleMeta(string module)
        {
            Code code = new Code();

            if (File.Exists(module + ".m"))
            {                
                code.Load(module);                
            }
            else if (File.Exists(module + ".s"))
            {
                code = new Compiler().Compile(File.ReadAllText(module + ".s"));                
            }
            else
            {
                throw new Exception(string.Format("Fail to load module: {0}", module));                    
            }

            BinaryReader metaReader = code.MetaReader;
            while (true)
            {
                int tag = (int)metaReader.ReadByte();
                switch (tag)
                {
                    case (int)MetaTag.TAG_MODULE:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_USEMODULE:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_CLASSREF:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_FIELDACCESS:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_METHODCALL:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_CLASS:
                        string className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        metaReader.ReadBytes(metaReader.ReadByte());
                        if (!this._allClasses.ContainsKey(className))
                        {
                            ClassReferenceInfo info = new ClassReferenceInfo();
                            info.ClassName = className;
                            info.ClassNumber = this._allClasses.Count + 1;
                            this._allClasses.Add(className, info);
                            string shortClassName = this.GetShortClassName(className);
                            if (this._classNameMap.ContainsKey(shortClassName))
                            {
                                this._classNameMap[shortClassName].Add(className);
                            }
                            else
                            {
                                List<string> list = new List<string>();
                                list.Add(className);
                                this._classNameMap.Add(shortClassName, list);
                            }
                        }
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_FACADECLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        if (!this._allClasses.ContainsKey(className))
                        {
                            ClassReferenceInfo info = new ClassReferenceInfo();
                            info.ClassName = className;
                            info.ClassNumber = this._allClasses.Count + 1;
                            this._allClasses.Add(className, info);
                            string shortClassName = this.GetShortClassName(className);
                            if (this._classNameMap.ContainsKey(shortClassName))
                            {
                                this._classNameMap[shortClassName].Add(className);
                            }
                            else
                            {
                                List<string> list = new List<string>();
                                list.Add(className);
                                this._classNameMap.Add(shortClassName, list);
                            }
                        }
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadBytes(metaReader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_IMPLICITCLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        metaReader.ReadBytes(metaReader.ReadByte());
                        if (!this._allClasses.ContainsKey(className))
                        {
                            ClassReferenceInfo info = new ClassReferenceInfo();
                            info.ClassName = className;
                            info.ClassNumber = this._allClasses.Count + 1;
                            this._allClasses.Add(className, info);
                            string shortClassName = this.GetShortClassName(className);
                            if (this._classNameMap.ContainsKey(shortClassName))
                            {
                                this._classNameMap[shortClassName].Add(className);
                            }
                            else
                            {
                                List<string> list = new List<string>();
                                list.Add(className);
                                this._classNameMap.Add(shortClassName, list);
                            }
                        }
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        metaReader.ReadBytes(metaReader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_LOCALVARIABLE:
                    case (int)MetaTag.TAG_FIELD:
                    case (int)MetaTag.TAG_STATICFIELD:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_METHOD:
                    case (int)MetaTag.TAG_STATICMETHOD:
                        metaReader.ReadBytes(metaReader.ReadByte());
                        metaReader.ReadUInt16();
                        metaReader.ReadInt32();
                        metaReader.ReadByte();
                        metaReader.ReadUInt16();
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL:
                        metaReader.ReadUInt16();
                        metaReader.ReadBytes(metaReader.ReadUInt16());
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL_S:
                        metaReader.ReadUInt16();
                        metaReader.ReadBytes(metaReader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_ENUM:
                        EnumerationInfo enumInfo = new EnumerationInfo();
                        enumInfo.Name = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                        enumInfo.Fields = new Dictionary<string, int>();
                        int n = metaReader.ReadUInt16();                        
                        for (int i = 0; i < n; i++)
                        {
                            string enumField = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                            int enumValue = metaReader.ReadUInt16();
                            enumInfo.Fields.Add(enumField, enumValue);
                        }
                        if (this._allEnums.ContainsKey(enumInfo.Name))
                        {
                            throw new Exception(string.Format("Enum '{0}' already defined in other module.", enumInfo.Name));
                        }
                        this._allEnums.Add(enumInfo.Name, enumInfo);
                        /* TODO: check if is correct */
                        string shortEnumName = this.GetShortClassName(enumInfo.Name);
                        if (this._classNameMap.ContainsKey(shortEnumName))
                        {
                            this._classNameMap[shortEnumName].Add(enumInfo.Name);
                        }
                        else
                        {
                            List<string> list = new List<string>();
                            list.Add(enumInfo.Name);
                            this._classNameMap.Add(shortEnumName, list);
                        }
                        continue;
                    case (int)MetaTag.TAG_END:
                        break;
                    default:
                        throw new Exception(string.Format("Unknown meta tag: {0}", tag));
                }
                break;
            }
        }

        private void ExportMeta()
        {
            this._code.AddModule(this._module, this._allClasses.Count, this._allFields.Count, this._allMethods.Count, this._stringLiterals.Count);
            foreach(string module in this._dependentModules)
            {
                this._code.AddUseModule(module);
            }
            foreach (string className in this._allClasses.Keys)
            {
                this._code.AddClassReference(this._allClasses[className]);
            }
            foreach(string fieldName in this._allFields.Keys)
            {
                this._code.AddFieldAccess(this._allFields[fieldName]);
            }
            foreach(string methodSignature in this._allMethods.Keys)
            {
                this._code.AddMethodCall(this._allMethods[methodSignature]);
            }            
            foreach (string className in this._classes.Keys)
            {
                this._code.AddClass(this._classes[className], this._classStaticFields[className].Count, this._classInstanceFields[className].Count, this._classMethods[className].Count);
                foreach (string fieldName in this._classStaticFields[className].Keys)
                {
                    this._code.AddField(this._classStaticFields[className][fieldName]);
                }
                foreach (string fieldName in this._classInstanceFields[className].Keys)
                {
                    this._code.AddField(this._classInstanceFields[className][fieldName]);
                }
                foreach (string methodSignature in this._classMethods[className].Keys)
                {
                    this._code.AddMethod(this._classMethods[className][methodSignature]);
                    if (this._localVariables[className].ContainsKey(methodSignature))
                    {
                        foreach (string variableName in this._localVariables[className][methodSignature].Keys)
                        {
                            this._code.AddLocalVariable(this._localVariables[className][methodSignature][variableName]);
                        }
                    }
                }                
            }
            foreach (string enumType in this._enums.Keys)
            {
                this._code.AddEnumeration(this._enums[enumType]);
            }
            foreach (StringInfo literals in this._stringLiterals.Values)
            {
                this._code.AddStringLiteral(literals);
            }
            this._code.EndMeta();
        }

        public void Load(object operand, Code code)
        {
            if (operand is Identifier)
            {
                string variableName = ((Identifier)operand).Name;
                /*if (this._allClasses.ContainsKey(variableName))
                {
                    // is a class
                    code.LoadClass(this._allClasses[variableName].ClassNumber);
                    return;
                }
                else */if (!this._localVariables[this._currentClass][this._currentMethod].ContainsKey(variableName))
                {
                    Variable variable = new Variable();
                    variable.VariableName = variableName;
                    variable.VariableNumber = this._localVariables[this._currentClass][this._currentMethod].Count + 1;
                    this._localVariables[this._currentClass][this._currentMethod].Add(variableName, variable);
                }
                code.LoadV(this._localVariables[this._currentClass][this._currentMethod][variableName].VariableNumber);
            }
            else if (operand is ArrayReference)
            {
                code.LoadA();
            }
            else if (operand is FieldReference)
            {
                code.LoadF(((FieldReference)operand).FieldNumber);
            }
            else if (operand is Constant)
            {
                if (((Constant)operand).Value is string)
                {
                    string literal = (string)((Constant)operand).Value;
                    if (!this._stringLiterals.ContainsKey(literal))
                    {
                        StringInfo info = new StringInfo();
                        info.Value = literal;
                        info.StringNumber = this._stringLiterals.Count + 1;
                        this._stringLiterals.Add(literal, info);
                    }
                    code.LoadS(this._stringLiterals[literal].StringNumber);
                }
                else
                {
                    code.LoadC((Constant)operand);
                }
            }
            else if (!(operand is ClassReference || operand is Expression || operand is MethodCall))
            {
                throw new Exception(string.Format("Load() unknown type: {0}", operand.GetType().Name));
            }            
        }

        public void Store(object target, Code code)
        {
            if (target is Identifier)
            {
                string variableName = ((Identifier)target).Name;
                if (!this._localVariables[this._currentClass][this._currentMethod].ContainsKey(variableName))
                {
                    Variable variable = new Variable();
                    variable.VariableName = variableName;
                    variable.VariableNumber = this._localVariables[this._currentClass][this._currentMethod].Count + 1;
                    this._localVariables[this._currentClass][this._currentMethod].Add(variableName, variable);
                }
                code.StoreV(this._localVariables[this._currentClass][this._currentMethod][variableName].VariableNumber);
            }
            else if (target is ArrayReference)
            {
                code.StoreA();
            }
            else if (target is FieldReference)
            {
                code.StoreF(((FieldReference)target).FieldNumber);
            }
            else
            {
                throw new Exception(string.Format("Store() type not supported: {0}", target.GetType().Name));
            }
        }

        public void Call(string methodSignature, Code code)
        {
            if (!this._allMethods.ContainsKey(methodSignature))
            {
                MethodCallInfo info = new MethodCallInfo();
                info.MethodSignature = methodSignature;
                info.MethodCallNumber = this._allMethods.Count + 1;
                this._allMethods.Add(methodSignature, info);
            }
            code.Call(this._allMethods[methodSignature].MethodCallNumber);
        }

        public int CreateTemporaryVariable()
        {   
            Variable variable = new Variable();            
            variable.VariableNumber = this._localVariables[this._currentClass][this._currentMethod].Count + 1;
            variable.VariableName = "$" + variable.VariableNumber;
            this._localVariables[this._currentClass][this._currentMethod].Add(variable.VariableName, variable);
            return variable.VariableNumber;
        }

        #endregion

        public Code Compile(string statements)
        {
            this._code = new Code();
            this._lex = new LexicalAnalyzer(new MemoryStream(Encoding.UTF8.GetBytes(statements)));
            this._asm = new Assembler();
            while (this._lex.PeekToken().TokenType != TokenType.EOF)
            {
                this.ParseModule();
            }
            this._asm.ResolveLabels(this._code);
            this.ExportMeta();
            return this._code;
        }
    }

    public class Optimizer
    {
        private bool _debug = false;

        #region Optimize branching

        public enum BranchType
        {
            Long, Medium, Short
        }

        public class BranchInfo
        {
            public int BranchFrom;
            public int BranchTo;
            public int BranchOffset;
            public BranchType BranchType;
        }

        public class CaseBranchInfo
        {
            public int BranchFrom;
            public int CaseCount;
            public BranchType BranchType;
            public int[] BranchOffsets;
            public int[] BranchTos;
        }

        public class TryCatchInfo
        {
            public int BranchFrom;
            public int BranchToCatch;
            public int BranchToFinally;
            public int CatchOffset;
            public int FinallyOffset;
            public BranchType BranchType;
        }

        private Code OptimizeBranching(Code code)
        {
            if (this._debug)
            {
                Console.WriteLine("optimizing branching ..");
            }

            DateTime start = DateTime.Now;
            List<int[]> methodOffsets = this.RetrieveMethodOffsets(new BinaryReader(code.MetaReader.BaseStream));
            List<BranchInfo> branches = null;
            List<CaseBranchInfo> caseBranches = null;
            List<TryCatchInfo> tryCatches = null;
            this.RetrieveBranchingInfo(new BinaryReader(code.Reader.BaseStream), out branches, out caseBranches, out tryCatches);
            this.OptimizeBranches(methodOffsets, branches, caseBranches, tryCatches);
            Code optimizedCode = this.GenerateOptimizedCode(new BinaryReader(code.MetaReader.BaseStream), new BinaryReader(code.Reader.BaseStream), methodOffsets, branches, caseBranches, tryCatches);
            if (this._debug)
            {
                Console.WriteLine("optimizing branching took: {0}", DateTime.Now - start);
                Console.WriteLine("branch optimization reduce code size from {0} bytes to {1} bytes, {2}%", code.CodeSize, optimizedCode.CodeSize, (code.CodeSize - optimizedCode.CodeSize) * 100 / code.CodeSize);
            }
            return optimizedCode;
        }

        private List<int[]> RetrieveMethodOffsets(BinaryReader reader)
        {
            if (this._debug)
            {
                Console.WriteLine("retrieving method offsets ..");
            }
            List<int[]> methodOffsets = new List<int[]>();

            while (true)
            {
                int tag = (int)reader.ReadByte();
                switch (tag)
                {
                    case (int)MetaTag.TAG_MODULE:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_USEMODULE:
                        reader.ReadBytes(reader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_CLASSREF:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_FIELDACCESS:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_METHODCALL:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_CLASS:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_FACADECLASS:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadBytes(reader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_IMPLICITCLASS:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        reader.ReadBytes(reader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_STATICFIELD:
                    case (int)MetaTag.TAG_FIELD:
                    case (int)MetaTag.TAG_LOCALVARIABLE:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_STATICMETHOD:
                    case (int)MetaTag.TAG_METHOD:
                        reader.ReadBytes(reader.ReadByte());
                        reader.ReadUInt16();
                        int position = (int)reader.BaseStream.Position;
                        int offset = reader.ReadInt32();
                        methodOffsets.Add(new int[] { offset, position, 0 });
                        reader.ReadByte();
                        reader.ReadUInt16();
                        reader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL:
                        reader.ReadUInt16();
                        reader.ReadBytes(reader.ReadUInt16());
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL_S:
                        reader.ReadUInt16();
                        reader.ReadBytes(reader.ReadByte());
                        continue;
                    case (int)MetaTag.TAG_ENUM:
                        Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                        int n = reader.ReadUInt16();
                        for (int i = 0; i < n; i++)
                        {
                            Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
                            reader.ReadUInt16();
                        }
                        continue;
                    case (int)MetaTag.TAG_END:
                        break;
                    default:
                        throw new Exception(string.Format("Unknown meta tag: {0}", tag));
                }
                break;
            }
            return methodOffsets;
        }

        private void RetrieveBranchingInfo(BinaryReader reader, out List<BranchInfo> branches, out List<CaseBranchInfo> caseBranches, out List<TryCatchInfo> tryCatches)
        {
            if (this._debug)
            {
                Console.WriteLine("retrieving branch info..");
            }
            branches = new List<BranchInfo>();
            caseBranches = new List<CaseBranchInfo>();
            tryCatches = new List<TryCatchInfo>();
            BranchInfo branch = null;
            byte oper = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                oper = reader.ReadByte();

                switch ((int)oper)
                {
                    case (int)Instruction.NOP:
                    case (int)Instruction.DUP:
                    case (int)Instruction.ADD:
                    case (int)Instruction.SUB:
                    case (int)Instruction.MUL:
                    case (int)Instruction.DIV:
                    case (int)Instruction.MOD:
                    case (int)Instruction.NEG:
                    case (int)Instruction.AND:
                    case (int)Instruction.OR:
                    case (int)Instruction.XOR:
                    case (int)Instruction.NOT:
                    case (int)Instruction.LSHIFT:
                    case (int)Instruction.RSHIFT:
                    case (int)Instruction.CE:
                    case (int)Instruction.CNE:
                    case (int)Instruction.CL:
                    case (int)Instruction.CLE:
                    case (int)Instruction.CG:
                    case (int)Instruction.CGE:
                    case (int)Instruction.POP:
                    case (int)Instruction.RET:
                    case (int)Instruction.RETV:
                    case (int)Instruction.RETC:
                    case (int)Instruction.HALT:
                    case (int)Instruction.VERSION:
                    case (int)Instruction.LDNULL:
                    case (int)Instruction.LDZERO:
                    case (int)Instruction.LDARRAY:
                    case (int)Instruction.STOREA:
                    case (int)Instruction.CASTBYTE:
                    case (int)Instruction.CASTCHAR:
                    case (int)Instruction.CASTINT:
                    case (int)Instruction.CASTLONG:
                    case (int)Instruction.CASTFLOAT:
                    case (int)Instruction.CASTSTRING:
                    case (int)Instruction.NEWARRAY:
                    case (int)Instruction.SIZE:
                    case (int)Instruction.HASH:
                    case (int)Instruction.TIME:
                    case (int)Instruction.LEAVETRY:
                    case (int)Instruction.LEAVECATCH:
                    case (int)Instruction.LEAVEFINALLY:
                    case (int)Instruction.THROW:
                    case (int)Instruction.RETHROW:
                    case (int)Instruction.ISINSTANCE:
                    case (int)Instruction.SUPER:
                    case (int)Instruction.LOADMODULE:
                    case (int)Instruction.CLASSNAME:
                    case (int)Instruction.HIERARCHY:
                    case (int)Instruction.STATICFIELDS:
                    case (int)Instruction.FIELDS:
                    case (int)Instruction.STATICMETHODS:
                    case (int)Instruction.METHODS:
                    case (int)Instruction.CREATEINSTANCE:
                    case (int)Instruction.GETFIELD:
                    case (int)Instruction.SETFIELD:
                    case (int)Instruction.INVOKEMETHOD:
                    case (int)Instruction.INVOKECONSTRUCTOR:
                    case (int)Instruction.FORK:
                    case (int)Instruction.JOIN:
                    case (int)Instruction.SLEEP:
                    case (int)Instruction.NICE:
                    case (int)Instruction.WAIT:
                    case (int)Instruction.SIGNAL:
                    case (int)Instruction.EXIT:
                    case (int)Instruction.CURTHREAD:
                        break;

                    case (int)Instruction.BR_S:
                    case (int)Instruction.BE_S:
                    case (int)Instruction.BNE_S:
                    case (int)Instruction.BL_S:
                    case (int)Instruction.BLE_S:
                    case (int)Instruction.BG_S:
                    case (int)Instruction.BGE_S:
                    case (int)Instruction.BTRUE_S:
                    case (int)Instruction.BFALSE_S:                    
                        int n = reader.ReadSByte();
                        branch = new BranchInfo();
                        branch.BranchFrom = (int)reader.BaseStream.Position;
                        branch.BranchTo = (int)reader.BaseStream.Position + n;
                        branch.BranchOffset = n;
                        branch.BranchType = BranchType.Short;
                        branches.Add(branch);
                        break;

                    case (int)Instruction.BR:
                    case (int)Instruction.BE:
                    case (int)Instruction.BNE:
                    case (int)Instruction.BL:
                    case (int)Instruction.BLE:
                    case (int)Instruction.BG:
                    case (int)Instruction.BGE:
                    case (int)Instruction.BTRUE:
                    case (int)Instruction.BFALSE:
                        n = reader.ReadInt16();
                        branch = new BranchInfo();
                        branch.BranchFrom = (int)reader.BaseStream.Position;
                        branch.BranchTo = (int)reader.BaseStream.Position + n;
                        branch.BranchOffset = n;
                        branch.BranchType = BranchType.Medium;
                        branches.Add(branch);
                        break;

                    case (int)Instruction.BR_L:                        
                    case (int)Instruction.BE_L:
                    case (int)Instruction.BNE_L:
                    case (int)Instruction.BL_L:
                    case (int)Instruction.BLE_L:
                    case (int)Instruction.BG_L:
                    case (int)Instruction.BGE_L:
                    case (int)Instruction.BTRUE_L:
                    case (int)Instruction.BFALSE_L:                    
                        n = reader.ReadInt32();
                        branch = new BranchInfo();
                        branch.BranchFrom = (int)reader.BaseStream.Position;
                        branch.BranchTo = (int)reader.BaseStream.Position + n;
                        branch.BranchOffset = n;
                        branch.BranchType = BranchType.Long;
                        branches.Add(branch);
                        break;
                    
                    case (int)Instruction.BMAP_L:
                        n = reader.ReadUInt16();                        
                        CaseBranchInfo caseBranch = new CaseBranchInfo();
                        caseBranch.BranchFrom = (int)reader.BaseStream.Position;
                        caseBranch.BranchOffsets = new int[n];
                        caseBranch.BranchTos = new int[n];
                        caseBranch.BranchType = BranchType.Long;
                        caseBranch.CaseCount = n;
                        for (int i = 0; i < n; i++)
                        {
                            caseBranch.BranchOffsets[i] = reader.ReadInt32();
                            caseBranch.BranchTos[i] = caseBranch.BranchFrom + caseBranch.BranchOffsets[i];
                        }
                        caseBranches.Add(caseBranch);
                        break;

                    case (int)Instruction.BMAP:
                        n = reader.ReadUInt16();
                        caseBranch = new CaseBranchInfo();
                        caseBranch.BranchFrom = (int)reader.BaseStream.Position;
                        caseBranch.BranchOffsets = new int[n];
                        caseBranch.BranchTos = new int[n];
                        caseBranch.BranchType = BranchType.Medium;
                        caseBranch.CaseCount = n;
                        for (int i = 0; i < n; i++)
                        {
                            caseBranch.BranchOffsets[i] = reader.ReadInt16();
                            caseBranch.BranchTos[i] = caseBranch.BranchFrom + caseBranch.BranchOffsets[i];
                        }
                        caseBranches.Add(caseBranch);
                        break;

                    case (int)Instruction.BMAP_S:
                        n = reader.ReadUInt16();
                        caseBranch = new CaseBranchInfo();
                        caseBranch.BranchFrom = (int)reader.BaseStream.Position;
                        caseBranch.BranchOffsets = new int[n];
                        caseBranch.BranchTos = new int[n];
                        caseBranch.BranchType = BranchType.Short;
                        caseBranch.CaseCount = n;
                        for (int i = 0; i < n; i++)
                        {
                            caseBranch.BranchOffsets[i] = reader.ReadSByte();
                            caseBranch.BranchTos[i] = caseBranch.BranchFrom + caseBranch.BranchOffsets[i];
                        }
                        caseBranches.Add(caseBranch);
                        break;

                    case (int)Instruction.INC_S:
                    case (int)Instruction.DEC_S:
                    case (int)Instruction.CALL_S:
                    case (int)Instruction.CALLC_S:
                    case (int)Instruction.LDCBYTE:
                    case (int)Instruction.LDCBOOL:
                    case (int)Instruction.LDCSTRING_S:
                    case (int)Instruction.LDVAR_S:
                    case (int)Instruction.LDFIELD_S:
                    case (int)Instruction.LDCLASS_S:
                    case (int)Instruction.STORE_S:
                    case (int)Instruction.STOREF_S:
                    case (int)Instruction.NEWINSTANCE_S:
                        reader.ReadByte();
                        break;

                    case (int)Instruction.INC:
                    case (int)Instruction.DEC:
                    case (int)Instruction.CALL:
                    case (int)Instruction.CALLC:                    
                    case (int)Instruction.LDCSTRING:                    
                    case (int)Instruction.LDVAR:
                    case (int)Instruction.LDFIELD:
                    case (int)Instruction.LDCLASS:
                    case (int)Instruction.STORE:
                    case (int)Instruction.STOREF:
                    case (int)Instruction.NEWINSTANCE:
                        reader.ReadUInt16();
                        break;

                    case (int)Instruction.LDCCHAR:
                        reader.ReadChar();
                        break;
                    
                    case (int)Instruction.LDCINT16:
                        reader.ReadInt16();
                        break;

                    case (int)Instruction.LDCINT32:
                        reader.ReadInt32();
                        break;

                    case (int)Instruction.LDCINT64:
                        reader.ReadInt64();
                        break;

                    case (int)Instruction.LDCFLOAT64:
                        reader.ReadDouble();
                        break;

                    case (int)Instruction.ENTERTRY_L:
                        TryCatchInfo tryCatch = new TryCatchInfo();
                        tryCatch.CatchOffset = reader.ReadInt32();
                        tryCatch.FinallyOffset = reader.ReadInt32();
                        tryCatch.BranchFrom = (int)reader.BaseStream.Position;                        
                        tryCatch.BranchType = BranchType.Long;
                        tryCatch.BranchToCatch = tryCatch.BranchFrom + tryCatch.CatchOffset;
                        tryCatch.BranchToFinally = tryCatch.BranchFrom + tryCatch.FinallyOffset;
                        tryCatches.Add(tryCatch);
                        break;

                    case (int)Instruction.ESC:
                        reader.ReadUInt16();
                        break;

                    default:
                        throw new Exception(string.Format("unknown instruction code: {0} at {1:0000}", oper, reader.BaseStream.Position));
                }
            }
        }
        
        private void OptimizeBranches(List<int[]> methodOffsets, List<BranchInfo> branches, List<CaseBranchInfo> caseBranches, List<TryCatchInfo> tryCatches)
        {
            if (this._debug)
            {
                Console.WriteLine("optimizing branches ..");
                Console.WriteLine("total branches: {0}", branches.Count);
                Console.WriteLine("total case branches: {0}", caseBranches.Count);
                Console.WriteLine("total try catch blocks: {0}", tryCatches.Count);
                Console.WriteLine("total method offsets: {0}", methodOffsets.Count);                
            }
            int phase = 1;
            while (true)
            {
                bool branchReduced = false;                

                #region Branch optimization
                foreach (BranchInfo branch in branches)
                {
                    if (branch.BranchType != BranchType.Short)
                    {
                        BranchType branchType = BranchType.Long;

                        if (branch.BranchOffset >= -128 && branch.BranchOffset <= 127)
                        {
                            branchType = BranchType.Short;
                        }
                        else if (branch.BranchOffset >= -32768 && branch.BranchOffset <= 32767)
                        {
                            branchType = BranchType.Medium;
                        }

                        if (branch.BranchType != branchType)
                        {
                            branchReduced = true;                            
                            int saving = 0;
                            if (branch.BranchType == BranchType.Long && branchType == BranchType.Medium)
                            {
                                saving = 2;
                            }
                            else if (branch.BranchType == BranchType.Long && branchType == BranchType.Short)
                            {
                                saving = 3;
                            }
                            else if (branch.BranchType == BranchType.Medium && branchType == BranchType.Short)
                            {
                                saving = 1;
                            }
                            branch.BranchType = branchType;
                            if (branch.BranchOffset < 0)
                            {
                                branch.BranchOffset += saving;
                            }
                            foreach (BranchInfo otherBranch in branches)
                            {
                                if (otherBranch != branch)
                                {
                                    if (otherBranch.BranchFrom > branch.BranchFrom && otherBranch.BranchTo < branch.BranchFrom)
                                    {
                                        otherBranch.BranchOffset += saving;
                                    }
                                    else if (otherBranch.BranchFrom < branch.BranchFrom && otherBranch.BranchTo >= branch.BranchFrom)
                                    {
                                        otherBranch.BranchOffset -= saving;
                                    }
                                }
                            }
                            foreach (CaseBranchInfo caseBranch in caseBranches)
                            {
                                for (int i = 0; i < caseBranch.CaseCount; i++)
                                {
                                    if (caseBranch.BranchFrom > branch.BranchFrom && caseBranch.BranchTos[i] < branch.BranchFrom)
                                    {
                                        caseBranch.BranchOffsets[i] += saving;
                                    }
                                    else if (caseBranch.BranchFrom < branch.BranchFrom && caseBranch.BranchTos[i] >= branch.BranchFrom)
                                    {
                                        caseBranch.BranchOffsets[i] -= saving;
                                    }
                                }
                            }
                            foreach (TryCatchInfo tryCatch in tryCatches)
                            {
                                if (tryCatch.BranchFrom < branch.BranchFrom && tryCatch.BranchToCatch >= branch.BranchFrom)
                                {
                                    tryCatch.CatchOffset -= saving;
                                }
                                if (tryCatch.BranchFrom < branch.BranchFrom && tryCatch.BranchToFinally >= branch.BranchFrom)
                                {
                                    tryCatch.FinallyOffset -= saving;
                                }
                            }
                            foreach (int[] methodOffset in methodOffsets)
                            {
                                if (methodOffset[0] > branch.BranchFrom)
                                {
                                    methodOffset[2] -= saving;
                                }
                            }
                        }
                    }
                }
                #endregion
                
                #region Case branch optimization
                foreach (CaseBranchInfo caseBranch in caseBranches)
                {
                    if (caseBranch.BranchType != BranchType.Short)
                    {
                        BranchType branchType = BranchType.Short;

                        for (int i = 0; i < caseBranch.CaseCount; i++)
                        {
                            if (caseBranch.BranchOffsets[i] >= -128 && caseBranch.BranchOffsets[i] <= 127)
                            {
                            }
                            else if (caseBranch.BranchOffsets[i] >= -32768 && caseBranch.BranchOffsets[i] <= 32767)
                            {
                                if (branchType != BranchType.Long)
                                {
                                    branchType = BranchType.Medium;
                                }
                            }
                            else
                            {
                                branchType = BranchType.Long;
                            }
                        }

                        if (caseBranch.BranchType != branchType)
                        {
                            branchReduced = true;
                            int saving = 0;
                            if (caseBranch.BranchType == BranchType.Long && branchType == BranchType.Medium)
                            {
                                saving = caseBranch.CaseCount * 2;
                            }
                            else if (caseBranch.BranchType == BranchType.Long && branchType == BranchType.Short)
                            {
                                saving = caseBranch.CaseCount * 3;
                            }
                            else if (caseBranch.BranchType == BranchType.Medium && branchType == BranchType.Short)
                            {
                                saving = caseBranch.CaseCount;
                            }
                            caseBranch.BranchType = branchType;
                            foreach (BranchInfo branch in branches)
                            {
                                if (branch.BranchFrom > caseBranch.BranchFrom && branch.BranchTo < caseBranch.BranchFrom)
                                {
                                    branch.BranchOffset += saving;
                                }
                                else if (branch.BranchFrom < caseBranch.BranchFrom && branch.BranchTo > caseBranch.BranchFrom)
                                {
                                    branch.BranchOffset -= saving;
                                }
                            }
                            foreach (CaseBranchInfo otherCaseBranch in caseBranches)
                            {
                                if (otherCaseBranch != caseBranch)
                                {
                                    for (int i = 0; i < otherCaseBranch.CaseCount; i++)
                                    {
                                        if (otherCaseBranch.BranchFrom > caseBranch.BranchFrom && otherCaseBranch.BranchTos[i] < caseBranch.BranchFrom)
                                        {
                                            otherCaseBranch.BranchOffsets[i] += saving;
                                        }
                                        else if (otherCaseBranch.BranchFrom < caseBranch.BranchFrom && otherCaseBranch.BranchTos[i] > caseBranch.BranchFrom)
                                        {
                                            otherCaseBranch.BranchOffsets[i] -= saving;
                                        }
                                    }
                                }
                            }
                            foreach (TryCatchInfo tryCatch in tryCatches)
                            {
                                if (tryCatch.BranchFrom < caseBranch.BranchFrom && tryCatch.BranchToCatch > caseBranch.BranchFrom)
                                {
                                    tryCatch.CatchOffset -= saving;
                                }
                                if (tryCatch.BranchFrom < caseBranch.BranchFrom && tryCatch.BranchToFinally > caseBranch.BranchFrom)
                                {
                                    tryCatch.FinallyOffset -= saving;
                                }
                            }
                            foreach (int[] methodOffset in methodOffsets)
                            {
                                if (methodOffset[0] > caseBranch.BranchFrom)
                                {
                                    methodOffset[2] -= saving;
                                }
                            }
                        }
                    }
                }
                #endregion
                
                #region Try catch block optimization
                foreach (TryCatchInfo tryCatch in tryCatches)
                {
                    if (tryCatch.BranchType != BranchType.Short)
                    {
                        BranchType branchType = BranchType.Long;

                        if (tryCatch.CatchOffset >= -128 && tryCatch.CatchOffset <= 127 && tryCatch.FinallyOffset >= -128 && tryCatch.FinallyOffset <= 127)
                        {
                            branchType= BranchType.Short;
                        }
                        else if (tryCatch.CatchOffset >= -32768 && tryCatch.CatchOffset <= 32767&&tryCatch.FinallyOffset >= -32768 && tryCatch.FinallyOffset <= 32767)
                        {
                            branchType = BranchType.Medium;
                        }

                        if (branchType != tryCatch.BranchType)
                        {
                            branchReduced = true;
                            int saving = 0;
                            if (tryCatch.BranchType == BranchType.Long && branchType == BranchType.Medium)
                            {
                                saving = 2 * 2;
                            }
                            else if (tryCatch.BranchType == BranchType.Long && branchType == BranchType.Short)
                            {
                                saving = 2 * 3;
                            }
                            else if (tryCatch.BranchType == BranchType.Medium && branchType == BranchType.Short)
                            {
                                saving = 2 * 1;
                            }
                            tryCatch.BranchType = branchType;
                            foreach (BranchInfo branch in branches)
                            {
                                if (branch.BranchFrom > tryCatch.BranchFrom && branch.BranchTo < tryCatch.BranchFrom)
                                {
                                    branch.BranchOffset += saving;
                                }
                                else if (branch.BranchFrom < tryCatch.BranchFrom && branch.BranchTo > tryCatch.BranchFrom)
                                {
                                    branch.BranchOffset -= saving;
                                }
                            }
                            foreach (CaseBranchInfo caseBranch in caseBranches)
                            {
                                for (int i = 0; i < caseBranch.CaseCount; i++)
                                {
                                    if (caseBranch.BranchFrom > tryCatch.BranchFrom && caseBranch.BranchTos[i] < tryCatch.BranchFrom)
                                    {
                                        caseBranch.BranchOffsets[i] += saving;
                                    }
                                    else if (caseBranch.BranchFrom < tryCatch.BranchFrom && caseBranch.BranchTos[i] > tryCatch.BranchFrom)
                                    {
                                        caseBranch.BranchOffsets[i] -= saving;
                                    }
                                }
                             
                            }
                            foreach (TryCatchInfo otherTryCatch in tryCatches)
                            {
                                if (otherTryCatch != tryCatch)
                                {
                                    if (otherTryCatch.BranchFrom < tryCatch.BranchFrom && otherTryCatch.BranchToCatch > tryCatch.BranchFrom)
                                    {
                                        otherTryCatch.CatchOffset -= saving;
                                    }
                                    if (otherTryCatch.BranchFrom < tryCatch.BranchFrom && otherTryCatch.BranchToFinally > tryCatch.BranchFrom)
                                    {
                                        otherTryCatch.FinallyOffset -= saving;
                                    }
                                }
                            }
                            foreach (int[] methodOffset in methodOffsets)
                            {
                                if (methodOffset[0] > tryCatch.BranchFrom)
                                {
                                    methodOffset[2] -= saving;
                                }
                            }
                        }
                    }
                }
                #endregion

                if (!branchReduced)
                {
                    break;                
                }
                phase++;
            }
            if (this._debug)
            {
                Console.WriteLine("number of phases executed: {0}", phase);
            }
        }

        private Code GenerateOptimizedCode(BinaryReader metaReader, BinaryReader codeReader, List<int[]> methodOffsets, List<BranchInfo> branches, List<CaseBranchInfo> caseBranches, List<TryCatchInfo> tryCatches)
        {
            if (this._debug)
            {
                Console.WriteLine("generating optimized code ..");
            }

            Code code = new Code();
            BinaryWriter metaWriter = new BinaryWriter(code.MetaReader.BaseStream);
            BinaryWriter codeWriter = new BinaryWriter(code.Reader.BaseStream);

            metaWriter.Write(metaReader.ReadBytes((int)metaReader.BaseStream.Length));
            foreach (int[] methodOffset in methodOffsets)
            {
                metaWriter.BaseStream.Seek(methodOffset[1], SeekOrigin.Begin);
                metaWriter.Write(methodOffset[0] + methodOffset[2]);
            }

            Dictionary<int, BranchInfo> map = new Dictionary<int, BranchInfo>();
            foreach (BranchInfo branch in branches)
            {
                map.Add(branch.BranchFrom, branch);
            }

            Dictionary<int, CaseBranchInfo> caseBranchMap = new Dictionary<int, CaseBranchInfo>();
            foreach (CaseBranchInfo caseBranch in caseBranches)
            {
                caseBranchMap.Add(caseBranch.BranchFrom, caseBranch);
            }

            Dictionary<int, TryCatchInfo> tryCatchMap = new Dictionary<int, TryCatchInfo>();
            foreach (TryCatchInfo tryCatch in tryCatches)
            {
                tryCatchMap.Add(tryCatch.BranchFrom, tryCatch);
            }

            while (codeReader.BaseStream.Position < codeReader.BaseStream.Length)
            {
                byte oper = codeReader.ReadByte();

                switch ((int)oper)
                {
                    case (int)Instruction.NOP:
                    case (int)Instruction.DUP:
                    case (int)Instruction.ADD:
                    case (int)Instruction.SUB:
                    case (int)Instruction.MUL:
                    case (int)Instruction.DIV:
                    case (int)Instruction.MOD:
                    case (int)Instruction.NEG:
                    case (int)Instruction.AND:
                    case (int)Instruction.OR:
                    case (int)Instruction.XOR:
                    case (int)Instruction.NOT:
                    case (int)Instruction.LSHIFT:
                    case (int)Instruction.RSHIFT:
                    case (int)Instruction.CE:
                    case (int)Instruction.CNE:
                    case (int)Instruction.CL:
                    case (int)Instruction.CLE:
                    case (int)Instruction.CG:
                    case (int)Instruction.CGE:
                    case (int)Instruction.POP:
                    case (int)Instruction.RET:
                    case (int)Instruction.RETV:
                    case (int)Instruction.RETC:
                    case (int)Instruction.HALT:
                    case (int)Instruction.VERSION:
                    case (int)Instruction.LDNULL:
                    case (int)Instruction.LDZERO:
                    case (int)Instruction.LDARRAY:
                    case (int)Instruction.STOREA:
                    case (int)Instruction.CASTBYTE:
                    case (int)Instruction.CASTCHAR:
                    case (int)Instruction.CASTINT:
                    case (int)Instruction.CASTLONG:
                    case (int)Instruction.CASTFLOAT:
                    case (int)Instruction.CASTSTRING:
                    case (int)Instruction.NEWARRAY:
                    case (int)Instruction.SIZE:
                    case (int)Instruction.HASH:
                    case (int)Instruction.TIME:
                    case (int)Instruction.LEAVETRY:
                    case (int)Instruction.LEAVECATCH:
                    case (int)Instruction.LEAVEFINALLY:
                    case (int)Instruction.THROW:
                    case (int)Instruction.RETHROW:
                    case (int)Instruction.ISINSTANCE:
                    case (int)Instruction.SUPER:
                    case (int)Instruction.LOADMODULE:
                    case (int)Instruction.CLASSNAME:
                    case (int)Instruction.HIERARCHY:
                    case (int)Instruction.STATICFIELDS:
                    case (int)Instruction.FIELDS:
                    case (int)Instruction.STATICMETHODS:
                    case (int)Instruction.METHODS:
                    case (int)Instruction.CREATEINSTANCE:
                    case (int)Instruction.GETFIELD:
                    case (int)Instruction.SETFIELD:
                    case (int)Instruction.INVOKEMETHOD:
                    case (int)Instruction.INVOKECONSTRUCTOR:
                    case (int)Instruction.FORK:
                    case (int)Instruction.JOIN:
                    case (int)Instruction.SLEEP:
                    case (int)Instruction.NICE:
                    case (int)Instruction.WAIT:
                    case (int)Instruction.SIGNAL:
                    case (int)Instruction.EXIT:
                    case (int)Instruction.CURTHREAD:
                        codeWriter.Write(oper);
                        break;

                    case (int)Instruction.BR_S:
                    case (int)Instruction.BE_S:
                    case (int)Instruction.BNE_S:
                    case (int)Instruction.BL_S:
                    case (int)Instruction.BLE_S:
                    case (int)Instruction.BG_S:
                    case (int)Instruction.BGE_S:
                    case (int)Instruction.BTRUE_S:
                    case (int)Instruction.BFALSE_S:
                        codeReader.ReadSByte();                        
                        if (!map.ContainsKey((int)codeReader.BaseStream.Position))
                        {
                            throw new Exception("Internal error: cannot find branching information");
                        }
                        BranchInfo branch = map[(int)codeReader.BaseStream.Position];
                        codeWriter.Write(oper);
                        codeWriter.Write((sbyte)branch.BranchOffset);
                        break;

                    case (int)Instruction.BR:
                    case (int)Instruction.BE:
                    case (int)Instruction.BNE:
                    case (int)Instruction.BL:
                    case (int)Instruction.BLE:
                    case (int)Instruction.BG:
                    case (int)Instruction.BGE:
                    case (int)Instruction.BTRUE:
                    case (int)Instruction.BFALSE:
                        codeReader.ReadInt16();                        
                        if (!map.ContainsKey((int)codeReader.BaseStream.Position))
                        {
                            throw new Exception("Internal error: cannot find branching information");
                        }
                        branch = map[(int)codeReader.BaseStream.Position];
                        if (branch.BranchType == BranchType.Short)
                        {
                            codeWriter.Write((byte)(oper + 10));
                            codeWriter.Write((sbyte)branch.BranchOffset);
                        }
                        else if (branch.BranchType == BranchType.Medium)
                        {
                            codeWriter.Write(oper);
                            codeWriter.Write((sbyte)branch.BranchOffset);
                        }
                        break;

                    case (int)Instruction.BR_L:
                    case (int)Instruction.BE_L:
                    case (int)Instruction.BNE_L:
                    case (int)Instruction.BL_L:
                    case (int)Instruction.BLE_L:
                    case (int)Instruction.BG_L:
                    case (int)Instruction.BGE_L:
                    case (int)Instruction.BTRUE_L:
                    case (int)Instruction.BFALSE_L:                        
                        codeReader.ReadInt32();
                        if (!map.ContainsKey((int)codeReader.BaseStream.Position))
                        {
                            throw new Exception("Internal error: cannot find branching information");
                        }
                        branch = map[(int)codeReader.BaseStream.Position];                        
                        if (branch.BranchType == BranchType.Short)
                        {
                            codeWriter.Write((byte)(oper - 10));
                            codeWriter.Write((sbyte)branch.BranchOffset);
                        }
                        else if (branch.BranchType == BranchType.Medium)
                        {
                            codeWriter.Write((byte)(oper - 20));
                            codeWriter.Write((short)branch.BranchOffset);
                        }
                        else
                        {
                            codeWriter.Write(oper);
                            codeWriter.Write((int)branch.BranchOffset);
                        }
                        break;

                    case (int)Instruction.BMAP_L:
                    case (int)Instruction.BMAP:
                    case (int)Instruction.BMAP_S:                        
                        int n = (int)codeReader.ReadUInt16();
                        int position = (int)codeReader.BaseStream.Position;
                        
                        if (!caseBranchMap.ContainsKey(position))
                        {
                            throw new Exception("Internal error: cannot find case branch info");
                        }

                        if (oper == (int)Instruction.BMAP_L)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                codeReader.ReadInt32();
                            }
                        }
                        else if (oper == (int)Instruction.BMAP)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                codeReader.ReadInt16();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < n; i++)
                            {
                                codeReader.ReadSByte();
                            }
                        }

                        if (caseBranchMap[position].BranchType == BranchType.Long)
                        {
                            codeWriter.Write((byte)Instruction.BMAP_L);
                            codeWriter.Write((short)n);
                            for (int i = 0; i < n; i++)
                            {                                
                                codeWriter.Write(caseBranchMap[position].BranchOffsets[i]);
                            }
                        }
                        else if (caseBranchMap[position].BranchType == BranchType.Medium)
                        {
                            codeWriter.Write((byte)Instruction.BMAP);
                            codeWriter.Write((short)n);
                            for (int i = 0; i < n; i++)
                            {                                
                                codeWriter.Write((short)caseBranchMap[position].BranchOffsets[i]);
                            }
                        }
                        else
                        {
                            codeWriter.Write((byte)Instruction.BMAP_S);
                            codeWriter.Write((short)n);
                            for (int i = 0; i < n; i++)
                            {                                
                                codeWriter.Write((sbyte)caseBranchMap[position].BranchOffsets[i]);
                            }
                        }
                        break;

                    case (int)Instruction.INC_S:
                    case (int)Instruction.DEC_S:
                    case (int)Instruction.CALL_S:
                    case (int)Instruction.CALLC_S:
                    case (int)Instruction.LDCBYTE:
                    case (int)Instruction.LDCBOOL:
                    case (int)Instruction.LDCSTRING_S:
                    case (int)Instruction.LDVAR_S:
                    case (int)Instruction.LDFIELD_S:
                    case (int)Instruction.LDCLASS_S:
                    case (int)Instruction.STORE_S:
                    case (int)Instruction.STOREF_S:
                    case (int)Instruction.NEWINSTANCE_S:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadByte());
                        break;

                    case (int)Instruction.INC:
                    case (int)Instruction.DEC:
                    case (int)Instruction.CALL:
                    case (int)Instruction.CALLC:                    
                    case (int)Instruction.LDCSTRING:                    
                    case (int)Instruction.LDVAR:
                    case (int)Instruction.LDFIELD:
                    case (int)Instruction.LDCLASS:
                    case (int)Instruction.STORE:
                    case (int)Instruction.STOREF:
                    case (int)Instruction.NEWINSTANCE:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadUInt16());
                        break;

                    case (int)Instruction.LDCCHAR:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadChar());
                        break;

                    case (int)Instruction.LDCINT16:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadInt16());
                        break;

                    case (int)Instruction.LDCINT32:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadInt32());
                        break;

                    case (int)Instruction.LDCINT64:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadInt64());
                        break;

                    case (int)Instruction.LDCFLOAT64:
                        codeWriter.Write(oper);
                        codeWriter.Write(codeReader.ReadDouble());
                        break;

                    case (int)Instruction.ENTERTRY_L:
                    case (int)Instruction.ENTERTRY:
                    case (int)Instruction.ENTERTRY_S:
                        if (oper == (int)Instruction.ENTERTRY_L)
                        {
                            codeReader.ReadInt32();
                            codeReader.ReadInt32();
                        }
                        else if (oper == (int)Instruction.ENTERTRY)
                        {
                            codeReader.ReadInt16();
                            codeReader.ReadInt16();
                        }
                        else
                        {
                            codeReader.ReadSByte();
                            codeReader.ReadSByte();
                        }
                        position = (int)codeReader.BaseStream.Position;
                        if (!tryCatchMap.ContainsKey(position))
                        {
                            throw new Exception("Internal error: cannot find try catch info");
                        }
                        TryCatchInfo tryCatch = tryCatchMap[position];
                        if (tryCatch.BranchType == BranchType.Long)
                        {
                            codeWriter.Write((byte)Instruction.ENTERTRY_L);
                            codeWriter.Write(tryCatch.CatchOffset);
                            codeWriter.Write(tryCatch.FinallyOffset);
                        }
                        else if (tryCatch.BranchType == BranchType.Medium)
                        {
                            codeWriter.Write((byte)Instruction.ENTERTRY);
                            codeWriter.Write((short)tryCatch.CatchOffset);
                            codeWriter.Write((short)tryCatch.FinallyOffset);
                        }
                        else
                        {
                            codeWriter.Write((byte)Instruction.ENTERTRY_S);
                            codeWriter.Write((sbyte)tryCatch.CatchOffset);
                            codeWriter.Write((sbyte)tryCatch.FinallyOffset);
                        }                        
                        break;

                    case (int)Instruction.ESC:                        
                        codeWriter.Write(oper);
                        n = codeReader.ReadUInt16();
                        codeWriter.Write((ushort)n);
                        break;

                    default:
                        throw new Exception(string.Format("unknown instruction code: {0}", oper));
                }
            }

            if (this._debug)
            {
                Console.WriteLine("optimized code generated");
            }

            return code;
        }
        #endregion

        public Code Optimize(Code code)
        {
            code = this.OptimizeBranching(code);
            return code;
        }
    }

    public class Code
    {
        private MemoryStream _meta = new MemoryStream();
        private MemoryStream _code = new MemoryStream();
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private BinaryWriter _metaWriter;
        private BinaryReader _metaReader;

        public Code()
        {
            this._writer = new BinaryWriter(this._code);
            this._reader = new BinaryReader(this._code);
            this._metaWriter = new BinaryWriter(this._meta);
            this._metaReader = new BinaryReader(this._meta);
        }

        public Code(MemoryStream stream)
        {
            this._code = stream;
            this._writer = new BinaryWriter(this._code);
            this._reader = new BinaryReader(this._code);
            this._metaWriter = new BinaryWriter(this._meta);
            this._metaReader = new BinaryReader(this._meta);
        }

        public BinaryReader Reader
        {
            get
            {
                this._reader.BaseStream.Seek(0, SeekOrigin.Begin);
                return this._reader;
            }
        }

        public BinaryReader MetaReader
        {
            get
            {
                this._metaReader.BaseStream.Seek(0, SeekOrigin.Begin);
                return this._metaReader;
            }
        }

        public void Seek(int offset)
        {
            this._reader.BaseStream.Seek((long)offset, SeekOrigin.Begin);
        }

        public void Branch(int offset)
        {
            int curPos = this.Position;
            this._reader.BaseStream.Seek((long)offset, SeekOrigin.Current);
            //Console.WriteLine("Branch from {0} to {1}", curPos, this.Position);
        }

        public void BranchMap_L(int index)
        {
            int branchPoint = this.Position;
            this._reader.BaseStream.Seek((long)index * 4, SeekOrigin.Current);
            int offset = this._reader.ReadInt32();
            this._reader.BaseStream.Seek((long)branchPoint + offset, SeekOrigin.Begin);
        }
        
        public void BranchMap(int index)
        {
            int branchPoint = this.Position;
            this._reader.BaseStream.Seek((long)index * 2, SeekOrigin.Current);
            int offset = this._reader.ReadInt16();
            this._reader.BaseStream.Seek((long)branchPoint + offset, SeekOrigin.Begin);
        }

        public void BranchMap_S(int index)
        {
            int branchPoint = this.Position;
            this._reader.BaseStream.Seek((long)index, SeekOrigin.Current);
            int offset = this._reader.ReadSByte();
            this._reader.BaseStream.Seek((long)branchPoint + offset, SeekOrigin.Begin);
        }

        public void Jump(int position)
        {
            this._reader.BaseStream.Seek((long)position, SeekOrigin.Begin);
        }

        public int Position
        {
            get
            {
                return (int)this._reader.BaseStream.Position;
            }
        }

        public int SeekEnd()
        {
            this._reader.BaseStream.Seek(0, SeekOrigin.End);
            return (int)this._reader.BaseStream.Position;
        }

        public void AnchorLabel(int label)
        {
            int position = this.Position;
            this._writer.BaseStream.Seek((long)label, SeekOrigin.Begin);
            this._writer.Write(position - label - 4);
            this._writer.BaseStream.Seek((long)position, SeekOrigin.Begin);
        }

        public void AnchorCaseLabel(int mapOffset, int label)
        {
            int position = this.Position;
            this._writer.BaseStream.Seek((long)label, SeekOrigin.Begin);
            this._writer.Write(position - mapOffset);
            this._writer.BaseStream.Seek((long)position, SeekOrigin.Begin);
        }

        public void AnchorCatch(int label)
        {
            int position = this.Position;
            this._writer.BaseStream.Seek((long)label, SeekOrigin.Begin);
            this._writer.Write(position - label - 8);
            this._writer.BaseStream.Seek((long)position, SeekOrigin.Begin);
        }

        public void AnchorFinally(int label)
        {
            int position = this.Position;
            this._writer.BaseStream.Seek((long)label + 4, SeekOrigin.Begin);
            this._writer.Write(position - label - 8);
            this._writer.BaseStream.Seek((long)position, SeekOrigin.Begin);
        }

        public int CodeSize
        {
            get
            {
                return (int)this._code.Length;
            }
        }

        #region meta generator
        public void AddModule(string moduleName, int classCount, int fieldCount, int methodCount, int stringLiteralCount)
        {
            this._metaWriter.Write((byte)MetaTag.TAG_MODULE);
            byte[] p = Encoding.UTF8.GetBytes(moduleName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)classCount);
            this._metaWriter.Write((ushort)fieldCount);
            this._metaWriter.Write((ushort)methodCount);
            this._metaWriter.Write((ushort)stringLiteralCount);            
        }

        public void AddUseModule(string moduleName)
        {
            this._metaWriter.Write((byte)MetaTag.TAG_USEMODULE);
            byte[] p = Encoding.UTF8.GetBytes(moduleName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
        }

        public void AddClass(ClassInfo classInfo, int staticFieldCount, int instanceFieldCount, int methodCount)
        {
            byte[] p = null;
            this._metaWriter.Write((byte)(classInfo.IsFacade ? MetaTag.TAG_FACADECLASS : classInfo.IsImplicitClass ? MetaTag.TAG_IMPLICITCLASS : MetaTag.TAG_CLASS));
            p = Encoding.UTF8.GetBytes(classInfo.ClassName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            p = Encoding.UTF8.GetBytes(classInfo.ParentClassName != null ? classInfo.ParentClassName : "");
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)staticFieldCount);
            this._metaWriter.Write((ushort)instanceFieldCount);
            this._metaWriter.Write((ushort)methodCount);
            if (classInfo.IsFacade)
            {
                p = Encoding.UTF8.GetBytes(classInfo.TypeName);
                this._metaWriter.Write((byte)p.Length);
                this._metaWriter.Write(p);
                p = Encoding.UTF8.GetBytes(classInfo.AssemblyFile);
                this._metaWriter.Write((byte)p.Length);
                this._metaWriter.Write(p);
            }
            else if (classInfo.IsImplicitClass)
            {
                p = Encoding.UTF8.GetBytes(classInfo.ImplicitDataType);
                this._metaWriter.Write((byte)p.Length);
                this._metaWriter.Write(p);
            }
        }

        public void AddField(FieldInfo fieldInfo)
        {
            byte[] p = null;
            this._metaWriter.Write(fieldInfo.IsStatic ? (byte)MetaTag.TAG_STATICFIELD : (byte)MetaTag.TAG_FIELD);
            p = Encoding.UTF8.GetBytes(fieldInfo.FieldName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)fieldInfo.FieldNumber);
        }

        public void AddMethod(MethodInfo methodInfo)
        {
            byte[] p = null;
            this._metaWriter.Write(methodInfo.IsStatic ? (byte)MetaTag.TAG_STATICMETHOD : (byte)MetaTag.TAG_METHOD);
            p = Encoding.UTF8.GetBytes(methodInfo.MethodName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)methodInfo.MethodNumber);
            this._metaWriter.Write(methodInfo.CodeAddress);
            this._metaWriter.Write((byte)1); // always return value
            this._metaWriter.Write((ushort)methodInfo.Arguments);
            this._metaWriter.Write((ushort)methodInfo.Variables);
        }

        public void AddClassReference(ClassReferenceInfo info)
        {
            byte[] p = null;
            this._metaWriter.Write((byte)MetaTag.TAG_CLASSREF);
            p = Encoding.UTF8.GetBytes(info.ClassName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)info.ClassNumber);
        }

        public void AddFieldAccess(FieldAccessInfo info)
        {
            byte[] p = null;
            this._metaWriter.Write((byte)MetaTag.TAG_FIELDACCESS);
            p = Encoding.UTF8.GetBytes(info.FieldName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((short)info.FieldAccessNumber);
        }

        public void AddMethodCall(MethodCallInfo info)
        {
            byte[] p = null;
            this._metaWriter.Write((byte)MetaTag.TAG_METHODCALL);
            p = Encoding.UTF8.GetBytes(info.MethodSignature);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)info.MethodCallNumber);
        }

        public void AddLocalVariable(Variable variable)
        {
            byte[] p = null;
            this._metaWriter.Write((byte)MetaTag.TAG_LOCALVARIABLE);
            p = Encoding.UTF8.GetBytes(variable.VariableName);
            this._metaWriter.Write((byte)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)variable.VariableNumber);
        }

        public void AddStringLiteral(StringInfo literal)
        {
            byte[] p = null;

            this._metaWriter.Write(literal.Value.Length <= 255 ? (byte)MetaTag.TAG_STRINGLITERAL_S : (byte)MetaTag.TAG_STRINGLITERAL);
            this._metaWriter.Write((short)literal.StringNumber);
            p = Encoding.UTF8.GetBytes(literal.Value);
            if (literal.Value.Length <= 255)
            {
                this._metaWriter.Write((byte)p.Length);
            }
            else
            {
                this._metaWriter.Write((ushort)p.Length);
            }
            this._metaWriter.Write(p);            
        }

        public void AddEnumeration(EnumerationInfo info)
        {
            this._metaWriter.Write((byte)MetaTag.TAG_ENUM);
            byte[] p = Encoding.UTF8.GetBytes(info.Name);
            this._metaWriter.Write((ushort)p.Length);
            this._metaWriter.Write(p);
            this._metaWriter.Write((ushort)info.Fields.Count);
            foreach (string enumField in info.Fields.Keys)
            {
                p = Encoding.UTF8.GetBytes(enumField);
                this._metaWriter.Write((ushort)p.Length);
                this._metaWriter.Write(p);
                this._metaWriter.Write((ushort)info.Fields[enumField]);
            }
        }

        public void EndMeta()
        {
            //Console.WriteLine(".endmeta");
            this._metaWriter.Write((byte)MetaTag.TAG_END);
        }
        #endregion

        #region code generator

        public void Nop()
        {
            this._writer.Write((byte)Instruction.NOP);
        }

        public void Dup()
        {
            this._writer.Write((byte)Instruction.DUP);
        }

        public void LoadC(Constant operand)
        {
            if (operand.Value == null)
            {
                this._writer.Write((byte)Instruction.LDNULL);
            }            
            else if (operand.Value is byte)
            {
                if ((byte)operand.Value == 0)
                {
                    this._writer.Write((byte)Instruction.LDZERO);
                }
                else
                {
                    this._writer.Write((byte)Instruction.LDCBYTE);
                    this._writer.Write((byte)operand.Value);
                }
            }
            else if (operand.Value is char)
            {
                if ((char)operand.Value == 0)
                {
                    this._writer.Write((byte)Instruction.LDZERO);
                }
                else
                {
                    this._writer.Write((byte)Instruction.LDCCHAR);
                    this._writer.Write((char)operand.Value);
                }
            }
            else if (operand.Value is short)
            {
                if ((short)operand.Value == 0)
                {
                    this._writer.Write((byte)Instruction.LDZERO);
                }
                else
                {
                    this._writer.Write((byte)Instruction.LDCINT16);
                    this._writer.Write((short)operand.Value);
                }
            }
            else if (operand.Value is int)
            {
                if ((int)operand.Value == 0)
                {
                    this._writer.Write((byte)Instruction.LDZERO);
                }
                else
                {
                    this._writer.Write((byte)Instruction.LDCINT32);
                    this._writer.Write((int)operand.Value);
                }
            }
            else if (operand.Value is long)
            {
                if ((long)operand.Value == 0)
                {
                    this._writer.Write((byte)Instruction.LDZERO);
                }
                else
                {
                    this._writer.Write((byte)Instruction.LDCINT64);
                    this._writer.Write((long)operand.Value);
                }
            }
            else if (operand.Value is double)
            {
                if ((double)operand.Value == 0)
                {
                    this._writer.Write((byte)Instruction.LDZERO);
                }
                else
                {
                    this._writer.Write((byte)Instruction.LDCFLOAT64);
                    this._writer.Write((double)operand.Value);
                }
            }
            else if (operand.Value is bool)
            {
                this._writer.Write((byte)Instruction.LDCBOOL);
                this._writer.Write((byte)((bool)operand.Value ? 1 : 0));
            }
            else if (operand.Value is string)
            {
                throw new Exception("LoadC(): for string literal, use LoadS() instead");
            }
        }

        public void LoadS(int stringNumber)
        {
            if (stringNumber <= 255)
            {
                this._writer.Write((byte)Instruction.LDCSTRING_S);
                this._writer.Write((byte)stringNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.LDCSTRING);
                this._writer.Write((ushort)stringNumber);
            }
        }

        public void LoadV(int variableNumber)
        {
            //Console.WriteLine("loadv {0}", variableNumber);
            if (variableNumber <= 255)
            {
                this._writer.Write((byte)Instruction.LDVAR_S);
                this._writer.Write((byte)variableNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.LDVAR);
                this._writer.Write((ushort)variableNumber);
            }
        }

        public void LoadA()
        {
            //Console.WriteLine("loada");
            this._writer.Write((byte)Instruction.LDARRAY);
        }

        public void LoadF(int fieldNumber)
        {
            //Console.WriteLine("loadfield {0}", fieldNumber);
            if (fieldNumber <= 255)
            {
                this._writer.Write((byte)Instruction.LDFIELD_S);
                this._writer.Write((byte)fieldNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.LDFIELD);
                this._writer.Write((ushort)fieldNumber);
            }
        }

        public void LoadClass(int classNumber)
        {
            //Console.WriteLine("loadclass {0}", classNumber);
            if (classNumber <= 255)
            {
                this._writer.Write((byte)Instruction.LDCLASS_S);
                this._writer.Write((byte)classNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.LDCLASS);
                this._writer.Write((ushort)classNumber);
            }
        }

        public void StoreV(int variableNumber)
        {
            //Console.WriteLine("storev {0}", variableNumber);
            if (variableNumber <= 255)
            {
                this._writer.Write((byte)Instruction.STORE_S);
                this._writer.Write((byte)variableNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.STORE);
                this._writer.Write((ushort)variableNumber);
            }
        }

        public void StoreA()
        {
            //Console.WriteLine("storea");
            this._writer.Write((byte)Instruction.STOREA);
        }

        public void StoreF(int fieldNumber)
        {
            //Console.WriteLine("storefield {0}", fieldNumber);
            if (fieldNumber <= 255)
            {
                this._writer.Write((byte)Instruction.STOREF_S);
                this._writer.Write((byte)fieldNumber);
            }
            else            
            {
                this._writer.Write((byte)Instruction.STOREF);
                this._writer.Write((ushort)fieldNumber);
            }
        }

        public void Add()
        {
            //Console.WriteLine("add");
            this._writer.Write((byte)Instruction.ADD);
        }

        public void Sub()
        {
            //Console.WriteLine("sub");
            this._writer.Write((byte)Instruction.SUB);
        }

        public void Mul()
        {
            //Console.WriteLine("mul");
            this._writer.Write((byte)Instruction.MUL);
        }

        public void Div()
        {
            //Console.WriteLine("div");
            this._writer.Write((byte)Instruction.DIV);
        }

        public void Mod()
        {
            //Console.WriteLine("mod");
            this._writer.Write((byte)Instruction.MOD);
        }

        public void Neg()
        {
            //Console.WriteLine("neg");
            this._writer.Write((byte)Instruction.NEG);
        }

        public void And()
        {
            //Console.WriteLine("and");
            this._writer.Write((byte)Instruction.AND);
        }

        public void Or()
        {
            //Console.WriteLine("or");
            this._writer.Write((byte)Instruction.OR);
        }

        public void Xor()
        {
            //Console.WriteLine("xor");
            this._writer.Write((byte)Instruction.XOR);
        }

        public void Not()
        {
            //Console.WriteLine("not");
            this._writer.Write((byte)Instruction.NOT);
        }

        public void LShift()
        {
            this._writer.Write((byte)Instruction.LSHIFT);
        }

        public void RShift()
        {
            this._writer.Write((byte)Instruction.RSHIFT);
        }

        public void Inc(int variableNumber)
        {
            if (variableNumber <= 255)
            {
                this._writer.Write((byte)Instruction.INC_S);
                this._writer.Write((byte)variableNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.INC);
                this._writer.Write((ushort)variableNumber);
            }
        }

        public void Dec(int variableNumber)
        {
            if (variableNumber <= 255)
            {
                this._writer.Write((byte)Instruction.DEC_S);
                this._writer.Write((byte)variableNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.DEC);
                this._writer.Write((ushort)variableNumber);
            }
        }

        public int Br(int offset)
        {
            //Console.WriteLine("br {0}", offset);            
            this._writer.Write((byte)Instruction.BR_L);
            int label = this.Position;
            this._writer.Write(offset);            
            return label;
        }

        public int BTrue(int offset)
        {
            //Console.WriteLine("btrue {0}", offset);
            this._writer.Write((byte)Instruction.BTRUE_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }

        public int BFalse(int offset)
        {
            //Console.WriteLine("bfalse {0}", offset);
            this._writer.Write((byte)Instruction.BFALSE_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }

        public int Be(int offset)
        {
            this._writer.Write((byte)Instruction.BE_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }
        
        public int Bl(int offset)
        {
            this._writer.Write((byte)Instruction.BL_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }

        public int Ble(int offset)
        {
            this._writer.Write((byte)Instruction.BLE_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }
        
        public int Bg(int offset)
        {
            this._writer.Write((byte)Instruction.BG_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }

        public int Bge(int offset)
        {
            this._writer.Write((byte)Instruction.BGE_L);
            int label = this.Position;
            this._writer.Write(offset);
            return label;
        }

        public void Ce()
        {
            //Console.WriteLine("ce");
            this._writer.Write((byte)Instruction.CE);
        }

        public void Cne()
        {
            //Console.WriteLine("cne");
            this._writer.Write((byte)Instruction.CNE);
        }

        public void Cl()
        {
            //Console.WriteLine("cl");
            this._writer.Write((byte)Instruction.CL);
        }

        public void Cle()
        {
            //Console.WriteLine("cle");
            this._writer.Write((byte)Instruction.CLE);
        }

        public void Cg()
        {
            //Console.WriteLine("cg");
            this._writer.Write((byte)Instruction.CG);
        }

        public void Cge()
        {
            //Console.WriteLine("cge");
            this._writer.Write((byte)Instruction.CGE);
        }

        public void CastByte()
        {
            this._writer.Write((byte)Instruction.CASTBYTE);
        }

        public void CastChar()
        {
            this._writer.Write((byte)Instruction.CASTCHAR);
        }

        public void CastInt()
        {
            this._writer.Write((byte)Instruction.CASTINT);
        }

        public void CastLong()
        {
            this._writer.Write((byte)Instruction.CASTLONG);
        }

        public void CastFloat()
        {
            this._writer.Write((byte)Instruction.CASTFLOAT);
        }

        public void CastString()
        {
            this._writer.Write((byte)Instruction.CASTSTRING);
        }

        public void Pop()
        {
            //Console.WriteLine("pop");
            this._writer.Write((byte)Instruction.POP);
        }

        public void Call(int methodNumber)
        {
            //Console.WriteLine("call {0}", methodNumber);
            if (methodNumber <= 255)
            {
                this._writer.Write((byte)Instruction.CALL_S);
                this._writer.Write((byte)methodNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.CALL);
                this._writer.Write((ushort)methodNumber);
            }
        }

        public void CallC(int methodNumber)
        {
            //Console.WriteLine("callc {0}", methodNumber);
            if (methodNumber <= 255)
            {
                this._writer.Write((byte)Instruction.CALLC_S);
                this._writer.Write((byte)methodNumber);
            }
            else
            {
                this._writer.Write((byte)Instruction.CALLC);
                this._writer.Write((ushort)methodNumber);
            }
        }
        
        public void Ret()
        {
            //Console.WriteLine("ret");
            this._writer.Write((byte)Instruction.RET);
        }

        public void RetV()
        {
            //Console.WriteLine("retv");
            this._writer.Write((byte)Instruction.RETV);
        }

        public void RetC()
        {
            //Console.WriteLine("retc");
            this._writer.Write((byte)Instruction.RETC);
        }

        public void NewArray()
        {
            //Console.WriteLine("newarray");
            this._writer.Write((byte)Instruction.NEWARRAY);
        }

        public void NewInstance(int classNumber)
        {
            //Console.WriteLine("newinstance {0}", classNumber);
            this._writer.Write((byte)Instruction.NEWINSTANCE);
            this._writer.Write((ushort)classNumber);
        }

        public void Size()
        {
            //Console.WriteLine("size");
            this._writer.Write((byte)Instruction.SIZE);
        }

        public void Hash()
        {
            //Console.WriteLine("hash");
            this._writer.Write((byte)Instruction.HASH);
        }

        public void Time()
        {
            this._writer.Write((byte)Instruction.TIME);
        }

        public int EnterTry(int catchOffset, int finallyOffset)
        {
            this._writer.Write((byte)Instruction.ENTERTRY_L);
            int position = (int)this._code.Position;
            this._writer.Write(catchOffset);
            this._writer.Write(finallyOffset);
            return position;
        }

        public void LeaveTry()
        {
            this._writer.Write((byte)Instruction.LEAVETRY);
        }

        public void LeaveCatch()
        {
            this._writer.Write((byte)Instruction.LEAVECATCH);
        }

        public void LeaveFinally()
        {
            this._writer.Write((byte)Instruction.LEAVEFINALLY);
        }

        public void Throw()
        {
            this._writer.Write((byte)Instruction.THROW);
        }

        public void ReThrow()
        {
            this._writer.Write((byte)Instruction.RETHROW);
        }

        public void Esc(int literalNumber)
        {
            this._writer.Write((byte)Instruction.ESC);
            this._writer.Write((ushort)literalNumber);
        }

        public void BMap(int tableSize)
        {
            this._writer.Write((byte)Instruction.BMAP_L);
            this._writer.Write((ushort)tableSize);
        }

        public int MapEntry(int offset)
        {
            int position = (int)this._code.Position;
            this._writer.Write(offset);
            return position;
        }

        public void IsInstance()
        {
            this._writer.Write((byte)Instruction.ISINSTANCE);
        }

        public void Super()
        {
            this._writer.Write((byte)Instruction.SUPER);
        }

        public void LoadModule()
        {
            this._writer.Write((byte)Instruction.LOADMODULE);
        }

        public void ClassName()
        {
            this._writer.Write((byte)Instruction.CLASSNAME);
        }

        public void Hierarchy()
        {
            this._writer.Write((byte)Instruction.HIERARCHY);
        }

        public void StaticFields()
        {
            this._writer.Write((byte)Instruction.STATICFIELDS);
        }

        public void Fields()
        {
            this._writer.Write((byte)Instruction.FIELDS);
        }

        public void StaticMethods()
        {
            this._writer.Write((byte)Instruction.STATICMETHODS);
        }

        public void Methods()
        {
            this._writer.Write((byte)Instruction.METHODS);
        }

        public void CreateInstance()
        {
            this._writer.Write((byte)Instruction.CREATEINSTANCE);
        }

        public void GetField()
        {
            this._writer.Write((byte)Instruction.GETFIELD);
        }

        public void SetField()
        {
            this._writer.Write((byte)Instruction.SETFIELD);
        }

        public void InvokeMethod()
        {
            this._writer.Write((byte)Instruction.INVOKEMETHOD);
        }

        public void InvokeConstructor()
        {
            this._writer.Write((byte)Instruction.INVOKECONSTRUCTOR);
        }

        public void Version()
        {
            this._writer.Write((byte)Instruction.VERSION);
        }

        public void Fork()
        {
            this._writer.Write((byte)Instruction.FORK);
        }

        public void Join()
        {
            this._writer.Write((byte)Instruction.JOIN);
        }

        public void Sleep()
        {
            this._writer.Write((byte)Instruction.SLEEP);
        }

        public void Nice()
        {
            this._writer.Write((byte)Instruction.NICE);
        }

        public void Wait()
        {
            this._writer.Write((byte)Instruction.WAIT);
        }
        
        public void Signal()
        {
            this._writer.Write((byte)Instruction.SIGNAL);
        } 

        public void Exit()
        {
            this._writer.Write((byte)Instruction.EXIT);
        }

        public void Halt()
        {
            this._writer.Write((byte)Instruction.HALT);
        }

        public void CurThread()
        {
            this._writer.Write((byte)Instruction.CURTHREAD);
        }
        #endregion

        public void Load(string module)
        {
            using (FileStream stream = new FileStream(string.Format("{0}.m", module), FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(stream);
                if (reader.ReadByte() != 'O' || reader.ReadByte() != 'M')
                {
                    throw new Exception(string.Format("Invalid module file: {0}.m", module));
                }
                int metaSize = reader.ReadInt32();
                int codeSize = reader.ReadInt32();
                this._meta = new MemoryStream(reader.ReadBytes(metaSize));
                this._code = new MemoryStream(reader.ReadBytes(codeSize));
                this._reader = new BinaryReader(this._code);
                this._writer = new BinaryWriter(this._code);
                this._metaReader = new BinaryReader(this._meta);
                this._metaWriter = new BinaryWriter(this._meta);                
            }
        }

        public void Write(string module)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(module, FileMode.Create)))
            {
                writer.Write((byte)'O');
                writer.Write((byte)'M');
                writer.Write((int)this._meta.Length);
                writer.Write((int)this._code.Length);
                writer.Write(this._meta.ToArray());
                writer.Write(this._code.ToArray());
            }
        }

        private string EncodeString(string s)
        {
            return s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("\"", "\\\"");
        }

        public override string ToString()
        {
            Dictionary<int, string> classeReferences = new Dictionary<int, string>();
            Dictionary<int, string> methodCalls = new Dictionary<int, string>();
            Dictionary<int, string> fieldAccesses = new Dictionary<int, string>();
            Dictionary<int, string> classMethodMap = new Dictionary<int, string>();
            Dictionary<string, Dictionary<string, Dictionary<int, string>>> methodVariables = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();
            Dictionary<int, string> stringLiterals = new Dictionary<int, string>();
            StringBuilder s = new StringBuilder();

            #region meta data
            BinaryReader metaReader = this.MetaReader;
            string currentClassName = string.Empty;
            string currentMethodSignature = string.Empty;
            while (true)
            {
                int tag = (int)metaReader.ReadByte();
                switch (tag)
                {
                    case (int)MetaTag.TAG_MODULE:
                        string moduleName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int classCount = metaReader.ReadUInt16();
                        int fieldCount = metaReader.ReadUInt16();
                        int methodCount = metaReader.ReadUInt16();
                        int stringLiteralCount = metaReader.ReadUInt16();
                        s.Append(string.Format(".module {0} <classrefs: {1}, fieldaccesses: {2}, methodcalls: {3}, stringliterals: {4}>\n", moduleName, classCount, fieldCount, methodCount, stringLiteralCount));
                        continue;
                    case (int)MetaTag.TAG_USEMODULE:
                        moduleName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));                        
                        s.Append(string.Format("\t.usemodule {0}\n", moduleName));
                        continue;
                    case (int)MetaTag.TAG_CLASSREF:
                        string className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int classNumber = metaReader.ReadUInt16();
                        s.Append(string.Format("\t.classref <{0}> {1}\n", classNumber, className));
                        classeReferences.Add(classNumber, className);
                        continue;
                    case (int)MetaTag.TAG_FIELDACCESS:
                        string fieldName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int fieldNumber = metaReader.ReadUInt16();
                        s.Append(string.Format("\t.fieldaccess <{0}> {1}\n", fieldNumber, fieldName));
                        fieldAccesses.Add(fieldNumber, fieldName);
                        continue;
                    case (int)MetaTag.TAG_METHODCALL:
                        string methodSignature = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int methodNumber = metaReader.ReadUInt16();
                        s.Append(string.Format("\t.methodcall <{0}> {1}\n", methodNumber, methodSignature));
                        methodCalls.Add(methodNumber, methodSignature);                        
                        continue;
                    case (int)MetaTag.TAG_CLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        string parentClassName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int staticFieldCount = metaReader.ReadUInt16();
                        int instanceFieldCount = metaReader.ReadUInt16();
                        methodCount = metaReader.ReadUInt16();
                        currentClassName = className;
                        methodVariables.Add(currentClassName, new Dictionary<string, Dictionary<int, string>>());
                        s.Append(string.Format("\t.class {0} {4}<static fields: {1}, instance fields: {2}, methods: {3}>\n", className, staticFieldCount, instanceFieldCount, methodCount, parentClassName != "" ? "inherits " + parentClassName + " " : ""));
                        continue;
                    case (int)MetaTag.TAG_FACADECLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        parentClassName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        staticFieldCount = metaReader.ReadUInt16();
                        instanceFieldCount = metaReader.ReadUInt16();
                        methodCount = metaReader.ReadUInt16();
                        string typeName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        string assemblyFile = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        currentClassName = className;
                        methodVariables.Add(currentClassName, new Dictionary<string, Dictionary<int, string>>());
                        s.Append(string.Format("\t.facadeclass {0} {6}(\"{4}\", \"{5}\") <static fields: {1}, instance fields: {2} methods: {3}>\n", className, staticFieldCount, instanceFieldCount, methodCount, typeName, assemblyFile, parentClassName != "" ? "inherits " + parentClassName + " " : ""));
                        continue;
                    case (int)MetaTag.TAG_IMPLICITCLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        parentClassName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        staticFieldCount = metaReader.ReadUInt16();
                        instanceFieldCount = metaReader.ReadUInt16();
                        methodCount = metaReader.ReadUInt16();
                        typeName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        currentClassName = className;
                        methodVariables.Add(currentClassName, new Dictionary<string, Dictionary<int, string>>());
                        s.Append(string.Format("\t.implicitclass {0} for (\"{4}\") <static fields: {1}, instance fields: {2} methods: {3}>\n", className, staticFieldCount, instanceFieldCount, methodCount, typeName));
                        continue;
                    case (int)MetaTag.TAG_LOCALVARIABLE:
                        string variableName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int variableNumber = metaReader.ReadUInt16();
                        s.Append(string.Format("\t\t\t.localvar {0} <{1}>\n", variableName, variableNumber));
                        methodVariables[currentClassName][currentMethodSignature].Add(variableNumber, variableName);
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL:
                        int stringNumber = metaReader.ReadUInt16();
                        string literal = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                        s.Append(string.Format("\t.stringliteral <{0}> \"{1}\"\n", stringNumber, this.EncodeString(literal)));
                        stringLiterals.Add(stringNumber, literal);
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL_S:
                        stringNumber = metaReader.ReadUInt16();
                        literal = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        s.Append(string.Format("\t.stringliteral <{0}> \"{1}\"\n", stringNumber, this.EncodeString(literal)));
                        stringLiterals.Add(stringNumber, literal);
                        continue;
                    case (int)MetaTag.TAG_ENUM:
                        string enumType = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                        s.Append(string.Format("\t.enum {0}\n", enumType));
                        int n = metaReader.ReadUInt16();                        
                        for (int i = 0; i < n; i++)
                        {
                            string enumField = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                            int enumValue = metaReader.ReadUInt16();
                            s.Append(string.Format("\t\t.field {0} = {1}\n", enumField, enumValue));
                        }
                        continue;
                    case (int)MetaTag.TAG_STATICFIELD:
                        fieldName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        fieldNumber = metaReader.ReadUInt16();
                        s.Append(string.Format("\t\t.staticfield {0} <{1}>\n", fieldName, fieldNumber));
                        continue;
                    case (int)MetaTag.TAG_FIELD:
                        fieldName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        fieldNumber = metaReader.ReadUInt16();
                        s.Append(string.Format("\t\t.field {0} <{1}>\n", fieldName, fieldNumber));
                        continue;
                    case (int)MetaTag.TAG_STATICMETHOD:
                        methodSignature = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        methodNumber = metaReader.ReadUInt16();
                        int codeOffset = metaReader.ReadInt32();
                        bool returnType = metaReader.ReadByte() == 1 ? true : false;
                        int arguments = metaReader.ReadUInt16();
                        int variables = metaReader.ReadUInt16();
                        s.Append(string.Format("\t\t.staticmethod <{0}> {1} variables: {2} offset: {3}\n", methodNumber, methodSignature, variables, codeOffset));
                        if (codeOffset != -1 && !classMethodMap.ContainsKey(codeOffset))
                        {
                            classMethodMap.Add(codeOffset, string.Format("{0}.{1}", currentClassName, methodSignature));
                        }
                        methodVariables[currentClassName].Add(methodSignature, new Dictionary<int, string>());
                        currentMethodSignature = methodSignature;
                        continue;
                    case (int)MetaTag.TAG_METHOD:
                        methodSignature = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        methodNumber = metaReader.ReadUInt16();
                        codeOffset = metaReader.ReadInt32();
                        returnType = metaReader.ReadByte() == 1 ? true : false;
                        arguments = metaReader.ReadUInt16();
                        variables = metaReader.ReadUInt16();
                        s.Append(string.Format("\t\t.method <{0}> {1} variables: {2} offset: {3}\n", methodNumber, methodSignature, variables, codeOffset));
                        if (codeOffset != -1 && !classMethodMap.ContainsKey(codeOffset))
                        {
                            classMethodMap.Add(codeOffset, string.Format("{0}.{1}", currentClassName, methodSignature));
                        }
                        methodVariables[currentClassName].Add(methodSignature, new Dictionary<int, string>());
                        currentMethodSignature = methodSignature;
                        continue;
                    case (int)MetaTag.TAG_END:
                        s.Append(".endmeta\n"); 
                        break;
                    default:
                        throw new Exception(string.Format("Unknown meta tag: {0} ", tag));
                }
                break;
            }
            #endregion

            #region code

            BinaryReader reader = this.Reader;

            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int offset = (int)reader.BaseStream.Position;
                    int oper = (int)reader.ReadByte();
                    if (classMethodMap.ContainsKey(offset))
                    {
                        int index = classMethodMap[offset].LastIndexOf('.');
                        currentClassName = classMethodMap[offset].Substring(0, index);
                        currentMethodSignature = classMethodMap[offset].Substring(index + 1);
                        s.Append(string.Format("\n{0}:\n\n", classMethodMap[offset]));
                    }

                    s.Append(string.Format("\t[{0:0000}]: ", offset));

                    switch (oper)
                    {
                        case (int)Instruction.NOP:
                            s.Append(string.Format("nop"));
                            break;
                        case (int)Instruction.DUP:
                            s.Append(string.Format("dup"));
                            break;
                        case (int)Instruction.ADD:
                            s.Append(string.Format("add"));
                            break;
                        case (int)Instruction.SUB:
                            s.Append(string.Format("sub"));
                            break;
                        case (int)Instruction.MUL:
                            s.Append(string.Format("mul"));
                            break;
                        case (int)Instruction.DIV:
                            s.Append(string.Format("div"));
                            break;
                        case (int)Instruction.MOD:
                            s.Append(string.Format("mod"));
                            break;
                        case (int)Instruction.NEG:
                            s.Append(string.Format("neg"));
                            break;
                        case (int)Instruction.AND:
                            s.Append(string.Format("and"));
                            break;
                        case (int)Instruction.OR:
                            s.Append(string.Format("or"));
                            break;
                        case (int)Instruction.XOR:
                            s.Append(string.Format("xor"));
                            break;
                        case (int)Instruction.NOT:
                            s.Append(string.Format("not"));
                            break;
                        case (int)Instruction.LSHIFT:
                            s.Append(string.Format("lshift"));
                            break;
                        case (int)Instruction.RSHIFT:
                            s.Append(string.Format("rshift"));
                            break;
                        case (int)Instruction.INC:
                            int n = reader.ReadUInt16();
                            s.Append(string.Format("inc <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.INC_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("inc.s <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.DEC:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("dec <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.DEC_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("dec.s <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.BR_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("br.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BR:
                            n = reader.ReadInt16();
                            s.Append(string.Format("br [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BR_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("br.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BE_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("be.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BE:
                            n = reader.ReadInt32();
                            s.Append(string.Format("be [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BE_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("be.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BNE_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("bne.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BNE:
                            n = reader.ReadInt16();
                            s.Append(string.Format("bne [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BNE_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("bne.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BL_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("bl.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BL:
                            n = reader.ReadInt16();
                            s.Append(string.Format("bl [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BL_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("bl.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BLE_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("ble.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BLE:
                            n = reader.ReadInt16();
                            s.Append(string.Format("ble [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BLE_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("ble.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BG_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("bg.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BG:
                            n = reader.ReadInt16();
                            s.Append(string.Format("bg [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BG_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("bg.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BGE_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("bge.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BGE:
                            n = reader.ReadInt16();
                            s.Append(string.Format("bge [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BGE_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("bge.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BTRUE_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("btrue.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BTRUE:
                            n = reader.ReadInt16();
                            s.Append(string.Format("btrue [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BTRUE_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("btrue.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BFALSE_S:
                            n = reader.ReadSByte();
                            s.Append(string.Format("bfalse.s [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BFALSE:
                            n = reader.ReadInt16();
                            s.Append(string.Format("bfalse [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BFALSE_L:
                            n = reader.ReadInt32();
                            s.Append(string.Format("bfalse.l [{0:0000}]", n + reader.BaseStream.Position, n));
                            break;
                        case (int)Instruction.BMAP_L:
                            n = (int)reader.ReadUInt16();
                            s.Append(string.Format("bmap.l\n"));
                            int mapPosition = (int)reader.BaseStream.Position;
                            for (int i = 0; i < n; i++)
                            {
                                s.Append(string.Format("\t[{0:0000}]: ", (int)reader.BaseStream.Position));
                                int entry = reader.ReadInt32();
                                s.Append(string.Format("\t.mapentry: [{0:0000}]", mapPosition + entry));
                                if (i < n - 1)
                                {
                                    s.Append("\n");
                                }
                            }
                            break;
                        case (int)Instruction.BMAP:
                            n = (int)reader.ReadInt16();
                            s.Append(string.Format("bmap\n"));
                            mapPosition = (int)reader.BaseStream.Position;
                            for (int i = 0; i < n; i++)
                            {
                                s.Append(string.Format("\t[{0:0000}]: ", (int)reader.BaseStream.Position));
                                int entry = (int)reader.ReadUInt16();
                                s.Append(string.Format("\t.mapentry: [{0:0000}]", mapPosition + entry));
                                if (i < n - 1)
                                {
                                    s.Append("\n");
                                }
                            }
                            break;
                        case (int)Instruction.BMAP_S:
                            n = (int)reader.ReadUInt16();
                            s.Append(string.Format("bmap.s\n"));
                            mapPosition = (int)reader.BaseStream.Position;
                            for (int i = 0; i < n; i++)
                            {
                                s.Append(string.Format("\t[{0:0000}]: ", (int)reader.BaseStream.Position));
                                int entry = (int)reader.ReadSByte();
                                s.Append(string.Format("\t.mapentry: [{0:0000}]", mapPosition + entry));
                                if (i < n - 1)
                                {
                                    s.Append("\n");
                                }
                            }
                            break;
                        case (int)Instruction.CE:
                            s.Append(string.Format("ce"));
                            break;
                        case (int)Instruction.CNE:
                            s.Append(string.Format("cne"));
                            break;
                        case (int)Instruction.CL:
                            s.Append(string.Format("cl"));
                            break;
                        case (int)Instruction.CLE:
                            s.Append(string.Format("cle"));
                            break;
                        case (int)Instruction.CG:
                            s.Append(string.Format("cg"));
                            break;
                        case (int)Instruction.CGE:
                            s.Append(string.Format("cge"));
                            break;
                        case (int)Instruction.POP:
                            s.Append(string.Format("pop"));
                            break;
                        case (int)Instruction.CALL_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("call.s <{0}>", methodCalls[n]));
                            break;
                        case (int)Instruction.CALL:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("call <{0}>", methodCalls[n]));
                            break;
                        case (int)Instruction.CALLC_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("callc.s <{0}>", methodCalls[n]));
                            break;
                        case (int)Instruction.CALLC:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("callc <{0}>", methodCalls[n]));
                            break;
                        case (int)Instruction.RET:
                            s.Append(string.Format("ret"));
                            break;
                        case (int)Instruction.RETV:
                            s.Append(string.Format("retv"));
                            break;
                        case (int)Instruction.RETC:
                            s.Append(string.Format("retc"));
                            break;
                        case (int)Instruction.HALT:
                            s.Append(string.Format("halt"));
                            break;
                        case (int)Instruction.LDNULL:
                            s.Append(string.Format("loadnull"));
                            break;
                        case (int)Instruction.LDZERO:
                            s.Append(string.Format("loadzero"));
                            break;
                        case (int)Instruction.LDCBYTE:
                            n = (int)reader.ReadByte();
                            s.Append(string.Format("loadbyte {0}", n));
                            break;
                        case (int)Instruction.LDCCHAR:
                            n = (int)reader.ReadChar();
                            s.Append(string.Format("loadchar '{0}'", (char)n));
                            break;
                        case (int)Instruction.LDCINT16:
                            n = reader.ReadInt16();
                            s.Append(string.Format("loadshort {0}", n));
                            break;
                        case (int)Instruction.LDCINT32:
                            n = reader.ReadInt32();
                            s.Append(string.Format("loadint {0}", n));
                            break;
                        case (int)Instruction.LDCINT64:
                            long l = reader.ReadInt64();
                            s.Append(string.Format("loadlong {0}", l));
                            break;
                        case (int)Instruction.LDCFLOAT64:
                            double f = reader.ReadDouble();
                            s.Append(string.Format("loadfloat {0}", f));
                            break;
                        case (int)Instruction.LDCBOOL:
                            n = (int)reader.ReadByte();
                            s.Append(string.Format("loadbool {0}", n == 1 ? "true" : "false"));
                            break;
                        case (int)Instruction.LDCSTRING_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("loadstring.s \"{0}\"", this.EncodeString(stringLiterals[n])));
                            break;
                        case (int)Instruction.LDCSTRING:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("loadstring \"{0}\"", this.EncodeString(stringLiterals[n])));
                            break;
                        case (int)Instruction.LDVAR_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("loadvar.s <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.LDVAR:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("loadvar <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.LDARRAY:
                            s.Append(string.Format("loadarray"));
                            break;
                        case (int)Instruction.LDFIELD_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("loadfield.s <{0}>", fieldAccesses[n]));
                            break;
                        case (int)Instruction.LDFIELD:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("loadfield <{0}>", fieldAccesses[n]));
                            break;
                        case (int)Instruction.LDCLASS_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("loadclass.s <{0}>", classeReferences[n]));
                            break;
                        case (int)Instruction.LDCLASS:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("loadclass <{0}>", classeReferences[n]));
                            break;
                        case (int)Instruction.STORE_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("storevar.s <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.STORE:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("storevar <{0}>", methodVariables[currentClassName][currentMethodSignature][n]));
                            break;
                        case (int)Instruction.STOREA:
                            s.Append(string.Format("storearray"));
                            break;
                        case (int)Instruction.STOREF_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("storefield.s <{0}>", fieldAccesses[n]));
                            break;
                        case (int)Instruction.STOREF:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("storefield <{0}>", fieldAccesses[n]));
                            break;
                        case (int)Instruction.CASTBYTE:
                            s.Append(string.Format("castbyte"));
                            break;
                        case (int)Instruction.CASTCHAR:
                            s.Append(string.Format("castchar"));
                            break;
                        case (int)Instruction.CASTINT:
                            s.Append(string.Format("castint"));
                            break;
                        case (int)Instruction.CASTLONG:
                            s.Append(string.Format("castlong"));
                            break;
                        case (int)Instruction.CASTFLOAT:
                            s.Append(string.Format("castfloat"));
                            break;
                        case (int)Instruction.CASTSTRING:
                            s.Append(string.Format("caststring"));
                            break;
                        case (int)Instruction.NEWARRAY:
                            s.Append(string.Format("newarray"));
                            break;
                        case (int)Instruction.NEWINSTANCE_S:
                            n = reader.ReadByte();
                            s.Append(string.Format("newinstance.s <{0}>", classeReferences[n]));
                            break;
                        case (int)Instruction.NEWINSTANCE:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("newinstance <{0}>", classeReferences[n]));
                            break;
                        case (int)Instruction.SIZE:
                            s.Append(string.Format("size"));
                            break;
                        case (int)Instruction.HASH:
                            s.Append(string.Format("hash"));
                            break;
                        case (int)Instruction.TIME:
                            s.Append(string.Format("time"));
                            break;
                        case (int)Instruction.ENTERTRY_L:
                            n = reader.ReadInt32();
                            int m = reader.ReadInt32();
                            s.Append(string.Format("entertry.l .catch:[{0:0000}], .finally:[{1:0000}]", this.Position + n, this.Position + m));
                            break;
                        case (int)Instruction.ENTERTRY:
                            n = reader.ReadInt16();
                            m = reader.ReadInt16();
                            s.Append(string.Format("entertry .catch:[{0:0000}], .finally:[{1:0000}]", this.Position + n, this.Position + m));
                            break;
                        case (int)Instruction.ENTERTRY_S:
                            n = reader.ReadSByte();
                            m = reader.ReadSByte();
                            s.Append(string.Format("entertry.s .catch:[{0:0000}], .finally:[{1:0000}]", this.Position + n, this.Position + m));
                            break;
                        case (int)Instruction.LEAVETRY:
                            s.Append(string.Format("leavetry"));
                            break;
                        case (int)Instruction.LEAVECATCH:
                            s.Append(string.Format("leavecatch"));
                            break;
                        case (int)Instruction.LEAVEFINALLY:
                            s.Append(string.Format("leavefinally"));
                            break;
                        case (int)Instruction.THROW:
                            s.Append(string.Format("throw"));
                            break;
                        case (int)Instruction.RETHROW:
                            s.Append(string.Format("rethrow"));
                            break;
                        case (int)Instruction.ISINSTANCE:
                            s.Append(string.Format("isinstance"));
                            break;
                        case (int)Instruction.SUPER:
                            s.Append(string.Format("super"));
                            break;
                        case (int)Instruction.LOADMODULE:
                            s.Append(string.Format("loadmodule"));
                            break;
                        case (int)Instruction.CLASSNAME:
                            s.Append(string.Format("classname"));
                            break;
                        case (int)Instruction.HIERARCHY:
                            s.Append(string.Format("hierarchy"));
                            break;
                        case (int)Instruction.STATICFIELDS:
                            s.Append(string.Format("staticfields"));
                            break;
                        case (int)Instruction.FIELDS:
                            s.Append(string.Format("fields"));
                            break;
                        case (int)Instruction.STATICMETHODS:
                            s.Append(string.Format("staticmethods"));
                            break;
                        case (int)Instruction.METHODS:
                            s.Append(string.Format("methods"));
                            break;
                        case (int)Instruction.CREATEINSTANCE:
                            s.Append(string.Format("createinstance"));
                            break;
                        case (int)Instruction.GETFIELD:
                            s.Append(string.Format("getfield"));
                            break;
                        case (int)Instruction.SETFIELD:
                            s.Append(string.Format("setfield"));
                            break;
                        case (int)Instruction.INVOKEMETHOD:
                            s.Append(string.Format("invokemethod"));
                            break;
                        case (int)Instruction.INVOKECONSTRUCTOR:
                            s.Append(string.Format("invokeconstructor"));
                            break;
                        case (int)Instruction.VERSION:
                            s.Append(string.Format("version"));
                            break;
                        case (int)Instruction.FORK:
                            s.Append(string.Format("fork"));
                            break;
                        case (int)Instruction.JOIN:
                            s.Append(string.Format("join"));
                            break;
                        case (int)Instruction.SLEEP:
                            s.Append(string.Format("sleep"));
                            break;
                        case (int)Instruction.NICE:
                            s.Append(string.Format("nice"));
                            break;
                        case (int)Instruction.WAIT:
                            s.Append(string.Format("wait"));
                            break;
                        case (int)Instruction.SIGNAL:
                            s.Append(string.Format("signal"));
                            break;
                        case (int)Instruction.EXIT:
                            s.Append(string.Format("exit"));
                            break;
                        case (int)Instruction.CURTHREAD:
                            s.Append(string.Format("curthread"));
                            break;
                        case (int)Instruction.ESC:
                            n = reader.ReadUInt16();
                            s.Append(string.Format("// {0}", this.EncodeString(stringLiterals[n])));
                            break;
                        default:
                            throw new Exception(string.Format("unknown instruction code: {0}", oper));
                    }
                    s.Append("\n");
                }
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace + " " + s.ToString()); Console.ReadKey(); }
            #endregion

            return s.ToString();
        }
    }

    #region Enumerations

    public enum MetaTag
    {
        TAG_MODULE = 1,
        TAG_CLASS = 2,
        TAG_FACADECLASS = 3,
        TAG_IMPLICITCLASS = 4,
        TAG_FIELD = 5,
        TAG_METHOD = 6,
        TAG_STATICFIELD = 7,
        TAG_STATICMETHOD = 8,
        TAG_LOCALVARIABLE = 9,
        TAG_STRINGLITERAL = 10,
        TAG_STRINGLITERAL_S = 11,
        TAG_CLASSREF = 12,
        TAG_FIELDACCESS = 13,
        TAG_METHODCALL = 14,
        TAG_USEMODULE = 15,
        TAG_ENUM = 16,
        TAG_END = 17
    }

    public enum Instruction
    {
        NOP = 0,

        ADD = 1,
        SUB = 2,
        MUL = 3,
        DIV = 4, 
        MOD = 5,
        NEG = 6,
        AND = 7,
        OR = 8,
        XOR = 9,
        NOT = 10,
        LSHIFT = 11,
        RSHIFT = 12,
        INC = 13,        
        INC_S = 14,
        DEC = 15,
        DEC_S = 16,

        LDNULL = 21,
        LDZERO = 22,
        LDCBYTE = 23,
        LDCCHAR = 24,
        LDCINT16 = 25,
        LDCINT32 = 26,
        LDCINT64 = 27,
        LDCFLOAT64 = 28,
        LDCBOOL = 29,
        LDCSTRING = 30,
        LDCSTRING_S = 31,

        LDVAR = 32,
        LDARRAY = 33,
        LDFIELD = 34,
        LDCLASS = 35,
        LDVAR_S = 36,
        LDARRAY_S = 37,
        LDFIELD_S = 38,
        LDCLASS_S = 39,

        STORE = 41,
        STOREA = 42,
        STOREF = 43,
        STORE_S = 44,
        STOREA_S = 45,
        STOREF_S = 46,

        CE = 51,
        CNE = 52,
        CL = 53,
        CLE = 54, 
        CG = 55,
        CGE = 56,

        BR = 61,
        BE = 62,
        BNE = 63,
        BL = 64,
        BLE = 65,
        BG = 66,
        BGE = 67,
        BTRUE = 68,
        BFALSE = 69,
        BMAP = 70,

        BR_S = 71,
        BE_S = 72,
        BNE_S = 73,
        BL_S = 74,
        BLE_S = 75,
        BG_S = 76,
        BGE_S = 77,
        BTRUE_S = 78,
        BFALSE_S = 79,
        BMAP_S = 80,
        
        BR_L = 81,
        BE_L = 82,
        BNE_L = 83,
        BL_L = 84,
        BLE_L = 85,
        BG_L = 86,
        BGE_L = 87,
        BTRUE_L = 88,
        BFALSE_L = 89,
        BMAP_L = 90,
        
        CASTBYTE = 101,
        CASTCHAR = 102,
        CASTINT = 103,
        CASTLONG = 104,
        CASTFLOAT = 105,
        CASTSTRING = 106,

        CALL = 111,
        CALLC = 112,
        CALL_S = 113,
        CALLC_S = 114,

        RET = 115,
        RETV = 116,
        RETC = 117,        

        NEWARRAY = 121,
        NEWINSTANCE = 122,
        NEWINSTANCE_S = 123,
        ISINSTANCE = 124,
        SUPER = 125,
        SIZE = 126,
        HASH = 127,
        TIME = 128,
        DUP = 129,
        POP = 130,

        ENTERTRY = 131,
        ENTERTRY_S = 132,
        ENTERTRY_L = 133,
        LEAVETRY = 134,
        LEAVECATCH = 135,
        LEAVEFINALLY = 136,
        THROW = 137,
        RETHROW = 138,

        LOADMODULE= 141,
        CLASSNAME = 142,
        HIERARCHY = 143,
        STATICFIELDS = 144,
        FIELDS = 145,
        STATICMETHODS = 146,
        METHODS = 147,
        CREATEINSTANCE = 148,
        GETFIELD = 149,
        SETFIELD = 150,
        INVOKEMETHOD = 151,
        INVOKECONSTRUCTOR = 152,

        SIN = 161, // reserved
        COS = 162, // reserved
        TAN = 163, // reserved
        SQRT = 164, // reserved
        EXP = 165, // reserved

        NEWGUID = 171, // reserved

        FORK = 181,
        JOIN = 182,
        CURTHREAD = 183,
        THREADINFO = 184, // reserved
        SUSPEND = 185, // reserved
        RESUME = 186, // reserved
        WAIT = 187,
        SIGNAL = 188,
        KILL = 189, // reserved
        EXIT = 190,
        SLEEP = 191,
        NICE = 192,

        VERSION = 251,
        STATS = 252, // reserved
        ESC = 253,
        EXT = 254, // reserved
        HALT = 255
    }

    #endregion

    public class ClassInstance
    {
        public ClassDefinition Definition;
        public object[] Fields;
        public IFacadeClass FacadeInstance;
    }

    public class ClassDefinition
    {
        public string ClassName;
        public int ClassNumber;
        public string ParentClassName;
        public int Module;
        public int FieldCount;
        public Dictionary<string, int[]> FieldMap;
        public Dictionary<string, int[]> MethodMap;
        public bool IsFacade;
        public string TypeName;
        public string AssemblyFile;
        public Type FacadeClassType;
        public object[] Fields;
        public IFacadeClass FacadeInstance;
        public bool InheritanceExpanded;
        public bool IsInitialized;
    }

    public class ExceptionInfo
    {
        // try-catch information
        public int TryOffset;
        public int CatchOffset;
        public int FinallyOffset;
        public TryCatchPhase Phase;

        // save state
        public int Module;
        public int CallPointer;
        public int DataPointer;
        public int StackPointer;

        // return values
        public TryExitAction ExitAction;
        public object ExceptionObject;
        public int BranchTo;
    }

    public enum TryExitAction
    {
        None = 0,
        Ret = 1,
        RetV = 2,
        RetC = 3,
        Branch = 4,
        Throw = 5
    }

    public enum TryCatchPhase
    {
        InsideTry = 1,
        InsideCatch = 2,
        InsideFinally = 3
    }

    public enum DataType
    {
        BYTE = 1,
        CHAR = 2,
        SHORT = 3,
        INT = 4,
        LONG = 5,
        FLOAT = 6,
        STRING = 7,
        BOOL = 8
    }

    public enum ThreadState
    {
        RUNNING, SUSPENDING, WAITING, SLEEPING, JOINING, EXITING
    }

    public class ThreadContext
    {
        private static int nextid = 1;        
        public int threadid;
        public ThreadState state;
        public long sleepUntil;
        public int threadToJoin;
        public int threadToWait;
        public object semaphoreToWait;

        public object[] stack;
        public int[] callstack;
        public string[] callTrace;
        public ExceptionInfo[] exceptionStack;

        public int callstackp;
        public int calltracep;
        public int exceptionp;
        public int stackp;
        public int datap;
        public int module;
        public int codep;

        public ThreadContext()
        {
            this.threadid = ThreadContext.nextid++;
            this.state = ThreadState.RUNNING;
            this.sleepUntil = 0L;
            this.threadToJoin = 0;
            this.stack = new object[1024];
            this.callstack = new int[1024];
            this.callTrace = new string[1024];
            this.exceptionStack = new ExceptionInfo[1024];
            this.callstackp = 0;
            this.calltracep = 0;
            this.exceptionp = 0;
            this.stackp = 0;
            this.datap = 0;
            this.module = 0;
        }
    }

    public class RuntimeEngine
    {
        private bool _showStatistics = false;
        private bool _trace = false;
        private bool _forceRebuild = false;
        private bool _active = true;
        private bool _ticked = false;
        private long _ticks = 0L;

        private int Compare(object left, object right)
        {
            //Console.WriteLine("compare {0} with {1}", left, right);
            if (left == null || right == null)
            {
                return left == right ? 0 : -1;
            }
            if (left is IComparable)
            {
                try
                {
                    switch (this.ComputeTypes(left, right))
                    {
                        case ((int)DataType.BYTE << 3) | (int)DataType.BYTE:
                            return ((byte)left).CompareTo((byte)right);
                        case ((int)DataType.CHAR << 3) | (int)DataType.BYTE:
                            return ((char)left).CompareTo((char)(byte)right);
                        case ((int)DataType.INT << 3) | (int)DataType.BYTE:
                            return ((int)left).CompareTo((int)(byte)right);
                        case ((int)DataType.LONG << 3) | (int)DataType.BYTE:
                            return ((long)left).CompareTo((long)(byte)right);
                        case ((int)DataType.FLOAT << 3) | (int)DataType.BYTE:
                            return ((float)left).CompareTo((float)(byte)right);
                        case ((int)DataType.STRING << 3) | (int)DataType.BYTE:
                            return ((string)left).CompareTo(right.ToString());
                        case ((int)DataType.BYTE << 3) | (int)DataType.CHAR:
                            return ((char)(byte)left).CompareTo((char)right);
                        case ((int)DataType.CHAR << 3) | (int)DataType.CHAR:
                            return ((char)left).CompareTo((char)right);
                        case ((int)DataType.INT << 3) | (int)DataType.CHAR:
                            return ((int)left).CompareTo((int)(char)right);
                        case ((int)DataType.LONG << 3) | (int)DataType.CHAR:
                            return ((long)left).CompareTo((long)(char)right);
                        case ((int)DataType.FLOAT << 3) | (int)DataType.CHAR:
                            return ((double)left).CompareTo((double)(char)right);
                        case ((int)DataType.STRING << 3) | (int)DataType.CHAR:
                            return ((string)left).CompareTo(right.ToString());
                        case ((int)DataType.BYTE << 3) | (int)DataType.INT:
                            return ((int)(byte)left).CompareTo((int)right);
                        case ((int)DataType.CHAR << 3) | (int)DataType.INT:
                            return ((int)(char)left).CompareTo((int)right);
                        case ((int)DataType.INT << 3) | (int)DataType.INT:
                            return ((int)left).CompareTo((int)right);                            
                        case ((int)DataType.LONG << 3) | (int)DataType.INT:
                            return ((long)left).CompareTo((long)(int)right);
                        case ((int)DataType.FLOAT << 3) | (int)DataType.INT:
                            return ((double)left).CompareTo((double)(int)right);
                        case ((int)DataType.STRING << 3) | (int)DataType.INT:
                            return ((string)left).CompareTo(right.ToString());
                        case ((int)DataType.BYTE << 3) | (int)DataType.LONG:
                            return ((long)(int)left).CompareTo((long)right);
                        case ((int)DataType.CHAR << 3) | (int)DataType.LONG:
                            return ((long)(char)left).CompareTo((long)right);
                        case ((int)DataType.INT << 3) | (int)DataType.LONG:
                            return ((long)(int)left).CompareTo((long)right);
                        case ((int)DataType.LONG << 3) | (int)DataType.LONG:
                            return ((long)left).CompareTo((long)right);
                        case ((int)DataType.FLOAT << 3) | (int)DataType.LONG:
                            return ((double)left).CompareTo((double)(long)right);
                        case ((int)DataType.STRING << 3) | (int)DataType.LONG:
                            return ((string)left).CompareTo(right.ToString());
                        case ((int)DataType.BYTE << 3) | (int)DataType.FLOAT:
                            return ((double)(byte)left).CompareTo((double)right);
                        case ((int)DataType.CHAR << 3) | (int)DataType.FLOAT:
                            return ((double)(char)left).CompareTo((double)right);
                        case ((int)DataType.INT << 3) | (int)DataType.FLOAT:
                            return ((double)(int)left).CompareTo((double)right);
                        case ((int)DataType.LONG << 3) | (int)DataType.FLOAT:
                            return ((double)(long)left).CompareTo((float)right);
                        case ((int)DataType.FLOAT << 3) | (int)DataType.FLOAT:
                            return ((double)left).CompareTo((double)right);
                        case ((int)DataType.STRING << 3) | (int)DataType.FLOAT:
                            return ((string)left).CompareTo(right.ToString());
                        case ((int)DataType.BYTE << 3) | (int)DataType.STRING:
                            return (left.ToString()).CompareTo((string)right);
                        case ((int)DataType.CHAR << 3) | (int)DataType.STRING:
                            return (left.ToString()).CompareTo((string)right);
                        case ((int)DataType.INT << 3) | (int)DataType.STRING:
                            return (left.ToString()).CompareTo((string)right);
                        case ((int)DataType.LONG << 3) | (int)DataType.STRING:
                            return (left.ToString()).CompareTo((string)right);
                        case ((int)DataType.FLOAT << 3) | (int)DataType.STRING:
                            return (left.ToString()).CompareTo((string)right);
                        case ((int)DataType.STRING << 3) | (int)DataType.STRING:
                            return (left.ToString()).CompareTo((string)right);
                        default:
                            throw new Exception("compare - operand unsupported type");                                
                    }                    
                }
                catch
                {
                    throw new Exception(string.Format("Compare(): unsupported operand type: {0}:{1}", left != null ? left.GetType().Name : "null", right != null ? right.GetType().Name : "null"));
                }
            }
            else
            {
                throw new Exception(string.Format("Compare(): unsupported operand type: {0}:{1}", left != null ? left.GetType().Name : "null", right != null ? right.GetType().Name : "null"));
            }
        }

        private void Trace(string oper, object operand, int position, int stackp)
        {   
            if (operand != null)
            {
                Console.WriteLine("[{0:0000}:{1:0000}] {2} {3}", position, stackp, oper, operand is string ? string.Format("\"{0}\"", operand) : operand);
            }
            else
            {
                Console.WriteLine("[{0:0000}:{1:0000}] {2}", position, stackp, oper);
            }
        }

        private MemoryStream _code = new MemoryStream();

        public bool ShowStatistics
        {
            get { return this._showStatistics; }
            set { this._showStatistics = value; }
        }

        // global catalog
        
        private List<ClassDefinition[]> _moduleClasses = new List<ClassDefinition[]>();
        private List<string[]> _moduleClassNames = new List<string[]>();
        private List<string[]> _moduleFields = new List<string[]>();
        private List<string[]> _moduleMethodSignatures = new List<string[]>();
        private List<int[]> _moduleMethodArguments = new List<int[]>();
        private List<string[]> _moduleStringLiterals = new List<string[]>();
        private List<string> _modules = new List<string>();
        private List<string> _modulesLoaded = new List<string>();

        // class catalog

        private Dictionary<string, ClassDefinition> _classes = new Dictionary<string, ClassDefinition>();
        private Dictionary<string, ClassDefinition> _implicitClasses = new Dictionary<string, ClassDefinition>();
        private List<string> _classNames = new List<string>();

        private int ComputeArguments(string methodSignature)
        {
            int start = methodSignature.IndexOf("(");
            int end = methodSignature.IndexOf(")");
            return int.Parse(methodSignature.Substring(start + 1, end - start - 1));
        }

        public void LoadModule(string module)
        {
            if (File.Exists(module + ".s") && (this._forceRebuild || !File.Exists(module + ".m")))
            {
                DateTime start = DateTime.Now;
                Code code = new Compiler().Compile(File.ReadAllText(module + ".s"));
                code = new Optimizer().Optimize(code);
                this.LoadModule(code);
                if (this._showStatistics)
                {
                    Console.WriteLine("Time taken to compile and load module '{0}' from memory: {1}ms", module, (DateTime.Now - start).Milliseconds);
                }
            }
            else if (File.Exists(module + ".m"))
            {
                if (File.Exists(module + ".s") && File.GetLastWriteTime(module + ".s") > File.GetLastWriteTime(module + ".m"))
                {
                    DateTime start = DateTime.Now;
                    Code code = new Compiler().Compile(File.ReadAllText(module + ".s"));
                    code = new Optimizer().Optimize(code);
                    this.LoadModule(code);
                    if (this._showStatistics)
                    {
                        Console.WriteLine("Time taken to compile and load module '{0}' from memory: {1}ms", module, (DateTime.Now - start).Milliseconds);
                    }
                }
                else
                {
                    DateTime start = DateTime.Now;
                    Code code = new Code();
                    code.Load(module);
                    this.LoadModule(code);
                    if (this._showStatistics)
                    {
                        Console.WriteLine("Time taken to load module '{0}' from file: {1}ms", module, (DateTime.Now - start).Milliseconds);
                    }
                }
            }            
            else
            {
                throw new Exception(string.Format("Unable to load module '{0}': not found", module));
            }
        }

        public void LoadModule(Code code)
         { 
            // load meta

            int currentModule = -1;
            string currentModuleName = null;
            string currentClass = null;
            
            BinaryReader metaReader = code.MetaReader;
            string moduleName = null;

            while (true)
            {
                int tag = (int)metaReader.ReadByte();
                switch (tag)
                {
                    case (int)MetaTag.TAG_MODULE:
                        moduleName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int classCount = metaReader.ReadUInt16();
                        int fieldCount = metaReader.ReadUInt16();
                        int methodCount = metaReader.ReadUInt16();
                        int stringLiteralCount = metaReader.ReadUInt16();
                        if (this._modules.Contains(moduleName))
                        {
                            return;
                        }
                        this._moduleClasses.Add(new ClassDefinition[classCount]);
                        this._moduleClassNames.Add(new string[classCount]);
                        this._moduleFields.Add(new string[fieldCount]);
                        this._moduleMethodSignatures.Add(new string[methodCount]);
                        this._moduleMethodArguments.Add(new int[methodCount]);
                        this._moduleStringLiterals.Add(new string[stringLiteralCount]);
                        this._modules.Add(moduleName);
                        currentModuleName = moduleName;
                        currentModule = this._modules.Count;
                        if (this._showStatistics)
                        {
                            Console.WriteLine("module: {0} <{1}>", moduleName, currentModule);
                        }
                        continue;
                    case (int)MetaTag.TAG_USEMODULE:
                        moduleName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        if (this._showStatistics)
                        {
                            Console.WriteLine("loading dependent module: {0}", moduleName);
                        }
                        this.LoadModule(moduleName);
                        continue;
                    case (int)MetaTag.TAG_CLASSREF:
                        string className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int classNumber = metaReader.ReadUInt16();
                        this._moduleClassNames[currentModule - 1][classNumber - 1] = className;
                        continue;
                    case (int)MetaTag.TAG_FIELDACCESS:
                        string fieldName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int fieldNumber = metaReader.ReadUInt16();
                        this._moduleFields[currentModule - 1][fieldNumber - 1] = fieldName;
                        continue;
                    case (int)MetaTag.TAG_METHODCALL:
                        string methodSignature = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int methodNumber = metaReader.ReadUInt16();
                        this._moduleMethodSignatures[currentModule - 1][methodNumber - 1] = methodSignature;
                        this._moduleMethodArguments[currentModule - 1][methodNumber - 1] = this.ComputeArguments(methodSignature);
                        continue;
                    case (int)MetaTag.TAG_CLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        string parentClassName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        int staticFieldCount = metaReader.ReadUInt16();
                        int instanceFieldCount = metaReader.ReadUInt16();
                        methodCount = metaReader.ReadUInt16();
                        if (this._classes.ContainsKey(className))
                        {
                            throw new Exception(string.Format("Class '{0}' already defined.", className));
                        }
                        ClassDefinition definition = new ClassDefinition();
                        definition.ClassName = className;
                        definition.ClassNumber = this._classNames.Count + 1;
                        definition.ParentClassName = parentClassName;
                        definition.IsFacade = false;
                        definition.Module = currentModule;
                        definition.FieldCount = instanceFieldCount;
                        definition.FieldMap = new Dictionary<string, int[]>();
                        definition.MethodMap = new Dictionary<string, int[]>();
                        definition.TypeName = null;
                        definition.AssemblyFile = null;
                        definition.FacadeClassType = null;
                        definition.FacadeInstance = null;
                        definition.Fields = new object[staticFieldCount];
                        definition.InheritanceExpanded = false;
                        definition.IsInitialized = false;
                        this._classes.Add(className, definition);
                        this._classNames.Add(className);
                        currentClass = className;
                        if (this._showStatistics)
                        {
                            Console.WriteLine("adding class definition '{0}' of module <{1}>", className, currentModule);
                        }
                        continue;
                    case (int)MetaTag.TAG_FACADECLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        parentClassName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        instanceFieldCount = metaReader.ReadUInt16();
                        staticFieldCount = metaReader.ReadUInt16();
                        methodCount = metaReader.ReadUInt16();
                        string typeName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        string assemblyFile = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));                        
                        if (this._classes.ContainsKey(className))
                        {
                            throw new Exception(string.Format("Class '{0}' already defined.", className));
                        }
                        definition = new ClassDefinition();
                        definition.ClassName = className;
                        definition.ClassNumber = this._classNames.Count + 1;
                        definition.ParentClassName = parentClassName;
                        definition.IsFacade = true;
                        definition.Module = currentModule;
                        definition.FieldCount = instanceFieldCount;
                        definition.FieldMap = new Dictionary<string, int[]>();
                        definition.MethodMap = new Dictionary<string, int[]>();
                        definition.TypeName = typeName;
                        definition.AssemblyFile = assemblyFile;

                        DateTime start = DateTime.Now;
                        definition.FacadeClassType = Assembly.LoadFrom(assemblyFile).GetType(typeName);
                        if (this._showStatistics)
                        {
                            Console.WriteLine("Time taken to load dependent assembly '{0}': {1}ms", assemblyFile, (DateTime.Now - start).Milliseconds);                                
                        }

                        definition.FacadeInstance = (IFacadeClass)Activator.CreateInstance(definition.FacadeClassType);
                        definition.Fields = new object[staticFieldCount];
                        definition.InheritanceExpanded = false;
                        definition.IsInitialized = false;
                        this._classes.Add(className, definition);
                        this._classNames.Add(className);
                        currentClass = className;
                        if (this._showStatistics)
                        {
                            Console.WriteLine("adding facade class definition '{0}' of module <{1}>", className, currentModule);
                        }
                        continue;
                    case (int)MetaTag.TAG_IMPLICITCLASS:
                        className = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        parentClassName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        staticFieldCount = metaReader.ReadUInt16();
                        instanceFieldCount = metaReader.ReadUInt16();
                        methodCount = metaReader.ReadUInt16();
                        typeName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        if (this._classes.ContainsKey(className) || this._implicitClasses.ContainsKey(className))
                        {
                            throw new Exception(string.Format("Class '{0}' already defined.", className));
                        }
                        definition = new ClassDefinition();
                        definition.ClassName = className;
                        definition.ClassNumber = this._classNames.Count + 1;
                        definition.ParentClassName = parentClassName;
                        definition.IsFacade = false;
                        definition.Module = currentModule;
                        definition.FieldCount = instanceFieldCount;
                        definition.FieldMap = new Dictionary<string, int[]>();
                        definition.MethodMap = new Dictionary<string, int[]>();
                        definition.TypeName = null;
                        definition.AssemblyFile = null;
                        definition.FacadeClassType = null;
                        definition.FacadeInstance = null;
                        definition.Fields = new object[staticFieldCount];
                        definition.InheritanceExpanded = false;
                        definition.IsInitialized = false;
                        this._classes.Add(className, definition);
                        this._implicitClasses.Add(typeName, definition);
                        this._classNames.Add(className);
                        currentClass = className;
                        if (this._showStatistics)
                        {
                            Console.WriteLine("adding class definition '{0}' of module <{1}>", className, currentModule);
                        }
                        continue;
                    case (int)MetaTag.TAG_LOCALVARIABLE:
                        Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        metaReader.ReadUInt16();
                        continue;
                    case (int)MetaTag.TAG_STATICFIELD:
                    case (int)MetaTag.TAG_FIELD:
                        fieldName = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        fieldNumber = metaReader.ReadUInt16();
                        this._classes[currentClass].FieldMap.Add(fieldName, new int [] { fieldNumber, tag == (int)MetaTag.TAG_STATICFIELD ? 1 : 0 });
                        continue;
                    case (int)MetaTag.TAG_STATICMETHOD:
                    case (int)MetaTag.TAG_METHOD:
                        methodSignature = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        methodNumber = metaReader.ReadUInt16();
                        int codeOffset = metaReader.ReadInt32();                        
                        string returnType = (int)metaReader.ReadByte() == 1 ? "var" : "void";
                        int arguments = metaReader.ReadUInt16();
                        int variables = metaReader.ReadUInt16();
                        this._code.Seek(0, SeekOrigin.End);
                        int codePosition = (int)this._code.Position;
                        this._classes[currentClass].MethodMap.Add(methodSignature, 
                            new int[] { 
                                currentModule, // module
                                arguments, // arguments
                                variables, // variables
                                codePosition + codeOffset, // offset
                                tag == (int)MetaTag.TAG_STATICMETHOD ? 1 : 0 // static method?
                            } );
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL:
                        int stringNumber = metaReader.ReadUInt16();
                        string literal = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                        this._moduleStringLiterals[currentModule - 1][stringNumber - 1] = literal;
                        continue;
                    case (int)MetaTag.TAG_STRINGLITERAL_S:
                        stringNumber = metaReader.ReadUInt16();
                         literal = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadByte()));
                        this._moduleStringLiterals[currentModule - 1][stringNumber - 1] = literal;
                        continue;
                    case (int)MetaTag.TAG_ENUM:
                        string enumType = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                        int n = metaReader.ReadUInt16();                        
                        for (int i = 0; i < n; i++)
                        {
                            string enumField = Encoding.UTF8.GetString(metaReader.ReadBytes(metaReader.ReadUInt16()));
                            int enumValue = metaReader.ReadUInt16();
                        }
                        continue;
                    case (int)MetaTag.TAG_END:
                        break;
                    default:
                        throw new Exception(string.Format("Unknown meta tag: {0}", tag));
                }
                break;
            }

            // resolve classes            
            for (int i = 0 ; i < this._moduleClassNames[currentModule - 1].Length ; i++)
            {
                string className = this._moduleClassNames[currentModule - 1][i];
                if (!this._classes.ContainsKey(className))
                {
                    throw new Exception(string.Format("Unresolved class '{0}' in '{1}'", className, currentModuleName));
                }
                this._moduleClasses[currentModule - 1][i] = this._classes[className];
            }

            // load code
            this._code.Seek(0, SeekOrigin.End);

            //code.Reader.BaseStream.CopyTo(this._code);
            RuntimeEngine.CopyStream(code.Reader.BaseStream, this._code);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        public void ExpandAllClassInheritance()
        {
            foreach (ClassDefinition definition in this._classes.Values)
            {
                this.ExpandClassInheritance(definition);
            }
        }

        public void ExpandClassInheritance(ClassDefinition definition)
        {
            if (!definition.InheritanceExpanded && definition.ParentClassName != "")
            {
                if (this._trace) Console.WriteLine("Expanding inheritance information for class: {0}", definition.ClassName);

                ClassDefinition parentDefinition = this._classes[definition.ParentClassName];
                this.ExpandClassInheritance(parentDefinition);

                definition.Fields = new object[definition.Fields.Length + parentDefinition.Fields.Length]; // expand static fields
                definition.FieldCount += parentDefinition.FieldCount; // expand instance field count

                foreach (string fieldName in definition.FieldMap.Keys)
                {
                    if (definition.FieldMap[fieldName][1] == 1) // if static field
                    {
                        definition.FieldMap[fieldName][0] += parentDefinition.Fields.Length; // map my static field number behind my parents static field number
                    }
                    else
                    {
                        definition.FieldMap[fieldName][0] += parentDefinition.FieldCount; // map my instance field number behind my parents instance field number
                    }
                }

                foreach (string fieldName in parentDefinition.FieldMap.Keys)
                {
                    if (!definition.FieldMap.ContainsKey(fieldName)) // my overrides my parent field
                    {
                        definition.FieldMap.Add(fieldName, parentDefinition.FieldMap[fieldName]); // merge parent fields with my
                    }
                }

                foreach (string methodSignature in parentDefinition.MethodMap.Keys)
                {
                    if (!definition.MethodMap.ContainsKey(methodSignature) && !methodSignature.StartsWith("__")) // my overrides my parent method and excludes contructors
                    {
                        definition.MethodMap.Add(methodSignature, parentDefinition.MethodMap[methodSignature]); // merge parent method with my
                    }
                }                

                definition.InheritanceExpanded = true;
            }
        }

        private string BuildStackTrace(string[] calltrace, int calltracep)
        {
            StringBuilder s = new StringBuilder();
            while (calltracep > 0)
            {
                if (s.Length > 0)
                {
                    s.Append("in ");
                }
                s.Append(calltrace[calltracep - 1]); 
                s.Append("\n");
                calltracep --;
            }
            return s.ToString();
        }

        private string[] BuildInheritanceHierarchy(ClassDefinition definition)
        {
            List<string> hierarchy = new List<string>();
            hierarchy.Add(definition.ClassName);
            while (!string.IsNullOrEmpty(definition.ParentClassName))
            {
                definition = this._classes[definition.ParentClassName];
                hierarchy.Add(definition.ClassName);
            }
            return hierarchy.ToArray();
        }

        private int ComputeTypes(object left, object right)
        {
            int types = 0;
            if (left is byte)
            {
                types = (int)DataType.BYTE << 3;
            }
            else if (left is char)
            {
                types = (int)DataType.CHAR << 3;
            }
            else if (left is short)
            {
                types = (int)DataType.SHORT << 3;
            }
            else if (left is int)
            {
                types = (int)DataType.INT << 3;
            }
            else if (left is long)
            {
                types = (int)DataType.LONG << 3;
            }
            else if (left is double)
            {
                types = (int)DataType.FLOAT << 3;
            }
            else if (left is string)
            {
                types = (int)DataType.STRING << 3;
            }
            if (right is byte)
            {
                types |= (int)DataType.BYTE;
            }
            else if (right is char)
            {
                types |= (int)DataType.CHAR;
            }
            else if (right is short)
            {
                types |= (int)DataType.SHORT;
            }
            else if (right is int)
            {
                types |= (int)DataType.INT;
            }
            else if (right is long)
            {
                types |= (int)DataType.LONG;
            }
            else if (right is double)
            {
                types |= (int)DataType.FLOAT;
            }
            else if (right is string)
            {
                types |= (int)DataType.STRING;
            }
            return types;
        }

        private bool IsInstanceOf(ClassDefinition definition, ClassDefinition parentDefinition)
        {
            while (true)
            {
                if (definition == parentDefinition)
                {
                    return true;
                }
                if (definition.ParentClassName != null)
                {
                    definition = this._classes[definition.ParentClassName];
                }
                else
                {
                    return false;
                }
            }
        }

        private ClassInstance GetParentInstance(ClassInstance instance)
        {
            if (instance.Definition.IsFacade)
            {
                throw new Exception("Illegal operation: cannot operate on facade instance.");
            }
            string parentClassName = instance.Definition.ParentClassName;
            if (parentClassName == null)
            {
                throw new Exception("Illegal operation: instance does not have a parent instance.");
            }
            ClassDefinition definition = this._classes[parentClassName];
            ClassInstance parent = new ClassInstance();
            parent.Definition = definition;
            parent.Fields = instance.Fields;
            parent.FacadeInstance = null;
            return parent;
        }

        private ClassDefinition GetParentDefinition(ClassDefinition definition)
        {
            if (definition.IsFacade)
            {
                throw new Exception("Illegal operation: cannot operate on facade definition.");
            }
            string parentClassName = definition.ParentClassName;
            if (parentClassName == null)
            {
                throw new Exception("Illegal operation: definition does not have a parent definition.");
            }
            return this._classes[parentClassName];
        }

        private string[] GetFields(Dictionary<string, int[]> fieldMap, bool staticFields)
        {
            List<string> fields = new List<string>();
            foreach (string field in fieldMap.Keys)
            {
                if (staticFields)
                {
                    if (fieldMap[field][1] == 1)
                    {
                        fields.Add(field);
                    }
                }
                else
                {
                    if (fieldMap[field][1] == 0)
                    {
                        fields.Add(field);
                    }
                }
            }
            return fields.ToArray();
        }

        private string[] GetMethods(Dictionary<string, int[]> methodMap, bool staticMethods)
        {
            List<string> methods = new List<string>();
            foreach (string methodSignature in methodMap.Keys)
            {
                if (staticMethods)
                {
                    if (methodMap[methodSignature][4] == 1)
                    {
                        methods.Add(methodSignature);
                    }
                }
                else
                {
                    if (methodMap[methodSignature][4] == 0)
                    {
                        methods.Add(methodSignature);
                    }
                }
            }
            return methods.ToArray();
        }

        private int ParseMethodArguments(string methodSignature)
        {
            return int.Parse(methodSignature.Substring(methodSignature.IndexOf("(") + 1, methodSignature.IndexOf(")") - methodSignature.IndexOf("(") - 1));
        }

        public void Execute(string module, string classToRun, object[] parameters)
        {
            DateTime start = DateTime.Now;
            this.LoadModule(module);
            if (this._showStatistics)
            {
                Console.WriteLine("Time taken to load all modules: {0}ms", (DateTime.Now - start).Milliseconds);
            }
            this.Execute(classToRun, parameters);
        }

        public void Execute(Code moduleCode, string classToRun, object[] parameters)
        {
            this.LoadModule(moduleCode);
            this.Execute(classToRun, parameters);
        }

        protected void TimerTick()
        {
            while (this._active)
            {
                Thread.Sleep(1); // 1ms
                this._ticked = true;
                this._ticks = DateTime.Now.Ticks;
            }
        }

        protected void Branch(int offset, Code code, int module, int exceptionp, ExceptionInfo[] exceptionStack)
        {
            if (exceptionp > 0)
            {
                ExceptionInfo info = exceptionStack[exceptionp - 1];
                int branchTo = code.Position + offset;
                if (branchTo < info.TryOffset || branchTo > info.CatchOffset)
                {
                    code.Branch(info.FinallyOffset);
                    return;
                }
            }

            code.Branch(offset);
        }

        public void Execute(string classToRun, object[] parameters)
        {
            DateTime start = DateTime.Now;
            this.ExpandAllClassInheritance();
            if (this._showStatistics)
            {
                Console.WriteLine("Time taken to expand inheritance: {0}ms", (DateTime.Now - start).Milliseconds);
                Console.WriteLine("===========================================================================\n");
            }

            Code code = new Code(this._code);
            BinaryReader reader = new BinaryReader(this._code);

            int n = 0;
            int m = 0;
            object exceptionObject;
            string s = string.Empty;
            string q = string.Empty;
            ExceptionInfo info = null;
            double f = 0.0;
            object lOperand;
            object rOperand;
            int curPos = 0;

            Thread timerTicker = new Thread(new ThreadStart(this.TimerTick));
            List<object> semaphores = new List<object>();
            List<ThreadContext> threads = new List<ThreadContext>();
            threads.Add(new ThreadContext());
            int currentThreadIndex = 0;
            ThreadContext currentThread = threads[currentThreadIndex];

            DateTime startTime = DateTime.Now;
            long instructionsExecuted = 0;
            long contextSwitches = 0;

            if (!this._classes.ContainsKey(classToRun))
            {
                throw new Exception(string.Format("Class '{0}' undefined", classToRun));
            }

            currentThread.module = this._classes[classToRun].Module;

            int classNumber = -1;
            for (int i = 0; i < this._moduleClassNames[currentThread.module - 1].Length; i++)
            {
                if (this._moduleClassNames[currentThread.module - 1][i] == classToRun)
                {
                    classNumber = i + 1;
                    break;
                }
            }

            if (classNumber == -1)
            {
                throw new Exception(string.Format("Internal error: class '{0}' undefined", classToRun));
            }

            int methodNumber = -1;
            for (int i = 0; i < this._moduleMethodSignatures[currentThread.module - 1].Length; i++)
            {
                if (this._moduleMethodSignatures[currentThread.module - 1][i] == "main(1):1")
                {
                    methodNumber = i + 1;
                    break;
                }
            }

            if (methodNumber == -1)
            {
                throw new Exception(string.Format("Method 'main(args)' undefined in class '{0}'", classToRun));
            }
            
            // generate startup code

            currentThread.codep = code.SeekEnd();

            int fieldIndex = currentThread.stackp;
            currentThread.stack[currentThread.stackp++] = null; // allocate field

            currentThread.stack[currentThread.stackp++] = this._moduleClasses[currentThread.module - 1][classNumber - 1];
            currentThread.stack[currentThread.stackp++] = parameters;                     

            int offset = code.EnterTry(0, 0);
            code.Call(methodNumber);
            // result will be in stack[0]
            code.LeaveTry();
            code.AnchorCatch(offset);
            // exception on stack
            code.StoreV(1);
            code.LeaveCatch();
            code.AnchorFinally(offset);
            code.LeaveFinally();
            code.Halt();

            // begin execution

            int threadid = currentThread.threadid;
            object[] stack = currentThread.stack;
            int[] callstack = currentThread.callstack;
            ExceptionInfo[] exceptionStack = currentThread.exceptionStack;
            string[] callTrace = currentThread.callTrace;
            int stackp = currentThread.stackp;
            int exceptionp = currentThread.exceptionp;
            int callstackp = currentThread.callstackp;
            int calltracep = currentThread.calltracep;
            int datap = currentThread.datap;
            int module = currentThread.module;
            code.Seek(currentThread.codep);

            this._active = true;
            timerTicker.Start();
            try
            {
                #region Execution loop
                while (true)
                {
                    if (this._ticked)
                    {
                        while (true)
                        {
                            if (currentThread.state == ThreadState.EXITING)
                            {
                                if (threads.Count == 1)
                                {
                                    return;
                                }

                                #region remove current thread
                                threads.RemoveAt(currentThreadIndex);
                                #endregion

                                #region context switch
                                currentThreadIndex = currentThreadIndex % threads.Count;
                                currentThread = threads[currentThreadIndex];
                                contextSwitches++;
                                #endregion

                                #region load next thread info
                                threadid = currentThread.threadid;
                                stack = currentThread.stack;
                                callstack = currentThread.callstack;
                                exceptionStack = currentThread.exceptionStack;
                                callTrace = currentThread.callTrace;
                                stackp = currentThread.stackp;
                                exceptionp = currentThread.exceptionp;
                                callstackp = currentThread.callstackp;
                                calltracep = currentThread.calltracep;
                                datap = currentThread.datap;
                                module = currentThread.module;
                                code.Seek(currentThread.codep);
                                #endregion

                                if (currentThread.state == ThreadState.EXITING)
                                {
                                    continue;
                                }
                            }
                            else if (threads.Count > 1)
                            {
                                #region save current thread info
                                currentThread.stack = stack;
                                currentThread.callstack = callstack;
                                currentThread.exceptionStack = exceptionStack;
                                currentThread.callTrace = callTrace;
                                currentThread.stackp = stackp;
                                currentThread.exceptionp = exceptionp;
                                currentThread.callstackp = callstackp;
                                currentThread.calltracep = calltracep;
                                currentThread.datap = datap;
                                currentThread.module = module;
                                currentThread.codep = code.Position;
                                #endregion

                                #region context switch
                                currentThreadIndex = (currentThreadIndex + 1) % threads.Count;
                                currentThread = threads[currentThreadIndex];
                                contextSwitches++;
                                #endregion

                                #region load next thread info
                                threadid = currentThread.threadid;
                                stack = currentThread.stack;
                                callstack = currentThread.callstack;
                                exceptionStack = currentThread.exceptionStack;
                                callTrace = currentThread.callTrace;
                                stackp = currentThread.stackp;
                                exceptionp = currentThread.exceptionp;
                                callstackp = currentThread.callstackp;
                                calltracep = currentThread.calltracep;
                                datap = currentThread.datap;
                                module = currentThread.module;
                                code.Seek(currentThread.codep);
                                #endregion
                            }

                            switch (currentThread.state)
                            {
                                case ThreadState.SLEEPING:
                                    if (this._ticks < currentThread.sleepUntil)
                                    {
                                        continue;
                                    }
                                    currentThread.state = ThreadState.RUNNING;
                                    break;
                                case ThreadState.JOINING:
                                    bool alive = false;
                                    for (int i = 0; i < threads.Count; i++)
                                    {
                                        if (threads[i].threadid == currentThread.threadToJoin)
                                        {
                                            alive = true;
                                            break;
                                        }
                                    }
                                    if (!alive)
                                    {
                                        currentThread.state = ThreadState.RUNNING;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                case ThreadState.WAITING:
                                    if (!semaphores.Contains(currentThread.semaphoreToWait))
                                    {
                                        semaphores.Add(currentThread.semaphoreToWait);
                                        currentThread.state = ThreadState.RUNNING;
                                        break;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                case ThreadState.RUNNING:
                                    break;
                            }

                            break;
                        }

                        this._ticked = false;
                    }

                    curPos = code.Position;
                    int oper = (int)reader.ReadByte();
                    instructionsExecuted++;

                    switch (oper)
                    {
                        case (int)Instruction.NOP:
                            if (this._trace) this.Trace("nop", null, curPos, stackp);
                            continue;
                        case (int)Instruction.ADD:
                            #region ADD
                            if (this._trace) this.Trace("add", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            switch (this.ComputeTypes(lOperand, rOperand))
                            {
                                case ((int)DataType.BYTE << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(byte)lOperand + (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(char)lOperand + (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)lOperand + (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (long)lOperand + (long)(byte)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (double)lOperand + (double)(byte)rOperand;
                                    break;
                                case ((int)DataType.STRING << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (string)lOperand + ((byte)rOperand).ToString();
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(byte)lOperand + (int)(char)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(char)lOperand + (int)(char)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)lOperand + (int)(char)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (long)lOperand + (long)(char)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (double)lOperand + (double)(char)rOperand;
                                    break;
                                case ((int)DataType.STRING << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (string)lOperand + ((char)rOperand).ToString();
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(byte)lOperand + (int)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(char)lOperand + (int)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)lOperand + (int)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (long)lOperand + (long)(int)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (double)lOperand + (double)(int)rOperand;
                                    break;
                                case ((int)DataType.STRING << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (string)lOperand + ((int)rOperand).ToString();
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(byte)lOperand + (long)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(char)lOperand + (long)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(int)lOperand + (long)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)lOperand + (long)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (double)lOperand + (double)(long)rOperand;
                                    break;
                                case ((int)DataType.STRING << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (string)lOperand + ((long)rOperand).ToString();
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(byte)lOperand + (double)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(char)lOperand + (double)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(int)lOperand + (double)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(long)lOperand + (double)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)lOperand + (double)rOperand;
                                    break;
                                case ((int)DataType.STRING << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (string)lOperand + ((double)rOperand).ToString();
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.STRING:
                                    stack[stackp - 1] = ((byte)lOperand).ToString() + (string)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.STRING:
                                    stack[stackp - 1] = ((char)lOperand).ToString() + (string)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.STRING:
                                    stack[stackp - 1] = ((int)lOperand).ToString() + (string)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.STRING:
                                    stack[stackp - 1] = ((long)lOperand).ToString() + (string)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.STRING:
                                    stack[stackp - 1] = ((double)lOperand).ToString() + (string)rOperand;
                                    break;
                                case ((int)DataType.STRING << 3) | (int)DataType.STRING:
                                    stack[stackp - 1] = (string)lOperand + (string)rOperand;
                                    break;
                                default:
                                    throw new Exception(string.Format("add - operand unsupported type: {0}:{1}", lOperand != null ? lOperand.GetType().Name : "null", rOperand != null ? rOperand.GetType().Name : "null"));
                            }
                            #endregion
                            continue;
                        case (int)Instruction.SUB:
                            #region SUB
                            if (this._trace) this.Trace("sub", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            switch (this.ComputeTypes(lOperand, rOperand))
                            {
                                case ((int)DataType.BYTE << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(byte)lOperand - (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(char)lOperand - (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)lOperand - (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (long)lOperand - (long)(byte)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (double)lOperand - (double)(byte)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(byte)lOperand - (int)(char)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(char)lOperand - (int)(char)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)lOperand - (int)(char)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (long)lOperand - (long)(char)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (double)lOperand - (double)(char)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(byte)lOperand - (int)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(char)lOperand - (int)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)lOperand - (int)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (long)lOperand - (long)(int)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (double)lOperand - (double)(int)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(byte)lOperand - (long)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(char)lOperand - (long)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(int)lOperand - (long)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)lOperand - (long)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (double)lOperand - (double)(long)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(byte)lOperand + (double)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(char)lOperand - (double)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(int)lOperand - (double)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(long)lOperand - (double)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)lOperand - (double)rOperand;
                                    break;
                                default:
                                    throw new Exception("sub - operand unsupported type");
                            }
                            #endregion
                            continue;
                        case (int)Instruction.MUL:
                            #region MUL
                            if (this._trace) this.Trace("mul", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            switch (this.ComputeTypes(lOperand, rOperand))
                            {
                                case ((int)DataType.BYTE << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(byte)lOperand * (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(char)lOperand * (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)lOperand * (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (long)lOperand * (long)(byte)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (double)lOperand * (double)(byte)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(byte)lOperand * (int)(char)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(char)lOperand * (int)(char)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)lOperand * (int)(char)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (long)lOperand * (long)(char)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (double)lOperand * (double)(char)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(byte)lOperand * (int)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(char)lOperand * (int)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)lOperand * (int)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (long)lOperand * (long)(int)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (double)lOperand * (double)(int)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(byte)lOperand * (long)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(char)lOperand * (long)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(int)lOperand * (long)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)lOperand * (long)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (double)lOperand * (double)(long)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(byte)lOperand * (double)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(char)lOperand * (double)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(int)lOperand * (double)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(long)lOperand * (double)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)lOperand * (double)rOperand;
                                    break;
                                default:
                                    throw new Exception("mul - operand unsupported type");
                            }
                            #endregion
                            continue;
                        case (int)Instruction.DIV:
                            #region DIV
                            if (this._trace) this.Trace("div", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            switch (this.ComputeTypes(lOperand, rOperand))
                            {
                                case ((int)DataType.BYTE << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(byte)lOperand / (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(char)lOperand / (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)lOperand / (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (long)lOperand / (long)(byte)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (double)lOperand / (double)(byte)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(byte)lOperand / (int)(char)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(char)lOperand / (int)(char)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)lOperand / (int)(char)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (long)lOperand / (long)(char)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (double)lOperand / (double)(char)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(byte)lOperand / (int)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(char)lOperand / (int)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)lOperand / (int)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (long)lOperand / (long)(int)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (double)lOperand / (double)(int)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(byte)lOperand / (long)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(char)lOperand / (long)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(int)lOperand / (long)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)lOperand / (long)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (double)lOperand / (double)(long)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(byte)lOperand / (double)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(char)lOperand / (double)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(int)lOperand / (double)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(long)lOperand / (double)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)lOperand / (double)rOperand;
                                    break;
                                default:
                                    throw new Exception(string.Format("div - operand unsupported type: {0} / {1}", lOperand.GetType().Name, rOperand.GetType().Name));
                            }
                            #endregion
                            continue;
                        case (int)Instruction.MOD:
                            #region MOD
                            if (this._trace) this.Trace("mod", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            switch (this.ComputeTypes(lOperand, rOperand))
                            {
                                case ((int)DataType.BYTE << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(byte)lOperand % (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)(char)lOperand % (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (int)lOperand % (int)(byte)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (long)lOperand % (long)(byte)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.BYTE:
                                    stack[stackp - 1] = (double)lOperand % (double)(byte)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(byte)lOperand % (int)(char)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)(char)lOperand % (int)(char)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (int)lOperand % (int)(char)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (long)lOperand % (long)(char)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.CHAR:
                                    stack[stackp - 1] = (double)lOperand % (double)(char)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(byte)lOperand % (int)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)(char)lOperand % (int)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (int)lOperand % (int)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (long)lOperand % (long)(int)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.INT:
                                    stack[stackp - 1] = (double)lOperand % (double)(int)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(byte)lOperand % (long)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(char)lOperand % (long)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)(int)lOperand % (long)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (long)lOperand % (long)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.LONG:
                                    stack[stackp - 1] = (double)lOperand % (double)(long)rOperand;
                                    break;
                                case ((int)DataType.BYTE << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(byte)lOperand % (double)rOperand;
                                    break;
                                case ((int)DataType.CHAR << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(char)lOperand % (double)rOperand;
                                    break;
                                case ((int)DataType.INT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(int)lOperand % (double)rOperand;
                                    break;
                                case ((int)DataType.LONG << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)(long)lOperand % (double)rOperand;
                                    break;
                                case ((int)DataType.FLOAT << 3) | (int)DataType.FLOAT:
                                    stack[stackp - 1] = (double)lOperand % (double)rOperand;
                                    break;
                                default:
                                    throw new Exception("mod - operand unsupported type");
                            }
                            #endregion
                            continue;
                        case (int)Instruction.NEG:
                            if (this._trace) this.Trace("neg", null, curPos, stackp);
                            rOperand = stack[stackp - 1];
                            if (rOperand is int)
                            {
                                stack[stackp - 1] = -(int)rOperand;
                            }
                            else if (rOperand is double)
                            {
                                stack[stackp - 1] = -(double)rOperand;
                            }
                            else
                            {
                                throw new Exception(string.Format("Illegal NEG operation on non-numerical operand: {0}", rOperand.GetType().Name));
                            }
                            continue;
                        case (int)Instruction.AND:
                            if (this._trace) this.Trace("and", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            if (lOperand is int && rOperand is int)
                            {
                                stack[stackp - 1] = (int)lOperand & (int)rOperand;
                            }
                            else if (lOperand is bool && rOperand is bool)
                            {
                                stack[stackp - 1] = (bool)lOperand && (bool)rOperand;
                            }
                            else
                            {
                                throw new Exception("AND - operand unsupported type");
                            }
                            continue;
                        case (int)Instruction.OR:
                            if (this._trace) this.Trace("or", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            if (lOperand is int && rOperand is int)
                            {
                                stack[stackp - 1] = (int)lOperand | (int)rOperand;
                            }
                            else if (lOperand is bool && rOperand is bool)
                            {
                                stack[stackp - 1] = (bool)lOperand || (bool)rOperand;
                            }
                            else
                            {
                                throw new Exception("OR - operand unsupported type");
                            }
                            continue;
                        case (int)Instruction.XOR:
                            if (this._trace) this.Trace("xor", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            if (lOperand is int && rOperand is int)
                            {
                                stack[stackp - 1] = (int)lOperand ^ (int)rOperand;
                            }
                            else if (lOperand is bool && rOperand is bool)
                            {
                                stack[stackp - 1] = (bool)lOperand ^ (bool)rOperand;
                            }
                            else
                            {
                                throw new Exception("XOR - operand unsupported type");
                            }
                            continue;
                        case (int)Instruction.NOT:
                            if (this._trace) this.Trace("not", null, curPos, stackp);
                            lOperand = stack[stackp - 1];
                            if (lOperand is int)
                            {
                                stack[stackp - 1] = ~(int)lOperand;
                            }
                            else if (lOperand is bool)
                            {
                                stack[stackp - 1] = !(bool)lOperand;
                            }
                            else
                            {
                                throw new Exception("NOT - operand unsupported type");
                            }
                            continue;
                        case (int)Instruction.LSHIFT:
                            if (this._trace) this.Trace("lshift", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            if ((lOperand is int || lOperand is char || lOperand is byte) && (rOperand is int || rOperand is char || rOperand is byte))
                            {
                                stack[stackp - 1] = (int)lOperand << (int)rOperand;
                            }
                            else
                            {
                                throw new Exception("LSHIFT - operand unsupported type");
                            }
                            continue;
                        case (int)Instruction.RSHIFT:
                            if (this._trace) this.Trace("rshift", null, curPos, stackp);
                            rOperand = stack[--stackp];
                            lOperand = stack[stackp - 1];
                            if ((lOperand is int || lOperand is char || lOperand is byte) && (rOperand is int || rOperand is char || rOperand is byte))
                            {
                                stack[stackp - 1] = (int)lOperand >> (int)rOperand;
                            }
                            else
                            {
                                throw new Exception("RSHIFT - operand unsupported type");
                            }
                            continue;
                        case (int)Instruction.INC_S:
                        case (int)Instruction.INC:
                            n = oper == (int)Instruction.INC_S ? (int)reader.ReadSByte() : (int)reader.ReadUInt16();
                            if (this._trace) this.Trace("inc", n, curPos, stackp);
                            object data = stack[datap + n - 1];
                            if (data is byte)
                            {
                                stack[datap + n - 1] = (byte)data + 1;
                            }
                            else if (data is int)
                            {
                                stack[datap + n - 1] = (int)data + 1;
                            }
                            else if (data is long)
                            {
                                stack[datap + n - 1] = (long)data + 1;
                            }
                            else
                            {
                                throw new Exception(string.Format("INC: Operand type not supported: {0}", data.GetType().Name));
                            }
                            continue;
                        case (int)Instruction.DEC_S:
                        case (int)Instruction.DEC:
                            n = oper == (int)Instruction.DEC_S ? (int)reader.ReadSByte() : (int)reader.ReadUInt16();
                            if (this._trace) this.Trace("dec", n, curPos, stackp);
                            data = stack[datap + n - 1];
                            if (data is byte)
                            {
                                stack[datap + n - 1] = (byte)data - 1;
                            }
                            else if (data is int)
                            {
                                stack[datap + n - 1] = (int)data - 1;
                            }
                            else if (data is long)
                            {
                                stack[datap + n - 1] = (long)data - 1;
                            }
                            else
                            {
                                throw new Exception(string.Format("DEC: Operand type not supported: {0}", data.GetType().Name));
                            }
                            continue;
                        case (int)Instruction.BR_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("br.s", n, curPos, stackp);
                            #region Handle try-catch block
                            if (exceptionp > 0)
                            {
                                info = exceptionStack[exceptionp - 1];
                                if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                {
                                    if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                    {
                                        info.ExitAction = TryExitAction.Branch;
                                        info.BranchTo = n;
                                        code.Jump(info.FinallyOffset);
                                        continue;
                                    }
                                }
                            }
                            #endregion
                            code.Branch(n);
                            continue;
                        case (int)Instruction.BR:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("br", n, curPos, stackp);
                            #region Handle try-catch block
                            if (exceptionp > 0)
                            {
                                info = exceptionStack[exceptionp - 1];
                                if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                {
                                    if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                    {
                                        info.ExitAction = TryExitAction.Branch;
                                        info.BranchTo = n;
                                        code.Jump(info.FinallyOffset);
                                        continue;
                                    }
                                }
                            }
                            #endregion
                            code.Branch(n);
                            continue;
                        case (int)Instruction.BR_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("br", n, curPos, stackp);
                            #region Handle try-catch block
                            if (exceptionp > 0)
                            {
                                info = exceptionStack[exceptionp - 1];
                                if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                {
                                    if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                    {
                                        info.ExitAction = TryExitAction.Branch;
                                        info.BranchTo = n;
                                        code.Jump(info.FinallyOffset);
                                        continue;
                                    }
                                }
                            }
                            #endregion
                            code.Branch(n);
                            continue;
                        case (int)Instruction.BE_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("be.s", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) == 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BE:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("be", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) == 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BE_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("be.l", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) == 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BNE_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("bne.s", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) != 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BNE:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("bne", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) != 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BNE_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("bne.l", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) != 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BL_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("bl.s", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) < 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BL:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("bl", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) < 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BL_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("bl.l", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) < 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BLE_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("ble.s", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) <= 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BLE:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("ble", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) <= 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BLE_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("ble.l", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) <= 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BG_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("bg.s", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) > 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BG:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("bg", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) > 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BG_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("bg.l", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) > 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BGE_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("bge.s", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) >= 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BGE:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("bge", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) >= 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BGE_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("bge.l", n, curPos, stackp);
                            stackp -= 2;
                            if (this.Compare(stack[stackp], stack[stackp + 1]) >= 0)
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BTRUE_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("btrue.s", n, curPos, stackp);
                            stackp--;
                            if ((bool)stack[stackp])
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BTRUE:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("btrue", n, curPos, stackp);
                            stackp--;
                            if ((bool)stack[stackp])
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BTRUE_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("btrue.l", n, curPos, stackp);
                            stackp--;
                            if ((bool)stack[stackp])
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BFALSE_S:
                            n = reader.ReadSByte();
                            if (this._trace) this.Trace("bfalse.s", n, curPos, stackp);
                            stackp--;
                            if (!(bool)stack[stackp])
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BFALSE:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("bfalse", n, curPos, stackp);
                            stackp--;
                            if (!(bool)stack[stackp])
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BFALSE_L:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("bfalse.l", n, curPos, stackp);
                            stackp--;
                            if (!(bool)stack[stackp])
                            {
                                #region Handle try-catch block
                                if (exceptionp > 0)
                                {
                                    info = exceptionStack[exceptionp - 1];
                                    if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                    {
                                        if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Branch;
                                            info.BranchTo = n;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                code.Branch(n);
                            }
                            continue;
                        case (int)Instruction.BMAP_L:
                            n = (int)reader.ReadUInt16();
                            if (this._trace) this.Trace("bmap.l", n, curPos, stackp);
                            int index = (int)stack[stackp - 1];
                            stackp--;
                            code.BranchMap_L(index);
                            continue;
                        case (int)Instruction.BMAP:
                            n = (int)reader.ReadUInt16();
                            if (this._trace) this.Trace("bmap", n, curPos, stackp);
                            index = (int)stack[stackp - 1];
                            stackp--;
                            code.BranchMap(index);
                            continue;
                        case (int)Instruction.BMAP_S:
                            n = (int)reader.ReadUInt16();
                            if (this._trace) this.Trace("bmap.s", n, curPos, stackp);
                            index = (int)stack[stackp - 1];
                            stackp--;
                            code.BranchMap_S(index);
                            continue;
                        case (int)Instruction.CE:
                            if (this._trace) this.Trace("ce", null, curPos, stackp);
                            stackp--;
                            stack[stackp - 1] = this.Compare(stack[stackp - 1], stack[stackp]) == 0;
                            continue;
                        case (int)Instruction.CNE:
                            if (this._trace) this.Trace("cne", null, curPos, stackp);
                            stackp--;
                            stack[stackp - 1] = this.Compare(stack[stackp - 1], stack[stackp]) != 0;
                            continue;
                        case (int)Instruction.CL:
                            if (this._trace) this.Trace("cl", null, curPos, stackp);
                            stackp--;
                            stack[stackp - 1] = this.Compare(stack[stackp - 1], stack[stackp]) < 0;
                            continue;
                        case (int)Instruction.CLE:
                            if (this._trace) this.Trace("cle", null, curPos, stackp);
                            stackp--;
                            stack[stackp - 1] = this.Compare(stack[stackp - 1], stack[stackp]) <= 0;
                            continue;
                        case (int)Instruction.CG:
                            if (this._trace) this.Trace("cg", null, curPos, stackp);
                            stackp--;
                            stack[stackp - 1] = this.Compare(stack[stackp - 1], stack[stackp]) > 0;
                            continue;
                        case (int)Instruction.CGE:
                            if (this._trace) this.Trace("cge", null, curPos, stackp);
                            stackp--;
                            stack[stackp - 1] = this.Compare(stack[stackp - 1], stack[stackp]) >= 0;
                            continue;
                        case (int)Instruction.POP:
                            if (this._trace) this.Trace("pop", null, curPos, stackp);
                            stackp--;
                            continue;
                        case (int)Instruction.CALL_S:
                        case (int)Instruction.CALL:
                            n = oper == (int)Instruction.CALL ? reader.ReadUInt16() : reader.ReadByte();
                            m = code.Position;
                            if (this._trace) this.Trace("call", n, curPos, stackp);

                            // resolve method dynamically
                            string methodSignature = this._moduleMethodSignatures[module - 1][n - 1];
                            int arguments = this._moduleMethodArguments[module - 1][n - 1];
                            data = stack[stackp - arguments - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving signature: {0} <arguments: {1}>", methodSignature, arguments);
                            }

                            ClassInstance instance = null;
                            ClassDefinition definition = null;

                            if (data == null)
                            {
                                throw new Exception(string.Format("Invokation of method on null object: {0}", methodSignature));
                            }
                            else if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                                definition = instance.Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = (ClassDefinition)data;
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                // convert to implicit class instance
                                definition = this._implicitClasses[data.GetType().Name];
                                instance = new ClassInstance();
                                instance.Definition = definition;
                                instance.Fields = new object[1];
                                instance.Fields[0] = data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Invocation of '{0}' on a non-class/non-instance: {1}", methodSignature, data.GetType().Name));
                            }

                            if (this._trace)
                            {
                                if (!definition.MethodMap.ContainsKey(methodSignature))
                                {
                                    Console.WriteLine("unresolved method '{1}' in instance of '{0}':", definition.ClassName, methodSignature);
                                    foreach (string signature in definition.MethodMap.Keys)
                                    {
                                        Console.WriteLine("found only: {0}", signature);
                                    }
                                }
                            }

                            if (!definition.MethodMap.ContainsKey(methodSignature))
                            {
                                throw new Exception(string.Format("Unresolved method '{0}' on '{1}'.", methodSignature, definition.ClassName));
                            }

                            int[] methodInfo = definition.MethodMap[methodSignature];
                            int methodModule = methodInfo[0];
                            int methodArguments = methodInfo[1];
                            int methodVariables = methodInfo[2];
                            int methodCodeAddress = methodInfo[3];
                            bool staticMethod = methodInfo[4] == 1;

                            if (!staticMethod && instance == null)
                            {
                                throw new Exception("Calling method on a non instance.");
                            }

                            if (definition.IsFacade) // if is facade, call and continue
                            {
                                // TODO: need to do for static method here!

                                if (this._trace) Console.WriteLine("calling facade callmethod({0}) with {1} arguments", methodSignature, methodArguments);
                                // prepare argument stacks
                                object[] argumentStack = new object[methodArguments];
                                for (int i = 0; i < methodArguments; i++)
                                {
                                    if (this._trace) Console.WriteLine("argument {0} = {1}", i + 1, stack[stackp - methodArguments + i]);
                                    argumentStack[i] = stack[stackp - methodArguments + i];
                                }
                                object result = null;
                                if (staticMethod)
                                {
                                    result = definition.FacadeInstance.CallMethod(methodSignature, argumentStack);
                                }
                                else
                                {
                                    result = instance.FacadeInstance.CallMethod(methodSignature, argumentStack);
                                }
                                stackp -= methodArguments;
                                stack[stackp - 1] = result; // TODO: pending test!
                            }
                            else
                            {
                                if (this._trace)
                                {
                                    Console.WriteLine("resolved method: module: {0}, arguments: {1}, variables: {2}, address: {3:0000}", methodModule, methodArguments, methodVariables, methodCodeAddress);
                                }

                                callstack[callstackp++] = datap; // datap 
                                callstack[callstackp++] = m; // codep
                                callstack[callstackp++] = module; // module 
                                callTrace[calltracep++] = definition.ClassName + "::" + methodSignature;

                                module = methodModule;
                                code.Seek(methodCodeAddress);
                                datap = stackp - methodArguments - 1;
                                stackp += methodVariables; // allocate stack for local variables
                            }
                            continue;
                        case (int)Instruction.CALLC_S:
                        case (int)Instruction.CALLC:
                            n = oper == (int)Instruction.CALLC ? reader.ReadUInt16() : reader.ReadByte();
                            m = code.Position;
                            if (this._trace) this.Trace("callc", n, curPos, stackp);

                            // resolve method dynamically
                            methodSignature = this._moduleMethodSignatures[module - 1][n - 1];
                            arguments = this._moduleMethodArguments[module - 1][n - 1];
                            data = stack[stackp - arguments - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving signature: {0} <arguments: {1}>", methodSignature, arguments);
                            }

                            instance = null;

                            if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Invokation of '{0}' on a non-instance: {1}", methodSignature, data.GetType().Name));
                            }

                            if (!instance.Definition.MethodMap.ContainsKey(methodSignature))
                            {
                                throw new Exception(string.Format("Unresolved constructor '{0}' for class '{1}'", methodSignature, instance.Definition.ClassName));
                            }

                            if (instance.Definition.IsFacade) // not possible!
                            {
                                throw new Exception("Illegal CALLC on facade class.");
                            }

                            methodInfo = instance.Definition.MethodMap[methodSignature];
                            methodModule = methodInfo[0];
                            methodArguments = methodInfo[1];
                            methodVariables = methodInfo[2];
                            methodCodeAddress = methodInfo[3];

                            if (this._trace)
                            {
                                Console.WriteLine("resolved method: module: {0}, arguments: {1}, variables: {2}, address: {3:0000}", methodModule, methodArguments, methodVariables, methodCodeAddress);
                            }

                            callstack[callstackp++] = datap; // datap 
                            callstack[callstackp++] = m; // codep
                            callstack[callstackp++] = module; // module
                            callTrace[calltracep++] = instance.Definition.ClassName + "::" + methodSignature;

                            module = methodModule;
                            code.Seek(methodCodeAddress);
                            datap = stackp - methodArguments - 1;
                            stackp += methodVariables; // allocate stack for local variables
                            continue;                        
                        case (int)Instruction.RETC:
                            if (this._trace) this.Trace("retc", null, curPos, stackp);
                            if (exceptionp > 0)
                            {
                                info = exceptionStack[exceptionp - 1];
                                if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                {
                                    info.ExitAction = TryExitAction.RetC;
                                    code.Jump(info.FinallyOffset);
                                    continue;
                                }
                            }
                            stackp = datap + 1;
                            module = callstack[--callstackp];
                            code.Seek(callstack[--callstackp]);
                            datap = callstack[--callstackp];
                            calltracep--;
                            continue;
                        case (int)Instruction.RET:
                            if (this._trace) this.Trace("ret", null, curPos, stackp);
                            if (exceptionp > 0)
                            {
                                info = exceptionStack[exceptionp - 1];
                                if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                {
                                    info.ExitAction = TryExitAction.Ret;
                                    code.Jump(info.FinallyOffset);
                                    continue;
                                }
                            }
                            stackp = datap;
                            module = callstack[--callstackp];
                            code.Seek(callstack[--callstackp]);
                            datap = callstack[--callstackp];
                            calltracep--;
                            continue;
                        case (int)Instruction.RETV:
                            if (this._trace) this.Trace("retv", null, curPos, stackp);
                            if (exceptionp > 0)
                            {
                                info = exceptionStack[exceptionp - 1];
                                if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                {
                                    info.ExitAction = TryExitAction.RetV;
                                    code.Jump(info.FinallyOffset);
                                    continue;
                                }
                            }
                            stack[datap] = stack[stackp - 1]; // compress stack with result copied
                            stackp = datap + 1;
                            module = callstack[--callstackp];
                            code.Seek(callstack[--callstackp]);
                            datap = callstack[--callstackp];
                            calltracep--;
                            continue;
                        case (int)Instruction.HALT:
                            if (this._trace) this.Trace("halt", null, curPos, stackp);
                            if (stack[fieldIndex] != null)
                            {
                                Console.WriteLine("Exception: " + stack[fieldIndex].ToString());
                            }
                            if (this._showStatistics)
                            {
                                Console.WriteLine("\n==========================================================================");
                                Console.WriteLine("exits: datap={0} stackp={1} stack[{4}]={2} stack[0]={3}", datap, stackp, stack[stackp - 1], stack[0], stackp - 1);
                                DateTime stopTime = DateTime.Now;
                                Console.WriteLine("total instruction executed: {0} total context switches: {1} execution time: {2}", instructionsExecuted, contextSwitches, (stopTime - startTime));
                            }
                            return;
                        case (int)Instruction.LDNULL:
                            if (this._trace) this.Trace("ldnull", null, curPos, stackp);
                            stack[stackp++] = null;
                            continue;
                        case (int)Instruction.LDZERO:
                            if (this._trace) this.Trace("ldzero", null, curPos, stackp);
                            stack[stackp++] = 0;
                            continue;
                        case (int)Instruction.LDCBYTE:
                            byte b = reader.ReadByte();
                            if (this._trace) this.Trace("ldcbyte", n, curPos, stackp);
                            stack[stackp++] = b;
                            continue;
                        case (int)Instruction.LDCCHAR:
                            char c = reader.ReadChar();
                            if (this._trace) this.Trace("ldcchar", n, curPos, stackp);
                            stack[stackp++] = c;
                            continue;
                        case (int)Instruction.LDCINT16:
                            n = reader.ReadInt16();
                            if (this._trace) this.Trace("ldcint16", n, curPos, stackp);
                            stack[stackp++] = n;
                            continue;
                        case (int)Instruction.LDCINT32:
                            n = reader.ReadInt32();
                            if (this._trace) this.Trace("ldcint32", n, curPos, stackp);
                            stack[stackp++] = n;
                            continue;
                        case (int)Instruction.LDCINT64:
                            long l = reader.ReadInt64();
                            if (this._trace) this.Trace("ldcint64", l, curPos, stackp);
                            stack[stackp++] = l;
                            continue;
                        case (int)Instruction.LDCFLOAT64:
                            f = reader.ReadDouble();
                            if (this._trace) this.Trace("ldcfloat64", f, curPos, stackp);
                            stack[stackp++] = f;
                            continue;
                        case (int)Instruction.LDCBOOL:
                            n = (int)reader.ReadByte();
                            if (this._trace) this.Trace("ldcbool", n != 0, curPos, stackp);
                            stack[stackp++] = n != 0;
                            continue;
                        case (int)Instruction.LDCSTRING_S:
                            n = (int)(uint)reader.ReadByte();
                            s = this._moduleStringLiterals[module - 1][n - 1];
                            if (this._trace) this.Trace("ldcstring.s", s, curPos, stackp);
                            stack[stackp++] = s;
                            continue;
                        case (int)Instruction.LDCSTRING:
                            n = reader.ReadInt32();
                            s = this._moduleStringLiterals[module - 1][n - 1];
                            if (this._trace) this.Trace("ldcstring.s", s, curPos, stackp);
                            stack[stackp++] = s;
                            continue;
                        case (int)Instruction.LDVAR_S:
                            n = reader.ReadByte();
                            if (this._trace) this.Trace("loadvar.s", n, curPos, stackp);
                            stack[stackp++] = stack[datap + n - 1];
                            continue;
                        case (int)Instruction.LDVAR:
                            n = reader.ReadUInt16();
                            if (this._trace) this.Trace("loadvar", n, curPos, stackp);
                            stack[stackp++] = stack[datap + n - 1];
                            continue;
                        case (int)Instruction.LDARRAY:
                            if (this._trace) this.Trace("loadarray", null, curPos, stackp);
                            n = (int)stack[--stackp];
                            object arr = stack[--stackp];
                            if (arr is object[])
                            {
                                stack[stackp++] = ((object[])arr)[n];
                            }
                            else if (arr is string)
                            {
                                stack[stackp++] = ((string)arr)[n];
                            }
                            else
                            {
                                throw new Exception("Array operator on non-array/string operand: " + arr.GetType().Name);
                            }
                            continue;
                        case (int)Instruction.LDFIELD_S:
                        case (int)Instruction.LDFIELD:
                            n = oper == (int)Instruction.LDFIELD ? reader.ReadUInt16() : reader.ReadByte();
                            if (this._trace) this.Trace("ldf", n, curPos, stackp);
                            data = stack[stackp - 1];

                            // resolve field dynamically
                            string fieldName = this._moduleFields[module - 1][n - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving field: {0}", fieldName);
                            }

                            instance = null;
                            definition = null;

                            if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                                definition = instance.Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = (ClassDefinition)data;
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                // convert to implicit class instance
                                definition = this._implicitClasses[data.GetType().Name];
                                instance = new ClassInstance();
                                instance.Definition = definition;
                                instance.Fields = new object[1];
                                instance.Fields[0] = data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (!definition.FieldMap.ContainsKey(fieldName))
                            {
                                throw new Exception(string.Format("Unresolved field '{0}' on instance.", fieldName));
                            }

                            int fieldNumber = definition.FieldMap[fieldName][0] - 1;
                            bool staticField = definition.FieldMap[fieldName][1] == 1;

                            if (!staticField && instance == null)
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (definition.IsFacade)
                            {// TODO
                                if (this._trace) Console.WriteLine("calling facade getvalue({0})", fieldName);
                                if (staticField)
                                {
                                    stack[stackp - 1] = definition.FacadeInstance.GetField(fieldName);
                                }
                                else
                                {
                                    stack[stackp - 1] = instance.FacadeInstance.GetField(fieldName);
                                }
                            }
                            else
                            {
                                if (staticField)
                                {
                                    stack[stackp - 1] = definition.Fields[fieldNumber];
                                }
                                else
                                {
                                    stack[stackp - 1] = instance.Fields[fieldNumber];
                                }
                            }

                            continue;
                        case (int)Instruction.LDCLASS_S:
                        case (int)Instruction.LDCLASS:
                            n = oper == (int)Instruction.LDCLASS ? reader.ReadUInt16() : reader.ReadByte();
                            if (this._trace) this.Trace("ldclass", n, curPos, stackp);
                            // resolve class definition
                            definition = this._moduleClasses[module - 1][n - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving class: {0}", definition.ClassName);
                            }
                            stack[stackp++] = definition;
                            if (!definition.IsFacade && !definition.IsInitialized) // invoke static constructor
                            {
                                definition.IsInitialized = true;

                                #region Implicit CALLSC
                                m = code.Position;
                                if (this._trace) this.Trace("implicit.callsc", null, curPos, stackp);
                                methodSignature = "__static(0)";

                                if (!definition.MethodMap.ContainsKey(methodSignature))
                                {
                                    if (this._trace) Console.WriteLine("skipping static constructor for {0}", definition.ClassName);
                                    continue;
                                }
                                methodInfo = definition.MethodMap[methodSignature];
                                methodModule = methodInfo[0];
                                methodArguments = methodInfo[1];
                                methodVariables = methodInfo[2];
                                methodCodeAddress = methodInfo[3];

                                if (this._trace)
                                {
                                    Console.WriteLine("resolved method: module: {0}, arguments: {1}, variables: {2}, address: {3:0000}", methodModule, methodArguments, methodVariables, methodCodeAddress);
                                }

                                callstack[callstackp++] = datap; // datap 
                                callstack[callstackp++] = m; // codep
                                callstack[callstackp++] = module; // module
                                callTrace[calltracep++] = definition.ClassName + "::" + methodSignature;

                                module = methodModule;
                                code.Seek(methodCodeAddress);
                                datap = stackp - methodArguments - 1;
                                stackp += methodVariables; // allocate stack for local variables
                                #endregion
                            }
                            continue;
                        case (int)Instruction.STORE_S:
                            n = reader.ReadByte();
                            if (this._trace) this.Trace("storev.s", n, curPos, stackp);
                            stack[datap + n - 1] = stack[--stackp];
                            continue;
                        case (int)Instruction.STORE:
                            n = reader.ReadUInt16();
                            if (this._trace) this.Trace("storev", n, curPos, stackp);
                            stack[datap + n - 1] = stack[--stackp];
                            continue;
                        case (int)Instruction.STOREA:
                            if (this._trace) this.Trace("storea", null, curPos, stackp);
                            n = (int)stack[stackp - 2];
                            arr = stack[stackp - 3];
                            if (arr is object[])
                            {
                                ((object[])arr)[n] = stack[stackp - 1];
                            }
                            else
                            {
                                throw new Exception("Array operator on non-array operand: " + arr.GetType().Name);
                            }
                            stackp -= 2;
                            continue;
                        case (int)Instruction.STOREF_S:
                        case (int)Instruction.STOREF:
                            n = oper == (int)Instruction.STOREF ? reader.ReadUInt16() : reader.ReadByte();
                            if (this._trace) this.Trace("storef", n, curPos, stackp);
                            data = stack[stackp - 2];

                            // resolve field dynamically
                            fieldName = this._moduleFields[module - 1][n - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving field: {0}", fieldName);
                            }

                            instance = null;
                            definition = null;

                            if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                                definition = instance.Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = (ClassDefinition)data;
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                // convert to implicit class instance
                                definition = this._implicitClasses[data.GetType().Name];
                                instance = new ClassInstance();
                                instance.Definition = definition;
                                instance.Fields = new object[1];
                                instance.Fields[0] = data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (!definition.FieldMap.ContainsKey(fieldName))
                            {
                                throw new Exception(string.Format("Unresolved field '{0}' on instance.", fieldName));
                            }

                            fieldNumber = definition.FieldMap[fieldName][0] - 1;
                            staticField = definition.FieldMap[fieldName][1] == 1;

                            if (!staticField && instance == null)
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (definition.IsFacade)
                            {
                                if (this._trace) Console.WriteLine("calling facade setvalue({0}, {1})", fieldName, stack[stackp - 1]);
                                if (staticField)
                                {
                                    definition.FacadeInstance.SetField(fieldName, stack[stackp - 1]);
                                }
                                else
                                {
                                    instance.FacadeInstance.SetField(fieldName, stack[stackp - 1]);
                                }
                            }
                            else
                            {
                                if (staticField)
                                {
                                    definition.Fields[fieldNumber] = stack[stackp - 1];
                                }
                                else
                                {
                                    instance.Fields[fieldNumber] = stack[stackp - 1];
                                }
                            }
                            stackp -= 2;
                            continue;
                        case (int)Instruction.NEWARRAY:
                            if (this._trace) this.Trace("newarray", null, curPos, stackp);
                            stack[stackp - 1] = new object[(int)stack[stackp - 1]];
                            continue;
                        case (int)Instruction.NEWINSTANCE_S:
                        case (int)Instruction.NEWINSTANCE:
                            n = oper == (int)Instruction.NEWINSTANCE ? reader.ReadUInt16() : reader.ReadByte();
                            if (this._trace) this.Trace("newinstance", n, curPos, stackp);
                            definition = this._moduleClasses[module - 1][n - 1];
                            if (this._trace)
                            {
                                Console.WriteLine("creating new instance of {0} (fields: {1})", definition.ClassName, definition.FieldCount);
                            }
                            instance = new ClassInstance();
                            instance.Definition = definition;
                            if (definition.IsFacade)
                            {
                                instance.Fields = null;
                                instance.FacadeInstance = (IFacadeClass)Activator.CreateInstance(definition.FacadeClassType);
                            }
                            else
                            {
                                instance.Fields = new object[definition.FieldCount];
                                instance.FacadeInstance = null;
                            }
                            stack[stackp++] = instance;
                            continue;
                        case (int)Instruction.SIZE:
                            if (this._trace) this.Trace("size", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is object[])
                            {
                                stack[stackp - 1] = ((object[])data).Length;
                            }
                            else if (data is string)
                            {
                                stack[stackp - 1] = ((string)data).Length;
                            }
                            else
                            {
                                throw new Exception(string.Format("SIZE operator expects array/string type: {0}", data.GetType().Name));
                            }
                            continue;
                        case (int)Instruction.CASTBYTE:
                            if (this._trace) this.Trace("cvbyte", null, curPos, stackp);
                            throw new Exception("cvbyte not supported");
                        case (int)Instruction.CASTCHAR:
                            if (this._trace) this.Trace("cvchar", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is byte)
                            {
                                stack[stackp - 1] = (char)(byte)data;
                            }
                            else if (data is char)
                            {
                                stack[stackp - 1] = (char)data;
                            }
                            else if (data is int)
                            {
                                stack[stackp - 1] = (char)(int)data;
                            }
                            else if (data is long)
                            {
                                stack[stackp - 1] = (char)(long)data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Illegal operand for cvchar: {0}", data.GetType().Name));
                            }
                            continue;
                        case (int)Instruction.CASTINT:
                            if (this._trace) this.Trace("cvint", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is byte)
                            {
                                stack[stackp - 1] = (int)(byte)data;
                            }
                            else if (data is char)
                            {
                                stack[stackp - 1] = (int)(char)data;
                            }
                            else if (data is long)
                            {
                                stack[stackp - 1] = (int)(long)data;
                            }
                            else if (data is double)
                            {
                                stack[stackp - 1] = (int)(double)data;
                            }
                            else if (data is string)
                            {
                                stack[stackp - 1] = int.Parse((string)data);
                            }
                            else
                            {
                                throw new Exception(string.Format("Illegal operand for cvint: {0}", data.GetType().Name));
                            }
                            continue;
                        case (int)Instruction.CASTLONG:
                            if (this._trace) this.Trace("cvlong", null, curPos, stackp);
                            throw new Exception("cvlong not supported");
                        case (int)Instruction.CASTFLOAT:
                            if (this._trace) this.Trace("cvfloat", null, curPos, stackp);
                            throw new Exception("cvfloat not supported");
                        case (int)Instruction.CASTSTRING:
                            if (this._trace) this.Trace("cvstring", null, curPos, stackp);
                            stack[stackp - 1] = stack[stackp - 1] != null ? stack[stackp - 1].ToString() : null;
                            continue;
                        case (int)Instruction.TIME:
                            if (this._trace) this.Trace("time", null, curPos, stackp);
                            stack[stackp++] = DateTime.Now.Ticks;
                            continue;
                        case (int)Instruction.ESC:
                            n = reader.ReadUInt16();
                            s = this._moduleStringLiterals[module - 1][n - 1];
                            if (this._trace) this.Trace("// {0}", s, curPos, stackp);
                            continue;
                        case (int)Instruction.HASH:
                            if (this._trace) this.Trace("hash", null, curPos, stackp);
                            stack[stackp - 1] = stack[stackp - 1].GetHashCode();
                            continue;
                        case (int)Instruction.ENTERTRY_L:
                        case (int)Instruction.ENTERTRY:
                        case (int)Instruction.ENTERTRY_S:
                            if (oper == (int)Instruction.ENTERTRY_L)
                            {
                                n = reader.ReadInt32();
                                m = reader.ReadInt32(); 
                                if (this._trace) this.Trace("entertry.l", null, curPos, stackp);
                            }
                            else if (oper == (int)Instruction.ENTERTRY)
                            {
                                n = reader.ReadInt16();
                                m = reader.ReadInt16();
                                if (this._trace) this.Trace("entertry", null, curPos, stackp);
                            }
                            else
                            {
                                n = reader.ReadSByte();
                                m = reader.ReadSByte();
                                if (this._trace) this.Trace("entertry.s", null, curPos, stackp);
                            }                           
                            info = new ExceptionInfo();
                            info.TryOffset = code.Position;
                            info.CatchOffset = n + code.Position;
                            info.FinallyOffset = m + code.Position;
                            info.Phase = TryCatchPhase.InsideTry;
                            info.Module = module;
                            info.CallPointer = callstackp;
                            info.DataPointer = datap;
                            info.StackPointer = stackp;                            
                            info.ExitAction = TryExitAction.None;
                            info.ExceptionObject = null;
                            info.BranchTo = 0;
                            exceptionStack[exceptionp++] = info;
                            continue;                       
                        case (int)Instruction.LEAVETRY:
                            if (this._trace) this.Trace("leavetry", null, curPos, stackp);
                            if (exceptionp == 0)
                            {
                                throw new Exception("Illegal operation: leavetry on non try block");
                            }
                            info = exceptionStack[exceptionp - 1];
                            info.Phase = TryCatchPhase.InsideFinally;
                            code.Jump(info.FinallyOffset);
                            continue;
                        case (int)Instruction.LEAVECATCH:
                            if (this._trace) this.Trace("leavecatch", null, curPos, stackp);
                            if (exceptionp == 0)
                            {
                                throw new Exception("Illegal operation: leavecatch on non try block");
                            }
                            info = exceptionStack[exceptionp - 1];
                            info.ExceptionObject = null;
                            info.Phase = TryCatchPhase.InsideFinally;
                            code.Jump(info.FinallyOffset);
                            continue;
                        case (int)Instruction.LEAVEFINALLY:
                            if (this._trace) this.Trace("leavefinally", null, curPos, stackp);
                            if (exceptionp == 0)
                            {
                                throw new Exception("Illegal operation: leavefinally on non try block");
                            }
                            info = exceptionStack[exceptionp - 1];
                            exceptionp--;
                            switch (info.ExitAction)
                            {
                                case TryExitAction.Throw:
                                    #region Process throw
                                    exceptionObject = info.ExceptionObject;
                                    if (exceptionp == 0)
                                    {
                                        throw new Exception("Illegal operation: throw on non try block");
                                    }

                                    info = exceptionStack[exceptionp - 1];
                                    switch (info.Phase)
                                    {
                                        case TryCatchPhase.InsideTry: // inside try                                    
                                            callstackp = info.CallPointer;
                                            stackp = info.StackPointer;
                                            datap = info.DataPointer;
                                            module = info.Module;
                                            stack[stackp++] = exceptionObject;
                                            info.Phase = TryCatchPhase.InsideCatch;
                                            code.Jump(info.CatchOffset);
                                            break;
                                        case TryCatchPhase.InsideCatch: // inside catch
                                            info.ExitAction = TryExitAction.Throw;
                                            info.ExceptionObject = exceptionObject;
                                            callstackp = info.CallPointer;
                                            stackp = info.StackPointer;
                                            datap = info.DataPointer;
                                            module = info.Module;
                                            info.Phase = TryCatchPhase.InsideFinally;
                                            code.Jump(info.FinallyOffset);
                                            break;
                                        case TryCatchPhase.InsideFinally: // inside finally
                                            exceptionp--;
                                            if (exceptionp == 0)
                                            {
                                                throw new Exception("Illegal operation: throw on non try block");
                                            }
                                            info = exceptionStack[exceptionp - 1];
                                            callstackp = info.CallPointer;
                                            stackp = info.StackPointer;
                                            datap = info.DataPointer;
                                            module = info.Module;
                                            stack[stackp++] = exceptionObject;
                                            code.Jump(info.CatchOffset);
                                            break;
                                    }
                                    #endregion
                                    break;                                
                                case TryExitAction.Ret:
                                    #region Process return
                                    if (exceptionp > 0)
                                    {
                                        info = exceptionStack[exceptionp - 1];
                                        if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.Ret;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                    stackp = datap;
                                    module = callstack[--callstackp];
                                    code.Seek(callstack[--callstackp]);
                                    datap = callstack[--callstackp];
                                    calltracep--;
                                    #endregion
                                    break;
                                case TryExitAction.RetV:
                                    #region Process return with value
                                    if (exceptionp > 0)
                                    {
                                        info = exceptionStack[exceptionp - 1];
                                        if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.RetV;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                    stack[datap] = stack[stackp - 1]; // compress stack with result copied
                                    stackp = datap + 1;
                                    module = callstack[--callstackp];
                                    code.Seek(callstack[--callstackp]);
                                    datap = callstack[--callstackp];
                                    calltracep--;                                   
                                    #endregion
                                    break;
                                case TryExitAction.RetC:
                                    #region Process return for constructor
                                    if (exceptionp > 0)
                                    {
                                        info = exceptionStack[exceptionp - 1];
                                        if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                        {
                                            info.ExitAction = TryExitAction.RetC;
                                            code.Jump(info.FinallyOffset);
                                            continue;
                                        }
                                    }
                                    stackp = datap + 1;
                                    module = callstack[--callstackp];
                                    code.Seek(callstack[--callstackp]);
                                    datap = callstack[--callstackp];
                                    calltracep--;
                                    #endregion
                                    break;
                                case TryExitAction.Branch:
                                    #region Process branching
                                    if (exceptionp > 0)
                                    {
                                        info = exceptionStack[exceptionp - 1];
                                        if (code.Position > info.TryOffset && code.Position < info.FinallyOffset)
                                        {
                                            if (code.Position + n < info.TryOffset || code.Position + n > info.FinallyOffset)
                                            {
                                                info.ExitAction = TryExitAction.Branch;
                                                info.BranchTo = n;
                                                code.Jump(info.FinallyOffset);
                                                continue;
                                            }
                                        }
                                    }
                                    info.ExitAction = TryExitAction.None;
                                    info.BranchTo = 0;
                                    code.Branch(info.BranchTo);                                    
                                    #endregion
                                    break;
                            }      
                            continue;
                        case (int)Instruction.THROW:
                            if (this._trace) this.Trace("throw", null, curPos, stackp);
                            if (exceptionp == 0)
                            {
                                throw new Exception("Illegal operation: throw on non try block");
                            }
                            info = exceptionStack[exceptionp - 1];
                            exceptionObject = stack[stackp - 1];
                            info.ExitAction = TryExitAction.Throw;                            
                            info.ExceptionObject = exceptionObject;

                            if (exceptionObject is ClassInstance && ((ClassInstance)exceptionObject).Definition.ClassName == "Exception")
                            {
                                ((ClassInstance)exceptionObject).Fields[0] = this.BuildStackTrace(callTrace, calltracep);
                            }
                            switch (info.Phase)
                            {
                                case TryCatchPhase.InsideTry: // inside try                                
                                    callstackp = info.CallPointer;
                                    stackp = info.StackPointer;
                                    datap = info.DataPointer;
                                    module = info.Module;
                                    stack[stackp++] = exceptionObject;
                                    info.Phase = TryCatchPhase.InsideCatch;
                                    code.Jump(info.CatchOffset);
                                    break;
                                case TryCatchPhase.InsideCatch: // inside catch
                                    callstackp = info.CallPointer;
                                    stackp = info.StackPointer;
                                    datap = info.DataPointer;
                                    module = info.Module;
                                    info.Phase = TryCatchPhase.InsideFinally;
                                    code.Jump(info.FinallyOffset);
                                    break;
                                case TryCatchPhase.InsideFinally: // inside finally
                                    exceptionp--;
                                    if (exceptionp == 0)
                                    {
                                        throw new Exception("Illegal operation: throw on non try block");
                                    }
                                    info = exceptionStack[exceptionp - 1];
                                    callstackp = info.CallPointer;
                                    stackp = info.StackPointer;
                                    datap = info.DataPointer;
                                    module = info.Module;
                                    stack[stackp++] = exceptionObject;
                                    code.Jump(info.CatchOffset);
                                    break;
                            }
                            continue;
                        case (int)Instruction.RETHROW:
                            if (this._trace) this.Trace("rethrow", null, curPos, stackp);
                            if (exceptionp == 0)
                            {
                                throw new Exception("Illegal operation: rethrow on non try block");
                            }
                            info = exceptionStack[exceptionp - 1];
                            if (info.Phase != TryCatchPhase.InsideCatch)
                            {
                                throw new Exception("Illegal operation: rethrow on non catch block");
                            }
                            callstackp = info.CallPointer;
                            stackp = info.StackPointer;
                            datap = info.DataPointer;
                            module = info.Module;
                            info.Phase = TryCatchPhase.InsideFinally;
                            code.Jump(info.FinallyOffset);
                            continue;
                        case (int)Instruction.ISINSTANCE:
                            if (this._trace) this.Trace("isinstance", null, curPos, stackp);
                            data = stack[stackp - 2];
                            definition = stack[stackp - 1] as ClassDefinition;
                            if (definition == null)
                            {
                                throw new Exception("Illegal operation for isinstance: expects class: " + stack[stackp - 1].GetType().Name);
                            }
                            bool match = false;
                            if (data is ClassInstance)
                            {
                                match = this.IsInstanceOf(((ClassInstance)data).Definition, definition);
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                match = this._implicitClasses[data.GetType().Name] == definition;
                            }
                            else
                            {
                                throw new Exception("Illegal operation for isinstance: unknown class: " + data.GetType().Name);
                            }
                            stackp--;
                            stack[stackp - 1] = match;
                            continue;
                        case (int)Instruction.SUPER:
                            if (this._trace) this.Trace("super", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is ClassInstance)
                            {
                                stack[stackp - 1] = this.GetParentInstance((ClassInstance)data);
                            }
                            else if (data is ClassDefinition)
                            {
                                stack[stackp - 1] = this.GetParentDefinition((ClassDefinition)data);
                            }
                            else
                            {
                                throw new Exception("Illegal operation for super: unknown class: " + data.GetType().Name);
                            }
                            continue;
                        case (int)Instruction.DUP:
                            if (this._trace) this.Trace("dup", null, curPos, stackp);
                            stack[stackp] = stack[stackp - 1];
                            stackp++;
                            continue;
                        case (int)Instruction.LOADMODULE:
                            if (this._trace) this.Trace("loadmodule", null, curPos, stackp);
                            string moduleName = (string)stack[stackp - 1];
                            stackp--;
                            this.LoadModule(moduleName);
                            this.ExpandAllClassInheritance();
                            continue;
                        case (int)Instruction.CLASSNAME:
                            if (this._trace) this.Trace("classname", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is ClassInstance)
                            {
                                stack[stackp - 1] = ((ClassInstance)data).Definition.ClassName;
                            }
                            else if (data is ClassDefinition)
                            {
                                stack[stackp - 1] = ((ClassDefinition)data).ClassName;
                            }
                            else
                            {
                                stack[stackp - 1] = data.GetType().Name;
                            }
                            continue;
                        case (int)Instruction.HIERARCHY:
                            if (this._trace) this.Trace("hierarchy", null, curPos, stackp);
                            data = stack[stackp - 1];
                            definition = null;
                            if (data is ClassInstance)
                            {
                                definition = ((ClassInstance)data).Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = ((ClassDefinition)data);
                            }
                            if (definition != null)
                            {
                                stack[stackp++] = this.BuildInheritanceHierarchy(definition);
                            }
                            else
                            {
                                stack[stackp++] = new string[1] { data.GetType().Name };
                            }
                            continue;
                        case (int)Instruction.STATICFIELDS:
                            if (this._trace) this.Trace("staticfields", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is ClassInstance)
                            {
                                stack[stackp - 1] = this.GetFields(((ClassInstance)data).Definition.FieldMap, true);
                            }
                            else if (data is ClassDefinition)
                            {
                                stack[stackp - 1] = this.GetFields(((ClassDefinition)data).FieldMap, true);
                            }
                            else
                            {
                                throw new Exception("Not a class/instance");
                            }
                            continue;
                        case (int)Instruction.FIELDS:
                            if (this._trace) this.Trace("fields", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is ClassInstance)
                            {
                                stack[stackp - 1] = this.GetFields(((ClassInstance)data).Definition.FieldMap, false);
                            }
                            else if (data is ClassDefinition)
                            {
                                stack[stackp - 1] = this.GetFields(((ClassDefinition)data).FieldMap, false);
                            }
                            else
                            {
                                throw new Exception("Not a class/instance");
                            }
                            continue;
                        case (int)Instruction.STATICMETHODS:
                            if (this._trace) this.Trace("staticmethods", null, curPos, stackp);
                            data = stack[stackp - 1];
                            if (data is ClassInstance)
                            {
                                stack[stackp - 1] = this.GetMethods(((ClassInstance)data).Definition.MethodMap, true);
                            }
                            else if (data is ClassDefinition)
                            {
                                stack[stackp - 1] = this.GetMethods(((ClassDefinition)data).MethodMap, true);
                            }
                            else
                            {
                                throw new Exception("Not a class/instance");
                            }
                            continue;
                        case (int)Instruction.METHODS:
                            data = stack[stackp - 1];
                            if (data is ClassInstance)
                            {
                                stack[stackp - 1] = this.GetMethods(((ClassInstance)data).Definition.MethodMap, false);
                            }
                            else if (data is ClassDefinition)
                            {
                                stack[stackp - 1] = this.GetMethods(((ClassDefinition)data).MethodMap, false);
                            }
                            else
                            {
                                throw new Exception("Not a class/instance");
                            }
                            continue;
                        case (int)Instruction.CREATEINSTANCE:
                            if (this._trace) this.Trace("createinstance", null, curPos, stackp);
                            string className = (string)stack[stackp - 1];
                            if (!this._classes.ContainsKey(className))
                            {
                                throw new Exception(string.Format("CREATEINSTANCE: class '{0}' not found", className));
                            }
                            definition = this._classes[className];
                            instance = new ClassInstance();
                            instance.Definition = definition;
                            if (definition.IsFacade)
                            {
                                instance.Fields = null;
                                instance.FacadeInstance = (IFacadeClass)Activator.CreateInstance(definition.FacadeClassType);
                            }
                            else
                            {
                                instance.Fields = new object[definition.FieldCount];
                                instance.FacadeInstance = null;
                            }
                            stack[stackp - 1] = instance;
                            continue;
                        case (int)Instruction.GETFIELD:
                            if (this._trace) this.Trace("getfield", null, curPos, stackp);
                            data = stack[stackp - 2];
                            instance = null;
                            fieldName = (string)stack[stackp - 1];
                            stackp--;
                            if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                                definition = instance.Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = (ClassDefinition)data;
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                // convert to implicit class instance
                                definition = this._implicitClasses[data.GetType().Name];
                                instance = new ClassInstance();
                                instance.Definition = definition;
                                instance.Fields = new object[1];
                                instance.Fields[0] = data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (!definition.FieldMap.ContainsKey(fieldName))
                            {
                                throw new Exception(string.Format("Unresolved field '{0}' on instance.", fieldName));
                            }

                            fieldNumber = definition.FieldMap[fieldName][0] - 1;
                            staticField = definition.FieldMap[fieldName][1] == 1;

                            if (!staticField && instance == null)
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (definition.IsFacade)
                            {// TODO
                                if (this._trace) Console.WriteLine("calling facade getvalue({0})", fieldName);
                                if (staticField)
                                {
                                    stack[stackp - 1] = definition.FacadeInstance.GetField(fieldName);
                                }
                                else
                                {
                                    stack[stackp - 1] = instance.FacadeInstance.GetField(fieldName);
                                }
                            }
                            else
                            {
                                if (staticField)
                                {
                                    stack[stackp - 1] = definition.Fields[fieldNumber];
                                }
                                else
                                {
                                    stack[stackp - 1] = instance.Fields[fieldNumber];
                                }
                            }
                            continue;
                        case (int)Instruction.SETFIELD:
                            if (this._trace) this.Trace("setfield", null, curPos, stackp);
                            data = stack[stackp - 3];
                            instance = null;
                            fieldName = (string)stack[stackp - 2];
                            if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                                definition = instance.Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = (ClassDefinition)data;
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                // convert to implicit class instance
                                definition = this._implicitClasses[data.GetType().Name];
                                instance = new ClassInstance();
                                instance.Definition = definition;
                                instance.Fields = new object[1];
                                instance.Fields[0] = data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (!definition.FieldMap.ContainsKey(fieldName))
                            {
                                throw new Exception(string.Format("Unresolved field '{0}' on instance.", fieldName));
                            }

                            fieldNumber = definition.FieldMap[fieldName][0] - 1;
                            staticField = definition.FieldMap[fieldName][1] == 1;

                            if (!staticField && instance == null)
                            {
                                throw new Exception(string.Format("Field access of '{0}' on a non-instance: {1}", fieldName, data.GetType().Name));
                            }

                            if (definition.IsFacade)
                            {
                                if (this._trace) Console.WriteLine("calling facade setvalue({0}, {1})", fieldName, stack[stackp - 1]);
                                if (staticField)
                                {
                                    definition.FacadeInstance.SetField(fieldName, stack[stackp - 1]);
                                }
                                else
                                {
                                    instance.FacadeInstance.SetField(fieldName, stack[stackp - 1]);
                                }
                            }
                            else
                            {
                                if (staticField)
                                {
                                    definition.Fields[fieldNumber] = stack[stackp - 1];
                                }
                                else
                                {
                                    instance.Fields[fieldNumber] = stack[stackp - 1];
                                }
                            }
                            stackp += 2;
                            continue;
                        case (int)Instruction.INVOKEMETHOD: // PENDING                           
                            m = code.Position;
                            if (this._trace) this.Trace("invokemethod", null, curPos, stackp);

                            // resolve method dynamically
                            methodSignature = (string)stack[stackp - 1];
                            stackp--;
                            arguments = this.ParseMethodArguments(methodSignature);
                            data = stack[stackp - arguments - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving signature: {0} <arguments: {1}>", methodSignature, arguments);
                            }

                            instance = null;
                            definition = null;

                            if (data == null)
                            {
                                throw new Exception(string.Format("Invokation of method on null object: {0}", methodSignature));
                            }
                            else if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                                definition = instance.Definition;
                            }
                            else if (data is ClassDefinition)
                            {
                                definition = (ClassDefinition)data;
                            }
                            else if (this._implicitClasses.ContainsKey(data.GetType().Name))
                            {
                                // convert to implicit class instance
                                definition = this._implicitClasses[data.GetType().Name];
                                instance = new ClassInstance();
                                instance.Definition = definition;
                                instance.Fields = new object[1];
                                instance.Fields[0] = data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Invocation of '{0}' on a non-class/non-instance: {1}", methodSignature, data.GetType().Name));
                            }

                            if (this._trace)
                            {
                                if (!definition.MethodMap.ContainsKey(methodSignature))
                                {
                                    Console.WriteLine("unresolved method '{1}' in instance of '{0}':", definition.ClassName, methodSignature);
                                    foreach (string signature in definition.MethodMap.Keys)
                                    {
                                        Console.WriteLine("found only: {0}", signature);
                                    }
                                }
                            }

                            if (!definition.MethodMap.ContainsKey(methodSignature))
                            {
                                throw new Exception(string.Format("Unresolved method '{0}' on '{1}'.", methodSignature, definition.ClassName));
                            }

                            methodInfo = definition.MethodMap[methodSignature];
                            methodModule = methodInfo[0];
                            methodArguments = methodInfo[1];
                            methodVariables = methodInfo[2];
                            methodCodeAddress = methodInfo[3];
                            staticMethod = methodInfo[4] == 1;

                            if (!staticMethod && instance == null)
                            {
                                throw new Exception("Calling method on a non instance.");
                            }

                            if (definition.IsFacade) // if is facade, call and continue
                            {
                                // TODO: need to do for static method here!

                                if (this._trace) Console.WriteLine("calling facade callmethod({0}) with {1} arguments", methodSignature, methodArguments);
                                // prepare argument stacks
                                object[] argumentStack = new object[methodArguments];
                                for (int i = 0; i < methodArguments; i++)
                                {
                                    if (this._trace) Console.WriteLine("argument {0} = {1}", i + 1, stack[stackp - methodArguments + i]);
                                    argumentStack[i] = stack[stackp - methodArguments + i];
                                }
                                object result = null;
                                if (staticMethod)
                                {
                                    result = definition.FacadeInstance.CallMethod(methodSignature, argumentStack);
                                }
                                else
                                {
                                    result = instance.FacadeInstance.CallMethod(methodSignature, argumentStack);
                                }
                                stackp -= methodArguments;
                                stack[stackp - 1] = result; // TODO: pending test!
                            }
                            else
                            {
                                if (this._trace)
                                {
                                    Console.WriteLine("resolved method: module: {0}, arguments: {1}, variables: {2}, address: {3:0000}", methodModule, methodArguments, methodVariables, methodCodeAddress);
                                }

                                callstack[callstackp++] = datap; // datap 
                                callstack[callstackp++] = m; // codep
                                callstack[callstackp++] = module; // module
                                callTrace[calltracep++] = definition.ClassName + "::" + methodSignature;

                                module = methodModule;
                                code.Seek(methodCodeAddress);
                                datap = stackp - methodArguments - 1;
                                stackp += methodVariables; // allocate stack for local variables
                            }
                            continue;
                        case (int)Instruction.INVOKECONSTRUCTOR:
                            m = code.Position;
                            if (this._trace) this.Trace("invokecontructor", null, curPos, stackp);
                            // resolve method dynamically
                            methodSignature = (string)stack[stackp - 1];
                            arguments = this.ParseMethodArguments(methodSignature);
                            stackp--;
                            data = stack[stackp - arguments - 1];

                            if (this._trace)
                            {
                                Console.WriteLine("resolving signature: {0} <arguments: {1}>", methodSignature, arguments);
                            }

                            instance = null;

                            if (data is ClassInstance)
                            {
                                instance = (ClassInstance)data;
                            }
                            else
                            {
                                throw new Exception(string.Format("Invokation of '{0}' on a non-instance: {1}", methodSignature, data.GetType().Name));
                            }

                            if (!instance.Definition.MethodMap.ContainsKey(methodSignature))
                            {
                                if (methodSignature == "__constructor(0)")
                                {
                                    if (this._trace)
                                    {
                                        Console.WriteLine("no constructor defined");
                                    }
                                    continue;
                                }
                                else
                                {
                                    throw new Exception(string.Format("Unresolved constructor '{0}' for class '{1}'", methodSignature, instance.Definition.ClassName));
                                }
                            }

                            if (instance.Definition.IsFacade) // not possible!
                            {
                                throw new Exception("Illegal CALLC on facade class.");
                            }

                            methodInfo = instance.Definition.MethodMap[methodSignature];
                            methodModule = methodInfo[0];
                            methodArguments = methodInfo[1];
                            methodVariables = methodInfo[2];
                            methodCodeAddress = methodInfo[3];

                            if (this._trace)
                            {
                                Console.WriteLine("resolved method: module: {0}, arguments: {1}, variables: {2}, address: {3:0000}", methodModule, methodArguments, methodVariables, methodCodeAddress);
                            }

                            callstack[callstackp++] = datap; // datap 
                            callstack[callstackp++] = m; // codep
                            callstack[callstackp++] = module; // module
                            callTrace[calltracep++] = instance.Definition.ClassName + "::" + methodSignature;

                            module = methodModule;
                            code.Seek(methodCodeAddress);
                            datap = stackp - methodArguments - 1;
                            stackp += methodVariables; // allocate stack for local variables
                            continue;
                        case (int)Instruction.VERSION:
                            if (this._trace) this.Trace("version", null, curPos, stackp);
                            stack[stackp++] = "Oriole Runtime " + Program.VERSION;
                            continue;
                        case (int)Instruction.FORK:
                            if (this._trace) this.Trace("fork", null, curPos, stackp);
                            
                            ThreadContext threadContext = new ThreadContext();
                            threadContext.stack = (object[])stack.Clone();
                            threadContext.callstack = (int[])callstack.Clone();
                            threadContext.exceptionStack = (ExceptionInfo[])exceptionStack.Clone();
                            threadContext.callTrace = (string[])callTrace.Clone();
                            threadContext.stackp = stackp;
                            threadContext.exceptionp = exceptionp;
                            threadContext.callstackp = callstackp;
                            threadContext.calltracep = calltracep;
                            threadContext.datap = datap;
                            threadContext.module = module;
                            threadContext.codep = code.Position;
                            threadContext.stack[threadContext.stackp++] = 0; // child thread
                            threads.Add(threadContext);

                            stack[stackp++] = threadContext.threadid; // parent thread
                            continue;
                        case (int)Instruction.EXIT:
                            if (this._trace) this.Trace("exit", null, curPos, stackp);
                            if (threads.Count > 1)
                            {
                                currentThread.state = ThreadState.EXITING;
                                this._ticked = true;
                            }
                            else
                            {
                                if (stack[fieldIndex] != null)
                                {
                                    Console.WriteLine("Exception: " + stack[fieldIndex].ToString());
                                }
                                if (this._showStatistics)
                                {
                                    Console.WriteLine("\n==========================================================================");
                                    Console.WriteLine("thread exits: datap={0} stackp={1} stack[{4}]={2} stack[0]={3}", datap, stackp, stack[stackp - 1], stack[0], stackp - 1);
                                    DateTime stopTime = DateTime.Now;
                                    Console.WriteLine("total instruction executed: {0} execution time: {1}", instructionsExecuted, (stopTime - startTime));
                                }
                                return;
                            }
                            continue;
                        case (int)Instruction.JOIN:
                            if (this._trace) this.Trace("join", null, curPos, stackp);
                            int threadToWait = (int)stack[stackp - 1];
                            stackp--;
                            currentThread.state = ThreadState.JOINING;
                            currentThread.threadToJoin = threadToWait;
                            this._ticked = true;
                            continue;
                        case (int)Instruction.CURTHREAD:
                            if (this._trace) this.Trace("curthread", null, curPos, stackp);
                            stack[stackp++] = threadid;
                            continue;
                        case (int)Instruction.SLEEP:
                            if (this._trace) this.Trace("sleep", null, curPos, stackp);
                            int milliseconds = (int)stack[stackp - 1];
                            stackp--; 
                            currentThread.state = ThreadState.SLEEPING;
                            currentThread.sleepUntil = this._ticks + milliseconds * 10000L;
                            this._ticked = true;
                            continue;
                        case (int)Instruction.NICE:
                            if (this._trace) this.Trace("nice", null, curPos, stackp);
                            this._ticked = true;
                            continue;
                        case (int)Instruction.WAIT:
                            if (this._trace) this.Trace("wait", null, curPos, stackp);
                            data = stack[stackp - 1];
                            stackp--;
                            if (!(data is ClassDefinition || data is ClassInstance))
                            {
                                throw new Exception("Illegal operation of WAIT on non-object operand: " + data.GetType().Name);
                            }
                            if (semaphores.Contains(data))
                            {
                                currentThread.state = ThreadState.WAITING;
                                currentThread.semaphoreToWait = data;
                                this._ticked = true;
                            }
                            else
                            {
                                semaphores.Add(data);
                            }
                            continue;
                        case (int)Instruction.SIGNAL:
                            if (this._trace) this.Trace("signal", null, curPos, stackp);
                            data = stack[stackp - 1];
                            stackp--;
                            if (!(data is ClassDefinition || data is ClassInstance))
                            {
                                throw new Exception("Illegal operation of SIGNAL on non-object operand: " + data.GetType().Name);
                            }
                            if (semaphores.Contains(data))
                            {
                                semaphores.Remove(data);
                            }
                            else
                            {
                                throw new Exception("Illegal operation of RELEASE releasing unlock object.");
                            }
                            continue;
                        default:
                            throw new Exception(string.Format("Illegal code: {0}", oper));
                    }
                }
                #endregion
            }
            finally
            {
                this._active = false;
                timerTicker.Join();
            }
        }
    }
} 
