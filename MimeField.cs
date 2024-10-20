﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jannesen.FileFormat.Mime
{
    public  class MimeField
    {
        private readonly    string                  _name;
        private             string                  _value;
        private             object                  _valueObject;
        private readonly    bool                    _readOnly;

        public              string                  Name
        {
            get {
                return _name;
            }
        }
        public              string                  Value
        {
            get {
                if (!_readOnly && _valueObject != null)
                    return _valueObject.ToString();

                return _value;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change Value.");

                _valueObject = null;
                _value       = value;
            }
        }
        public              MimeAddress             ValueAddress
        {
            get {
                if (_valueObject == null || _valueObject is not MimeAddress)
                    _valueObject = MimeAddress.Parse(_value, _readOnly);

                return (MimeAddress)_valueObject;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change Value.");

                _value       = null;
                _valueObject = value;
            }
        }
        public              MimeAddresses           ValueAddresses
        {
            get {
                if (_valueObject == null || _valueObject is not MimeAddresses)
                    _valueObject = MimeAddresses.Parse(_value, _readOnly);

                return (MimeAddresses)_valueObject;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change Value.");

                _value       = null;
                _valueObject = value;
            }
        }
        public              MimeContentDisposition  ValueContentDisposition
        {
            get {
                if (_valueObject == null || _valueObject is not MimeContentDisposition)
                    _valueObject = MimeContentDisposition.Parse(_value, _readOnly);

                return (MimeContentDisposition)_valueObject;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change Value.");

                _value       = null;
                _valueObject = value;
            }
        }
        public              MimeContentType         ValueContentType
        {
            get {
                if (_valueObject == null || _valueObject is not MimeContentType)
                    _valueObject = MimeContentType.Parse(_value, _readOnly);

                return (MimeContentType)_valueObject;
            }
            set {
                if (_readOnly)
                    throw new InvalidOperationException("Not allowed to change Value.");

                _value       = null;
                _valueObject = value;
            }
        }
        public              DateTime                ValueDateTime
        {
            get {
                try {
                    string  str = Value;
                    int     i;
                    int     day;
                    int     month;
                    int     year;

                    i = str.IndexOf(',');

                // Remove weekday
                    if (i>0) {
                        if (i > 3)
                            throw new InvalidOperationException("Invalid day name.");

                        str = str.Substring(i+1).TrimStart();
                    }

                    string[]    dateparts = str.Split(' ');
                    string[]    timeparts = dateparts[3].Split(':');
                    string      timezone  = dateparts[4];
                    day   = int.Parse(dateparts[0], NumberStyles.Integer, CultureInfo.InvariantCulture);

                    switch(dateparts[1].ToLowerInvariant()) {
                    case "jan":     month =  1;                         break;
                    case "feb":     month =  2;                         break;
                    case "mar":     month =  3;                         break;
                    case "apr":     month =  4;                         break;
                    case "may":     month =  5;                         break;
                    case "jun":     month =  6;                         break;
                    case "jul":     month =  7;                         break;
                    case "aug":     month =  8;                         break;
                    case "sep":     month =  9;                         break;
                    case "oct":     month = 10;                         break;
                    case "nov":     month = 11;                         break;
                    case "dec":     month = 12;                         break;
                    default:        month = int.Parse(dateparts[1], NumberStyles.Integer, CultureInfo.InvariantCulture);    break;
                    }

                    year  = int.Parse(dateparts[2], NumberStyles.Integer, CultureInfo.InvariantCulture);

                    if (year<100) year = (year>70) ? 1900+year:2000+year;

                    DateTime    dt = new DateTime(year, month, day, int.Parse(timeparts[0], NumberStyles.Integer, CultureInfo.InvariantCulture), int.Parse(timeparts[1], NumberStyles.Integer, CultureInfo.InvariantCulture), 0);

                    if (timeparts.Length>2)
                        dt = dt.AddSeconds(double.Parse(timeparts[2], CultureInfo.InvariantCulture));

                    if ((timezone[0]=='-' || timezone[0]=='+') && timezone.Length==5)
                        dt = dt.AddMinutes(-int.Parse(timezone.AsSpan(0, 3), NumberStyles.Integer, CultureInfo.InvariantCulture)*60 + int.Parse(timezone.AsSpan(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture));
                    else {
                        switch(timezone) {
                        case "(UTC)":                           break;
                        case "UT":                              break;
                        case "GMT":                             break;
                        case "EDT":     dt = dt.AddHours(  4);  break;
                        case "EST":     dt = dt.AddHours(  5);  break;
                        case "CDT":     dt = dt.AddHours(  5);  break;
                        case "CST":     dt = dt.AddHours(  6);  break;
                        case "MDT":     dt = dt.AddHours(  6);  break;
                        case "MST":     dt = dt.AddHours(  7);  break;
                        case "PDT":     dt = dt.AddHours(  7);  break;
                        case "PST":     dt = dt.AddHours(  8);  break;
                        case "Z":                               break;
                        case "A":       dt = dt.AddHours(  1);  break;
                        case "M":       dt = dt.AddHours( 12);  break;
                        case "N":       dt = dt.AddHours( -1);  break;
                        case "Y":       dt = dt.AddHours(-12);  break;
                        default:        throw new InvalidOperationException("Unknown timezone.");
                        }
                    }

                    return dt;
                }
                catch(Exception Err) {
                    throw new InvalidOperationException("Bad Date format: "+Err.Message);
                }
            }
            set {
                _value = value.ToString("R", CultureInfo.InvariantCulture);
            }
        }

        public                                      MimeField(string name, string value)
        {
            _name     = name;
            _value    = value;
            _readOnly = false;
        }
        public                                      MimeField(string name, string value, bool readOnly)
        {
            _name     = name;
            _value    = value;
            _readOnly = readOnly;
        }

        public  override    string                  ToString()
        {
            return _name + ": " + Value;
        }
        internal            void                    WriteTo(MimeWriter writer)
        {
            if (_name != "Bcc") {
                if (_valueObject is IMimeWriterTo value) {
                    writer.WriteHeaderField(_name, value);
                }
                else {
                    writer.WriteHeaderField(_name, _value);
                }
            }
        }
    }

    public  class MimeFields : List<MimeField>
    {
        private             bool                    _readOnly;

        public              MimeField               this[string name]
        {
            get {
                for (int i = 0 ; i < Count ; ++i) {
                    if (string.Equals(base[i].Name, name, StringComparison.OrdinalIgnoreCase))
                        return base[i];
                }

                return null;
            }
        }
        public              bool                    ReadOnly
        {
            get {
                return _readOnly;
            }
        }

        public                                      MimeFields()
        {
            _readOnly = false;
        }

        public  new         void                    Add(MimeField field)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to add field.");

            base.Add(field);
        }
        public  new         void                    AddRange(IEnumerable<MimeField> fields)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to add fields.");

            base.AddRange(fields);
        }
        public  new         void                    Clear()
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to clear addresses.");

            base.Clear();
        }
        public  new         void                    Insert(int index, MimeField field)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to insert field.");

            base.Insert(index, field);
        }
        public  new         void                    InsertRange(int index, IEnumerable<MimeField> fields)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to insert fields.");

            base.InsertRange(index, fields);
        }
        public  new         void                    Remove(MimeField field)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to remove field.");

            base.Remove(field);
        }
        public  new         void                    RemoveAll(Predicate<MimeField> match)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to remove field.");

            base.RemoveAll(match);
        }
        public  new         void                    RemoveAt(int Index)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to remove field.");

            base.RemoveAt(Index);
        }

        public              MimeField               Get(string name)
        {
            MimeField fld = this[name];

            if (fld==null && !_readOnly) {
                fld = new MimeField(name, null);
                base.Add(fld);
            }

            return fld;
        }
        public              MimeField               Set(string name)
        {
            if (_readOnly)
                throw new InvalidOperationException("Not allowed to set field.");

            MimeField fld = this[name];

            if (fld==null) {
                fld = new MimeField(name, null);
                base.Add(fld);
            }

            return fld;
        }
        public              string                  Value(string name)
        {
            MimeField fld = this[name];

            if (fld!=null)
                return fld.Value;

            return null;
        }
        public              string[]                Values(string name)
        {
            List<string>    rtn = new List<string>();

            for (int i = 0 ; i < Count ; ++i) {
                if (string.Equals(base[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    rtn.Add(base[i].Value);
            }

            return rtn.ToArray();
        }

        public              void                    SetCollectionReadOnly()
        {
            _readOnly = true;
        }

        internal            void                    WriteTo(MimeWriter writer)
        {
            for (int i = 0 ; i < Count ; ++i)
                base[i].WriteTo(writer);
        }
    }
}
