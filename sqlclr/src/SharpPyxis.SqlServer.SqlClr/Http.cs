using System;
using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SharpPyxis.SqlServer.SqlClr.Net;

namespace SharpPyxis.SqlServer.SqlClr
{
    public static class Http
    {
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
                result = new SendResult { Status = 0, Ok = false, Error = ex.Message };
            }

            yield return result;
        }

        public static void FillSendRow(
            object obj,
            out SqlInt32 status,
            out SqlBoolean ok,
            out SqlString reason,
            out SqlString response_headers,
            out SqlBytes body,
            out SqlString error)
        {
            SendResult result = (SendResult)obj;
            status = new SqlInt32(result.Status);
            ok = new SqlBoolean(result.Ok);
            reason = new SqlString(result.Reason ?? string.Empty);
            response_headers = new SqlString(result.ResponseHeaders ?? string.Empty);
            body = (result.Body == null || result.Body.Length == 0) ? SqlBytes.Null : new SqlBytes(result.Body);
            error = new SqlString(result.Error ?? string.Empty);
        }

        [SqlFunction(DataAccess = DataAccessKind.None, IsDeterministic = false)]
        public static SqlBytes SendStrict(
            SqlString method,
            SqlString url,
            SqlBytes body,
            SqlString contentType,
            SqlString accept,
            SqlString headers,
            SqlInt32 timeoutSeconds)
        {
            SendResult result = HttpSender.SendCore(
                method.IsNull ? null : method.Value,
                url.IsNull ? null : url.Value,
                (body == null || body.IsNull) ? null : body.Value,
                contentType.IsNull ? null : contentType.Value,
                accept.IsNull ? null : accept.Value,
                headers.IsNull ? null : headers.Value,
                timeoutSeconds.IsNull ? 0 : timeoutSeconds.Value);

            if (!result.Ok)
            {
                throw new Exception("HTTP " + result.Status + " " + result.Reason + (string.IsNullOrEmpty(result.Error) ? string.Empty : " - " + result.Error));
            }

            return (result.Body == null || result.Body.Length == 0) ? SqlBytes.Null : new SqlBytes(result.Body);
        }
    }
}