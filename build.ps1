
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet publish $root\IaBak.Client\IaBak.Client.csproj --runtime linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeNativeLibrariesInSingleFile=true; CheckExitCode
dotnet publish $root\IaBak.Client\IaBak.Client.csproj --runtime win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeNativeLibrariesInSingleFile=true; CheckExitCode


$winexe = "$root\IaBak.Client\bin\Debug\net5.0\win-x64\publish\iabak-sharp.exe"
$linuxexe = "$root\IaBak.Client\bin\Debug\net5.0\linux-x64\publish\iabak-sharp"
$ver = [Diagnostics.FileVersionInfo]::GetVersionInfo($winexe).FileVersion

$destwin = "$root\IaBak.Server\wwwroot\releases\iabak-sharp-v$ver-windows-x64.zip"
$destlinux = "$root\IaBak.Server\wwwroot\releases\iabak-sharp-v$ver-linux-x64.zip"
if((test-path $destwin) -or (test-path $destlinux)){
    throw 'Already existing.'
}

7z a $destwin $winexe
7z a $destlinux $linuxexe

@{
    LatestVersion= $ver.ToString();
    LatestVersionUrlWindowsX64 = "https://iabak.shaman.io/releases/iabak-sharp-v$ver-windows-x64.zip";
    LatestVersionUrlLinuxX64 = "https://iabak.shaman.io/releases/iabak-sharp-v$ver-linux-x64.zip";
    #LatestVersionUrlWindowsX64 = "https://github.com/antiufo/iabak-sharp/releases/download/v$ver/iabak-sharp-v$ver-windows-x64.zip";
    #LatestVersionUrlLinuxX64 = "https://github.com/antiufo/iabak-sharp/releases/download/v$ver/iabak-sharp-v$ver-linux-x64.zip";
} | convertto-json | out-file $root\IaBak.Server\wwwroot\latest-version.json -Encoding UTF8


# Separately: zip files must be there before json is copied!
$destfolder = "conta:/root/Repositories/iabak-sharp/IaBak.Server/wwwroot"
rclone copy $root\IaBak.Server\wwwroot $destfolder -vvv --include *.zip; CheckExitCode
rclone copy $root\IaBak.Server\wwwroot $destfolder -vvv --include *.json; CheckExitCode




