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
    [string]$Keys

)
$Keys | ConvertTo-Json 

./dotnet/build-package-nuget.ps1 -RepoName $RepoName -ProjectDir $ProjectDir -Name $Name -Configuration $Configuration -Arch $Arch -Version $Version -SolutionName "FiftyOne.Caching.sln" -CodeSigningCert $Keys['CodeSigningCert'] -CodeSigningCertPassword $Keys['CodeSigningCertPassword'] -SearchPattern $SearchPatern = "^(?!.*Test)Project\(.*csproj",


exit $LASTEXITCODE
