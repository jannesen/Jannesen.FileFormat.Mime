using System;
using System.Collections.Generic;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public enum MimeEncoding
    {
        Unknown             = -1,
        Text                = 1,
        Text7bit,
        Text8bit,
        Binary,
        QuotedPrintable,
        Base64,
        UUEncode,
    }
}
