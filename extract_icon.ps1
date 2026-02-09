Add-Type -AssemblyName System.Drawing
$sourcePath = "E:\SteamLibrary\steamapps\common\Nioh3Demo\Nioh3.exe"
$destPath = "F:\DESKTOP FILES\cheattables\Nioh3AffixEditor\app.ico"

$icon = [System.Drawing.Icon]::ExtractAssociatedIcon($sourcePath)
$fs = [System.IO.File]::Create($destPath)
$icon.Save($fs)
$fs.Close()
Write-Host "Icon extracted successfully to $destPath"
