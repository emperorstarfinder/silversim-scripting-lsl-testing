// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule", Justification = "Ever seen a compiler source code without such warnings?")]
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule", Justification = "Ever seen a compiler source code without such warnings?")]
    public class Parser : ParserBase
    {
        public Parser()
        {

        }

        bool IsLSLWhitespace(char c)
        {
            if(char.IsWhiteSpace(c))
            {
                return true;
            }
            if(c == '\x01')
            {
                return true;
            }
            return false;
        }

        public void ReadPass1(List<string> args)
        {
            char c;
            StringBuilder token = new StringBuilder();
            Begin();
            args.Clear();
            bool is_preprocess = false;
            int parencount = 0;
            CurrentLineNumber = -1;

            for(;;)
            {
                c = ReadC();
redo:
                switch(c)
                {
                    case '\x20':
                    case '\x09':
                    case '\r':
                        break;
                    case '\n':      /* these all are simply white space */
                        if(is_preprocess)
                        {
                            if (0 == args.Count)
                            {
                                return;
                            }
                            if (args[args.Count - 1] != "\\")
                            {
                                return;
                            }
                        }
                        break;

                    case ';':       /* end of statement */
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        if(args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add(";");
                        if (args.Count != 0 && parencount == 0)
                        {
                            return;
                        }
                        break;

                    case '{':       /* opening statement */
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        if (args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add("{");
                        return;

                    case '}':       /* closing statement */
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        if (args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add("}");
                        if(args.Count > 1)
                        {
                            throw new ArgumentException("incomplete statement before '}'");
                        }
                        return;
                
                    case '\"':      /* string literal */
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        MarkBeginOfLine();
                        token.Clear();
                        do
                        {
                            if (c == '\\')
                            {
                                token.Append(c);
                                c = ReadC();
                                token.Append(c);
                            }
                            else
                            {
                                token.Append(c);
                            }
                            c = ReadC();
                        } while(c != '\"');
                        token.Append("\"");
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        token.Clear();
                        break;
            
                    case '\'':      /* string literal */
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        MarkBeginOfLine();
                        token.Clear();
                        do
                        {
                            if (c == '\\')
                            {
                                token.Append(c);
                                c = ReadC();
                                token.Append(c);
                            }
                            else
                            {
                                token.Append(c);
                            }
                            c = ReadC();
                        } while(c != '\'');
                        token.Append("\'");
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        token.Clear();
                        break;

                    case '@':
                    case ',':       /* special tokens (all these do not make up compound literals) */
                    case '~':
                    case '?':
                    case '(':
                    case ')':
                    case '\\':
                    case '[':
                    case ']':
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if (0 != token.Length)
                        {
                            args.Add(token.ToString());
                        }
                        token.Clear();
                        if (args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add(new string(new char[] {c}));
                        if (c == '(')
                        {
                            ++parencount;
                        }
                        if( c == ')')
                        {
                            if(parencount == 0)
                            {
                                throw new ArgumentException("Mismatching ')'");
                            }
                            --parencount;
                        }
                        break;
                
                    case '<':
                        if (CurrentLineNumber < 0)
                        {
                            CurrentLineNumber = GetFileInfo().LineNumber;
                        }
                        if(is_preprocess &&
                            args.Count != 0 &&
                            (args[0] == "#include" || args[0] == "#include_once"))
                        { 
                            /* preprocessor literal */
                            if (0 != token.Length)
                            {
                                args.Add(token.ToString());
                            }
                            if (args.Count == 0)
                            {
                                MarkBeginOfLine();
                            }
                            token.Clear();
                            c = '\"';
                            do
                            {
                                token.Append(c);
                                c = ReadC();
                            } while(c != '>');
                            token.Append("\"");
                            if (0 != token.Length)
                            {
                                args.Add(token.ToString());
                            }
                            token.Clear();
                            break;
                        }
                        /* fall-through since it is a special case only in preprocessor handling */
                        goto defaultcase;

                    default:        /* regular tokens */
                defaultcase:
                        if (IsLSLWhitespace(c))
                        {
                            if(0 != token.Length)
                            {
                                args.Add(token.ToString());
                                token.Clear();
                            }
                        }
                        else
                        {
                            if (CurrentLineNumber < 0)
                            {
                                CurrentLineNumber = GetFileInfo().LineNumber;
                            }
                            if (token.Length == 0 && args.Count == 0 && c == '#')
                            {
                                is_preprocess = true;
                            }
                            MarkBeginOfLine();
                            while (!IsLSLWhitespace(c) && c != ';' && c != '(' && c != ')' && c != ',' && c != '\"' && c != '\'' && c != '~' && c != '\\' && c != '?' && c != '@' && c != '{' && c != '}' && c != '[' && c != ']')
                            {
                                token.Append(c);
                                if(token.EndsWith("//"))
                                {
                                    /* got C++-style comment */
                                    CurrentLineNumber = -1;
                                    while (c != '\n')
                                    {
                                        try
                                        {
                                            c = ReadC();
                                        }
                                        catch(EndOfFileException)
                                        {
                                            if (args.Count != 0)
                                            {
                                                return;
                                            }
                                            throw;
                                        }
                                    }
                                    token.Remove(token.Length - 2, 2);
                                    c = ' ';
                                    goto redo;
                                }
                                if(token.EndsWith("/*"))
                                {
                                    /* got C-style comment */
                                    CurrentLineNumber = -1;
                                    for(;;)
                                    {
                                        try
                                        {
                                            c = ReadC();
                                        }
                                        catch(EndOfFileException)
                                        {
                                            if (args.Count != 0)
                                            {
                                                return;
                                            }
                                            throw;
                                        }
                                        if (c != '*')
                                        {
                                            continue;
                                        }
                                        do
                                        {
                                            try
                                            {
                                                c = ReadC();
                                            }
                                            catch(EndOfFileException)
                                            {
                                                if (args.Count != 0)
                                                {
                                                    return;
                                                }
                                                throw;
                                            }
                                        } while(c == '*');

                                        if (c == '/')
                                        {
                                            break;
                                        }
                                    }

                                    token.Remove(token.Length - 2, 2);
                                    c = ' ';
                                    goto redo;
                                }

                                try
                                {
                                    c = ReadC();
                                }
                                catch(EndOfFileException)
                                {
                                    if(token.Length != 0)
                                    {
                                        args.Add(token.ToString());
                                        return;
                                    }
                                    throw;
                                }
                            }
                            args.Add(token.ToString());
                            token.Clear();
                            goto redo;
                        }
                        break;
                }
            }
        }

        public void EvalCompounds(List<string> args)
        {
            int argi = -1;
            while (++argi < args.Count)
            {
                int i = 0;
                char c;
                int curlength = args[argi].Length;
                while (i < curlength)
                {
                    c = args[argi][i];
                    /* ignore strings first */
                    if ('\"' == c)
                    {
                        break;
                    }
                    else if ('\'' == c)
                    {
                        break;
                    }
                    else if ('e' == c && i + 1 < curlength && args[argi][i + 1] == '-' && i > 0 && char.IsDigit(args[argi][i - 1]))
                    {
                        /* float component */
                        i += 2;
                    }
                    else if ('e' == c && i + 1 < curlength && args[argi][i + 1] == '+' && i > 0 && char.IsDigit(args[argi][i - 1]))
                    {
                        /* float component */
                        i += 2;
                    }
                    else if ('+' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal += */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "+=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '+')
                            {
                                /* compound literal ++ (contextual dependancy if there are more) */
                                int j = 2;
                                while (j < curlength && args[argi][j] == '+')
                                {
                                    ++j;
                                }
                                if (j < curlength)
                                {
                                    args.Insert(argi, args[argi].Substring(0, j));
                                    ++argi;
                                    args[argi] = args[argi].Substring(j, curlength - j);
                                    curlength -= j;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "+");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('.' == c)
                    {
                        if (i + 1 < curlength)
                        {
                            if (!Char.IsDigit(args[argi][i + 1]) &&
                                (i == 0 || !Char.IsDigit(args[argi][0])))
                            {
                                if (i > 0)
                                {
                                    args.Insert(argi, args[argi].Substring(0, i));
                                    ++argi;
                                    args[argi] = args[argi].Substring(i, curlength - i);
                                    curlength -= i;
                                }
                                args.Insert(argi++, ".");

                                i = 0;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
                            }
                            else
                            {
                                ++i;
                            }
                        }
                        else
                        {
                            ++i;
                        }
                    }
                    else if ('-' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal -= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "-=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '>')
                            {
                                /* compound literal -= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "->");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '-')
                            {
                                /* compound literal -- (contextual dependancy if there are more) */
                                int j = 2;
                                while (j < curlength && args[argi][j] == '-')
                                {
                                    ++j;
                                }
                                if (j < curlength)
                                {
                                    args.Insert(argi, args[argi].Substring(0, j));
                                    ++argi;
                                    args[argi] = args[argi].Substring(j, curlength - j);
                                    curlength -= j;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "-");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('*' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal *= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "*=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "*");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (':' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == ':')
                            {
                                /* compound literal :: */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "::");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, ":");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('/' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal /= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "/=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "/");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('%' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal %= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "%=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "%");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('<' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal <= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "<=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '<')
                            {
                                /* compound literals <<, <<= */
                                if (3 < curlength && args[argi][2] == '=')
                                {
                                    args.Insert(argi++, "<<=");
                                    args[argi] = args[argi].Substring(3, curlength - 3);
                                    curlength -= 3;
                                }
                                else if (2 < curlength)
                                {
                                    args.Insert(argi++, "<<");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "<");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('>' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal >= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, ">=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '>')
                            {
                                /* compound literals << <<= */
                                if (3 < curlength && args[argi][2] == '=')
                                {
                                    args.Insert(argi++, ">>=");
                                    args[argi] = args[argi].Substring(3, curlength - 3);
                                    curlength -= 3;
                                }
                                else if (2 < curlength)
                                {
                                    args.Insert(argi++, ">>");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, ">");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('=' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal == */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "==");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '>')
                            {
                                /* compound literal == */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "=>");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "=");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('!' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal != */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "!=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "!");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('^' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal ^= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "^=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "^");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('&' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal &= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "&=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '&')
                            {
                                /* compound literal && */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "&&");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "&");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('#' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '#')
                            {
                                /* compound literal ## */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "##");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "#");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ('|' == c)
                    {
                        if (i > 0)
                        {
                            args.Insert(argi, args[argi].Substring(0, i));
                            ++argi;
                            args[argi] = args[argi].Substring(i, curlength - i);
                            curlength -= i;
                        }

                        i = 0;
                        if (1 < curlength)
                        {
                            if (args[argi][1] == '=')
                            {
                                /* compound literal |= */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "|=");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else if (args[argi][1] == '|')
                            {
                                /* compound literal || */
                                if (2 < curlength)
                                {
                                    args.Insert(argi++, "||");
                                    args[argi] = args[argi].Substring(2, curlength - 2);
                                    curlength -= 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                args.Insert(argi++, "|");
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                curlength -= 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        ++i;
                    }
                }
            }
        }

        public void ReadPass2(List<string> arguments)
        {
            List<string> inargs = new List<string>(arguments);
            arguments.Clear();
            foreach(string it in inargs)
            {
                if(it == "+++")
                {
                    arguments.Add("++");
                    arguments.Add("+");
                }
                else if(it == "+++++")
                {
                    arguments.Add("++");
                    arguments.Add("+");
                    arguments.Add("++");
                }
                else if(it == "++++")
                {
                    arguments.Add("++");
                    arguments.Add("+");
                    arguments.Add("+");
                }
                else if(it == "---")
                {
                    arguments.Add("--");
                    arguments.Add("-");
                }
                else if(it == "----")
                {
                    arguments.Add("--");
                    arguments.Add("-");
                    arguments.Add("-");
                }
                else if(it == "-----")
                {
                    arguments.Add("--");
                    arguments.Add("-");
                    arguments.Add("--");
                }
                else
                {
                    arguments.Add(it);
                }
            }
        }

        public override void Read(List<string> args)
        {
            MarkBeginOfLine();
            ReadPass1(args);
            EvalCompounds(args);
            ReadPass2(args);
        }

    }
}
