using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SharpPyxis.SqlServer.SqlClr.Util;

namespace SharpPyxis.SqlServer.SqlClr
{
    public static class TextEncoding
    {
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlString UrlEncode(SqlString text)
        {
            if (text.IsNull)
            {
                return SqlString.Null;
            }

            return new SqlString(EncodingUtils.UrlEncode(text.Value) ?? string.Empty);
        }

        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlChars BytesToText(SqlBytes data, SqlString encoding)
        {
            if (data == null || data.IsNull)
            {
                return SqlChars.Null;
            }

            string? text = EncodingUtils.BytesToText(data.Value, encoding.IsNull ? null : encoding.Value);
            return text == null ? SqlChars.Null : new SqlChars(text);
        }

        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlBytes TextToBytes(SqlChars text, SqlString encoding)
        {
            if (text == null || text.IsNull)
            {
                return SqlBytes.Null;
            }

            string value = new string(text.Value);
            byte[] bytes = EncodingUtils.TextToBytes(value, encoding.IsNull ? null : encoding.Value);
            return new SqlBytes(bytes);
        }
    }
}