using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

#pragma warning disable CA2000 // Use recommended dispose pattern to ensure that object created by 'new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)' is disposed on all paths. If possible, wrap the creation within a 'using' statement or a 'using' declaration. Otherwise, use a try-finally pattern, with a dedicated local variable declared before the try region and an unconditional Dispose invocation on non-null value in the 'finally' region, say 'x?.Dispose()'. If the object is explicitly disposed within the try region or the dispose ownership is transfered to another object or method, assign 'null' to the local variable just after such an operation to prevent double dispose in 'finally'.

namespace Jannesen.FileFormat.Mime
{
    public class MimeMessage: MimeMultiPart
    {
        private             string              _body;
        private readonly    string              _messageGUID;

        public              string              Body
        {
            get {
                return _body;
            }
            set {
                if (Fields.ReadOnly)
                    throw new InvalidOperationException("not allowed to change body.");

                _body = value;
            }
        }
        public              string              MessageGUID
        {
            get {
                return _messageGUID;
            }
        }
        public              MimeAddress         From
        {
            get {
                return Fields.Get("From")?.ValueAddress;
            }
            set {
                Fields.Set("From").ValueAddress = value;
            }
        }
        public              MimeAddresses       To
        {
            get {
                return Fields.Get("To")?.ValueAddresses;
            }
            set {
                Fields.Set("To").ValueAddresses = value;
            }
        }
        public              MimeAddresses       Cc
        {
            get {
                return Fields.Get("Cc").ValueAddresses;
            }
            set {
                Fields.Set("Cc").ValueAddresses = value;
            }
        }
        public              MimeAddresses       Bcc
        {
            get {
                return Fields.Get("Bcc")?.ValueAddresses;
            }
            set {
                Fields.Set("Bcc").ValueAddresses = value;
            }
        }
        public              MimeAddress         Sender
        {
            get {
                MimeField   fld = Fields.Get("Sender")
                                    ?? Fields["From"];

                return fld?.ValueAddress;
            }
            set {
                Fields.Set("Sender").ValueAddress = value;
            }
        }
        public              MimeAddress         ReplyAddress
        {
            get {
                MimeField   fld = Fields.Get("Reply-To")
                                    ?? Fields["Sender"]
                                    ?? Fields["From"];

                return fld?.ValueAddress;
            }
            set {
                Fields.Set("Reply-To").ValueAddress = value;
            }
        }
        public              string              MessageID
        {
            get {
                string      ID = Fields.Value("Message-ID");

                if (ID != null && ID.StartsWith('<') && ID.EndsWith('>'))
                    ID = ID.Substring(1, ID.Length - 2);

                return ID;
            }
            set {
                Fields.Set("Message-ID").Value = "<" + value + ">";
            }
        }
        public              string              Subject
        {
            get {
                MimeField   fld = Fields["Subject"];

                if (fld != null)
                    return fld.Value;

                return null;
            }
            set {
                Fields.Set("Subject").Value = value;
            }
        }
        public              DateTime            Date
        {
            get {
                MimeField   fld = Fields["Date"];

                return (fld != null) ? fld.ValueDateTime : DateTime.MinValue;
            }
            set {
                Fields.Set("Date").ValueDateTime = value;
            }
        }
        public              string              MimeVersion
        {
            get {
                return Fields.Value("MIME-Version");
            }
        }

        public                                  MimeMessage()
        {
            _body          = null;
            _messageGUID   = System.Guid.NewGuid().ToString().Replace("-", "");
            this.MessageID = _messageGUID + "@" + GetFullComputerName();
            Fields.Add(new MimeField("From",       null));
            Fields.Add(new MimeField("To",         null));
            Fields.Add(new MimeField("Subject",    null));
            this.Date = DateTime.UtcNow;
        }

        public                                  MimeMessage(string fileName) : this(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read), true)
        {
        }

        public                                  MimeMessage(Stream stream) : this(stream, false)
        {
        }
        public                                  MimeMessage(Stream stream, bool closeStream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            try {
                _parseMessage(stream);
            }
            finally {
                if (closeStream)
                    stream.Close();
            }
        }

        public              void                WriteToFile(string fileName)
        {
            using (Stream File = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                WriteTo(File);
        }
        public              void                WriteTo(Stream stream)
        {
            using(MimeWriter Writer = new MimeWriter(stream)) {
                if (WriteHasData) {
                    WriteTo(Writer);
                }
                else {
                    if (_body != null && _body.Length > 0)
                        Writer.WriteBody(_body);
                }
            }
        }

#pragma warning disable CA1838 // CA1838: Avoid 'StringBuilder' parameters for P/Invokes
        public  static      string              GetFullComputerName()
        {
            Int32           bufferSize = 0x100;
            StringBuilder   nameBuffer = new StringBuilder(bufferSize);

            if (GetComputerNameEx(3, nameBuffer, ref bufferSize) == 0)
                throw new InvalidOperationException("Can't get full computername");

            return nameBuffer.ToString();
        }

        private             void                _parseMessage(Stream stream)
        {
            MimeReader      reader     = new MimeReader(stream);

            using (var bodyWriter = new StringWriter()) {
                SetFields(reader.ReadFields());

                if (MimeVersion != null) {
                    string  StrContentType = Fields.Value("Content-Type") ?? throw new MimeException("Invalid mime-message, missing 'Content-Type'.");

                    MimeContentType ContentType = MimeContentType.Parse(StrContentType, true);

                    if (ContentType.isMultipart) {
                        ParseMultiPart(ContentType, reader, bodyWriter);
                    }
                    else {
                        SetContent(reader.ReadData(ContentTransferEncoding, null));
                    }
                }
                else {
                    while (reader.ReadLine(false))
                        reader.WriteLineTo(bodyWriter);
                }

                _body = bodyWriter.ToString();
            }

            Fields.SetCollectionReadOnly();
        }

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern Int32 GetComputerNameEx(Int32 NameType, StringBuilder lpBuffer, ref Int32 lpnSize);
    }
}
