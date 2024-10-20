using System;
using System.Runtime.Serialization;

namespace Jannesen.FileFormat.Mime
{
    public  class MimeException: Exception
    {
        public                      MimeException(string Message) : base(Message)
        {
        }

        public override string      Source
        {
            get {
                return "Jannesen.FileFormat.Mime";
            }
        }
    }

    public  class MimeReaderException: Exception
    {
        public                      MimeReaderException(string Message) : base(Message)
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
