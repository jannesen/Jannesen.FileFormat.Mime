/*@
    Copyright � Jannesen Holding B.V. 2002-2010.
    Unautorised reproduction, distribution or reverse eniginering is prohibited.
*/
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
                return string.Compare(Type, Attachment, true)==0;
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
            MimeContentDisposition      rtn = new MimeContentDisposition();

            rtn.MimeParse(mimeValue, readOnly);

            return rtn;
        }
    }
}
