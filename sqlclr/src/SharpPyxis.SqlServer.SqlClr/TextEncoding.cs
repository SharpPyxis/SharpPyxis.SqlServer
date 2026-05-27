using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SharpPyxis.SqlServer.SqlClr.Util;

namespace SharpPyxis.SqlServer.SqlClr
{
    /// <summary>SQL CLR entry points for text encoding operations.</summary>
    public static class TextEncoding
    {
        /// <summary>Percent-encodes a string using RFC 3986 rules (<see cref="System.Uri.EscapeDataString"/>).</summary>
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlString UrlEncode(SqlString text)
        {
            if (text.IsNull)
            {
                return SqlString.Null;
            }

            return new SqlString(EncodingUtils.UrlEncode(text.Value) ?? string.Empty);
        }

        /// <summary>Decodes a byte array to a string using the specified encoding.</summary>
        /// <param name="data">Raw byte array to decode.</param>
        /// <param name="encoding">IANA or code-page encoding name (e.g. <c>utf-8</c>, <c>iso-8859-1</c>). Defaults to UTF-8.</param>
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

        /// <summary>Encodes a string to a byte array using the specified encoding.</summary>
        /// <param name="text">String to encode.</param>
        /// <param name="encoding">IANA or code-page encoding name (e.g. <c>utf-8</c>, <c>windows-1252</c>). Defaults to UTF-8.</param>
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

        /// <summary>Encodes a byte array to its Base64 string representation.</summary>
        /// <param name="data">Raw byte array to encode.</param>
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlString BytesToBase64(SqlBytes data)
        {
            if (data == null || data.IsNull)
            {
                return SqlString.Null;
            }

            return new SqlString(EncodingUtils.BytesToBase64(data.Value));
        }

        /// <summary>Decodes a Base64 string to its original byte array.</summary>
        /// <param name="encoded">Base64-encoded string to decode.</param>
        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = true)]
        public static SqlBytes Base64ToBytes(SqlString encoded)
        {
            if (encoded.IsNull)
            {
                return SqlBytes.Null;
            }

            return new SqlBytes(EncodingUtils.Base64ToBytes(encoded.Value));
        }
    }
}
