using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public class MimeParameterField : IMimeWriterTo
    {
        private             string              _type;
        private             MimeFields          _parameters;
        private             bool                _readOnly;

        public              string              Type
        {
            get {
                return _type;
            }
            set {
                if (_readOnly)
                    throw new MimeException("Not allowed to change type");

                _type = value;
            }
        }
        public              MimeFields          Parameters
        {
            get {
                if (_parameters == null) {
                    _parameters = new MimeFields();

                    if (_readOnly)
                        _parameters.SetCollectionReadOnly();
                }

                return _parameters;
            }
        }

        protected                               MimeParameterField()
        {
            _readOnly = false;
        }
        public                                  MimeParameterField(string type)
        {
            _type = type;
            _readOnly = false;
        }

        internal static     MimeParameterField  Parse(string mimeValue)
        {
            return Parse(mimeValue, false);
        }
        internal static     MimeParameterField  Parse(string mimeValue, bool readOnly)
        {
            MimeParameterField      rtn = new MimeParameterField();

            rtn.MimeParse(mimeValue, readOnly);

            return rtn;
        }

        public              bool                WriteHasData
        {
            get {
                return _type != null || (_parameters != null && _parameters.Count > 0);
            }
        }
        public              void                WriteTo(MimeWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteFieldValue(_type);

            if (_parameters != null) {
                for(int i = 0 ; i < Parameters.Count ; ++i)
                    writer.WriteFieldParameter(Parameters[i]);
            }
        }

        public  override    string              ToString()
        {
            StringBuilder   rtn = new StringBuilder();

            rtn.Append(_type);

            if (_parameters != null) {
                for(int i = 0 ; i < Parameters.Count ; ++i) {
                    MimeField   Parameter = Parameters[i];

                    rtn.Append("; ");
                    rtn.Append(Parameter.Name);
                    rtn.Append('=');
                    rtn.Append('"');
                    rtn.Append(Parameter.Value.Replace("\"", "\"\""));
                    rtn.Append('"');
                }
            }

            return rtn.ToString();
        }

        protected           void                SetType(string type)
        {
            _type = type;
        }
        protected           void                MimeParse(string mimeValue, bool readOnly)
        {
            int         Position = 0;

            if (mimeValue is null) {
                if (readOnly)
                    throw new ArgumentNullException(nameof(mimeValue));

                return;
            }

            try {
                MimeLexicalToken    typeToken;
                MimeLexicalToken    sepToken;
                MimeLexicalToken    nameToken;
                MimeLexicalToken    valueToken;

                typeToken = MimeLexicalToken.Parse(mimeValue, ref Position);

                if (typeToken.Type != MimeLexicalTokenType.Atom)
                    throw new Exception("invalid type");

                _type = typeToken.GetString(mimeValue);

                while ((sepToken = MimeLexicalToken.ParseSkipWhiteSpaceComment(mimeValue, ref Position)).Type == MimeLexicalTokenType.SemiColon) {
                    nameToken = MimeLexicalToken.ParseSkipWhiteSpaceComment(mimeValue, ref Position);
                    if (nameToken.Type != MimeLexicalTokenType.Atom)
                        throw new Exception("invalid paramater name.");

                    if (MimeLexicalToken.ParseSkipWhiteSpaceComment(mimeValue, ref Position).Type != MimeLexicalTokenType.Assign)
                        throw new Exception("invalid paramater name.");

                    valueToken = MimeLexicalToken.ParseSkipWhiteSpaceComment(mimeValue, ref Position);
                    if (valueToken.Type != MimeLexicalTokenType.Atom && valueToken.Type != MimeLexicalTokenType.QuotedString)
                        throw new Exception("invalid paramater value.");

                    if (_parameters == null)
                        _parameters = new MimeFields();

                    _parameters.Add(new MimeField(nameToken.GetString(mimeValue), valueToken.GetString(mimeValue)));
                }

                if (sepToken.Type != MimeLexicalTokenType.EOL)
                    throw new Exception("extra data.");

                if (readOnly) {
                    _readOnly = true;
                    _parameters?.SetCollectionReadOnly();
                }
            }
            catch(Exception Err) {
                throw new MimeException("Invalid mime field value '"+mimeValue+"',"+Err.Message);
            }
        }
    }
}
