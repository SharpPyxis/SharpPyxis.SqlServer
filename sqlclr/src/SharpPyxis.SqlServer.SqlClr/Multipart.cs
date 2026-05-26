using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SharpPyxis.SqlServer.SqlClr.Net.Multipart;

namespace SharpPyxis.SqlServer.SqlClr
{
    /// <summary>SQL CLR entry points for multipart/form-data assembly.</summary>
    public static class Multipart
    {
        /// <summary>
        /// Builds a <c>multipart/form-data</c> body and returns one row with the content type and binary body.
        /// </summary>
        /// <param name="packedFiles">
        /// Binary-packed files. Each entry: <c>[Int64 length][NCHAR(260) name (520 bytes UTF-16)][content]</c>.
        /// Pass <c>NULL</c> to include no files.
        /// </param>
        /// <param name="fileFieldName">Form field name applied to all uploaded files.</param>
        /// <param name="textFields">Optional text fields, one per line: <c>Name\tValue</c>.</param>
        /// <param name="boundary">Multipart boundary string; auto-generated if <c>NULL</c>.</param>
        [SqlFunction(
            DataAccess = DataAccessKind.None,
            IsDeterministic = false,
            FillRowMethodName = nameof(FillBuildRow),
            TableDefinition = "content_type nvarchar(200), body varbinary(max)")]
        public static IEnumerable Build(
            SqlBytes packedFiles,
            SqlString fileFieldName,
            SqlString textFields,
            SqlString boundary)
        {
            MultipartBuilt result = MultipartBuilder.Build(
                (packedFiles == null || packedFiles.IsNull) ? null : packedFiles.Value,
                fileFieldName.IsNull ? "file" : fileFieldName.Value,
                textFields.IsNull ? null : textFields.Value,
                boundary.IsNull ? null : boundary.Value);

            yield return result;
        }

        /// <summary>SQL CLR TVF fill-row callback; not for direct use.</summary>
        public static void FillBuildRow(object obj, out SqlString content_type, out SqlBytes body)
        {
            MultipartBuilt result = (MultipartBuilt)obj;
            content_type = new SqlString(result.ContentType ?? "multipart/form-data");
            body = (result.Body == null || result.Body.Length == 0) ? SqlBytes.Null : new SqlBytes(result.Body);
        }
    }
}
