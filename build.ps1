
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet publish $root\IaBak.Client\IaBak.Client.csproj --runtime linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true; CheckExitCode
dotnet publish $root\IaBak.Client\IaBak.Client.csproj --runtime win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true; CheckExitCode


$winexe = "$root\IaBak.Client\bin\Debug\net5.0\win-x64\publish\IaBak.Client.exe"
$linuxexe = "$root\IaBak.Client\bin\Debug\net5.0\linux-x64\publish\IaBak.Client"
$ver = [Diagnostics.FileVersionInfo]::GetVersionInfo($winexe).FileVersion

$destwin = "$root\releases\iabak-sharp-v$ver-windows-x64.exe"
$destlinux = "$root\releases\iabak-sharp-v$ver-linux-x64"
if((test-path $destwin) -or (test-path $destlinux)){
    throw 'Already existing.'
}

copy $winexe 
copy $linuxexe 

@{
    LatestVersion= $ver.ToString();
    LatestVersionUrlWindowsX64 = "https://github.com/antiufo/iabak-sharp/releases/download/v$ver/iabak-sharp-v$ver-windows-x64.exe";
    LatestVersionUrlLinuxX64 = "https://github.com/antiufo/iabak-sharp/releases/download/v$ver/iabak-sharp-v$ver-linux-x64";
} | convertto-json | out-file $root\latest-version.json -Encoding UTF8

