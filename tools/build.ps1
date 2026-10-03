#Requires -Version 5.1
<#
.SYNOPSIS
    编译 MyChar mod，并可选部署到游戏的 mods 目录。

.EXAMPLE
    # 只编译 + 暂存到 pack\MyChar
    .\tools\build.ps1

.EXAMPLE
    # 编译并部署到游戏
    .\tools\build.ps1 -Deploy

.EXAMPLE
    # 游戏装在别处
    .\tools\build.ps1 -Deploy -GameDir "D:\Steam\sts2\data_sts2_windows_x86_64" -ModsDir "D:\Steam\sts2\mods"
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    # 游戏数据目录（里面有 sts2.dll / GodotSharp.dll / 0Harmony.dll）
    [string]$GameDir = 'F:\sts2\Slay the Spire 2\data_sts2_windows_x86_64',

    # BaseLib 目录（里面有 BaseLib.dll）
    [string]$BaseLibDir = 'F:\sts2\Slay the Spire 2\mods\BaseLib.3.4.5',

    # 游戏的可选 mods 目录（= exe 同级的 mods 文件夹）
    [string]$ModsDir = 'F:\sts2\Slay the Spire 2\mods',

    # 加这个开关才会真正复制进游戏目录
    [switch]$Deploy
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot          # ...\sts2-mod
$modId = 'MyChar'
$proj = Join-Path $root "src\$modId\$modId.csproj"
$pack = Join-Path $root "pack\$modId"

Write-Host "[1/3] 编译 ($Configuration) ..." -ForegroundColor Cyan
dotnet build $proj -c $Configuration -p:GameDir="$GameDir" -p:BaseLibDir="$BaseLibDir" -v minimal
if ($LASTEXITCODE -ne 0) { throw "编译失败（exit $LASTEXITCODE）" }

$outDir = Join-Path $root "src\$modId\bin\$Configuration"
$dll = Join-Path $outDir "$modId.dll"
$pck = Join-Path $outDir "$modId.pck"
if (-not (Test-Path $dll)) { throw "找不到产物：$dll" }
if (-not (Test-Path $pck)) { throw "找不到产物：$pck（PckPacker 没打成功？看编译日志里的 'PCK packed'）" }

Write-Host "[2/3] 暂存 -> pack\$modId" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $pack | Out-Null
Copy-Item $dll $pack -Force
Copy-Item $pck $pack -Force
Get-ChildItem $pack -File | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize

if (-not $Deploy) {
    Write-Host "[3/3] 已跳过部署（加 -Deploy 才会复制进游戏目录）" -ForegroundColor Yellow
    return
}

$target = Join-Path $ModsDir $modId
Write-Host "[3/3] 部署 -> $target" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item (Join-Path $pack '*') $target -Recurse -Force
Write-Host "已部署到游戏，重启游戏后生效。" -ForegroundColor Green
Get-ChildItem $target -File | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize