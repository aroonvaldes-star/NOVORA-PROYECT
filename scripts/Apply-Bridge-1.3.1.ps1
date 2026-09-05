param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$SourceRoot = [System.IO.Path]::GetFullPath($SourceRoot)

function Get-RequiredFile {
    param([string]$RelativePath)

    $path = Join-Path $SourceRoot $RelativePath

    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Archivo requerido no encontrado: $path"
    }

    return $path
}

function Replace-RequiredText {
    param(
        [string]$Path,
        [string]$OldValue,
        [string]$NewValue
    )

    $text = [System.IO.File]::ReadAllText($Path)

    if (-not $text.Contains($OldValue, [System.StringComparison]::Ordinal)) {
        throw "No se encontró el texto requerido en $Path : $OldValue"
    }

    $text = $text.Replace($OldValue, $NewValue, [System.StringComparison]::Ordinal)
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Replace-RequiredRegex {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Replacement
    )

    $text = [System.IO.File]::ReadAllText($Path)
    $regex = [System.Text.RegularExpressions.Regex]::new(
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline)

    if (-not $regex.IsMatch($text)) {
        throw "No se encontró el patrón requerido en $Path : $Pattern"
    }

    $text = $regex.Replace($text, $Replacement, 1)
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Assert-ContainsText {
    param(
        [string]$Path,
        [string]$Expected
    )

    $text = [System.IO.File]::ReadAllText($Path)

    if (-not $text.Contains($Expected, [System.StringComparison]::Ordinal)) {
        throw "Validación fallida. $Path no contiene: $Expected"
    }
}

$projectFile = Get-RequiredFile 'src\NOVORA\NOVORA.csproj'
$updateFile = Get-RequiredFile 'src\NOVORA\Services\UpdateService.cs'
$mainWindow = Get-RequiredFile 'src\NOVORA\MainWindow.xaml'
$settingsWindow = Get-RequiredFile 'src\NOVORA\SettingsWindow.xaml'
$installerFile = Get-RequiredFile 'Installer\NOVORA.Installer.iss'

Write-Host 'Aplicando puente NOVORA-LINK 1.3.1...' -ForegroundColor Cyan

Replace-RequiredText $projectFile '<Version>1.3.0</Version>' '<Version>1.3.1</Version>'
Replace-RequiredText $updateFile 'aroonvaldes-star/NOVORA-PROYECT' 'aroonvaldes-star/NOVORA-LINK'
Replace-RequiredText $updateFile 'new Version(1, 2, 0)' 'new Version(1, 3, 1)'
Replace-RequiredRegex $updateFile 'new ProductInfoHeaderValue\(\s*"NOVORA",\s*"1\.3"\)' 'new ProductInfoHeaderValue("NOVORA", "1.3.1")'
Replace-RequiredText $mainWindow 'Title="NOVORA 1.3"' 'Title="NOVORA 1.3.1"'
Replace-RequiredText $settingsWindow 'Title="Configuración — NOVORA 1.3"' 'Title="Configuración — NOVORA 1.3.1"'
Replace-RequiredText $settingsWindow 'Text="BASE 1.3"' 'Text="BASE 1.3.1"'
Replace-RequiredText $installerFile '#define AppVersion "1.3"' '#define AppVersion "1.3.1"'

$updateText = [System.IO.File]::ReadAllText($updateFile)

if ($updateText.Contains('aroonvaldes-star/NOVORA-PROYECT', [System.StringComparison]::Ordinal)) {
    throw 'El updater todavía contiene una referencia activa a NOVORA-PROYECT.'
}

Assert-ContainsText $projectFile '<Version>1.3.1</Version>'
Assert-ContainsText $updateFile 'aroonvaldes-star/NOVORA-LINK'
Assert-ContainsText $updateFile 'https://api.github.com/repos/aroonvaldes-star/NOVORA-LINK/releases/latest'
Assert-ContainsText $updateFile 'new Version(1, 3, 1)'
Assert-ContainsText $updateFile 'new ProductInfoHeaderValue("NOVORA", "1.3.1")'
Assert-ContainsText $mainWindow 'Title="NOVORA 1.3.1"'
Assert-ContainsText $settingsWindow 'Title="Configuración — NOVORA 1.3.1"'
Assert-ContainsText $settingsWindow 'Text="BASE 1.3.1"'
Assert-ContainsText $installerFile '#define AppVersion "1.3.1"'

Write-Host 'Puente 1.3.1 aplicado y validado.' -ForegroundColor Green
Write-Host 'Canal futuro: aroonvaldes-star/NOVORA-LINK' -ForegroundColor Green
