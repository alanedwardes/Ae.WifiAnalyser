param ([Parameter(Mandatory)]$runtime)

$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath "$runtime.zip" -ErrorAction Ignore

dotnet clean src/Ae.WifiAnalyser

dotnet restore src/Ae.WifiAnalyser --runtime $runtime

dotnet publish src/Ae.WifiAnalyser `
    --configuration Release `
    --runtime $runtime `
    --framework net6.0 `
    --no-restore `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true

Compress-Archive -Path src/Ae.WifiAnalyser/bin/Release/net6.0/$runtime/publish/* -DestinationPath "$runtime.zip"