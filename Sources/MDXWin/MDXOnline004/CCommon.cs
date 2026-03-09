using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDXOnline004 {
    internal class CCommon {
        public const string ProtocolVersion = "004";

        public const string HttpCommand_Login = "Login";

        public const string HttpCommand_GetFolder = "GetFolder";
        public const string HttpCommand_GetFolder_Mode_UTF8 = "DAT_UTF8";
        public const string HttpCommand_GetFolder_Mode_SJIS = "DAT_SJIS";
        public const string HttpCommand_GetFolder_Mode_HTML = "HTML";

        public const string HttpCommand_GetPreviewFlac = "GetPreviewFlac";

        public const string HttpCommand_GetFullFilesZip = "GetFullFilesZip";

        public const string HttpCommand_GetMusicFileOnlyZip = "GetMusicFileOnlyZip";

        public const string HttpCommand_GetDocsHTML = "GetDocsHTML";

        public const string HttpCommand_GetBosPdxHQ = "GetBosPdxHQ";

        public const string HttpParam_ArchiveID = "ArchiveID";
    }
}
