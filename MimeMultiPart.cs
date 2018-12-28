/*@
    Copyright � Jannesen Holding B.V. 2002-2010.
    Unautorised reproduction, distribution or reverse eniginering is prohibited.
*/
using System;
using System.Collections.Generic;
using System.IO;

namespace Jannesen.FileFormat.Mime
{
    public class MimeMultiPart : MimePart
    {
        private             MimeParts           _parts;

        public              MimeParts           Parts
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

        protected           void                ParseMultiPart(MimeContentType contentType, MimeReader reader, StringWriter bodyWriter)
        {
            _parts   = new MimeParts();

            string Boundary = contentType.Boundary;
            if (Boundary == null)
                throw new MimeException("Invalid multipart-mime-message, missing 'Boundary'.");

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
                    MimeFields      PartFields                 = reader.ReadFields();
                    MimeField       FldContentTransferEncoding = PartFields["Content-Transfer-Encoding"];
                    byte[]          PartContent                = reader.ReadData(MimePart.StringToMimeEncoding(FldContentTransferEncoding?.Value), Boundary);
                    MimeField       FldPartContentType         = PartFields["Content-Type"];
                    MimeContentType PartContentType            = FldPartContentType?.ValueContentType;

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

        public  new         bool                WriteHasData
        {
            get {
                return (_parts != null && _parts.Count > 0) || base.WriteHasData;
            }
        }
        public  override    void                WriteContentTo(MimeWriter writer)
        {
            MimeField   FldContentType = Fields["Content-Type"];

            if (FldContentType != null && FldContentType.ValueContentType.isMultipart) {
                for (int i = 0 ; i < _parts.Count ; ++i) {
                    writer.WriteBoundary(ContentType.Boundary, false);
                    _parts[i].WriteTo(writer);
                }

                writer.WriteBoundary(ContentType.Boundary, true);
            }
            else {
                if (_parts != null && _parts.Count > 0)
                    throw new MimeException("Part is multipart but content-type is not.");

                base.WriteContentTo(writer);
            }
        }
    }
}
