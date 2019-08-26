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
            States.Pop();
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
                tNode = (List<TypeNode>) ParseBraces("{","}", ParseTypeList);
            }
            else
            {
                tNode = (List<TypeNode>) ParseBraces("(", ")",
                    delegate(Token arg1) { return ParseBraces("{", "}", ParseTypeList); });
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

            ctNode.ActualGenerics = ParseActualGenericParameters(CurrentToken);
            //todo: Проверить ActualGenerics
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

        private FormalArgumentNode ParseFormalArgument(Token token)
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

        private List<TypeNode> ParseActualGenericParameters(Token token)
        {
            List<TypeNode> typeNodes = null;
            if (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == "[")
            {
                getNextToken();
                typeNodes = ParseTypeList(CurrentToken);
                if (CurrentToken.Type != TokenType.Delimeter && CurrentToken.Value == "]")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("ActualGenericParameters parse error");
                }
            }
            else
            {
                Debug.Print("ActualGenericParameters parse error");
            }

            return typeNodes;
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
            do
            {
                result.Add(func.Invoke(CurrentToken));
            } while (CurrentToken.Type == TokenType.Delimeter && CurrentToken.Value == delimeter);

            return result;
        }

        public object ParseBraces<T>(string braceStart, string braceEnd, Func<Token, T> func)
        {
            object result = null;
            if(CurrentToken.Type==TokenType.Delimeter&& CurrentToken.Value==braceStart)
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