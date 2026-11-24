param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $root 'dist' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'The Windows .NET Framework C# compiler is required.' }
$output = Join-Path $OutputDirectory 'DaFeiYu.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /utf8output "/win32manifest:$root\app.manifest" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll "/resource:$root\assets\ocean-maid.png,DaFeiYu.Art" "/out:$output" @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Host "Built $output"
