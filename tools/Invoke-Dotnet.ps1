# Keep CLI, package and build caches inside the permitted temporary directory.
$env:DOTNET_CLI_HOME = $env:TEMP
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:MSBuildEnableWorkloadResolver = 'false'
$env:NUGET_PACKAGES = Join-Path $env:TEMP 'inkslab-validation-packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $env:TEMP 'inkslab-validation-http-cache'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $env:TEMP 'inkslab-validation-plugin-cache'
$existingPackages = Join-Path $env:USERPROFILE '.nuget/packages'
if (Test-Path -LiteralPath $existingPackages) {
    $env:NUGET_FALLBACK_PACKAGES = $existingPackages
}
& dotnet @args
exit $LASTEXITCODE
