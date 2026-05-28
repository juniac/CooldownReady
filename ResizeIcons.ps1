# Resize cooldownv2.png into all app icon assets.

$sourcePath = "Assets\cooldownv2.png"
$basePath = "Assets"

if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: $sourcePath not found"
    exit 1
}

Add-Type -AssemblyName System.Drawing

function New-IconBitmap {
    param(
        [System.Drawing.Image]$Source,
        [int]$Width,
        [int]$Height
    )

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    $scale = [Math]::Min($Width / $Source.Width, $Height / $Source.Height)
    $drawWidth = [int][Math]::Round($Source.Width * $scale)
    $drawHeight = [int][Math]::Round($Source.Height * $scale)
    $x = [int][Math]::Round(($Width - $drawWidth) / 2)
    $y = [int][Math]::Round(($Height - $drawHeight) / 2)

    $graphics.DrawImage($Source, $x, $y, $drawWidth, $drawHeight)
    $graphics.Dispose()

    return $bitmap
}

function Save-PngAsset {
    param(
        [System.Drawing.Image]$Source,
        [string]$Name,
        [int]$Width,
        [int]$Height
    )

    $outputPath = Join-Path $basePath $Name
    $bitmap = New-IconBitmap -Source $Source -Width $Width -Height $Height
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Host "Created: $outputPath ($Width x $Height)"
}

function Convert-BitmapToIconDibBytes {
    param([System.Drawing.Bitmap]$Bitmap)

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)
    $width = $Bitmap.Width
    $height = $Bitmap.Height
    $xorBytes = $width * $height * 4
    $maskStride = [int]([Math]::Floor(($width + 31) / 32) * 4)
    $maskBytes = $maskStride * $height

    $writer.Write([UInt32]40)
    $writer.Write([Int32]$width)
    $writer.Write([Int32]($height * 2))
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]0)
    $writer.Write([UInt32]$xorBytes)
    $writer.Write([Int32]0)
    $writer.Write([Int32]0)
    $writer.Write([UInt32]0)
    $writer.Write([UInt32]0)

    for ($y = $height - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $width; $x++) {
            $color = $Bitmap.GetPixel($x, $y)
            $writer.Write([byte]$color.B)
            $writer.Write([byte]$color.G)
            $writer.Write([byte]$color.R)
            $writer.Write([byte]$color.A)
        }
    }

    for ($i = 0; $i -lt $maskBytes; $i++) {
        $writer.Write([byte]0)
    }

    $writer.Flush()
    $bytes = $stream.ToArray()
    $writer.Dispose()
    $stream.Dispose()

    return $bytes
}

function Save-IcoAsset {
    param(
        [System.Drawing.Image]$Source,
        [string]$Name,
        [int[]]$Sizes
    )

    $frames = @()

    foreach ($size in $Sizes) {
        $bitmap = New-IconBitmap -Source $Source -Width $size -Height $size
        $bytes = Convert-BitmapToIconDibBytes -Bitmap $bitmap
        $bitmap.Dispose()
        $frames += [PSCustomObject]@{
            Size = $size
            Bytes = $bytes
        }
    }

    $outputPath = Join-Path $basePath $Name
    $stream = [System.IO.File]::Create($outputPath)
    $writer = New-Object System.IO.BinaryWriter($stream)

    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$frames.Count)

    $offset = 6 + (16 * $frames.Count)

    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }

        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$frame.Bytes.Length)
        $writer.Write([UInt32]$offset)

        $offset += $frame.Bytes.Length
    }

    foreach ($frame in $frames) {
        $writer.Write([byte[]]$frame.Bytes)
    }

    $writer.Dispose()
    $stream.Dispose()
    Write-Host "Created: $outputPath ($($Sizes -join ', ') px)"
}

$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $sourcePath))

try {
    Save-PngAsset -Source $sourceImage -Name "cooldown.png" -Width 512 -Height 512
    Save-PngAsset -Source $sourceImage -Name "icon_512.png" -Width 512 -Height 512
    Save-PngAsset -Source $sourceImage -Name "Square44x44Logo.scale-200.png" -Width 88 -Height 88
    Save-PngAsset -Source $sourceImage -Name "Square44x44Logo.targetsize-24_altform-unplated.png" -Width 24 -Height 24
    Save-PngAsset -Source $sourceImage -Name "Square150x150Logo.scale-200.png" -Width 300 -Height 300
    Save-PngAsset -Source $sourceImage -Name "StoreLogo.png" -Width 50 -Height 50
    Save-PngAsset -Source $sourceImage -Name "SplashScreen.scale-200.png" -Width 620 -Height 300
    Save-PngAsset -Source $sourceImage -Name "Wide310x150Logo.scale-200.png" -Width 620 -Height 300
    Save-PngAsset -Source $sourceImage -Name "LockScreenLogo.scale-200.png" -Width 192 -Height 192
    Save-IcoAsset -Source $sourceImage -Name "cooldown.ico" -Sizes @(256, 128, 64, 48, 32, 24, 16)
}
finally {
    $sourceImage.Dispose()
}

Write-Host "Icon resizing completed."
