using System;
using System.IO;
using System.Net;
using System.Text;

namespace SharpPyxis.SqlServer.SqlClr.Net
{
    internal sealed class SendResult
    {
        public int Status;
        public bool Ok;
        public string Reason = "";
        public string ResponseHeaders = "";
        public byte[] Body = Array.Empty<byte>();
        public string Error = "";
    }

    internal static class HttpSender
    {
        static HttpSender()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            }
            catch
            {
                // TLS 1.3 enum value absent on some .NET 4.8 builds; fall back to TLS 1.2 only
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
        }

        public static SendResult SendCore(
            string? method, string? url, byte[]? body,
            string? contentType, string? accept, string? headers,
            int timeoutSeconds)
        {
            var res = new SendResult();
            try
            {
                if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(url))
                {
                    res.Error = "method and url are required.";
                    return res;
                }

                var req = (HttpWebRequest)WebRequest.Create(url!);
                req.Method = method!.Trim().ToUpperInvariant();
                req.AllowAutoRedirect = false;
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.KeepAlive = true;
                req.Timeout = timeoutSeconds > 0 ? timeoutSeconds * 1000 : 100000;
                req.Accept = !string.IsNullOrWhiteSpace(accept) ? accept : "application/octet-stream";

                // Additional headers: tab-separated "Name\tValue", one per line
                if (!string.IsNullOrWhiteSpace(headers))
                {
                    var lines = headers!.Replace("\r", "").Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(new[] { '\t' }, 2);
                        if (parts.Length != 2) continue;
                        try { req.Headers[parts[0].Trim()] = parts[1] ?? string.Empty; }
                        catch { }
                    }
                }

                bool hasBody = body != null && body.Length > 0 &&
                               !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
                if (hasBody)
                {
                    req.ContentType = !string.IsNullOrWhiteSpace(contentType)
                        ? contentType
                        : "application/octet-stream";
                    req.ContentLength = body!.Length;
                    using (var stream = req.GetRequestStream())
                        stream.Write(body, 0, body.Length);
                }

                HttpWebResponse? resp = null;
                try
                {
                    resp = (HttpWebResponse)req.GetResponse();
                }
                catch (WebException ex) when (ex.Response is HttpWebResponse)
                {
                    // HTTP error response (4xx, 5xx): capture it as data, not as an exception
                    resp = (HttpWebResponse)ex.Response;
                }
                catch (Exception exSend)
                {
                    // TCP/network-level failure: no HTTP response available
                    res.Error = exSend.Message;
                    return res;
                }

                using (resp)
                {
                    res.Status = (int)resp!.StatusCode;
                    res.Ok = res.Status >= 200 && res.Status < 300;
                    res.Reason = resp.StatusDescription ?? "";

                    var hdr = new StringBuilder();
                    for (int i = 0; i < resp.Headers.Count; i++)
                        hdr.Append(resp.Headers.GetKey(i)).Append(": ").Append(resp.Headers.Get(i)).Append("\r\n");
                    res.ResponseHeaders = hdr.ToString();

                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            resp.GetResponseStream()?.CopyTo(ms);
                            res.Body = ms.ToArray();
                        }
                    }
                    catch (Exception exRead)
                    {
                        res.Error = exRead.Message;
                        res.Body = Array.Empty<byte>();
                    }
                }
            }
            catch (Exception ex)
            {
                res = new SendResult { Error = ex.Message };
            }

            return res;
        }
    }
}
