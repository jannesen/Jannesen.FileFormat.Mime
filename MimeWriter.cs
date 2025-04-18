﻿using System;
using System.IO;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public interface IMimeWriterTo
    {
        bool            WriteHasData                        { get ; }
        void            WriteTo(MimeWriter Writer);
    }

    public sealed class MimeWriter: IDisposable
    {
        public  const       int                 MaxLineWidth = 76;
        private static  readonly    char[]      _lookupTableHex     = [ '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F' ];
        private static  readonly    char[]      _lookupTableBase64  = [ 'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P',
                                                                        'Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d','e','f',
                                                                        'g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v',
                                                                        'w','x','y','z','0','1','2','3','4','5','6','7','8','9','+','/' ];
        private readonly    StreamWriter        _writer;
        private             int                 _linePos;

        public                                  MimeWriter(Stream stream)
        {
            _writer = new StreamWriter(stream, System.Text.Encoding.ASCII, 4096, true);
        }
        public              void                Dispose()
        {
            _writer.Dispose();
        }

        internal            void                WriteHeaderField(string name, string? value)
        {
            if (value != null && value.Length > 0) {
                _write(name);
                _write(": ");

                var encoding = _findCharset(value);

                if (encoding != null)
                    _writeEncoded(encoding, value);
                else
                    _write(value);

                WriteNewLine();
            }
        }
        internal            void                WriteHeaderField(string name, IMimeWriterTo value)
        {
            if (value != null && value.WriteHasData) {
                _write(name);
                _write(": ");
                value.WriteTo(this);
                WriteNewLine();
            }

        }
        internal            void                WriteFieldValue(string? value)
        {
            _write(value);
        }
        internal            void                WriteFieldParameter(MimeField parameter)
        {
            var Name  = parameter.Name;
            var Value = parameter.Value?.Replace("\"", "\"\"");

            _write(';');

            if (_linePos + Name.Length + (Value?.Length ?? 0) + 5 >= MaxLineWidth) {
                WriteNewLine();
                _write("\t");
            }
            else
                _write(' ');

            _write(Name);
            _write('=');
            _write('"');
            _write(Value);
            _write('"');
        }
        internal            void                WriteAddress(string address, string? displayName)
        {
            if (displayName != null) {
                _write('\"');

                var encoding = _findCharset(displayName);

                if (encoding != null)
                    _writeEncoded(encoding, displayName);
                else
                    _write(displayName.Replace("\"", "\\\""));

                _write("\" ");
            }

            _write('<');
            _write(address);
            _write('>');
        }
        internal            void                WriteAddressSep()
        {
            _write(',');
            WriteNewLine();
            _write('\t');
        }
        internal            void                WriteBody(string body)
        {
            var b = Encoding.ASCII.GetBytes(body);

            _writeContent_Text(b, b.Length);
        }
        internal            void                WriteContent(byte[]? content, int contentLength, MimeEncoding encoding)
        {
            if (content != null && contentLength > 0) {
                switch(encoding) {
                case MimeEncoding.Text:
                case MimeEncoding.Text7bit:
                case MimeEncoding.Text8bit:
                    _writeContent_Text(content, contentLength);
                    break;

                case MimeEncoding.QuotedPrintable:
                    _writeContent_QuotedPrintable(content, contentLength);
                    break;

                case MimeEncoding.Base64:
                    _writeContent_Base64(content, contentLength);
                    break;

                default:
                    throw new NotImplementedException("Not implemented WriteContent '"+encoding.ToString()+"' not implemented.");
                }
            }
        }
        internal            void                WriteBoundary(string boundary, bool end)
        {
            _write("--");
            _write(boundary);

            if (end)
                _write("--");

            WriteNewLine();
        }
        internal            void                WriteMimeText()
        {
            _write("This is a multi-part message in MIME format.\r\n\r\n");
        }
        internal            void                WriteNewLine()
        {
            _write("\r\n");
            _linePos = 0;
        }
        internal            void                WriteSoftNewLine()
        {
            _write("=\r\n");
            _linePos = 0;
        }

        private             void                _writeContent_Text(byte[] content, int contentLength)
        {
            for (var i = 0 ; i < contentLength ; ++i) {
                var b = content[i];

                switch(b) {
                case (byte)'\n':
                    WriteNewLine();
                    break;

                case (byte)'\r':
                    break;

                case (byte)'\t':
                    _write((char)b);
                    break;

                default:
                    if (b < 32 || b > 126)
                        throw new MimeException("Invalid character in text.");

                    _write((char)b);
                    break;
                }
            }

            WriteNewLine();
        }
        private             void                _writeContent_QuotedPrintable(byte[] content, int contentLength)
        {
            for (var i = 0 ; i < contentLength ; ++i) {
                var c = content[i];

                switch(c) {
                case (byte)' ':
                case (byte)'\t':
                    if (i < contentLength -1 && content[i+1] != '\r')
                        goto normal;

                    goto encode;

                case (byte)'\r':
                    if (i < contentLength -1 && content[i+1] == '\n') {
                        WriteNewLine();
                        ++i;
                        break;
                    }

                    goto encode;

                case (byte)'.':
                    if (_linePos != 0)
                        goto normal;

                    goto encode;

                case (byte)'-':
                    if (_linePos != 0)
                        goto normal;

                    goto encode;

                case (byte)'=':
                    goto encode;

                default:
                    if (c >= 32 && c <= 126)
                        goto normal;

                    goto encode;

normal:             {
                        if (_linePos >= MaxLineWidth)
                            WriteSoftNewLine();

                        _write((char)c);
                    }
                    break;

encode:             {
                        if (_linePos >= MaxLineWidth - 3)
                            WriteSoftNewLine();

                        _write('=');
                        _write(_lookupTableHex[c >> 4]);
                        _write(_lookupTableHex[c & 0x0f]);
                    }
                    break;
                }
            }

            if (_linePos > 0)
                WriteSoftNewLine();
        }
        private             void                _writeContent_Base64(byte[] content, int contentLength)
        {
            var pos = 0;
            var w = 0;

            while (pos < contentLength) {
                _write(_lookupTableBase64[((content[pos    ] >> 2) & 0x3F)]);

                if (pos + 1 < contentLength) {
                    _write(_lookupTableBase64[((content[pos    ] << 4) & 0x30) | ((content[pos + 1] >> 4) & 0x0F)]);

                    if (pos + 2 < contentLength) {
                        _write(_lookupTableBase64[((content[pos + 1] << 2) & 0x3C) | ((content[pos + 2] >> 6) & 0x03)]);
                        _write(_lookupTableBase64[((content[pos + 2]     ) & 0x3F)]);
                    }
                    else {
                        _write(_lookupTableBase64[((content[pos + 1] << 2) & 0x3C)]);
                        _write('=');
                    }
                }
                else {
                    _write(_lookupTableBase64[((content[pos    ] << 4) & 0x30)]);
                    _write('=');
                    _write('=');
                }

                pos += 3;
                w += 4;
                if (w >= MaxLineWidth) {
                    WriteNewLine();
                    w = 0;
                }
            }

            if (w > 0)
                WriteNewLine();
        }
        private static      Encoding?           _findCharset(string str)
        {
            for (var i = 0 ; i < str.Length ; ++i) {
                if (str[i] >= 0x7F) {
                    return Encoding.GetEncoding("utf-8");
                }
            }

            return null;
        }
        private             void                _writeEncoded(Encoding encoding, string value)
        {
            var data = encoding.GetBytes(value);

            _write("=?");
            _write(encoding.WebName);
            _write("?Q?");

            for(var i = 0 ; i < data.Length ; ++i) {
                var c = (char)data[i];

                if (c == ' ') {
                    _write('_');
                }
                else
                if (c >= 32 && c <= 126 && c != '"' && c != '?' && c != '_') {
                    _write(c);
                }
                else {
                    _write('=');
                    _write(_lookupTableHex[c >> 4]);
                    _write(_lookupTableHex[c & 0x0f]);
                }
            }

            _write("?=");
        }
        private             void                _write(char c)
        {
            _writer.Write(c);
            _linePos++;
        }
        private             void                _write(string? s)
        {
            if (s != null) {
                _writer.Write(s);
                _linePos += s.Length;
            }
        }
    }
}
