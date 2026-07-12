$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root "Assets"
New-Item -ItemType Directory -Path $assets -Force | Out-Null
$output = Join-Path $assets "PulseWidget.ico"

$bitmap = New-Object System.Drawing.Bitmap 64, 64, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$background = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 18, 22, 29))
$graphics.FillEllipse($background, 2, 2, 60, 60)

$body = New-Object System.Drawing.Drawing2D.GraphicsPath
$body.StartFigure()
$body.AddBezier(34, 7, 45, 13, 51, 23, 50, 34)
$body.AddLine(42, 42, 36, 55)
$body.AddLine(30, 44)
$body.AddLine(20, 36)
$body.AddLine(8, 33)
$body.AddLine(22, 24)
$body.AddBezier(22, 24, 25, 16, 29, 10, 34, 7)
$body.CloseFigure()

$bodyBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point 15, 12),
    (New-Object System.Drawing.Point 50, 49),
    ([System.Drawing.Color]::FromArgb(255, 98, 167, 255)),
    ([System.Drawing.Color]::FromArgb(255, 102, 227, 164)))
$graphics.FillPath($bodyBrush, $body)

$windowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 18, 22, 29))
$windowPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 102, 227, 164)), 2
$graphics.FillEllipse($windowBrush, 31, 20, 11, 11)
$graphics.DrawEllipse($windowPen, 31, 20, 11, 11)

$flame = New-Object System.Drawing.Drawing2D.GraphicsPath
$flame.AddPolygon([System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 20, 39),
    (New-Object System.Drawing.Point 10, 54),
    (New-Object System.Drawing.Point 27, 45)))
$flameBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 102, 227, 164))
$graphics.FillPath($flameBrush, $flame)

$iconHandle = $bitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($iconHandle)
$stream = [System.IO.File]::Create($output)
$icon.Save($stream)
$stream.Dispose()

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class NativeIconMethods {
    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr handle);
}
"@
[NativeIconMethods]::DestroyIcon($iconHandle) | Out-Null

$icon.Dispose()
$flameBrush.Dispose()
$flame.Dispose()
$windowPen.Dispose()
$windowBrush.Dispose()
$bodyBrush.Dispose()
$body.Dispose()
$background.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
