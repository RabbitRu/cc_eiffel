using Lexer;
using Syntaxer.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace Syntaxer
{
    class Syntaxer
    {
        public List<Token> Source { get; private set; }
        private int _currentTokenNumber = 0;
        public Token CurrentToken { get; private set; }
        public RootNode Root = new RootNode();
        private ClassNode ClassWAddonsGlobal { get; set; } = null;
        private Stack<int> States { get; } = new Stack<int>();

        private void saveState()
        {
            States.Push(_currentTokenNumber);
        }

        private Token loadState()
        {
            _currentTokenNumber = States.Pop();
            CurrentToken = Source[_currentTokenNumber];
            return CurrentToken;
        }

        private void popState()
        {
            try
            {
                States.Pop();
            }
            catch (Exception )
            {
                Debug.Print("Too much pop");
            }
        }

        private Token getNextToken()
        {
            if (_currentTokenNumber >= Source.Count)
            {
                CurrentToken = null;
                return null;
            }

            CurrentToken = Source[_currentTokenNumber];
            _currentTokenNumber++;
            return CurrentToken;
        }

        public void BuildAST(List<Token> tokens)
        {
            Source = tokens;
            getNextToken();
            while (_currentTokenNumber < tokens.Count)
            {
                Root.Children.Add(ParseExpression());
            }
        }

        public BaseNode ParseExpression()
        {
            BaseNode returnValue = null;

            switch (CurrentToken.Type)
            {
                case TokenType.Constant:
                    returnValue = ParseConstant(CurrentToken);
                    break;
                case TokenType.Delimeter:
                    returnValue = ParseDelimeter(CurrentToken);
                    break;
                case TokenType.Identifier:
                    returnValue = ParseIdentifier(CurrentToken);
                    break;
                case TokenType.ReservedWord:
                    (returnValue, ClassWAddonsGlobal) = ParseReservedWords(CurrentToken, ClassWAddonsGlobal);
                    break;
                default:
                    getNextToken();
                    break;
            }

            return returnValue;
        }

        private (BaseNode, ClassNode) ParseReservedWords(Token token, ClassNode classWAddons = null)
        {
            BaseNode returnValue = null;
            switch (token.Value)
            {
                case "class": //deferred | expanded | frozen
                    getNextToken();
                    returnValue = ParseClass(CurrentToken, classWAddons);
                    break;
                case "frozen": //headers
                case "expanded":
                case "deferred":
                    classWAddons = classWAddons ?? new ClassNode();
                    classWAddons.Headers.Add(token.Value);
                    getNextToken();
                    break;
                case "note":
                    getNextToken();
                    classWAddons = classWAddons ?? new ClassNode();
                    ParseNote(CurrentToken, classWAddons.Notes);
                    break;
                default:
                    getNextToken();
                    break;
            }

            return (returnValue, classWAddons);
        }

        private BaseNode ParseClass(Token token, ClassNode classWNotes = null)
        {
            ClassNode classN;
            classN = classWNotes ?? new ClassNode();

            classN.Name = token.Value;
            getNextToken();

            while (CurrentToken != null && CurrentToken.Value != "end")
            {
                switch (CurrentToken.Value)
                {
                    case "obsolete":
                        getNextToken();
                        classN.Obsolete = ParseObsolete(CurrentToken);
                        break;
                    case "note":
                        getNextToken();
                        ParseNote(CurrentToken, classN.Notes);
                        break;
                    case "inheritance":
                        getNextToken();
                        ParseInheritance(CurrentToken, classN.Inheritance);
                        break;
                    case "creators":
                        getNextToken();
                        ParseCreators(CurrentToken, classN.Creators);
                        break;
                    case "converters":
                        getNextToken();
                        ParseConverters(CurrentToken, classN.Converters);
                        break;
                    case "features":
                        getNextToken();
                        ParseFeatures(CurrentToken, classN.Features);
                        break;
                    case "invariant":
                        getNextToken();
                        break;
                    default:
                        ParseExpression();
                        break;
                }
            }

            ClassWAddonsGlobal = null;
            return classN;
        }

        private void ParseFeatures(Token currentToken, List<FeatureDeclarationNode> classNFeatures)
        {
            if (CurrentToken.Value == "feature" && currentToken.Type == TokenType.ReservedWord)
            {
                List<FeatureDeclarationNode> fdNode = ParseList(";", ParseFeatureDeclaration);
            }
        }

        private FeatureDeclarationNode ParseFeatureDeclaration(Token arg)
        {
            FeatureDeclarationNode result = new FeatureDeclarationNode();
            List<(bool frozen, FeatureNameNode FeatureName)> nfNode = ParseList(",", //NewFeature
                delegate
                {
                    bool frozen = false;
                    if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "frozen")
                    {
                        frozen = true;
                        getNextToken();
                    }

                    return (frozen, (FeatureNameNode) ParseBraces("{", "}", ParseTypeList));
                });

            result.NewFeatureList = nfNode;

            saveState();
            var fArgs = ParseFormalArguments(CurrentToken);
            if (fArgs == null)
                loadState();
            else
                popState();

            saveState();
            Func<(TypeNode, FeatureNameNode)> qMarkFunc = () => //QueryMark
            {
                saveState();
                if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == ":")
                {
                    getNextToken();
                }
                else
                {
                    loadState();
                    return (null, null);
                }

                TypeNode tNode = ParseType(CurrentToken);
                if (tNode == null)
                {
                    loadState();
                    return (null, null);
                }
                else
                {
                    popState();
                }

                FeatureNameNode fNode = null;
                saveState();
                if (CurrentToken.Value == "assign" && CurrentToken.Type == TokenType.ReservedWord)
                {
                    getNextToken();
                    fNode = ParseFeatureName(CurrentToken);
                    if (fNode == null)
                    {
                        loadState();
                    }
                }
                else
                {
                    loadState();
                }

                return (tNode, fNode);
            };
            var qMark = qMarkFunc.Invoke();
            result.QueryMarkType = qMark.Item1;
            result.QueryMarkAssigner = qMark.Item2;

            saveState(); //ExplicitValue
            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "=")
            {
                getNextToken();
                if (CurrentToken.Type == TokenType.Constant)
                {
                    result.ExplicitValue = CurrentToken.Value;
                    popState();
                }

                loadState();
            }

            if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "obsolete")
            {
                getNextToken();
                result.Obsolete = ParseObsolete(CurrentToken);
            }

            //Attribure o routine
            ParseFeatureBody(CurrentToken);

            return null;
        }

        private void ParseFeatureBody(Token currentToken)
        {
            bool deffered = false;
            if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "local")
            {
                getNextToken();
                var x = ParseEntityDeclarationsList(CurrentToken);
            }

            if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "deffered")
            {
                getNextToken();
                deffered = true;
            }

            //External нету
            if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "do") //Routine Mark
            {
                getNextToken();
            }
            else if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "once")
            {
                getNextToken();
                List<string> x = (List<string>) ParseBraces("(", ")",
                    delegate { return ParseList(",", arg => CurrentToken.Value); });
            }
            else
            {
                return;
            }

            ParseCompound(CurrentToken);
        }

        private void ParseCompound(Token token)
        {
            var x = ParseList(";",
                delegate { return ParseInstruction(CurrentToken); });
        }

        private object ParseInstruction(Token token)
        {
            //Creation Instruction
            TypeNode ExplicitCreationType;
            if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "create")
            {
                getNextToken();
                ExplicitCreationType = (TypeNode) ParseBraces("{", "}", ParseType);
            }

            //Variable
            FeatureNameNode Variable = ParseFeatureNameOrReservedWord(CurrentToken, "Result");

            object x;
            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == ".")
            {
                getNextToken();
                x = ParseUnqualifiedCall(CurrentToken);
            }


            return null;
        }

        private object ParseUnqualifiedCall(Token token)
        {
            FeatureNameNode fNode = ParseFeatureName(CurrentToken);
            object AArgs = ParseActualArguments(CurrentToken);
            return null;
        }

        private object ParseActualArguments(Token currentToken)
        {
            return ParseBraces("(", ")", ParseExpression);
        }

        private ExpressionNode ParseExpression(Token arg)
        {
            if (true)//Basic
            {
                //ReadOnly
                FeatureNameNode fNode = ParseFeatureNameOrReservedWord(CurrentToken, "Current");
                //Local
                if (fNode == null)
                {
                    fNode = ParseFeatureNameOrReservedWord(CurrentToken, "Result");
                }

                if (fNode == null)
                {
                    ParseFeatureCall(CurrentToken);
                }
            }
            else//Special
            {

            }
            return null;
        }

        private BaseNode ParseFeatureCall(Token token)
        {
            //ObjectCall
            //Target
            //ReadOnly
            saveState();
            BaseNode targetNode = ParseFeatureNameOrReservedWord(CurrentToken, "Current");
            //Local
            if (targetNode == null)
            {
                loadState();
                saveState();
                targetNode = ParseFeatureNameOrReservedWord(CurrentToken, "Result");
            }
            //Call
            if (targetNode == null)
            {
                loadState();
                saveState();
                targetNode = ParseFeatureCall(CurrentToken);
            }
            //Parenthesized_target
            if (targetNode == null)
            {
                loadState();
                saveState();
                targetNode = (BaseNode) ParseBraces("(", ")", ParseExpression);
            }

            if (targetNode != null)
            {
                popState();
                if (CurrentToken.Value == "." && CurrentToken.Type == TokenType.Delimeter)
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("FeatureCalls parse error");
                    return null;
                }
            }
            else
            {
                loadState();
            }

            FeatureNameNode fNode = ParseFeatureName(CurrentToken);
            object actuals = ParseActualArguments(CurrentToken);

        }

        private FeatureNameNode ParseFeatureNameOrReservedWord(Token currentToken, string reservedWord)
        {//Identifier\FeatureName\ReservedWord
            FeatureNameNode fNode = ParseFeatureName(CurrentToken);
            if (CurrentToken.Value == reservedWord && CurrentToken.Type == TokenType.ReservedWord)
            {
                fNode = new FeatureNameNode();
                fNode.Name = reservedWord;
            }
            else
            {
                Debug.Print("Identifier\\FeatureName\\ReservedWord parse error");
                return null;
            }

            return fNode;
        }

        private void ParseConverters(Token currentToken, List<(FeatureNameNode, List<TypeNode>)> classNConverters)
        {
            classNConverters = ParseList(",", ParseConverter);
        }

        private (FeatureNameNode, List<TypeNode>) ParseConverter(Token arg)
        {
            FeatureNameNode fNode = ParseFeatureName(CurrentToken);
            List<TypeNode> tNode = null;
            if (CurrentToken.Value == ":" && CurrentToken.Type == TokenType.Delimeter)
            {
                getNextToken();
                tNode = (List<TypeNode>) ParseBraces("{", "}", ParseTypeList);
            }
            else
            {
                tNode = (List<TypeNode>) ParseBraces("(", ")",
                    delegate { return ParseBraces("{", "}", ParseTypeList); });
            }

            return (fNode, tNode);
        }

        private void ParseCreators(Token currentToken, List<FeatureNameNode> classNCreators)
        {
            if (currentToken.Type == TokenType.ReservedWord && currentToken.Value == "create")
            {
                classNCreators = ParseList(",", ParseFeatureName);
            }
        }

        private void ParseInheritance(Token token, ClassNode.InheritanceNode inheritance)
        {
            if (token.Type == TokenType.Delimeter && token.Value == "{")
            {
                getNextToken();
                if (token.Type == TokenType.Identifier && token.Value == "NONE")
                {
                    getNextToken();
                    if (token.Type == TokenType.Delimeter && token.Value == "}")
                    {
                        inheritance.NonConformance = true;
                        getNextToken();
                    }
                    else
                    {
                        Debug.Print("Inheritance Non_conformance parse error");
                    }
                }
                else
                {
                    Debug.Print("Inheritance Non_conformance parse error");
                }

                inheritance.ParentList = ParseList(";", ParseClassTypeNode);
            }

            while (true)
            {
                ParseType(CurrentToken);
            }
        }

        private TypeNode ParseType(Token token)
        {
            TypeNode tNode = new TypeNode();

            saveState();
            //TupleType
            TupleNode tuple = ParseTupleNode(token);

            if (tuple == null)
            {
                loadState();
                saveState();

                //ClassType
                ClassTypeNode classType = ParseClassTypeNode(token);
                if (classType == null)
                {
                    //Anchored
                    tNode.AttachmentMark = ParseAttachmentMark(CurrentToken);

                    if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "like")
                    {
                    }
                    else
                    {
                        Debug.Print("Types anchored parse error");
                        return null;
                    }

                    if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "Current")
                    {
                        tNode.Anchor = new FeatureNameNode {Name = "Current"};
                    }
                    else
                    {
                        tNode.Anchor = ParseFeatureName(CurrentToken);
                        if (tNode.Anchor == null)
                        {
                            Debug.Print("Types anchored parse error");
                            return null;
                        }
                    }
                }
            }

            return tNode;
        }

        private ClassTypeNode ParseClassTypeNode(Token token)
        {
            ClassTypeNode ctNode = new ClassTypeNode();
            ctNode.AttachmentMark = ParseAttachmentMark(CurrentToken);
            if (CurrentToken.Type == TokenType.Identifier)
            {
                ctNode.ClassName = CurrentToken.Value;
                getNextToken();
            }
            else
            {
                Debug.Print("Types class_type parse error");
                return null;
            }

            return ctNode;
        }

        private TupleNode ParseTupleNode(Token token)
        {
            TupleNode tpl = new TupleNode();
            if (token.Type == TokenType.ReservedWord && token.Value == "TUPLE")
            {
                getNextToken();
            }
            else
            {
                Debug.Print("Tuple parse error");
                return null;
            }

            object x = null;
            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "[")
            {
                while (CurrentToken.Type != TokenType.Delimeter && CurrentToken.Value == "]")
                {
                    saveState();
                    x = ParseTypeList(CurrentToken);
                    if (x == null)
                    {
                        loadState();
                        x = ParseEntityDeclarationsList(CurrentToken).EntityDeclarationGroup;
                        if (x == null)
                        {
                            Debug.Print("Tuple parameters parse error");
                            return null;
                        }
                    }
                    else
                    {
                        popState();
                    }
                }
            }

            tpl.TupleParameters = x;
            return tpl;
        }

        private List<TypeNode> ParseTypeList(Token token)
        {
            List<TypeNode> typeList = new List<TypeNode>();
            var firsttime = true;
            do
            {
                if (!firsttime)
                    getNextToken();
                else
                    firsttime = false;

                typeList.Add(ParseType(CurrentToken));
                if (typeList.Last() == null)
                    return null;
            } while (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == ",");

            if (typeList.All(x => x == null))
                typeList = null;

            return typeList;
        }

        private FormalArgumentNode ParseFormalArguments(Token token)
        {
            FormalArgumentNode faNode = new FormalArgumentNode();
            faNode.EntityDeclarationList = new EntityDeclarationList();
            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "(")
            {
                while (CurrentToken.Type != TokenType.Delimeter && CurrentToken.Value == ")")
                {
                    faNode.EntityDeclarationList = ParseEntityDeclarationsList(CurrentToken);
                }
            }

            return faNode;
        }

        private EntityDeclarationList ParseEntityDeclarationsList(Token token)
        {
            var entityDeclarationList = new EntityDeclarationList();
            var firsttime1 = true;
            do
            {
                if (!firsttime1)
                    getNextToken();
                else
                    firsttime1 = false;

                (List<string> IdentifierList, TypeNode Type) entityDeclarationGroup;
                entityDeclarationGroup.IdentifierList = new List<string>();
                var firsttime2 = true;
                do
                {
                    if (!firsttime2)
                        getNextToken();
                    else
                        firsttime2 = false;

                    if (CurrentToken.Type == TokenType.Identifier)
                    {
                        entityDeclarationGroup.IdentifierList.Add(CurrentToken.Value);
                        getNextToken();
                    }
                    else
                    {
                        Debug.Print("EDL parse error");
                        return null;
                    }
                } while (CurrentToken.Type != TokenType.Delimeter && CurrentToken.Value == ",");

                if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == ":")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("EDL parse error");
                    return null;
                }

                entityDeclarationGroup.Type = ParseType(CurrentToken);
                if (entityDeclarationGroup.Type == null)
                    return null;
                entityDeclarationList.EntityDeclarationGroup.Add(entityDeclarationGroup);
            } while (CurrentToken.Type != TokenType.Delimeter && CurrentToken.Value == ";");

            return entityDeclarationList;
        }
        
        private string ParseAttachmentMark(Token token)
        {
            if (token.Type == TokenType.Delimeter && token.Value == "!" ||
                token.Type == TokenType.Delimeter && token.Value == "?")
            {
                //Attachment mark
                var returnValue = CurrentToken.Value;
                getNextToken();
                return returnValue;
            }

            return null;
        }

        private FeatureNameNode ParseFeatureName(Token token)
        {
            var fName = new FeatureNameNode();
            if (CurrentToken.Type == TokenType.Identifier)
            {
                fName.Name = CurrentToken.Value;
                getNextToken();
            }
            else
            {
                Debug.Print("Feature Name Parse Error 1");
                return null;
            }

            while (true)
            {
                if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "alias")
                {
                    getNextToken();
                }
                else
                {
                    break;
                }

                if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "\"")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 2");
                    return null;
                }

                if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "[") //Тут мб проще
                {
                    getNextToken();
                    if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "]")
                    {
                        getNextToken();
                        fName.AliasName = "[]";
                    }
                    else
                    {
                        Debug.Print("Feature Name Parse Error 3");
                        return null;
                    }
                }
                else if (CurrentToken.Type == TokenType.Operator &&
                         ReservedWords.Operators.Any(x => x == CurrentToken.Value))
                {
                    fName.AliasName = CurrentToken.Value;
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 4");
                    return null;
                }

                if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "\"")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 5");
                    return null;
                }

                if (CurrentToken.Type == TokenType.ReservedWord && CurrentToken.Value == "convert")
                {
                    fName.Convert = true;
                    getNextToken();
                }
            }

            return fName;
        }

        private string ParseObsolete(Token token)
        {
            if (CurrentToken.Type == TokenType.Constant)
            {
                var returnValue = CurrentToken.Value;
                getNextToken();
                return returnValue;
            }

            Debug.Print("Мусор после метки obsolete");
            getNextToken();
            return null;
        }

        private void ParseNote(Token token, ClassNode.NoteNode notes)
        {
            string nName;
            List<string> nContent;
            while (CurrentToken.Type == TokenType.Identifier)
            {
                // nName: nContent
                if (CurrentToken.Type == TokenType.Identifier)
                {
                    nName = CurrentToken.Value;
                }
                else
                {
                    Debug.Print("Note parse error1");
                    return;
                }

                getNextToken();

                if (CurrentToken.Type != TokenType.Delimeter || CurrentToken.Value != ":")
                {
                    return;
                }

                getNextToken();
                nContent = new List<string>();

                while (true)
                {
                    if (CurrentToken.Type == TokenType.Constant || CurrentToken.Type == TokenType.Identifier)
                    {
                        nContent.Add(CurrentToken.Value);
                    }
                    else
                    {
                        Debug.Print("Note parse error2");
                        break;
                    }

                    getNextToken();

                    if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == ",")
                    {
                        nContent.Add(CurrentToken.Value);
                        getNextToken();
                    }
                    else
                    {
                        //Норма
                        break;
                    }
                }

                notes.Add(nName, nContent);
            }
        }

        private BaseNode ParseIdentifier(Token identidier)
        {
            string iden = identidier.Value;

            getNextToken(); // получаем идентификатор.

            if (CurrentToken.Value[0] != '(') // Ссылка на переменную.
                return new VariableNode(iden);

            // Вызов функции.
            getNextToken(); // получаем (
            List<BaseNode> args = new List<BaseNode>();
            if (CurrentToken.Value[0] != ')')
            {
                while (true)
                {
                    BaseNode arg = ParseExpression();
                    if (arg == null) return null;
                    args.Add(arg);

                    if (CurrentToken.Value[0] == ')') break;

                    if (CurrentToken.Value[0] != ',')
                    {
                        Debug.Print("Expected ')' or ',' in argument list");
                        return null;
                    }

                    getNextToken();
                }
            }

            // получаем ')'.
            getNextToken();

            return new FunctionNode(new PrototypeNode(iden, args), ParseExpression());
        }

        private BaseNode ParseDelimeter(Token currenToken)
        {
            //throw new NotImplementedException();
            getNextToken();
            return null;
        }

        public BaseNode ParseConstant(Token tk)
        {
            var result = new ConstantValueNode(tk.Value);
            getNextToken();
            return result;
        }

        public BaseNode ParseIdentfierExpr()
        {
            return new VariableNode("", "");
        }

        public int GetOpPriopity(Token tk)
        {
            if (tk.Type != TokenType.Operator)
                throw new Exception();

            return ReservedWords.OpPriority[tk.Value];
        }

        public List<T> ParseList<T>(string delimeter, Func<Token, T> func)
        {
            var result = new List<T>();
            result.Add(func.Invoke(CurrentToken));
            while (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == delimeter)
            {
                result.Add(func.Invoke(CurrentToken));
                getNextToken();
            }

            return result;
        }

        public object ParseBraces<T>(string braceStart, string braceEnd, Func<Token, T> func)
        {
            object result = null;
            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == braceStart)
            {
                getNextToken();
                result = func.Invoke(CurrentToken);
            }
            else
            {
                Debug.Print("Brace print error");
                result = null;
            }

            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == braceEnd)
            {
                result = null;
            }


            return result;
        }
    }
}