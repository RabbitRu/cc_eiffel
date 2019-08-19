using Lexer;
using Syntaxer.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Syntaxer
{
    class Syntaxer
    {
        private List<Token> source;
        private int currentTokenNumber = 0;
        private Token currentToken;
        public RootNode Root = new RootNode();
        private ClassNode classWAddonsGlobal = null;
        private Stack<int> states = new Stack<int>();

        private void saveState()
        {
            states.Push(currentTokenNumber);
        }

        private Token loadState()
        {
            currentTokenNumber = states.Pop();
            currentToken = source[currentTokenNumber];
            return currentToken;
        }

        private Token getNextToken()
        {
            if (currentTokenNumber >= source.Count)
            {
                currentToken = null;
                return null;
            }

            currentToken = source[currentTokenNumber];
            currentTokenNumber++;
            return currentToken;
        }

        public void BuildAST(List<Token> tokens)
        {
            source = tokens;
            getNextToken();
            while (currentTokenNumber < tokens.Count)
            {
                Root.Children.Add(ParseExpression());
            }
        }

        public BaseNode ParseExpression()
        {
            BaseNode returnValue = null;

            switch (currentToken.Type)
            {
                case TokenType.Constant:
                    returnValue = ParseConstant(currentToken);
                    break;
                case TokenType.Delimeter:
                    returnValue = ParseDelimeter(currentToken);
                    break;
                case TokenType.Identifier:
                    returnValue = ParseIdentifier(currentToken);
                    break;
                case TokenType.ReservedWord:
                    (returnValue, classWAddonsGlobal) = ParseReservedWords(currentToken, classWAddonsGlobal);
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
                    returnValue = ParseClass(currentToken, classWAddons);
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
                    ParseNote(currentToken, classWAddons.Notes);
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

            while (currentToken != null && currentToken.Value != "end")
            {
                switch (currentToken.Value)
                {
                    case "obsolete":
                        getNextToken();
                        classN.Obsolete = ParseObsolete(currentToken);
                        break;
                    case "note":
                        getNextToken();
                        ParseNote(currentToken, classN.Notes);
                        break;
                    case "inheritance":
                        getNextToken();
                        ParseInheritance(currentToken, classN.Inheritance);
                        break;
                    case "creators":
                        getNextToken();
                        break;
                    case "converters":
                        getNextToken();
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

            classWAddonsGlobal = null;
            return classN;
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
            }

            while (true)
            {
                ParseType(currentToken);
            }
        }

        private TypeNode ParseType(Token token)
        {
            TypeNode tNode = new TypeNode();

            {
                //TupleType
                TupleNode tuple = ParseTupleNode(token); //todo: Реализовать
            }

            {
                //ClassType
                tNode.AttachmentMark = ParseAttachmentMark(currentToken);
                if (currentToken.Type == TokenType.Identifier)
                {
                    tNode.ClassName = currentToken.Value;
                    getNextToken();
                }
                else
                {
                    Debug.Print("Types class_type parse error");
                }

                //todo: Проверить ActualGenerics
            }

            {
                //Anchored
                tNode.AttachmentMark = ParseAttachmentMark(currentToken);

                if (currentToken.Type == TokenType.ReservedWord && currentToken.Value == "like")
                {
                }
                else
                {
                    Debug.Print("Types anchored parse error");
                }

                if (currentToken.Type == TokenType.ReservedWord && currentToken.Value == "Current")
                {
                    tNode.Anchor = new FeatureNameNode {Name = "Current"};
                }
                else
                {
                    tNode.Anchor = ParseFeatureName(currentToken);
                }
            }
            return tNode;
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

            if (currentToken.Type == TokenType.Delimeter && currentToken.Value == "[")
            {
                while (currentToken.Type != TokenType.Delimeter && currentToken.Value == "]")
                {
                    ParseActualGenericParameters(currentToken);
                    ParseFormalArgument(currentToken);
                }
            }

            return tpl;
        }

        private void ParseFormalArgument(Token token)
        {
            List<EntityDeclarationList> entityDeclarationList = new List<EntityDeclarationList>();
            if (currentToken.Type == TokenType.Delimeter && currentToken.Value == "(")
            {
                while (currentToken.Type != TokenType.Delimeter && currentToken.Value == ")")
                {
                    entityDeclarationList.Add(ParseEntityDeclarationsList(currentToken));
                }
            }
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

                    if (currentToken.Type == TokenType.Identifier)
                    {
                        entityDeclarationGroup.IdentifierList.Add(currentToken.Value);
                        getNextToken();
                    }
                    else
                    {
                        Debug.Print("EDL parse error");
                    }
                } while (currentToken.Type != TokenType.Delimeter && currentToken.Value == ",");

                if (currentToken.Type == TokenType.Delimeter && currentToken.Value == ":")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("EDL parse error");
                }

                entityDeclarationGroup.Type = ParseType(currentToken);
            } while (currentToken.Type != TokenType.Delimeter && currentToken.Value == ";");

            return null;
        }

        private void ParseActualGenericParameters(Token token)
        {
        }

        private string ParseAttachmentMark(Token token)
        {
            if (token.Type == TokenType.Delimeter && token.Value == "!" ||
                token.Type == TokenType.Delimeter && token.Value == "?")
            {
                //Attachment mark
                var returnValue = currentToken.Value;
                getNextToken();
                return returnValue;
            }

            return null;
        }

        private FeatureNameNode ParseFeatureName(Token token)
        {
            var fName = new FeatureNameNode();
            if (currentToken.Type == TokenType.Identifier)
            {
                fName.Name = currentToken.Value;
                getNextToken();
            }
            else
            {
                Debug.Print("Feature Name Parse Error 1");
                return null;
            }

            while (true)
            {
                if (currentToken.Type == TokenType.ReservedWord && currentToken.Value == "alias")
                {
                    getNextToken();
                }
                else
                {
                    break;
                }

                if (currentToken.Type == TokenType.Delimeter && currentToken.Value == "\"")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 2");
                    return null;
                }

                if (currentToken.Type == TokenType.Delimeter && currentToken.Value == "[") //Тут мб проще
                {
                    getNextToken();
                    if (currentToken.Type == TokenType.Delimeter && currentToken.Value == "]")
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
                else if (currentToken.Type == TokenType.Operator &&
                         ReservedWords.Operators.Any(x => x == currentToken.Value))
                {
                    fName.AliasName = currentToken.Value;
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 4");
                    return null;
                }

                if (currentToken.Type == TokenType.Delimeter && currentToken.Value == "\"")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 5");
                    return null;
                }

                if (currentToken.Type == TokenType.ReservedWord && currentToken.Value == "convert")
                {
                    fName.Convert = true;
                    getNextToken();
                }
            }

            return fName;
        }

        private string ParseObsolete(Token token)
        {
            if (currentToken.Type == TokenType.Constant)
            {
                var returnValue = currentToken.Value;
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
            while (currentToken.Type == TokenType.Identifier)
            {
                // nName: nContent
                if (currentToken.Type == TokenType.Identifier)
                {
                    nName = currentToken.Value;
                }
                else
                {
                    Debug.Print("Note parse error1");
                    return;
                }

                getNextToken();

                if (currentToken.Type != TokenType.Delimeter || currentToken.Value != ":")
                {
                    return;
                }

                getNextToken();
                nContent = new List<string>();

                while (true)
                {
                    if (currentToken.Type == TokenType.Constant || currentToken.Type == TokenType.Identifier)
                    {
                        nContent.Add(currentToken.Value);
                    }
                    else
                    {
                        Debug.Print("Note parse error2");
                        break;
                    }

                    getNextToken();

                    if (currentToken.Type == TokenType.Delimeter && currentToken.Value == ",")
                    {
                        nContent.Add(currentToken.Value);
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

            if (currentToken.Value[0] != '(') // Ссылка на переменную.
                return new VariableNode(iden);

            // Вызов функции.
            getNextToken(); // получаем (
            List<BaseNode> args = new List<BaseNode>();
            if (currentToken.Value[0] != ')')
            {
                while (true)
                {
                    BaseNode arg = ParseExpression();
                    if (arg == null) return null;
                    args.Add(arg);

                    if (currentToken.Value[0] == ')') break;

                    if (currentToken.Value[0] != ',')
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
    }
}