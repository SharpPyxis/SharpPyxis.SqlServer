/*==============================================================================
  script: uninstall-local-services-clr.sql
  purpose: drop the exposed sql functions, assembly, login, and asymmetric key
==============================================================================*/

declare
    @assembly_name sysname = N'SharpPyxis.SqlServer.SqlClr',
    @key_name sysname = N'SqlClrClientKey',
    @login_name sysname = N'SqlClrClientLogin',
    @database_name sysname = db_name(),
    @schema_name sysname = N'pyxis',
    @sql nvarchar(max);

-- 1) drop functions in the target database
set @sql = N'use ' + quotename(@database_name) + N';
if object_id(''' + quotename(@schema_name) + N'.http_send'', ''IF'') is not null drop function ' + quotename(@schema_name) + N'.http_send;
if object_id(''' + quotename(@schema_name) + N'.http_send_strict'', ''FN'') is not null drop function ' + quotename(@schema_name) + N'.http_send_strict;
if object_id(''' + quotename(@schema_name) + N'.http_multipart_build'', ''IF'') is not null drop function ' + quotename(@schema_name) + N'.http_multipart_build;
if object_id(''' + quotename(@schema_name) + N'.text_encoding_url_encode'', ''FN'') is not null drop function ' + quotename(@schema_name) + N'.text_encoding_url_encode;
if object_id(''' + quotename(@schema_name) + N'.text_encoding_text_to_bytes'', ''FN'') is not null drop function ' + quotename(@schema_name) + N'.text_encoding_text_to_bytes;
if object_id(''' + quotename(@schema_name) + N'.text_encoding_bytes_to_text'', ''FN'') is not null drop function ' + quotename(@schema_name) + N'.text_encoding_bytes_to_text;';
exec sp_executesql @sql;

-- 2) drop the assembly in the target database
set @sql = N'use ' + quotename(@database_name) + N';
if exists (select 1 from sys.assemblies where name = N''' + replace(@assembly_name, '''', '''''') + N''')
    drop assembly ' + quotename(@assembly_name) + N';';
exec sp_executesql @sql;

-- 3) revoke the grant, then drop the login and asymmetric key in master
set @sql = N'use [master];
if exists (select 1 from sys.server_principals where name = N''' + replace(@login_name, '''', '''''') + N''')
begin
    begin try
        revoke external access assembly from ' + quotename(@login_name) + N';
    end try
    begin catch
        print ''(info) revoke skipped (already revoked?).'';
    end catch
end';
exec sp_executesql @sql;

set @sql = N'use [master];
if exists (select 1 from sys.server_principals where name = N''' + replace(@login_name, '''', '''''') + N''')
    drop login ' + quotename(@login_name) + N';';
exec sp_executesql @sql;

set @sql = N'use [master];
if exists (select 1 from sys.asymmetric_keys where name = N''' + replace(@key_name, '''', '''''') + N''')
    drop asymmetric key ' + quotename(@key_name) + N';';
exec sp_executesql @sql;

print 'uninstallation complete.';
