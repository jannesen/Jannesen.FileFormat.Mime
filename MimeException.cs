using System;
using System.Runtime.Serialization;

namespace Jannesen.FileFormat.Mime
{
    [Serializable]
    public  class MimeException: Exception
    {
        public                      MimeException(string Message) : base("Mail message corrupt: "+Message)
        {
        }
        protected                   MimeException(SerializationInfo info, StreamingContext context): base(info, context)
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
