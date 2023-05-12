param(
    [string]$ProjectDir = ".",
    [string]$Name = "Release_AnyCPU",
    [string]$Configuration = "Release",
    [string]$Arch = "Any CPU"
)

./dotnet/run-integration-tests.ps1 -RepoName "caching-dotnet-test" -ProjectDir $ProjectDir -Name $Name -Configuration $Configuration -Arch $Arch

exit $LASTEXITCODE