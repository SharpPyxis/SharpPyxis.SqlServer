using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using SharpPyxis.SqlServer.SqlClr.Net.Multipart;

namespace SharpPyxis.SqlServer.SqlClr
{
    public static class Multipart
    {
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

        public static void FillBuildRow(object obj, out SqlString content_type, out SqlBytes body)
        {
            MultipartBuilt result = (MultipartBuilt)obj;
            content_type = new SqlString(result.ContentType ?? "multipart/form-data");
            body = (result.Body == null || result.Body.Length == 0) ? SqlBytes.Null : new SqlBytes(result.Body);
        }
    }
}