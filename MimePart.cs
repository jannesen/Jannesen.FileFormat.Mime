using System;
using System.Collections.Generic;
using System.IO;

namespace Jannesen.FileFormat.Mime
{
    public class MimePart : IMimeWriterTo
    {
        private             MimeFields              _fields;
        private             byte[]                  _content;
        private             int                     _contentLength;

        public              MimeFields              Fields
        {
            get {
                return _fields;
            }
        }
        public              string                  Name
        {
            get {
                try {
                    MimeContentType ct = ContentType;

                    if (ct != null)
                        return ct.Name;
                }
                catch(Exception) {
                }

                return null;
            }
        }
        public              MimeContentType         ContentType
        {
            get {
                return _fields.Get("Content-Type")?.ValueContentType;
            }
            set {
                _setMimeVersion();
                _fields.Set("Content-Type").ValueContentType = value;
            }
        }
        public              MimeEncoding            ContentTransferEncoding
        {
            get {
                MimeField   fld = Fields["Content-Transfer-Encoding"];

                return (fld != null) ? StringToMimeEncoding(fld.Value) : MimeEncoding.Text;
            }
            set {
                _setMimeVersion();
                Fields.Set("Content-Transfer-Encoding").Value = MimeEncodingToString(value);
            }
        }
        public              MimeContentDisposition  ContentDisposition
        {
            get {
                return _fields.Get("Content-Disposition")?.ValueContentDisposition;
            }
            set {
                _setMimeVersion();
                _fields.Set("Content-Disposition").ValueContentDisposition = value;
            }
        }
        public              string                  ContentDescription
        {
            get {
                return _fields.Get("Content-Description").Value;
            }
            set {
                _setMimeVersion();
                _fields.Set("Content-Description").Value = value;
            }
        }
        public              string                  ContentLocation
        {
            get {
                return _fields.Get("Content-Location")?.Value;
            }
            set {
                _setMimeVersion();
                _fields.Set("Content-Location").Value = value;
            }
        }
        public              string                  ContentID
        {
            get {
                return _fields.Get("Content-ID")?.Value;
            }
            set {
                _setMimeVersion();
                _fields.Set("Content-ID").Value = "<" + value +">";
            }
        }
        public              bool                    hasContent
        {
            get {
                return _content != null;
            }
        }

        public                                      MimePart()
        {
            _fields    = new MimeFields();
            _content   = null;
        }
        public                                      MimePart(MimeFields fields, byte[] content)
        {
            ArgumentNullException.ThrowIfNull(fields);
            ArgumentNullException.ThrowIfNull(content);

            _fields        = fields;
            _content       = content;
            _contentLength = content.Length;
        }

        public              Stream                  GetContentStream()
        {
            return (_content != null) ? new MemoryStream(_content, 0, _contentLength, false) : null;
        }
        public              void                    SetTextContent(string text, System.Text.Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(encoding);

            ContentType = MimeContentType.NewTextPlain();
            ContentType.Charset = encoding.BodyName;
            ContentTransferEncoding = (encoding.BodyName == "us-ascii") ? MimeEncoding.Text7bit : MimeEncoding.QuotedPrintable;
            SetContent(encoding.GetBytes(text));
        }
        public              void                    SetContentFromFile(string fileName)
        {
            using (Stream File = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                SetContentFromFile(File);
        }
        public              void                    SetContentFromFile(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            byte[]  buf = new byte[4096];
            int     rs;

            using (MemoryStream content = new MemoryStream()) {
                while ((rs = stream.Read(buf, 0, buf.Length)) > 0)
                    content.Write(buf, 0, rs);

                SetContent(content.ToArray());
            }
        }
        public              void                    SetContent(byte[] content)
        {
            ArgumentNullException.ThrowIfNull(content);

            SetContent(content, content.Length);
        }
        public              void                    SetContent(byte[] content, int length)
        {
            if (_fields.ReadOnly)
                throw new MimeException("Not allowed to SetFields.");

            _content       = content;
            _contentLength = length;
        }

        public  static      MimePart                NewAttachment(string name, MimeContentType contentType, byte[] data, int length)
        {
            MimePart    part = new MimePart() {
                                   ContentType             = contentType,
                                   ContentDisposition      = MimeContentDisposition.NewAttachment(name),
                                   ContentTransferEncoding = MimeEncoding.Base64
                               };
            part.ContentType.Name = name;
            part.SetContent(data, length);

            return part;
        }

        public  static      MimeEncoding            StringToMimeEncoding(string encodingText)
        {
            if (encodingText == null)
                return MimeEncoding.Text;

            if (string.Equals(encodingText, "7bit",                StringComparison.OrdinalIgnoreCase))   return MimeEncoding.Text7bit;
            if (string.Equals(encodingText, "8bit",                StringComparison.OrdinalIgnoreCase))   return MimeEncoding.Text8bit;
            if (string.Equals(encodingText, "binary",              StringComparison.OrdinalIgnoreCase))   return MimeEncoding.Binary;
            if (string.Equals(encodingText, "quoted-printable",    StringComparison.OrdinalIgnoreCase))   return MimeEncoding.QuotedPrintable;
            if (string.Equals(encodingText, "base64",              StringComparison.OrdinalIgnoreCase))   return MimeEncoding.Base64;
            if (string.Equals(encodingText, "uuencode",            StringComparison.OrdinalIgnoreCase))   return MimeEncoding.UUEncode;

            return MimeEncoding.Unknown;
        }
        public  static      string                  MimeEncodingToString(MimeEncoding encoding)
        {
            switch(encoding) {
            case MimeEncoding.Text7bit:         return "7bit";
            case MimeEncoding.Text8bit:         return "8bit";
            case MimeEncoding.Binary:           return "binary";
            case MimeEncoding.QuotedPrintable:  return "quoted-printable";
            case MimeEncoding.Base64:           return "base64";
            case MimeEncoding.UUEncode:         return "uuencode";
            default:                            throw new ArgumentException("Invalid value for MimeEncoding");
            }
        }

        protected           void                    SetFields(MimeFields fields)
        {
            _fields = fields;
        }

        public   virtual    bool                    WriteHasData
        {
            get {
                return (_fields != null && _fields.Count > 0) || _content != null;
            }
        }
        public              void                    WriteTo(MimeWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            Fields.WriteTo(writer);
            writer.WriteNewLine();

            if (this is MimeMessage)
                writer.WriteMimeText();

            WriteContentTo(writer);
        }
        internal virtual    void                    WriteContentTo(MimeWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteContent(_content, _contentLength, ContentTransferEncoding);
        }

        private             void                    _setMimeVersion()
        {
            if (this is MimeMessage) {
                if (_fields["MIME-Version"] == null)
                    _fields.Add(new MimeField("MIME-Version", "1.0"));
            }
        }
    }

    public class MimeParts : List<MimePart>
    {
        private             bool                    _ReadOnly;

        public              bool                    ReadOnly
        {
            get {
                return ReadOnly;
            }
        }

        public                                      MimeParts()
        {
            _ReadOnly = false;
        }

        public  new         void                    Add(MimePart part)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to add part.");

            base.Add(part);
        }
        public  new         void                    AddRange(IEnumerable<MimePart> parts)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to add parts.");

            base.AddRange(parts);
        }
        public  new         void                    Clear()
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to clear addresses.");

            base.Clear();
        }
        public  new         void                    Insert(int index, MimePart part)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to insert part.");

            base.Insert(index, part);
        }
        public  new         void                    InsertRange(int index, IEnumerable<MimePart> parts)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to insert parts.");

            base.InsertRange(index, parts);
        }
        public  new         void                    Remove(MimePart part)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to remove part.");

            base.Remove(part);
        }
        public  new         void                    RemoveAll(Predicate<MimePart> match)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to remove part.");

            base.RemoveAll(match);
        }
        public  new         void                    RemoveAt(int index)
        {
            if (_ReadOnly)
                throw new MimeException("Not allowed to remove part.");

            base.RemoveAt(index);
        }

        public              void                    SetCollectionReadOnly()
        {
            _ReadOnly = true;
        }
    }
}
