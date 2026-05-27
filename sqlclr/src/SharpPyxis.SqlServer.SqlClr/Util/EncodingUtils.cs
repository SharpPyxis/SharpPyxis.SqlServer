using System;
using System.Text;

namespace SharpPyxis.SqlServer.SqlClr.Util
{
    internal static class EncodingUtils
    {
        public static string? UrlEncode(string? fragment)
        {
            return fragment == null ? null : Uri.EscapeDataString(fragment);
        }

        public static byte[] TextToBytes(string? text, string? encodingName)
        {
            Encoding enc = string.IsNullOrWhiteSpace(encodingName) ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            return text == null ? Array.Empty<byte>() : enc.GetBytes(text);
        }

        public static string? BytesToText(byte[]? data, string? encodingName)
        {
            Encoding enc = string.IsNullOrWhiteSpace(encodingName) ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            return data == null ? null : enc.GetString(data);
        }

        public static string BytesToBase64(byte[] data) => Convert.ToBase64String(data);

        public static byte[] Base64ToBytes(string encoded) => Convert.FromBase64String(encoded);
    }
}
