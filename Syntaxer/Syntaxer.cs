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
                return null;
            var x = source[currentTokenNumber];
            currenToken = source[currentTokenNumber];
            currentTokenNumber++;
            return x;
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
                default:
                    getNextToken();
                    break;
            }

            return returnValue;
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
