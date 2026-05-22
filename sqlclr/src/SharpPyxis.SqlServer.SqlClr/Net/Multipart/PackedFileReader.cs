using System;
using System.IO;
using System.Text;

namespace SharpPyxis.SqlServer.SqlClr.Net.Multipart
{
    internal static class PackedFileReader
    {
        // Format: [Int64 length][NCHAR(260) name (520 bytes UTF-16)][content bytes]
        public static void ForEach(byte[]? packed, Action<string, byte[]> onPart)
        {
            if (packed == null || packed.Length == 0) return;

            using (var ms = new MemoryStream(packed))
            using (var br = new BinaryReader(ms, Encoding.Unicode))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    long len = br.ReadInt64();
                    char[] nameChars = br.ReadChars(260);
                    string name = new string(nameChars).TrimEnd();
                    int count = checked((int)len);
                    byte[] buf = br.ReadBytes(count);
                    onPart(name, buf);
                }
            }
        }
    }
}
