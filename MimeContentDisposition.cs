using System;
using System.Collections.Generic;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public class MimeContentDisposition: MimeParameterField
    {
        public const        string                  Inline     = "inline";
        public const        string                  Attachment = "attachment";

        public static       MimeContentDisposition  NewAttachment(string fileName)
        {
            return new MimeContentDisposition(Attachment) { FileName=fileName };
        }

        public              bool                    isAttachment
        {
            get {
                return string.Equals(Type, Attachment, StringComparison.OrdinalIgnoreCase);
            }
            set {
                SetType(Attachment);
            }
        }
        public              string                  FileName
        {
            get {
                return Parameters.Value("filename");
            }
            set {
                Parameters.Set("filename").Value = value;
            }
        }

        protected                                   MimeContentDisposition()
        {
        }

        public                                      MimeContentDisposition(string value) : base(value)
        {
        }

        public  static new  MimeContentDisposition  Parse(string mimeValue)
        {
            return Parse(mimeValue, false);
        }
        public  static new  MimeContentDisposition  Parse(string mimeValue, bool readOnly)
        {
            var rtn = new MimeContentDisposition();

            rtn.MimeParse(mimeValue, readOnly);

            return rtn;
        }
    }
}
