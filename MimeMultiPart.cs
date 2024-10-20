using System;
using System.Collections.Generic;
using System.IO;

namespace Jannesen.FileFormat.Mime
{
    public class MimeMultiPart: MimePart
    {
        private             MimeParts?          _parts;

        public              MimeParts?          Parts
        {
            get {
                if (_parts == null && !Fields.ReadOnly)
                    _parts = new MimeParts();

                return _parts;
            }
        }

        public                                  MimeMultiPart()
        {
        }
        protected                               MimeMultiPart(MimeContentType contentType, MimeFields fields, byte[] content) : base(fields, content)
        {
            ParseMultiPart(contentType, new MimeReader(new MemoryStream(content)), null);
        }

        protected           void                ParseMultiPart(MimeContentType contentType, MimeReader reader, StringWriter? bodyWriter)
        {
            ArgumentNullException.ThrowIfNull(contentType);
            ArgumentNullException.ThrowIfNull(reader);

            _parts   = new MimeParts();

            var Boundary = contentType.Boundary ?? throw new MimeException("Invalid multipart-mime-message, missing 'Boundary'.");

            while (true) {
                if (!reader.ReadLine(false))
                    throw new MimeException("Invalid multipart-mime-message, missing begin-boundary.");

                if (reader.TestBoundary(Boundary) == 1)
                    break;

                if (bodyWriter != null)
                    reader.WriteLineTo(bodyWriter);
            }

            while (reader.TestBoundary(Boundary) != -1) {
                if (reader.TestBoundary(Boundary) == 1) {
                    var PartFields                 = reader.ReadFields();
                    var FldContentTransferEncoding = PartFields["Content-Transfer-Encoding"];
                    var PartContent                = reader.ReadData(MimePart.StringToMimeEncoding(FldContentTransferEncoding?.Value), Boundary);
                    var FldPartContentType         = PartFields["Content-Type"];
                    var PartContentType            = FldPartContentType?.ValueContentType;

                    if (PartContentType != null && PartContentType.isMultipart)
                        _parts.Add(new MimeMultiPart(PartContentType, PartFields, PartContent));
                    else
                        _parts.Add(new MimePart(PartFields, PartContent));

                    PartFields.SetCollectionReadOnly();
                }
                else {
                    if (!reader.isLineEmpty)
                        throw new MimeException("Invalid multipart-mime-message, garbage in between parts.");

                    if (!reader.ReadLine(false))
                        throw new MimeException("Invalid multipart-mime-message, missing end-boundary.");
                }
            }


            _parts.SetCollectionReadOnly();
        }

        public   new        bool                WriteHasData
        {
            get {
                return (_parts != null && _parts.Count > 0) || base.WriteHasData;
            }
        }
        internal override   void                WriteContentTo(MimeWriter writer)
        {
            var contentType = Fields["Content-Type"]?.ValueContentType;

            if (contentType != null && contentType.isMultipart) {
                var boundary = contentType.Boundary ?? throw new InvalidOperationException("Multipart ContentType has no boundary.");

                if (_parts == null || _parts.Count == 0)
                    throw new MimeException("multipart without parts.");

                for (var i = 0 ; i < _parts.Count ; ++i) {
                    writer.WriteBoundary(boundary, false);
                    _parts[i].WriteTo(writer);
                }

                writer.WriteBoundary(boundary, true);
            }
            else {
                if (_parts != null && _parts.Count > 0)
                    throw new MimeException("Part is multipart but content-type is not.");

                base.WriteContentTo(writer);
            }
        }
    }
}
