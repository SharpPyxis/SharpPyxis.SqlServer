# SharpPyxis.SqlServer

SQL Server-specific components for SharpPyxis.

This repository contains the SQL Server half of an older mixed codebase that originally bundled both a local HTTP utility API and SQL CLR code in the same project family. The split is intentional: `SharpPyxis.SqlServer` keeps only the SQL Server-specific subset, while the local HTTP/API half now lives in `SharpPyxis.LocalServices`.

## Why this repository exists

SQL Server sometimes needs capabilities that are awkward to express in pure T-SQL, but still need to stay callable from inside the database engine. SQL CLR is one of the pragmatic ways to bridge that gap: a managed .NET assembly is deployed into SQL Server and selected methods are exposed as T-SQL functions.

This repository focuses on a deliberately narrow subset of those scenarios:

- outbound HTTP calls;
- multipart payload construction;
- text, byte, UTF-8, and URL-encoding helpers.

That makes the repository useful both as production code and as a compact, real-world SQL CLR example for developers who want to understand the full chain from C# source to T-SQL surface.

## What is in scope today

The current SQL CLR assembly exposes functions created by `sqlclr/db/Install-LocalServicesClr.sql`, including:

- `http_send`
- `http_send_strict`
- `http_multipart_build`
- `text_encoding_url_encode`
- `text_encoding_text_to_bytes`
- `text_encoding_bytes_to_text`

By default, these functions are created in the `pyxis` schema, although the install script keeps that configurable.

## Structure

- `sqlclr/src/SharpPyxis.SqlServer.SqlClr/`: SQL CLR project
- `sqlclr/db/`: install and uninstall scripts
- `sqlclr/tests/`: repository-level test area reserved for future use
- `sqlclr/SharpPyxis.SqlServer.SqlClr.sln`: repository solution

## Build

```powershell
dotnet msbuild .\sqlclr\SharpPyxis.SqlServer.SqlClr.sln /t:Build /p:Configuration=Debug
```

The current SQL CLR project targets the classic SQL Server / .NET Framework toolchain and is intentionally conservative in its dependency surface.

## Install in SQL Server

At a high level, installation is:

1. Build the SQL CLR assembly.
2. Update `@assembly_path` in `sqlclr/db/Install-LocalServicesClr.sql` so it points to the built DLL.
3. Execute the install script in the target database.

The install script takes care of:

- enabling CLR if needed;
- creating the asymmetric key and login;
- granting `external_access assembly`;
- creating the assembly in the target database;
- recreating the exposed SQL functions.

`sqlclr/db/Uninstall-LocalServicesClr.sql` removes the functions, assembly, login, and asymmetric key.

## Example SQL usage

```sql
select *
from pyxis.http_send(
	N'GET',
	N'https://example.org',
	null,
	null,
	N'application/json',
	null,
	30
);

select pyxis.text_encoding_url_encode(N'a value with spaces & symbols');
```

## Notes

- This repository is intentionally SQL Server-specific; it is not a shared core for every SharpPyxis project.
- SQL CLR is powerful, but it should stay conservative on dependencies and deployment assumptions.
- The separation from `SharpPyxis.LocalServices` is deliberate: local HTTP utilities and in-database SQL CLR code have different operational constraints.

## License

MIT
