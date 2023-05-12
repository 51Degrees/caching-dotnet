
param(
    [string]$ProjectDir = ".",
    [string]$Name
)

./dotnet/run-update-dependencies.ps1 -RepoName "caching-dotnet" -ProjectDir $ProjectDir -Name $Name

exit $LASTEXITCODE