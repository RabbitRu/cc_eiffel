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
                    returnValue = ParseReservedWords(currenToken);
                    break;
                default:
                    getNextToken();
                    break;
            }

            return returnValue;
        }

        private BaseNode ParseReservedWords(Token token)
        {
            BaseNode returnValue = null;
            ClassNode classWNotes = new ClassNode();
            switch (token.Value)
            {
                case "class":
                    getNextToken();
                    returnValue = ParseClass(currenToken, classWNotes);
                    break;
                case "note":
                    getNextToken();
                    classWNotes = new ClassNode();
                    ParseNote(currenToken, classWNotes.Notes);
                    break;
                default:
                    getNextToken();
                    break;
            }

            return returnValue;
        }

        private BaseNode ParseClass(Token token, ClassNode classWNotes = null)
        {
            ClassNode classN;
            classN = classWNotes ?? new ClassNode();

            while (currenToken != null && currenToken.Value != "end")
            {
                switch (currenToken.Value)
                {
                    case "obsolete":
                        getNextToken();
                        break;
                    case "note":
                        getNextToken();
                        ParseNote(currenToken, classN.Notes);
                        break;
                    case "inheritance":
                        getNextToken();
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
                        getNextToken();
                        break;
                }

            }

            return classN;
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
