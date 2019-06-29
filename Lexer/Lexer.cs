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


        public void Parse(string file)
        {
            fileAsTokens = new List<Token>();
            identifiers = new List<Token>();
            constants = new List<Token>();
            resedvedWords = new List<Token>();

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
                        while (i < file.Length & file[i] != '\n')
                        {
                            i++;
                        }
                    }
                }
                else if (!char.IsWhiteSpace(file[i]))
                {
                    char temp = file[i];
                    currentToken.Value = file[i].ToString();
                    if (char.IsLetter(file[i]))
                    {
                        i++;
                        while (i < file.Length & char.IsLetterOrDigit(file[i]) | char.IsSymbol(file[i]))
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
                        if (i < file.Length & isBase(file[i]))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        while (i < file.Length & isNumberEx(file[i]))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        if (i < file.Length & file[i] == '.')
                        {
                            currentToken.Value += file[i];
                            i++;
                            while (i < file.Length & isNumberEx(file[i]))
                            {
                                currentToken.Value += file[i];
                                i++;
                            }

                            if (i < file.Length & file[i] == 'e')
                            {
                                currentToken.Value += file[i];
                                i++;
                                while (i < file.Length & isNumberEx(file[i]))
                                {
                                    currentToken.Value += file[i];
                                    i++;
                                }
                            }
                        }

                        currentToken.Type = TokenType.Constant;
                    }
                    else if (file[i] == '\"')
                    {
                        i++;
                        while (i < file.Length & (file[i] != '\"' | file[i - 1] == '\\'))
                        {
                            currentToken.Value += file[i];
                            i++;
                        }

                        currentToken.Value += file[i];
                        i++;

                        currentToken.Type = TokenType.Constant;
                    }
                    else
                    {
                        i++;
                        currentToken.Type = TokenType.Delimeter;
                    }

                    fileAsTokens.Add(currentToken);
                }
                else
                {
                    i++;
                }

            }

            //Вывод результатов
            foreach (var token in fileAsTokens)
            {
                Console.Write(token.Type + "_" + token.Value + ' ');
            }
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
    }
}
