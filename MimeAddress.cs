using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public class MimeAddress: IMimeWriterTo
    {
        private             string              _address;
        private             string              _displayName;
        private             bool                _readOnly;

        public              string              Address
        {
            get {
                return _address;
            }
            set {
                if (_readOnly)
                    throw new MimeException("Not allowed to change Address.");

                _address = value;
            }
        }
        public              string              DisplayName
        {
            get {
                return _displayName;
            }
            set {
                if (_readOnly)
                    throw new MimeException("Not allowed to change DisplayName.");

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
        public                                  MimeAddress(string address, string displayName)
        {
            _address     = address;
            _displayName = (displayName != null && displayName.Length > 0) ? displayName : null;
        }
        public                                  MimeAddress(string address, string displayName, bool readOnly)
        {
            _address     = address;
            _displayName = (displayName != null && displayName.Length > 0) ? displayName : null;
            _readOnly    = readOnly;
        }

        public  static      MimeAddress         Parse(string mimeAddressString)
        {
            return Parse(mimeAddressString, false);
        }
        public  static      MimeAddress         Parse(string mimeAddressString, bool readOnly)
        {
            if (mimeAddressString == null) {
                if (readOnly)
                    throw new ArgumentNullException("MimeAddressString");

                return new MimeAddress();
            }

            try {
                int Position = 0;

                MimeAddress rtn = Parse(mimeAddressString, ref Position, readOnly);

                if (Position < mimeAddressString.Length)
                    throw new MimeException("data after address.");

                return rtn;
            }
            catch(Exception err) {
                throw new MimeException("Invalid address '"+mimeAddressString+"', "+err.Message);
            }
        }
        public  static      MimeAddress         Parse(string mimeAddressString, ref int position, bool readOnly)
        {
            MimeLexicalToken    addressesToken      = new MimeLexicalToken();
            MimeLexicalToken    displayNameToken    = new MimeLexicalToken();
            MimeLexicalToken    tempToken           = new MimeLexicalToken();
            MimeLexicalToken    curToken;

            curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

            if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

            if (curToken.Type == MimeLexicalTokenType.Atom) {
                tempToken = curToken;

                curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

                if (curToken.Type == MimeLexicalTokenType.At) {
                    addressesToken = tempToken;
                    addressesToken.AddAddress(curToken);

                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);
                    if (curToken.Type != MimeLexicalTokenType.Atom)
                        throw new Exception("invalid address.");

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
                    throw new Exception("dubble address");

                if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

                while ((curToken = MimeLexicalToken.Parse(mimeAddressString, ref position)).Type == MimeLexicalTokenType.Atom ||
                        curToken.Type == MimeLexicalTokenType.At)
                    addressesToken.AddAddress(curToken);

                if (curToken.Type == MimeLexicalTokenType.WhiteSpace)
                    curToken = MimeLexicalToken.Parse(mimeAddressString, ref position);

                if (curToken.Type != MimeLexicalTokenType.AngleBracketClose)
                    throw new Exception("missing '>'.");

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
                throw new Exception("missing address");

            if (curToken.Type != MimeLexicalTokenType.EOL && curToken.Type != MimeLexicalTokenType.WhiteSpace)
                position = curToken.Begin;

            return new MimeAddress(addressesToken.GetString(mimeAddressString), displayNameToken.GetString(mimeAddressString), readOnly);
        }

        public              bool                WriteHasData
        {
            get {
                return _address != null;
            }
        }
        public              void                WriteTo(MimeWriter writer)
        {
            if (_displayName != null)
                writer.WriteDisplayName(_displayName);

            writer.WriteAddress(_address);
        }

        public  override    string              ToString()
        {
            string  rtn = "";

            if (_displayName != null)
                rtn += "\"" + _displayName.Replace("\"", "\\\"") + "\" ";

            rtn += "<" + _address + ">";

            return rtn;
        }
    }
}
