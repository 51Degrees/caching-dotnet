param(
    [string]$ProjectDir = ".",
    [string]$Name = "Release_AnyCPU",
    [string]$Configuration = "Release",
    [string]$Arch = "Any CPU",
    [Parameter(Mandatory=$true)]
    [string]$Version
)

./dotnet/build-package-nuget.ps1 -RepoName "caching-dotnet-test" -ProjectDir $ProjectDir -Name $Name -Configuration $Configuration -Arch $Arch -Version $Version

exit $LASTEXITCODE