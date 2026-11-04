param([string]$GamePath)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$frameCounts = @{ walk=16; tools=12; melee=12; crouch=12; fishing=9; eat=6; misc=8 }
foreach ($variant in @('are','dumpling')) {
    $layout = Get-Content "PhoebeFarmer/assets/$variant/frames.json" -Raw | ConvertFrom-Json
    foreach ($sheet in $frameCounts.Keys) {
        if (-not (Test-Path "PhoebeFarmer/assets/$variant/$sheet.png") -or $layout.$sheet.Count -ne $frameCounts[$sheet]) {
            throw "Missing or incomplete artwork: $variant/$sheet"
        }
    }
}

if (-not $GamePath) {
    $GamePath = Join-Path $PSScriptRoot '.build/game-refs'
    if (-not (Test-Path -LiteralPath (Join-Path $GamePath 'Stardew Valley.dll'))) {
        git clone https://github.com/StardewModders/mod-reference-assemblies.git $GamePath
        if ($LASTEXITCODE -ne 0) { throw 'Cannot fetch build reference assemblies.' }
        git -C $GamePath checkout 0141355fecb8536715ecff22f99374b5e632cbed
        if ($LASTEXITCODE -ne 0) { throw 'Cannot select pinned reference assemblies.' }
    }
}
dotnet run --project tests/Checks.csproj --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Geometry checks failed.' }
dotnet build PhoebeFarmer/PhoebeFarmer.csproj -c Release "-p:GamePath=$GamePath" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$package = Join-Path $PSScriptRoot 'dist/PhoebeFarmer'
New-Item -ItemType Directory -Force $package | Out-Null
Copy-Item 'PhoebeFarmer/bin/Release/net6.0/PhoebeFarmer.dll' $package
Copy-Item 'PhoebeFarmer/manifest.json','PhoebeFarmer/config.json','README.md','LICENSE','CREDITS.md' $package
Copy-Item 'PhoebeFarmer/assets' $package -Recurse -Force
New-Item -ItemType Directory -Force (Join-Path $package 'art/previews') | Out-Null
Copy-Item 'art/previews/*.jpg' (Join-Path $package 'art/previews')
$version = (Get-Content 'PhoebeFarmer/manifest.json' -Raw | ConvertFrom-Json).Version
$archive = Join-Path $PSScriptRoot "dist/PhoebeFarmer-$version.zip"
Compress-Archive -LiteralPath $package -DestinationPath $archive -Force
Get-FileHash -Algorithm SHA256 $archive
