param([string]$NuGetPackageRoot, [string]$SolutionDir);

If ($Env:OS -ne "" -and $Env:OS -ne $null -and $Env:OS.ToLower().Contains("windows")) {
    "$($NuGetPackageRoot)nsis-tool\3.0.8\tools\makensis.exe $($ProjectDir)scripts\installer.nsi" | Invoke-Expression
}
else {
    "makensis $($ProjectDir)scripts\installer.nsi" | Invoke-Expression
}