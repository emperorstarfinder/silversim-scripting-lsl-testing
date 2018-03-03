// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable RCS1029

using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SilverSim.Scripting.Lsl
{
    public class Parser : ParserBase
    {
        private readonly CultureInfo m_CurrentCulture;

        public Parser(CultureInfo currentCulture)
        {
            m_CurrentCulture = currentCulture;
        }

        private bool IsLSLWhitespace(char c)
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

        public void ReadPass1(List<TokenInfo> args)
        {
            char c;
            var token = new TokenInfoBuilder();
            Begin();
            args.Clear();
            bool is_preprocess = false;
            int parencount = 0;
            CurrentLineNumber = -1;

            for(;;)
            {
                CurrentLineNumber = GetFileInfo().LineNumber;
                c = ReadC();
redo:
                switch(c)
                {
                    case '\x20':
                    case '\x09':
                    case '\r':
                    case '\0':
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

                    case ':':       /* end of case */
                    case ';':       /* end of statement */
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        if(args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add(new TokenInfo(c.ToString(), CurrentLineNumber));
                        if (args.Count != 0 && parencount == 0)
                        {
                            return;
                        }
                        break;

                    case '{':       /* opening statement */
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        if (args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add(new TokenInfo("{", CurrentLineNumber));
                        return;

                    case '}':       /* closing statement */
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        if (args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add(new TokenInfo("}", CurrentLineNumber));
                        if(args.Count > 1)
                        {
                            throw new ArgumentException(this.GetLanguageString(m_CurrentCulture, "IncompleteStatementBeforeClosingBrace", "incomplete statement before '}'"));
                        }
                        return;

                    case '\"':      /* string literal */
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        MarkBeginOfLine();
                        token.Clear();
                        do
                        {
                            if (c == '\\')
                            {
                                token.Append(c, CurrentLineNumber);
                                c = ReadC();
                                token.Append(c, CurrentLineNumber);
                            }
                            else
                            {
                                token.Append(c, CurrentLineNumber);
                            }
                            c = ReadC();
                        } while(c != '\"');
                        token.Append("\"", CurrentLineNumber);
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        token.Clear();
                        break;

                    case '\'':      /* string literal */
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        MarkBeginOfLine();
                        token.Clear();
                        do
                        {
                            if (c == '\\')
                            {
                                token.Append(c, CurrentLineNumber);
                                c = ReadC();
                                token.Append(c, CurrentLineNumber);
                            }
                            else
                            {
                                token.Append(c, CurrentLineNumber);
                            }
                            c = ReadC();
                        } while(c != '\'');
                        token.Append("\'", CurrentLineNumber);
                        if (0 != token.Length)
                        {
                            args.Add(token);
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
                        if (0 != token.Length)
                        {
                            args.Add(token);
                        }
                        token.Clear();
                        if (args.Count == 0)
                        {
                            MarkBeginOfLine();
                        }
                        args.Add(new TokenInfo(c, CurrentLineNumber));
                        if (c == '(')
                        {
                            ++parencount;
                        }
                        if( c == ')')
                        {
                            if(parencount == 0)
                            {
                                throw new ArgumentException(this.GetLanguageString(m_CurrentCulture, "MismatchingClosingParenthesis", "Mismatching ')'"));
                            }
                            --parencount;
                        }
                        break;

                    case '<':
                        if(is_preprocess &&
                            args.Count != 0 &&
                            (args[0] == "#include" || args[0] == "#include_once"))
                        {
                            /* preprocessor literal */
                            if (0 != token.Length)
                            {
                                args.Add(token);
                            }
                            if (args.Count == 0)
                            {
                                MarkBeginOfLine();
                            }
                            token.Clear();
                            CurrentLineNumber = GetFileInfo().LineNumber;
                            c = '\"';
                            do
                            {
                                token.Append(c, CurrentLineNumber);
                                c = ReadC();
                            } while(c != '>');
                            token.Append('\"', CurrentLineNumber);
                            if (0 != token.Length)
                            {
                                args.Add(token);
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
                                args.Add(token);
                                token.Clear();
                            }
                        }
                        else
                        {
                            if (token.Length == 0 && args.Count == 0 && c == '#')
                            {
                                is_preprocess = true;
                            }
                            MarkBeginOfLine();
                            while (!IsLSLWhitespace(c) && c != ';' && c != ':' && c != '(' && c != ')' && c != ',' && c != '\"' && c != '\'' && c != '~' && c != '\\' && c != '?' && c != '@' && c != '{' && c != '}' && c != '[' && c != ']')
                            {
                                if(c == '.' && token.Length > 0 && !char.IsNumber(token[token.Length - 1]))
                                {
                                    args.Add(token);
                                    token.Clear();
                                }

                                token.Append(c, CurrentLineNumber);
                                if(token.EndsWith("//"))
                                {
                                    /* got C++-style comment */
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
                                    if(token.Length != 0)
                                    {
                                        args.Add(token);
                                    }
                                    token.Clear();
                                    c = ' ';
                                    goto redo;
                                }
                                if(token.EndsWith("/*"))
                                {
                                    /* got C-style comment */
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
                                    if (token.Length != 0)
                                    {
                                        args.Add(token);
                                    }
                                    token.Clear();
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
                                        args.Add(token);
                                        return;
                                    }
                                    throw;
                                }
                            }
                            args.Add(token);
                            token.Clear();
                            goto redo;
                        }
                        break;
                }
            }
        }

        public void EvalCompounds(List<TokenInfo> args)
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
                    if ('\"' == c || '\'' == c)
                    {
                        break;
                    }
                    else if ((c == 'e' || c == 'E') && i + 1 < curlength && (args[argi][i + 1] == '-' || args[argi][i + 1] == '+') && i > 0 && char.IsDigit(args[argi][i - 1]))
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
                                    args.Insert(argi, new TokenInfo("+=", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("+", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                args.Insert(argi, new TokenInfo(".", args[argi].LineNumber));
                                ++argi;

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
                                    args.Insert(argi, new TokenInfo("-=", args[argi].LineNumber));
                                    ++argi;
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
                                /* compound literal -> */
                                if (2 < curlength)
                                {
                                    args.Insert(argi, new TokenInfo("->", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("-", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("*=", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("*", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("::", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo(":", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("/=", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("/", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("%=", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("%", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("<=", args[argi].LineNumber));
                                    ++argi;
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
                                    args.Insert(argi, new TokenInfo("<<=", args[argi].LineNumber));
                                    ++argi;
                                    args[argi] = args[argi].Substring(3, curlength - 3);
                                    curlength -= 3;
                                }
                                else if (2 < curlength)
                                {
                                    args.Insert(argi, new TokenInfo("<<", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("<", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo(">=", args[argi].LineNumber));
                                    ++argi;
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
                                /* compound literals >> >>= */
                                if (3 < curlength && args[argi][2] == '=')
                                {
                                    args.Insert(argi, new TokenInfo(">>=", args[argi].LineNumber));
                                    ++argi;
                                    args[argi] = args[argi].Substring(3, curlength - 3);
                                    curlength -= 3;
                                }
                                else if (2 < curlength)
                                {
                                    args.Insert(argi, new TokenInfo(">>", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo(">", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("==", args[argi].LineNumber));
                                    ++argi;
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
                                    args.Insert(argi, new TokenInfo("=>", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("=", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("!=", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("!", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("^=", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("^", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("&=", args[argi].LineNumber));
                                    ++argi;
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
                                    args.Insert(argi, new TokenInfo("&&", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("&", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("##", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("#", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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
                                    args.Insert(argi, new TokenInfo("|=", args[argi].LineNumber));
                                    ++argi;
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
                                    args.Insert(argi, new TokenInfo("||", args[argi].LineNumber));
                                    ++argi;
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
                                args.Insert(argi, new TokenInfo("|", args[argi].LineNumber));
                                ++argi;
                                args[argi] = args[argi].Substring(1, curlength - 1);
                                --curlength;
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

        public void ReadPass2(List<TokenInfo> arguments)
        {
            var inargs = new List<TokenInfo>(arguments);
            arguments.Clear();
            foreach(TokenInfo it in inargs)
            {
                if(it == "+++")
                {
                    arguments.Add(new TokenInfo("++", it.LineNumber ));
                    arguments.Add(new TokenInfo("+", it.LineNumber ));
                }
                else if(it == "+++++")
                {
                    arguments.Add(new TokenInfo("++", it.LineNumber ));
                    arguments.Add(new TokenInfo("+", it.LineNumber ));
                    arguments.Add(new TokenInfo("++", it.LineNumber ));
                }
                else if(it == "++++")
                {
                    arguments.Add(new TokenInfo("++", it.LineNumber ));
                    arguments.Add(new TokenInfo("+", it.LineNumber ));
                    arguments.Add(new TokenInfo("+", it.LineNumber ));
                }
                else if(it == "---")
                {
                    arguments.Add(new TokenInfo("--", it.LineNumber ));
                    arguments.Add(new TokenInfo("-", it.LineNumber ));
                }
                else if(it == "----")
                {
                    arguments.Add(new TokenInfo("--", it.LineNumber ));
                    arguments.Add(new TokenInfo("-", it.LineNumber ));
                    arguments.Add(new TokenInfo("-", it.LineNumber ));
                }
                else if(it == "-----")
                {
                    arguments.Add(new TokenInfo("--", it.LineNumber ));
                    arguments.Add(new TokenInfo("-", it.LineNumber ));
                    arguments.Add(new TokenInfo("--", it.LineNumber ));
                }
                else
                {
                    arguments.Add(it);
                }
            }
        }

        public override void Read(List<TokenInfo> args)
        {
            MarkBeginOfLine();
            ReadPass1(args);
            EvalCompounds(args);
            ReadPass2(args);
        }
    }
}
