using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexer;
using Syntaxer.Nodes;

namespace Syntaxer
{
    class Syntaxer
    {
        private List<Token> source;
        private int currentTokenNumber = 0;
        private Token currenToken;
        public RootNode Root = new RootNode();
        private ClassNode classWAddonsGlobal = null;

        private Token getNextToken()
        {
            if (currentTokenNumber >= source.Count)
            {
                currenToken = null;
                return null;
            }

            currenToken = source[currentTokenNumber];
            currentTokenNumber++;
            return currenToken;
        }

        public void BuildAST(List<Token> tokens)
        {
            source = tokens;
            getNextToken();
            while (currentTokenNumber<tokens.Count)
            {
                Root.Children.Add(ParseExpression());
            }

        }

        public BaseNode ParseExpression()
        {
            BaseNode returnValue = null;

            switch (currenToken.Type)
            {
                case TokenType.Constant:
                    returnValue = ParseConstant(currenToken);
                    break;
                case TokenType.Delimeter:
                    returnValue = ParseDelimeter(currenToken);
                    break;
                case TokenType.Identifier:
                    returnValue = ParseIdentifier(currenToken);
                    break;
                case TokenType.ReservedWord:
                    (returnValue, classWAddonsGlobal) = ParseReservedWords(currenToken, classWAddonsGlobal);
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
                case "class"://deferred | expanded | frozen
                    getNextToken();
                    returnValue = ParseClass(currenToken, classWAddons);
                    break;
                case "frozen"://headers
                case "expanded":
                case "deferred":
                    classWAddons = classWAddons ?? new ClassNode();
                    classWAddons.Headers.Add(token.Value);
                    getNextToken();
                    break;
                case "note":
                    getNextToken();
                    classWAddons = classWAddons ?? new ClassNode();
                    ParseNote(currenToken, classWAddons.Notes);
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

            while (currenToken != null && currenToken.Value != "end")
            {
                switch (currenToken.Value)
                {
                    case "obsolete":
                        getNextToken();
                        classN.Obsolete = ParseObsolete(currenToken);
                        break;
                    case "note":
                        getNextToken();
                        ParseNote(currenToken, classN.Notes);
                        break;
                    case "inheritance":
                        getNextToken();
                        ParseInheritance(currenToken, classN.Inheritance);
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

            }
        }

        private FeatureNameNode ParseFeatureName(Token token)
        {
            var fName = new FeatureNameNode();
            if (currenToken.Type == TokenType.Identifier)
            {
                fName.Name = currenToken.Value;
                getNextToken();
            }
            else
            {
                Debug.Print("Feature Name Parse Error 1");
                return null;
            }

            while (true)
            {
                if (currenToken.Type == TokenType.ReservedWord && currenToken.Value == "alias")
                {
                    getNextToken();
                }
                else
                {
                    break;
                }

                if (currenToken.Type==TokenType.Delimeter && currenToken.Value == "\"")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 2");
                    return null;
                }
                
                if (currenToken.Type == TokenType.Delimeter && currenToken.Value == "[")//Тут мб проще
                {
                    getNextToken();
                    if (currenToken.Type == TokenType.Delimeter && currenToken.Value == "]")
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
                else if(currenToken.Type == TokenType.Operator && ReservedWords.Operators.Any(x => x == currenToken.Value))
                {
                    fName.AliasName = currenToken.Value;
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 4");
                    return null;
                }
                
                if (currenToken.Type == TokenType.Delimeter && currenToken.Value == "\"")
                {
                    getNextToken();
                }
                else
                {
                    Debug.Print("Feature Name Parse Error 5");
                    return null;
                }

                if (currenToken.Type == TokenType.ReservedWord && currenToken.Value == "convert")
                {
                    fName.Convert = true;
                    getNextToken();
                }
            }

            return fName;
        }

        private string ParseObsolete(Token token)
        {
            if (currenToken.Type == TokenType.Constant)
            {
                var returnValue = currenToken.Value;
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
            while (currenToken.Type == TokenType.Identifier)
            {// nName: nContent
                if (currenToken.Type==TokenType.Identifier)
                {
                    nName = currenToken.Value;
                }
                else
                {
                    Debug.Print("Note parse error1");
                    return;
                }
                getNextToken();

                if (currenToken.Type != TokenType.Delimeter || currenToken.Value != ":")
                {
                    return;
                }

                getNextToken();
                nContent = new List<string>();

                while (true)
                {
                    if (currenToken.Type == TokenType.Constant || currenToken.Type == TokenType.Identifier)
                    {
                        nContent.Add(currenToken.Value);
                    }
                    else
                    {
                        Debug.Print("Note parse error2");
                        break;
                    }

                    getNextToken();

                    if (currenToken.Type == TokenType.Delimeter && currenToken.Value == ",")
                    {
                        nContent.Add(currenToken.Value);
                        getNextToken();
                    }
                    else
                    {//Норма
                        break;
                    }

                }

                notes.Add(nName, nContent);
            }
        }

        private BaseNode ParseIdentifier(Token identidier)
        {
            string iden = identidier.Value;

            getNextToken();// получаем идентификатор.

            if (currenToken.Value[0] != '(')// Ссылка на переменную.
                return new VariableNode(iden);

            // Вызов функции.
            getNextToken();  // получаем (
            List<BaseNode> args = new List<BaseNode>();
            if (currenToken.Value[0] != ')')
            {
                while (true)
                {
                    BaseNode arg = ParseExpression();
                    if (arg == null) return null;
                    args.Add(arg);

                    if (currenToken.Value[0] == ')') break;

                    if (currenToken.Value[0] != ',')
                    {
                        Debug.Print("Expected ')' or ',' in argument list");
                        return null;
                    }

                    getNextToken();
                }
            }

            // получаем ')'.
            getNextToken();

            return new FunctionNode(new PrototypeNode(iden,args), ParseExpression());
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
            return new VariableNode("","");
        }

        public int GetOpPriopity(Token tk)
        {
            if(tk.Type!=TokenType.Operator)
                throw new Exception();

            return ReservedWords.OpPriority[tk.Value];
        }

    }
}
