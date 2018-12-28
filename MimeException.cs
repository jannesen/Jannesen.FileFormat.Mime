/*@
    Copyright � Jannesen Holding B.V. 2002-2010.
    Unautorised reproduction, distribution or reverse eniginering is prohibited.
*/
using System;

namespace Jannesen.FileFormat.Mime
{
    [Serializable]
    public  class MimeException: Exception
    {
        public                      MimeException(string Message) : base("Mail message corrupt: "+Message)
        {
        }

        public override string      Source
        {
            get {
                return "Jannesen.FileFormat.Mime";
            }
        }
    }
}
