using System;
using System.Collections.Generic;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public class MimeContentType: MimeParameterField
    {
        public  const       string              MessageRFC822                           = "message/rfc822";
        public  const       string              ApplicationZip                          = "application/x-zip-compressed";
        public  const       string              ApplicationMSExcel                      = "application/vnd.ms-excel";
        public  const       string              ApplicationEdifact                      = "application/edifact";
        public  const       string              ApplicationEdine                        = "application/edine";
        public  const       string              ApplicationPdf                          = "application/pdf";
        public  const       string              ApplicationXPKCS7Signature              = "application/x-pkcs7-signature";
        public  const       string              ApplicationPKCS7Signature               = "application/pkcs7-signature";
        public  const       string              ApplicationOctetStream                  = "application/octet-stream";
        public  const       string              TextPlain                               = "text/plain";
        public  const       string              TextHtml                                = "text/html";
        public  const       string              TextCsv                                 = "text/csv";
        public  const       string              TextXml                                 = "text/xml";
        public  const       string              TextXsl                                 = "text/xsl";
        public  const       string              MultipartAlternative                    = "multipart/alternative";
        public  const       string              MultipartMixed                          = "multipart/mixed";
        public  const       string              MultipartRelated                        = "multipart/related";
        public  const       string              MultipartSigned                         = "multipart/signed";
        public  static      MimeContentType     NewMessageRFC822()                      { return new MimeContentType(MessageRFC822);                }
        public  static      MimeContentType     NewApplicationZip()                     { return new MimeContentType(ApplicationZip);               }
        public  static      MimeContentType     NewApplicationMSExcel()                 { return new MimeContentType(ApplicationMSExcel);           }
        public  static      MimeContentType     NewApplicationEdifact()                 { return new MimeContentType(ApplicationEdifact);           }
        public  static      MimeContentType     NewApplicationEdine()                   { return new MimeContentType(ApplicationEdine);             }
        public  static      MimeContentType     NewApplicationPdf()                     { return new MimeContentType(ApplicationPdf);               }
        public  static      MimeContentType     NewApplicationXPKCS7Signature()         { return new MimeContentType(ApplicationXPKCS7Signature);   }
        public  static      MimeContentType     NewApplicationPKCS7Signature()          { return new MimeContentType(ApplicationPKCS7Signature);    }
        public  static      MimeContentType     NewApplicationOctetStream()             { return new MimeContentType(ApplicationOctetStream);       }
        public  static      MimeContentType     NewTextPlain()                          { return new MimeContentType(TextPlain);                    }
        public  static      MimeContentType     NewTextHtml()                           { return new MimeContentType(TextHtml);                     }
        public  static      MimeContentType     NewTextCsv()                            { return new MimeContentType(TextCsv);                      }
        public  static      MimeContentType     NewTextXml()                            { return new MimeContentType(TextXml);                      }
        public  static      MimeContentType     NewTextXsl()                            { return new MimeContentType(TextXsl);                      }
        public  static      MimeContentType     NewMultipartAlternative()               { return new MimeContentType(MultipartAlternative);         }
        public  static      MimeContentType     NewMultipartMixed()                     { return new MimeContentType(MultipartMixed);               }
        public  static      MimeContentType     NewMultipartRelated()                   { return new MimeContentType(MultipartRelated);             }
        public  static      MimeContentType     NewMultipartSigned()                    { return new MimeContentType(MultipartSigned);              }

        public              bool                isMultipart
        {
            get {
                return Type.Length>10 && string.Compare(Type, 0, "multipart/", 0, 10, StringComparison.OrdinalIgnoreCase)==0;
            }
        }
        public              string              Boundary
        {
            get {
                return Parameters.Value("boundary");
            }
            set {
                Parameters.Set("boundary").Value = value;
            }
        }
        public              string              Charset
        {
            get {
                return Parameters.Value("charset");
            }
            set {
                Parameters.Set("charset").Value = value;
            }
        }
        public              Encoding            Encoding
        {
            get {
                string  charset = this.Charset;

                if (charset==null)
                    return Encoding.ASCII;

                return Encoding.GetEncoding(charset);
            }
        }
        public              string              Name
        {
            get {
                return Parameters.Value("name");
            }
            set {
                Parameters.Set("name").Value = value;
            }
        }

        protected                               MimeContentType()
        {
        }

        public                                  MimeContentType(string type) : base(type)
        {
            if (isMultipart)
                Boundary = "boundary_" + System.Guid.NewGuid().ToString().Replace("-", "");
        }

        internal static new MimeContentType     Parse(string mimeValue)
        {
            return Parse(mimeValue, false);
        }
        internal static new MimeContentType     Parse(string mimeValue, bool readOnly)
        {
            MimeContentType     rtn = new MimeContentType();

            rtn.MimeParse(mimeValue, readOnly);

            return rtn;
        }

        public  static      MimeContentType     FromFileExtension(string fileName)
        {
            string  Extension = System.IO.Path.GetExtension(fileName);

            if (string.Equals(Extension, ".eml", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(MessageRFC822);
            if (string.Equals(Extension, ".zip", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(ApplicationZip);
            if (string.Equals(Extension, ".edn", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(ApplicationEdifact);
            if (string.Equals(Extension, ".xls", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(ApplicationMSExcel);
            if (string.Equals(Extension, ".xml", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(TextXml);
            if (string.Equals(Extension, ".xsl", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(TextXsl);
            if (string.Equals(Extension, ".csv", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(TextCsv);
            if (string.Equals(Extension, ".txt", StringComparison.OrdinalIgnoreCase))   return new MimeContentType(TextPlain);

            return new MimeContentType(string.Concat("application/x-extension-", Extension.AsSpan(1)));
        }
        public  static      string              ToFileExtension(string ContentType)
        {
            switch(ContentType) {
            case MessageRFC822:                     return ".eml";
            case ApplicationZip:                    return ".zip";
            case ApplicationMSExcel:                return ".xls";
            case ApplicationEdifact:                return ".edn";
            case ApplicationEdine:                  return ".edn";
            case ApplicationXPKCS7Signature:        return ".p7s";
            case ApplicationPKCS7Signature:         return ".p7s";
            case TextXml:                           return ".xml";
            case TextCsv:                           return ".csv";
            case TextPlain:                         return ".txt";
            case TextXsl:                           return ".xsl";
            case MultipartMixed:                    return ".txt";
            case MultipartSigned:                   return ".txt";
            default:                                return ".dat";
            }
        }

        public  static      MimeContentType     MapContentType(string fileName, MimeContentType contentType)
        {
            ArgumentNullException.ThrowIfNull(contentType);

            if (contentType.Type == ApplicationOctetStream && fileName != null)
                return FromFileExtension(fileName);

            return contentType;
        }
    }
}
