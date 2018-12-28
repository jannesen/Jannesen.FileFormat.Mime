/*@
    Copyright � Jannesen Holding B.V. 2002-2010.
    Unautorised reproduction, distribution or reverse eniginering is prohibited.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jannesen.FileFormat.Mime
{
    public class MimeAddresses: List<MimeAddress>, IMimeWriterTo
    {
        private             bool                _readOnly;

        public                                  MimeAddresses()
        {
            _readOnly = false;
        }

        public  static      MimeAddresses       Parse(string mimeAddressesString)
        {
            return Parse(mimeAddressesString, false);
        }
        public  static      MimeAddresses       Parse(string mimeAddressesString, bool readOnly)
        {
            if (mimeAddressesString == null) {
                if (readOnly)
                    throw new ArgumentNullException("MimeAddressesString");

                return new MimeAddresses();
            }

            try {
                MimeAddresses   rtn      = new MimeAddresses();
                int             position = 0;

                for (;;) {
                    rtn.Add(MimeAddress.Parse(mimeAddressesString, ref position, readOnly));

                    if (position >= mimeAddressesString.Length)
                        break;

                    if (MimeLexicalToken.Parse(mimeAddressesString, ref position).Type != MimeLexicalTokenType.Comma)
                        throw new Exception("data after addresses.");
                }

                rtn._readOnly = readOnly;

                return rtn;
            }
            catch(Exception err) {
                throw new MimeException("Invalid address '" + mimeAddressesString + "', " + err.Message);
            }
        }

        public  new         void                Add(MimeAddress address)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to add address.");

            base.Add(address);
        }
        public  new         void                AddRange(IEnumerable<MimeAddress> addresses)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to add addresses.");

            base.AddRange(addresses);
        }
        public  new         void                Clear()
        {
            if (_readOnly)
                throw new MimeException("Not allowed to clear addresses.");

            base.Clear();
        }
        public  new         void                Insert(int index, MimeAddress address)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to insert address.");

            base.Insert(index, address);
        }
        public  new         void                InsertRange(int index, IEnumerable<MimeAddress> addresses)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to insert address.");

            base.InsertRange(index, addresses);
        }
        public  new         void                Remove(MimeAddress address)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to remove address.");

            base.Remove(address);
        }
        public  new         void                RemoveAll(Predicate<MimeAddress> match)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to remove addresses.");

            base.RemoveAll(match);
        }
        public  new         void                RemoveAt(int index)
        {
            if (_readOnly)
                throw new MimeException("Not allowed to remove address.");

            base.RemoveAt(index);
        }

        public              bool                WriteHasData
        {
            get {
                return Count > 0;
            }
        }
        public              void                WriteTo(MimeWriter writer)
        {
            for (int i = 0 ; i < Count ; ++i) {
                if (i > 0)
                    writer.WriteAddressSep();

                base[i].WriteTo(writer);
            }
        }

        public  override    string              ToString()
        {
            string  rtn = "";

            for (int i = 0 ; i < Count ; ++i) {
                if (i>0)
                    rtn += ", ";

                rtn += base[i].ToString();
            }

            return rtn;
        }
    }
}
