$csprojPath = Get-ChildItem -Filter *.csproj | Select-Object -First 1
$libsDir = Join-Path $PSScriptRoot "libs"

New-Item -ItemType Directory -Force -Path $libsDir | Out-Null

[xml]$xml = Get-Content $csprojPath.FullName

$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("msb", $xml.Project.NamespaceURI)

$refs = $xml.SelectNodes("//Reference")

foreach ($ref in $refs) {
    $hint = $ref.HintPath

    if ($hint -and (Test-Path $hint)) {

        $fileName = Split-Path $hint -Leaf
        $destPath = Join-Path $libsDir $fileName

        Copy-Item $hint $destPath -Force

        Write-Host "Copied: $fileName -> libs"

        # обновляем HintPath
        $ref.HintPath = "libs\$fileName"
    }
}

$xml.Save($csprojPath.FullName)

Write-Host "Done. csproj updated."