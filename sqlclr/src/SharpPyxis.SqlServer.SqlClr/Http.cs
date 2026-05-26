using System;
using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SharpPyxis.SqlServer.SqlClr.Net;

namespace SharpPyxis.SqlServer.SqlClr
{
    /// <summary>SQL CLR entry points for HTTP requests.</summary>
    public static class Http
    {
        /// <summary>
        /// Executes an HTTP request and returns exactly one row. Never throws.
        /// The caller must always inspect the return values:
        /// <c>ok=1</c> — 2xx response;
        /// <c>ok=0, status&gt;0</c> — server returned a non-2xx status;
        /// <c>ok=0, status=0</c> — TCP/network-level failure (no HTTP response), see <c>error</c>.
        /// </summary>
        /// <paramref name="headers"/>: optional, tab-separated <c>Name\tValue</c> per line.
        [SqlFunction(
            DataAccess = DataAccessKind.None,
            IsDeterministic = false,
            FillRowMethodName = nameof(FillSendRow),
            TableDefinition = "status int, ok bit, reason nvarchar(200), response_headers nvarchar(max), body varbinary(max), error nvarchar(max)")]
        public static IEnumerable Send(
            SqlString method,
            SqlString url,
            SqlBytes body,
            SqlString contentType,
            SqlString accept,
            SqlString headers,
            SqlInt32 timeoutSeconds)
        {
            SendResult result;
            try
            {
                result = HttpSender.SendCore(
                    method.IsNull ? null : method.Value,
                    url.IsNull ? null : url.Value,
                    (body == null || body.IsNull) ? null : body.Value,
                    contentType.IsNull ? null : contentType.Value,
                    accept.IsNull ? null : accept.Value,
                    headers.IsNull ? null : headers.Value,
                    timeoutSeconds.IsNull ? 0 : timeoutSeconds.Value);
            }
            catch (Exception ex)
            {
                result = new SendResult { Error = ex.Message };
            }

            yield return result;
        }

        /// <summary>SQL CLR TVF fill-row callback; not for direct use.</summary>
        public static void FillSendRow(
            object obj,
            out SqlInt32 status,
            out SqlBoolean ok,
            out SqlString reason,
            out SqlString responseHeaders,
            out SqlBytes body,
            out SqlString error)
        {
            SendResult result = (SendResult)obj;
            status = new SqlInt32(result.Status);
            ok = new SqlBoolean(result.Ok);
            reason = new SqlString(result.Reason ?? string.Empty);
            responseHeaders = new SqlString(result.ResponseHeaders ?? string.Empty);
            body = (result.Body == null || result.Body.Length == 0) ? SqlBytes.Null : new SqlBytes(result.Body);
            error = new SqlString(result.Error ?? string.Empty);
        }
    }
}
