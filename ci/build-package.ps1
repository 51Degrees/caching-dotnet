param(
    [string]$ProjectDir = ".",
    [Parameter(Mandatory=$true)]
    [string]$RepoName,
    [string]$Name = "Release_x64",
    [string]$Configuration = "CoreRelease",
    [string]$Arch = "x64",
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [Parameter(Mandatory=$true)]
    [Hashtable]$Keys

)

./dotnet/build-package-nuget.ps1 -RepoName $RepoName -ProjectDir $ProjectDir -Name $Name -Configuration $Configuration -Arch $Arch -Version $Version -SolutionName "FiftyOne.Caching.sln" -CodeSigningCert $Keys['CodeSigningCert'] -CodeSigningCertPassword $Keys['CodeSigningCertPassword'] -SearchPattern "^(?!.*Test)Project\(.*csproj"
exit $LASTEXITCODE
