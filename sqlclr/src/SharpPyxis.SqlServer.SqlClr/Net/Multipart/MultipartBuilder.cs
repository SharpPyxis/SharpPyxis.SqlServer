using System;
using System.IO;
using System.Text;

namespace SharpPyxis.SqlServer.SqlClr.Net.Multipart
{
    internal sealed class MultipartBuilt
    {
        public string ContentType = "multipart/form-data";
        public byte[] Body = Array.Empty<byte>();
    }

    internal static class MultipartBuilder
    {
        public static MultipartBuilt Build(byte[]? packedFiles, string fileFieldName, string? textFields, string? boundary)
        {
            string b = string.IsNullOrWhiteSpace(boundary) ? ("----pyxis_" + Guid.NewGuid().ToString("N")) : boundary!.Trim();
            string ct = "multipart/form-data; boundary=" + b;

            using (var ms = new MemoryStream())
            {
                Action<string> Write = s =>
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(s);
                    ms.Write(bytes, 0, bytes.Length);
                };

                // Text fields: "Name\tValue" per line
                if (!string.IsNullOrWhiteSpace(textFields))
                {
                    string rawTextFields = textFields!;
                    string[] lines = rawTextFields.Replace("\r", "").Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] kv = line.Split(new[] { '\t' }, 2);
                        if (kv.Length != 2) continue;

                        Write("--" + b + "\r\n");
                        Write("Content-Disposition: form-data; name=\"" + kv[0].Trim() + "\"\r\n\r\n");
                        Write(kv[1] ?? string.Empty);
                        Write("\r\n");
                    }
                }

                PackedFileReader.ForEach(packedFiles, (name, buf) =>
                {
                    string safeName = string.IsNullOrWhiteSpace(name) ? "file.bin" : name;
                    string mime = GuessMimeFromName(safeName);
                    Write("--" + b + "\r\n");
                    Write("Content-Disposition: form-data; name=\"" + fileFieldName + "\"; filename=\"" + safeName + "\"\r\n");
                    Write("Content-Type: " + mime + "\r\n\r\n");
                    ms.Write(buf, 0, buf.Length);
                    Write("\r\n");
                });

                Write("--" + b + "--\r\n");

                return new MultipartBuilt { ContentType = ct, Body = ms.ToArray() };
            }
        }

        private static string GuessMimeFromName(string name)
        {
            string ext = Path.GetExtension(name);
            if (!string.IsNullOrEmpty(ext)) ext = ext.TrimStart('.').ToLowerInvariant();
            switch (ext)
            {
                case "pdf": return "application/pdf";
                case "png": return "image/png";
                case "jpg":
                case "jpeg": return "image/jpeg";
                case "bmp": return "image/bmp";
                case "gif": return "image/gif";
                case "tif":
                case "tiff": return "image/tiff";
                case "xml": return "application/xml";
                case "json": return "application/json";
                case "txt": return "text/plain";
                default: return "application/octet-stream";
            }
        }
    }
}
