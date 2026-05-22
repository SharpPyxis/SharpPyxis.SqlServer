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
            var res = new SendResult(); // Status=0 / Ok=false par défaut

            try
            {
                if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(url))
                {
                    res.Error = "method et url requis.";
                    return res;
                }

                string requestMethod = method!.Trim().ToUpperInvariant();
                string requestUrl = url!;

                var client = HttpClientFactory.Client;
                if (timeoutSeconds > 0)
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                using (var req = new HttpRequestMessage(new HttpMethod(requestMethod), requestUrl))
                {
                    // Accept
                    req.Headers.Accept.Clear();
                    if (!string.IsNullOrWhiteSpace(accept))
                        req.Headers.TryAddWithoutValidation("Accept", accept);
                    else
                        req.Headers.TryAddWithoutValidation("Accept", "application/octet-stream");

                    // Headers additionnels
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

                    // Corps (non-GET)
                    var hasBody = body != null && body.Length > 0 &&
                                  !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
                    if (hasBody)
                    {
                        req.Content = new ByteArrayContent(body);
                        req.Content.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(
                                !string.IsNullOrWhiteSpace(contentType) ? contentType : "application/octet-stream");
                    }

                    // .NET Framework : SendAsync + GetResult (sync)
                    HttpResponseMessage? resp = null;
                    try
                    {
                        // Optionnel: demander les en-têtes d'abord si tu veux limiter la taille ensuite
                        resp = HttpClientFactory.Client
                            .SendAsync(req, HttpCompletionOption.ResponseHeadersRead)
                            .GetAwaiter().GetResult();
                    }
                    catch (Exception exSend)
                    {
                        res.Error = exSend.Message;
                        return res; // status=0, ok=false
                    }

                    HttpResponseMessage response = resp!;
                    using (response)
                    {
                        res.Status = (int)response.StatusCode;
                        res.Ok = response.IsSuccessStatusCode;
                        res.Reason = response.ReasonPhrase ?? "";

                        // En-têtes → texte "Name: Value"
                        var hdr = new StringBuilder();
                        foreach (var h in response.Headers)
                            hdr.Append(h.Key).Append(": ").Append(string.Join(", ", h.Value)).Append("\r\n");
                        if (response.Content != null)
                        {
                            foreach (var h in response.Content.Headers)
                                hdr.Append(h.Key).Append(": ").Append(string.Join(", ", h.Value)).Append("\r\n");

                            try
                            {
                                // Lecture corps protégée
                                res.Body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                            }
                            catch (Exception exRead)
                            {
                                // Le serveur a répondu mais le stream s’est mal passé : on capture
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
                // Catch global (dernière ligne de défense)
                res.Status = 0;
                res.Ok = false;
                res.Reason = "";
                res.ResponseHeaders = "";
                res.Body = Array.Empty<byte>();
                res.Error = ex.Message;
            }

            return res;
        }
    }
}
