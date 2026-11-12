param([switch]$Migrate, [int]$UiPort = 5176)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$config = Get-Content -LiteralPath '.local/dev.json' -Raw | ConvertFrom-Json
$env:ConnectionStrings__Database = $config.connectionString
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:PublicUrl = "http://127.0.0.1:$UiPort"
if ($Migrate) { dotnet run --project backend -- --migrate; exit $LASTEXITCODE }
dotnet run --project backend --no-launch-profile --urls http://127.0.0.1:5092
