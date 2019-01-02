using System;

namespace Jannesen.FileFormat.Mime
{
    public enum MimeLexicalTokenType
    {
        None            = 0,
        EOL,
        WhiteSpace,
        Atom,
        QuotedString,
        Comment,
        Phrase,
        Address,
        AngleBracketOpen,
        AngleBracketClose,
        DomainLiteral,
        At,
        Comma,
        SemiColon,
        Colon,
        Assign
    }

    public struct MimeLexicalToken
    {
        public          MimeLexicalTokenType    Type;
        public          int                     Begin;
        public          int                     End;

        public  static  MimeLexicalToken        Parse(string str, ref int Position)
        {
            int                 Length = str.Length;
            MimeLexicalToken    rtn = new MimeLexicalToken();

            rtn.Begin =
            rtn.End   = Position;

            if (Position>= Length) {
                rtn.Type = MimeLexicalTokenType.EOL;
                return rtn;
            }

            switch(str[Position]) {
            case ' ':
            case '\t':
            case '\n':
            case '\r':
                while (rtn.End < Length && _isLinearWhiteSpace(str[rtn.End]))
                    ++rtn.End;

                rtn.Type = MimeLexicalTokenType.WhiteSpace;
                break;

            case '<':
                rtn.Type = MimeLexicalTokenType.AngleBracketOpen;
                ++rtn.End;
                break;

            case '>':
                rtn.Type = MimeLexicalTokenType.AngleBracketClose;
                ++rtn.End;
                break;

            case '@':
                rtn.Type = MimeLexicalTokenType.At;
                ++rtn.End;
                break;

            case ',':
                rtn.Type = MimeLexicalTokenType.Comma;
                ++rtn.End;
                break;

            case ';':
                rtn.Type = MimeLexicalTokenType.SemiColon;
                ++rtn.End;
                break;

            case ':':
                rtn.Type = MimeLexicalTokenType.Colon;
                ++rtn.End;
                break;

            case '=':
                rtn.Type = MimeLexicalTokenType.Assign;
                ++rtn.End;
                break;

            case '\"':
                rtn.Type = MimeLexicalTokenType.QuotedString;
                ++rtn.End;

                while (str[rtn.End] != '\"') {
                    if (str[rtn.End] == '\\')
                        ++rtn.End;

                    if (++rtn.End >= Length)
                        throw new MimeException("Unterminated string in '"+str+"'.");
                }

                ++rtn.End;
                break;

            case '(':
                rtn.Type = MimeLexicalTokenType.Comment;
                ++rtn.End;

                while (rtn.End < Length && str[rtn.End] != ')') {
                    if (str[rtn.End] == '\\')
                        ++rtn.End;

                    if (++rtn.End >= Length)
                        throw new MimeException("Unterminated comment in '"+str+"'.");
                }

                ++rtn.End;
                break;

            case '[':
                rtn.Type = MimeLexicalTokenType.DomainLiteral;
                ++rtn.End;

                while (rtn.End < Length && str[rtn.End] != ')') {
                    if (str[rtn.End] == '\\')
                        ++rtn.End;

                    if (++rtn.End >= Length)
                        throw new MimeException("Unterminated domain-literal in '"+str+"'.");
                }

                ++rtn.End;
                break;

            case ')':
            case ']':
                throw new MimeException("Invalid character in '"+str+"'.");

            default:
                rtn.Type = MimeLexicalTokenType.Atom;

                while (rtn.End < Length && !_isSpecial(str[rtn.End])) {
                    if (str[rtn.End] == '\\')
                        ++rtn.End;

                    ++rtn.End;
                }
                break;
            }

            Position = rtn.End;
            return rtn;
        }
        public  static  MimeLexicalToken        ParseSkipWhiteSpaceComment(string str, ref int position)
        {
            MimeLexicalToken    rtn;

            do {
                rtn = Parse(str, ref position);
            }
            while (rtn.Type == MimeLexicalTokenType.WhiteSpace || rtn.Type == MimeLexicalTokenType.Comment);

            return rtn;
        }

        public          void                    AddPhrase(MimeLexicalToken token)
        {
            if (this.Type != MimeLexicalTokenType.None) {
                if (token.Type != MimeLexicalTokenType.None) {
                    this.End   = token.End;
                    this.Type  = MimeLexicalTokenType.Phrase;
                }
            }
            else {
                this.Begin = token.Begin;
                this.End   = token.End;
                this.Type  = token.Type;
            }
        }
        public          void                    AddAddress(MimeLexicalToken token)
        {
            if (this.Type != MimeLexicalTokenType.None) {
                if (token.Type != MimeLexicalTokenType.None) {
                    this.End   = token.End;
                    this.Type  = MimeLexicalTokenType.Address;
                }
            }
            else {
                this.Begin = token.Begin;
                this.End   = token.End;
                this.Type  = token.Type;
            }
        }

        public          void                    TrimTrailingWhiteSpace(string str)
        {
            while (End > 0 && _isLinearWhiteSpace(str[End-1]))
                --End;
        }

        public          string                  GetString(string str)
        {
            string  rtn;

            if (Type == MimeLexicalTokenType.None)
                return null;

            if (Type == MimeLexicalTokenType.QuotedString) {
                if (str[Begin] != '"' || str[End - 1] != '"')
                    throw new MimeException("Internal error, incorrent MimeLexicalTokenType.String");

                rtn = str.Substring(Begin + 1, (End-Begin) - 2);
            }
            else
            if (Type == MimeLexicalTokenType.Comment) {
                if (str[Begin] != '(' || str[End - 1] != ')')
                    throw new MimeException("Internal error, incorrent MimeLexicalTokenType.Comment");

                rtn = str.Substring(Begin + 1, (End-Begin) - 2);
            }
            else
                rtn = str.Substring(Begin, (End-Begin));

            {
                int p = 0;

                while ((p = rtn.IndexOf('\\', p)) >= 0) {
                    rtn = rtn.Remove(p, 1);
                    ++p;
                }
            }

            return rtn;
        }

        private static  bool                    _isLinearWhiteSpace(char c)
        {
            return c == ' '  ||
                   c == '\t';
        }
        private static  bool                    _isSpecial(char c)
        {
            return c == ' '  ||
                   c == '\t' ||
                   c == '(' ||
                   c == ')' ||
                   c == '<' ||
                   c == '>' ||
                   c == '[' ||
                   c == ']' ||
                   c == '@' ||
                   c == ',' ||
                   c == ';' ||
                   c == ':' ||
                   c == '=' ||
                   c == '"';
        }
    }
}
