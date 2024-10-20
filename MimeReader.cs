using System;
using System.IO;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public sealed class MimeReader
    {
        private readonly    Stream          _stream;
        private             int             _unChar;
        private             int             _position;
        private             int             _positionBeginMessage;
        private readonly    char[]          _curLine;
        private             int             _curLength;
        private readonly    byte[]          _decode_buf_in;
        private readonly    byte[]          _decode_buf_out;

        public              int             PositionBeginMessage
        {
            get {
                return _positionBeginMessage;
            }
        }
        public              bool            isLineEmpty
        {
            get {
                return _curLength == 0;
            }
        }

        public                              MimeReader(Stream stream)
        {
            _stream               = stream;
            _unChar               = -1;
            _positionBeginMessage = -1;
            _position             = 0;
            _curLength            = 0;
            _curLine              = new char[1024];
            _decode_buf_in        = new byte[4];
            _decode_buf_out       = new byte[3];
        }

        public              MimeFields      ReadFields()
        {
            bool            xheader = true;
            MimeFields      fields  = new MimeFields();

            while (ReadLine(true) && _curLength>0) {
                int i = _curLineIndexOf(':');
                if (i<0)
                    throw new MimeReaderException("Syntax error in field ':' not found).");

                if (i<_curLength-1 && _curLine[i+1] != ' ')
                    throw new MimeReaderException("Syntax error in field ': ' not found).");

                string  Name  = _curLineToString(0, i);
                string  Value = _curLineToString(i+2, _curLength-(i+2));

                if (xheader) {
                    if (Name.StartsWith("x-", StringComparison.Ordinal))
                        _positionBeginMessage = _position;
                    else
                        xheader = false;
                }

                fields.Add(new MimeField(Name, Value, true));
            }

            return fields;
        }
        public              byte[]          ReadData(MimeEncoding encoding, string boundary)
        {
            using(MemoryStream dataStream = new MemoryStream()) {
                while (true) {
                    if (!ReadLine(false))
                        return dataStream.ToArray();

                    if (isLineEmpty) {
                        switch(encoding) {
                        case MimeEncoding.Base64:
                        case MimeEncoding.UUEncode:
                            return dataStream.ToArray();
                        }
                    }

                    if (boundary != null && TestBoundary(boundary) != 0)
                        return dataStream.ToArray();

                    switch(encoding) {
                    case MimeEncoding.QuotedPrintable:  _decodeQuotedPrintableTo(dataStream);   break;
                    case MimeEncoding.Base64:           _decodeBase64To(dataStream);            break;
                    case MimeEncoding.UUEncode:         _decodeUUEncodeTo(dataStream);          break;
                    default:                            _decodeTextTo(dataStream);              break;
                    }
                }
            }
        }

        public              bool            ReadLine(bool unfolding)
        {
            int     pos = 0;
            int     c;

            if (_unChar != -1) {
                c = _unChar;
                _unChar = -1;
                ++_position;

                if (c=='\n') {
                    _curLength = 0;
                    return true;
                }

                if (c != '\r')
                    _curLine[pos++] = (char)c;
            }

            for (;;) {
                c = _stream.ReadByte();
                if (c == -1)
                    break;

                ++_position;

                if (c=='\n') {
                    if (pos==0 || !unfolding)
                        break;

                    c = _stream.ReadByte();
                    ++_position;

                    if (c != '\t' && c != ' ') {
                        --_position;
                        _unChar = c;
                        break;
                    }
                    else
                        c = ' ';
                }

                if (c != '\r') {
                    if (pos >= _curLine.Length)
                        throw new MimeReaderException("Invalid mime-message, line to long.");

                    _curLine[pos++] = (char)c;
                }
            }

            _curLength = pos;

            return (c != -1 || pos > 0);
        }
        internal            int             TestBoundary(string boundary)
        {
            if (_curLength < 2+boundary.Length)
                return 0;

            if (_curLine[0] != '-' || _curLine[1] != '-')
                return 0;

            for (int i=0 ; i<boundary.Length ; ++i)
                if (_curLine[2+i] != boundary[i])
                    return 0;

            if (_curLength == 2+boundary.Length)
                return 1;


            if (_curLength == 2+boundary.Length+2) {
                if (_curLine[2+boundary.Length]=='-' || _curLine[2+boundary.Length+1]=='-')
                    return -1;
            }

            return 0;
        }
        internal            void            WriteLineTo(StringWriter content)
        {
            content.WriteLine(_curLine, 0, _curLength);
        }

        private             void            _decodeTextTo(MemoryStream content)
        {
            ArgumentNullException.ThrowIfNull(content);

            for (int i = 0 ; i < _curLength ; ++i)
                content.WriteByte((byte)_curLine[i]);

            content.WriteByte((byte)'\r');
            content.WriteByte((byte)'\n');
        }
        private             void            _decodeQuotedPrintableTo(MemoryStream content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int     pos = 0;

            while (pos<_curLength) {
                int     c = _curLine[pos++];

                if (c == '=') {
                    if (pos+2>_curLength)
                        return ;

                    c = (_hexCharToNibble(_curLine[pos]) << 4) | _hexCharToNibble(_curLine[pos + 1]);

                    pos += 2;
                }

                content.WriteByte((byte)c);
            }

            content.WriteByte((byte)'\r');
            content.WriteByte((byte)'\n');
        }
        private             void            _decodeBase64To(MemoryStream content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int     pos = 0;

            while (pos<_curLength) {
                int i = 0;
                int n = 3;

                while (i<4 && pos<_curLength) {
                    int     c = _curLine[pos++];

                    if (c >= 'A' && c <= 'Z')   _decode_buf_in[i] = (byte)(c - 'A');
                    else
                    if (c >= 'a' && c <= 'z')   _decode_buf_in[i] = (byte)(c - 'a' + 26);
                    else
                    if (c >= '0' && c <= '9')   _decode_buf_in[i] = (byte)(c - '0' + 52);
                    else
                    if (c == '+')               _decode_buf_in[i] = 62;
                    else
                    if (c == '/')               _decode_buf_in[i] = 63;
                    else
                    if (c == '=') {
                        _decode_buf_in[i] = 0;

                        if (n > (i * 6 / 8))
                            n = (i * 6 / 8);
                    }
                    else
                        throw new MimeReaderException("Bad base64 data.");

                    ++i;
                }

                if (i<4) {
                    if (n > (i * 6 / 8))
                        throw new MimeReaderException("Bad base64 data.");
                }

                _decode_buf_out[0] = (byte)(_decode_buf_in[0]<<2 | _decode_buf_in[1]>>4);
                _decode_buf_out[1] = (byte)(_decode_buf_in[1]<<4 | _decode_buf_in[2]>>2);
                _decode_buf_out[2] = (byte)(_decode_buf_in[2]<<6 | _decode_buf_in[3]);

                content.Write(_decode_buf_out, 0, n);
            }
        }
        private             void            _decodeUUEncodeTo(MemoryStream content)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (_curLength>0) {
                int     pos = 1;
                int     n   = (_curLine[0] - 32);

                if (n<0 || n>64)
                    throw new MimeReaderException("Bad UUEncoded data.");

                for (int b = 0 ; b < n ; b+=3) {
                    for (int i = 0 ; i < 4 ; ++i) {
                        if (pos<_curLength) {
                            int     c = _curLine[pos++];

                            if (c < 32 || c >= 32+64)
                                throw new MimeReaderException("Bad UUEncoded data.");

                            _decode_buf_in[i] = (byte)(c-32);
                        }
                        else
                            _decode_buf_in[i] = 0;
                    }

                    _decode_buf_out[b+0] = (byte)(_decode_buf_in[0]<<2 | _decode_buf_in[1]>>4);
                    _decode_buf_out[b+1] = (byte)(_decode_buf_in[1]<<4 | _decode_buf_in[2]>>2);
                    _decode_buf_out[b+2] = (byte)(_decode_buf_in[2]<<6 | _decode_buf_in[3]);
                }

                content.Write(_decode_buf_out, 0, n);
            }
        }

        private             int             _curLineIndexOf(char chr)
        {
            for (int i=0 ; i<_curLength ; ++i) {
                if (_curLine[i] == chr)
                    return i;
            }

            return -1;
        }
        private             string          _curLineToString(int offset, int length)
        {
            if (length>0)
                return new string(_curLine, offset, length);
            else
                return string.Empty;
        }

        private static      byte            _hexCharToNibble(char c)
        {
            if (c >= '0' && c <= '9')       return (byte)(c - '0'     );
            if (c >= 'A' && c <= 'F')       return (byte)(c - 'A' + 10);

            return 0;
        }
    }
}
