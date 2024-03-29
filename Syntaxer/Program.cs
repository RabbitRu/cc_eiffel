﻿using Lexer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntaxer
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"C:\Users\zombi\Documents\Study\CC\cc_eiffel\Lexer\TestSrc\test1.txt";
            string fileAsString = File.ReadAllText(path);
            var lexer = new Lexer.Lexer();
            var tokens = lexer.Parse(fileAsString, false);
            var syntaxer = new Syntaxer();
            syntaxer.BuildAST(tokens);
        }
    }
}
