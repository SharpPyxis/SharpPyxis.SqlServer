/*==============================================================================
  script: install-local-services-clr.sql
  purpose: install the sql clr assembly and exposed sql functions
==============================================================================*/

declare
    @assembly_path nvarchar(4000) = N'C:\Path\To\SharpPyxis.SqlServer.SqlClr.dll',
    @assembly_name nvarchar(200) = N'SharpPyxis.SqlServer.SqlClr',
    @key_name nvarchar(200) = N'SqlClrClientKey',
    @login_name nvarchar(200) = N'SqlClrClientLogin',
    @database_name nvarchar(200) = db_name(),
    @schema_name nvarchar(200) = N'pyxis',
    @permission_set nvarchar(50) = N'external_access',
    @sql nvarchar(max);

-- 0) ensure clr is enabled at the instance level
print '1) verifying / enabling clr (instance)...';
if (
       select value_in_use
       from sys.configurations
       where name = 'clr enabled'
   ) = 0
begin
    print '- enabling clr via sp_configure...';
    exec sp_configure 'clr enabled', 1;
    reconfigure;
end;
else
begin
    print '- clr already enabled.';
end;

-- 1) create the asymmetric key and login in master
print '2) ensuring asymmetric key and login (master)...';

if exists (
              select 1
              from sys.server_principals
              where name = @login_name
          )
begin
    print '- login already exists: ' + @login_name;
end;
else
begin
    if exists (
                  select 1
                  from sys.asymmetric_keys
                  where name = @key_name
              )
    begin
        print '- dropping existing asymmetric key: ' + @key_name;
        set @sql = N'drop asymmetric key ' + quotename(@key_name) + N';';
        exec sys.sp_executesql @sql;
    end;

    print '- creating asymmetric key from dll file...';
    set @sql = N'create asymmetric key ' + quotename(@key_name)
             + N' from executable file = N''' + replace(@assembly_path, '''', '''''') + N''';';
    exec sp_executesql @sql;

    print '- creating sql login from asymmetric key...';
    set @sql = N'create login ' + quotename(@login_name) + N' from asymmetric key ' + quotename(@key_name) + N';';
    exec sp_executesql @sql;
end;

-- 2) grant external_access assembly to the login
print '3) ensuring external access assembly grant...';

if not exists (
                  select 1
                  from sys.server_permissions sp
                  join sys.server_principals p on sp.grantee_principal_id = p.principal_id
                  where p.name = @login_name
                    and sp.permission_name = 'external access assembly'
              )
begin
    set @sql = N'grant external access assembly to ' + quotename(@login_name) + N';';
    exec sp_executesql @sql;
    print '- grant applied.';
end;
else
begin
    print '- grant already present.';
end;

-- 3) ensure the schema exists and install the assembly in the target database
print '4) ensuring schema and assembly in the target database...';

set @sql = N'use ' + quotename(@database_name) + N';';
exec sp_executesql @sql;

set @sql = N'if not exists (select 1 from sys.schemas where name = N''' + @schema_name + N''')
begin
    exec(''create schema ' + quotename(@schema_name) + N' authorization dbo;'');
end;';
exec sp_executesql @sql;

print '- schema ensured: ' + @schema_name;

if exists (
              select 1
              from sys.assemblies
              where name = @assembly_name
          )
begin
    print '- dropping existing assembly in target database...';
    set @sql = N'drop assembly ' + quotename(@assembly_name) + N';';
    exec sp_executesql @sql;
end;

print '- creating assembly from file (permission_set=' + @permission_set + ')...';
set @sql = N'create assembly ' + quotename(@assembly_name)
         + N' from ''' + replace(@assembly_path, '''', '''''') + N''' with permission_set = ' + @permission_set + N';';
exec sp_executesql @sql;

-- 4) recreate exposed sql functions
print '5) recreating sql clr functions...';

-- http_send (tvf)
if object_id(@schema_name + N'.http_send', 'IF') is not null
begin
    exec(N'drop function ' + quotename(@schema_name) + N'.http_send;');
end;

set @sql = N'
create function ' + quotename(@schema_name) + N'.http_send
(
    @method nvarchar(10),
    @url nvarchar(4000),
    @body varbinary(max) = null,
    @content_type nvarchar(200) = null,
    @accept nvarchar(200) = null,
    @headers nvarchar(max) = null,
    @timeout_seconds int = 30
)
returns table
(
    status int,
    ok bit,
    reason nvarchar(200),
    response_headers nvarchar(max),
    body varbinary(max),
    error nvarchar(max)
)
as external name [' + @assembly_name + N'].SharpPyxis.SqlServer.SqlClr.Http.Send;';
exec sp_executesql @sql;

-- http_send_strict (scalar returning body or raising)
if object_id(@schema_name + N'.http_send_strict', 'FN') is not null
begin
    exec(N'drop function ' + quotename(@schema_name) + N'.http_send_strict;');
end;

set @sql = N'
create function ' + quotename(@schema_name) + N'.http_send_strict
(
    @method nvarchar(10),
    @url nvarchar(4000),
    @body varbinary(max) = null,
    @content_type nvarchar(200) = null,
    @accept nvarchar(200) = null,
    @headers nvarchar(max) = null,
    @timeout_seconds int = 30
)
returns varbinary(max)
as external name [' + @assembly_name + N'].SharpPyxis.SqlServer.SqlClr.Http.SendStrict;';
exec sp_executesql @sql;

-- http_multipart_build (tvf returning content_type, body)
if object_id(@schema_name + N'.http_multipart_build', 'IF') is not null
begin
    exec(N'drop function ' + quotename(@schema_name) + N'.http_multipart_build;');
end;

set @sql = N'
create function ' + quotename(@schema_name) + N'.http_multipart_build
(
    @packed_files varbinary(max),
    @file_field_name nvarchar(100),
    @text_fields nvarchar(max),
    @boundary nvarchar(200)
)
returns table
(
    content_type nvarchar(200),
    body varbinary(max)
)
as external name [' + @assembly_name + N'].SharpPyxis.SqlServer.SqlClr.Multipart.Build;';
exec sys.sp_executesql @sql;

-- text_encoding_url_encode (scalar)
if object_id(@schema_name + N'.text_encoding_url_encode', 'FN') is not null
begin
    exec(N'drop function ' + quotename(@schema_name) + N'.text_encoding_url_encode;');
end;

set @sql = N'
create function ' + quotename(@schema_name) + N'.text_encoding_url_encode(@text nvarchar(max))
returns nvarchar(max)
as external name [' + @assembly_name + N'].SharpPyxis.SqlServer.SqlClr.TextEncoding.UrlEncode;';
exec sp_executesql @sql;

-- text_encoding_text_to_bytes (scalar)
if object_id(@schema_name + N'.text_encoding_text_to_bytes', 'FN') is not null
begin
    exec(N'drop function ' + quotename(@schema_name) + N'.text_encoding_text_to_bytes;');
end;

set @sql = N'
create function ' + quotename(@schema_name) + N'.text_encoding_text_to_bytes(@text nvarchar(max), @encoding nvarchar(40) = null)
returns varbinary(max)
as external name [' + @assembly_name + N'].SharpPyxis.SqlServer.SqlClr.TextEncoding.TextToBytes;';
exec sp_executesql @sql;

-- text_encoding_bytes_to_text (scalar)
if object_id(@schema_name + N'.text_encoding_bytes_to_text', 'FN') is not null
begin
    exec(N'drop function ' + quotename(@schema_name) + N'.text_encoding_bytes_to_text;');
end;

set @sql = N'
create function ' + quotename(@schema_name) + N'.text_encoding_bytes_to_text(@data varbinary(max), @encoding nvarchar(40) = null)
returns nvarchar(max)
as external name [' + @assembly_name + N'].SharpPyxis.SqlServer.SqlClr.TextEncoding.BytesToText;';
exec sp_executesql @sql;

print 'installation complete.';

