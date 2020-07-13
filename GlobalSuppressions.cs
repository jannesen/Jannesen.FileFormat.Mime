using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Code Quality",  "IDE0068:Use recommended dispose pattern")]
[assembly: SuppressMessage("Design",        "CA1031:Do not catch general exception types")]
[assembly: SuppressMessage("Design",        "CA1032:Implement standard exception constructors")]
[assembly: SuppressMessage("Design",        "CA1051:Do not declare visible instance fields")]
[assembly: SuppressMessage("Design",        "CA1060:Move pinvokes to native methods class")]
[assembly: SuppressMessage("Design",        "CA1062:Validate arguments of public methods")]
[assembly: SuppressMessage("Design",        "CA1710:Identifiers should have correct suffix")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
[assembly: SuppressMessage("Naming",        "CA1707:Identifiers should not contain underscores")]
[assembly: SuppressMessage("Style",         "IDE0016:Use 'throw' expression")]
[assembly: SuppressMessage("Style",         "IDE1006:Naming Styles")]
[assembly: SuppressMessage("Usage",         "CA2227:Collection properties should be read only")]

[assembly: SuppressMessage("Reliability",   "CA2000:Dispose objects before losing scope", Justification = "<Pending>", Scope = "member", Target = "~M:Jannesen.FileFormat.Mime.MimeMessage.#ctor(System.String)")]
[assembly: SuppressMessage("Style",         "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>", Scope = "member", Target = "~M:Jannesen.FileFormat.Mime.MimeAddress.Parse(System.String,System.Int32@,System.Boolean)~Jannesen.FileFormat.Mime.MimeAddress")]

[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Not supported by NET46")]