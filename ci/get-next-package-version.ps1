
param (
    [Parameter(Mandatory=$true)]
    [string]$VariableName
)

./dotnet/get-next-package-version.ps1 -RepoName "caching-dotnet" -VariableName $VariableName


exit $LASTEXITCODE