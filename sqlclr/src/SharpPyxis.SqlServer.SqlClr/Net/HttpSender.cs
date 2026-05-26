using System;
using System.Net.Http;
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

                string requestMethod = method!.Trim().ToUpperInvariant();
                string requestUrl = url!;

                var client = HttpClientFactory.Client;
                if (timeoutSeconds > 0)
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                using (var req = new HttpRequestMessage(new HttpMethod(requestMethod), requestUrl))
                {
                    req.Headers.Accept.Clear();
                    if (!string.IsNullOrWhiteSpace(accept))
                        req.Headers.TryAddWithoutValidation("Accept", accept);
                    else
                        req.Headers.TryAddWithoutValidation("Accept", "application/octet-stream");

                    // Additional headers: tab-separated "Name\tValue", one per line
                    if (!string.IsNullOrWhiteSpace(headers))
                    {
                        string rawHeaders = headers!;
                        var lines = rawHeaders.Replace("\r", "").Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split(new[] { '\t' }, 2);
                            if (parts.Length != 2) continue;

                            var name = parts[0].Trim();
                            string value = parts[1] ?? string.Empty;

                            if (!req.Headers.TryAddWithoutValidation(name, value))
                            {
                                if (req.Content == null) req.Content = new ByteArrayContent(Array.Empty<byte>());
                                req.Content.Headers.TryAddWithoutValidation(name, value);
                            }
                        }
                    }

                    var hasBody = body != null && body.Length > 0 &&
                                  !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
                    if (hasBody)
                    {
                        req.Content = new ByteArrayContent(body);
                        req.Content.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(
                                !string.IsNullOrWhiteSpace(contentType) ? contentType : "application/octet-stream");
                    }

                    HttpResponseMessage? resp = null;
                    try
                    {
                        // SQL CLR threads cannot use async/await; block synchronously
                        resp = client
                            .SendAsync(req, HttpCompletionOption.ResponseHeadersRead)
                            .GetAwaiter().GetResult();
                    }
                    catch (Exception exSend)
                    {
                        res.Error = exSend.Message;
                        return res;
                    }

                    HttpResponseMessage response = resp!;
                    using (response)
                    {
                        res.Status = (int)response.StatusCode;
                        res.Ok = response.IsSuccessStatusCode;
                        res.Reason = response.ReasonPhrase ?? "";

                        var hdr = new StringBuilder();
                        foreach (var h in response.Headers)
                            hdr.Append(h.Key).Append(": ").Append(string.Join(", ", h.Value)).Append("\r\n");
                        if (response.Content != null)
                        {
                            foreach (var h in response.Content.Headers)
                                hdr.Append(h.Key).Append(": ").Append(string.Join(", ", h.Value)).Append("\r\n");

                            try
                            {
                                res.Body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                            }
                            catch (Exception exRead)
                            {
                                res.Error = res.Error.Length == 0 ? exRead.Message : res.Error + " | " + exRead.Message;
                                res.Body = Array.Empty<byte>();
                            }
                        }

                        res.ResponseHeaders = hdr.ToString();
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
