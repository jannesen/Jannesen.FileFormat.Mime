using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public class MimeAddress: IMimeWriterTo
    {
        private             string?             _address;
        private             string?             _displayName;
        private readonly    bool                _readOnly;

        public              string?             Address
        {
            get {
                return _address;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change Address.");

                _address = value;
            }
        }
        public              string?             DisplayName
        {
            get {
                return _displayName;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change DisplayName.");

                _displayName = value;
            }
        }

        public                                  MimeAddress()
        {
        }
        public                                  MimeAddress(string address)
        {
            _address     = address;
            _displayName = null;
        }
        public                                  MimeAddress(string address, string? displayName)
        {
            _address     = address;
            _displayName = (displayName != null && displayName.Length > 0) ? displayName : null;
        }
        public                                  MimeAddress(string address, string? displayName, bool readOnly)
        {
            _address     = address;
            _displayName = (displayName != null && displayName.Length > 0) ? displayName : null;
            _readOnly    = readOnly;
        }

        internal static     MimeAddress         Parse(string mimeAddressString)
        {
            return Parse(mimeAddressString, false);
        }
        internal static     MimeAddress         Parse(string? mimeAddressString, bool readOnly)
        {
            if (mimeAddressString == null) {
                if (readOnly)
                    throw new ArgumentNullException(nameof(mimeAddressString));

                return new MimeAddress();
            }

            try {
                var Position = 0;
                var rtn = Parse(mimeAddressString, ref Position, readOnly);

                if (Position < mimeAddressString.Length)
                    throw new MimeException("data after address.");

                return rtn;
            }
            catch(Exception err) {
                throw new MimeException("Invalid address '"+mimeAddressString+"', "+err.Message);
            }
        }
        internal static     MimeAddress         Parse(string mimeAddressString, ref int position, bool readOnly)
        {
            var addressesToken      = new MimeLexicalToken();
            var displayNameToken    = new MimeLexicalToken();

            var curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

            if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

            if (curToken.Type == MimeLexicalTokenType.Atom) {
                var tempToken = curToken;

                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

                if (curToken.Type == MimeLexicalTokenType.At) {
                    addressesToken = tempToken;
                    addressesToken.AddAddress(curToken);

                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
                    if (curToken.Type != MimeLexicalTokenType.Atom)
                        throw new MimeException("invalid address.");

                    addressesToken.AddAddress(curToken);

                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
                }
                else {
                    displayNameToken = tempToken;

                    while (curToken.Type == MimeLexicalTokenType.Atom || curToken.Type == MimeLexicalTokenType.WhiteSpace) {
                        displayNameToken.AddPhrase(curToken);
                        curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
                    }

                    displayNameToken.TrimTrailingWhiteSpace(mimeAddressString);
                }
            }
            else
            if (curToken.Type == MimeLexicalTokenType.QuotedString) {
                displayNameToken = curToken;

                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

                if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
            }

            if (curToken.Type == MimeLexicalTokenType.AngleBracketOpen) {
                if (addressesToken.Type != MimeLexicalTokenType.None)
                    throw new MimeException("dubble address");

                while ((curToken = MimeLexicalToken.Parse(mimeAddressString, ref position)).Type == MimeLexicalTokenType.Atom ||
                        curToken.Type == MimeLexicalTokenType.At)
                    addressesToken.AddAddress(curToken);

                if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

                if (curToken.Type != MimeLexicalTokenType.AngleBracketClose)
                    throw new MimeException("missing '>'.");

                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
            }

            if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

            if (curToken.Type == MimeLexicalTokenType.Comment) {
                if (displayNameToken.Type == MimeLexicalTokenType.None)
                    displayNameToken = curToken;

                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
            }

            if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

            if (addressesToken.Type == MimeLexicalTokenType.None)
                throw new MimeException("missing address");

            if (curToken.Type != MimeLexicalTokenType.EOL && curToken.Type != MimeLexicalTokenType.WhiteSpace)
                position = curToken.Begin;

            return new MimeAddress(addressesToken.GetString(mimeAddressString) ?? throw new InvalidOperationException("Parse failed."),
                                   displayNameToken.GetString(mimeAddressString), readOnly);
        }

        public              bool                WriteHasData
        {
            get {
                return _address != null;
            }
        }
        public              void                WriteTo(MimeWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            if (_address != null) {
                writer.WriteAddress(_address, _displayName);
            }
        }

        public  override    string              ToString()
        {
            var rtn = "";

            if (_displayName != null)
                rtn += "\"" + _displayName.Replace("\"", "\\\"") + "\" ";

            rtn += "<" + _address + ">";

            return rtn;
        }
    }
}
