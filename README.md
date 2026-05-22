# SharpPyxis.SqlServer

SQL Server-specific components for SharpPyxis.

This repository currently hosts the SQL CLR subset and deployment scripts used to expose selected .NET capabilities inside SQL Server.

## Structure

- `sqlclr/src/SharpPyxis.SqlServer.SqlClr/`: SQL CLR project
- `sqlclr/db/`: install and uninstall scripts
- `sqlclr/tests/`: test area reserved for future use
- `sqlclr/SharpPyxis.SqlServer.SqlClr.sln`: repository solution

## Build

```powershell
dotnet msbuild .\sqlclr\SharpPyxis.SqlServer.SqlClr.sln /t:Build /p:Configuration=Debug
```

## License

MIT
