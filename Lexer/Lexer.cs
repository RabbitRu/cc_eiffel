using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    public class Lexer
    {
        private List<Token> fileAsTokens;

        private List<Token> identifiers;
        private List<Token> constants;
        private List<Token> resedvedWords;
        private List<Token> delimeters;


        public void Parse(string file)
        {
            fileAsTokens = new List<Token>();

            identifiers = new List<Token>();
            constants = new List<Token>();
            resedvedWords = new List<Token>();
            delimeters = new List<Token>();

            Token currentToken;
            string smb = "";
            int i = 0;
            while (i < file.Length)
            {
                currentToken = new Token();
                if (file[i] == '-')
                {
                    if (i < file.Length - 1 & file[i + 1] == '-') //Это комментарий, пропускаем
                    {
                        while (i < file.Length && file[i] != '\n')
                        {
                            i++;
                        }
                    }
                }

                if (!char.IsWhiteSpace(file[i]))
                {
                    currentToken.Value = file[i].ToString();
                    if (char.IsLetter(file[i]))
                    {
                        i++;
                        while (i < file.Length && (char.IsLetterOrDigit(file[i]) | file[i] == '_'))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        if (ReservedWords.Words.Any(x => x.Equals(currentToken.Value)))
                        {
                            currentToken.Type = TokenType.ReservedWord;
                            resedvedWords.Add(currentToken);
                        }
                        else
                        {
                            currentToken.Type = TokenType.Identifier;
                            identifiers.Add(currentToken);
                        }
                    }
                    else if (char.IsNumber(file[i]))
                    {
                        i++;
                        if (i < file.Length && isBase(file[i]))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        while (i < file.Length && isNumberEx(file[i]))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        if (i < file.Length && file[i] == '.')
                        {
                            currentToken.Value += file[i];
                            i++;
                            while (i < file.Length && isNumberEx(file[i]))
                            {
                                currentToken.Value += file[i];
                                i++;
                            }

                            if (i < file.Length && file[i] == 'e')
                            {
                                currentToken.Value += file[i];
                                i++;
                                while (i < file.Length && isNumberEx(file[i]))
                                {
                                    currentToken.Value += file[i];
                                    i++;
                                }
                            }
                        }

                        currentToken.Type = TokenType.Constant;
                        constants.Add(currentToken);
                    }
                    else if (file[i] == '\"')
                    {
                        i++;
                        while (i < file.Length && (file[i] != '\"' | file[i - 1] == '\\'))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        currentToken.Value += file[i];
                        i++;

                        currentToken.Type = TokenType.Constant;
                        constants.Add(currentToken);
                    }
                    /*else if (file[i] == '-' || file[i] == '+')
                    {

                    }*/
                    else
                    {
                        i++;
                        currentToken.Type = TokenType.Delimeter;
                        delimeters.Add(currentToken);

                    }

                    Console.Write(currentToken.Type.ToString()[0] + "_" + currentToken.Value + ' ');
                    fileAsTokens.Add(currentToken);
                }
                else
                {
                    Console.Write(file[i]);
                    i++;
                }

            }

            //Вывод результатов
            foreach (var token in fileAsTokens)
            {
                //Console.Write(token.Type + "_" + token.Value + ' ');
            }

            Console.Write("\n\n");
            foreach (var token in fileAsTokens)
            {
                if (token.Type == TokenType.Delimeter)
                    Console.Write(" " + token.Value + ' ');
            }

            fileAsTokens = FindOperators(fileAsTokens);

            Console.Write("\n\n");
            foreach (var token in fileAsTokens)
            {
                if(token.Type == TokenType.Operator)
                Console.Write(" " + token.Value + ' ');
            }

            Console.Write("\n\n");
            foreach (var token in fileAsTokens)
            {
                if (token.Type == TokenType.Delimeter)
                    Console.Write(" " + token.Value + ' ');
            }
        }

        public List<Token> FindOperators(List<Token> source)
        {
            var modifiedSource = new List<Token>(source);
            if (modifiedSource.Count < 2)
                return null;
            bool skipDouble = false;

            for (int i = 1; i < modifiedSource.Count; i++)
            {
                if (modifiedSource[i].Type == TokenType.Delimeter)
                {
                    var t = modifiedSource[i - 1].Value + " " + modifiedSource[i].Value;
                    if (ReservedWords.Operators.Any(x =>
                        x == (modifiedSource[i - 1].Value + modifiedSource[i].Value) ||
                        x == (modifiedSource[i - 1].Value + " " + modifiedSource[i].Value)) && !skipDouble)
                    {
                        modifiedSource[i - 1].Type = TokenType.Operator;
                        modifiedSource[i - 1].Value += modifiedSource[i].Value;
                        modifiedSource.RemoveAt(i);
                        i--;
                        skipDouble = true;
                    }
                    else if (ReservedWords.Operators.Any(x =>
                        x == modifiedSource[i].Value))
                    {
                        modifiedSource[i].Type = TokenType.Operator;
                        skipDouble = false;
                    }
                    else
                    {
                        skipDouble = false;
                    }
                }
                

            }

            return modifiedSource;
        }

        private bool isBase(char c)
        {
            char[] base_ = new[] {'b', 'c', 'x', 'B', 'C', 'X'};
            return base_.Any(x => x.Equals(c));
        }

        private bool isNumberEx(char c)
        {
            char[] hexNumber = new[] {'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F', '_'};
            return hexNumber.Any(x => x.Equals(c)) || char.IsNumber(c);
        }

        private bool isDelimeter(char c)
        {
            char[] dlm = new[] {'(',')','}','{',']','[' };
            return dlm.Any(x => x.Equals(c));
        }
    }
}
