$exePath = "C:\Users\areol\OneDrive - Wizard株式会社\優香共有用\gnosticlibrary\Production\PickleBall\アプリ\Photon X Pro\src\UI\PhotonXPro.UI.Maui\bin\Release\net10.0-windows10.0.19041.0\win-x64\PhotonXPro.UI.Maui.exe"

# Register "Open with Photon X Pro"
$regPath = "HKCU:\Software\Classes\SystemFileAssociations\.pdf\shell\PhotonXPro"
if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force }
Set-ItemProperty -Path $regPath -Name "MUIVerb" -Value "Photon X Pro で開く"
Set-ItemProperty -Path $regPath -Name "Icon" -Value "$exePath,0"

$commandPath = "$regPath\command"
if (-not (Test-Path $commandPath)) { New-Item -Path $commandPath -Force }
Set-ItemProperty -Path $commandPath -Name "(Default)" -Value "`"$exePath`" `"%1`""

# Register "Merge with Photon X Pro" (Placeholder logic)
$mergePath = "HKCU:\Software\Classes\SystemFileAssociations\.pdf\shell\PhotonXProMerge"
if (-not (Test-Path $mergePath)) { New-Item -Path $mergePath -Force }
Set-ItemProperty -Path $mergePath -Name "MUIVerb" -Value "Photon X Pro で結合"
Set-ItemProperty -Path $mergePath -Name "Icon" -Value "$exePath,0"

$mergeCommandPath = "$mergePath\command"
if (-not (Test-Path $mergeCommandPath)) { New-Item -Path $mergeCommandPath -Force }
Set-ItemProperty -Path $mergeCommandPath -Name "(Default)" -Value "`"$exePath`" --merge `"%1`""

Write-Host "Context menu registration completed successfully. Try right-clicking a PDF file." -ForegroundColor Green
