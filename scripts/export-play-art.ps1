[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
# Technical export on Windows; generated masters and official character artwork stay untouched.
Add-Type -AssemblyName System.Drawing
$Root=Join-Path $PSScriptRoot '../References/Release'
function Export-Png([string]$Source,[string]$Target,[int]$Width,[int]$Height){
    $InputImage=[Drawing.Image]::FromFile($Source)
    $Bitmap=New-Object Drawing.Bitmap($Width,$Height)
    $Graphics=[Drawing.Graphics]::FromImage($Bitmap)
    try{
        $Graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $Graphics.DrawImage($InputImage,0,0,$Width,$Height)
        $Bitmap.Save($Target,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$Graphics.Dispose();$Bitmap.Dispose();$InputImage.Dispose()}
}
Export-Png (Join-Path $Root 'icon-proposal.png') (Join-Path $Root 'icon-512.png') 512 512
Export-Png (Join-Path $Root 'feature-graphic-proposal.png') (Join-Path $Root 'feature-graphic-1024x500.png') 1024 500
Write-Host 'Submission-size artwork exported. Final artwork approval and Console submission remain separate.'
