# PowerShell script to resize cooldown.png to various sizes
# Requires ImageMagick or use built-in .NET methods

$sourcePath = "Assets\cooldown.png"
$basePath = "Assets\"

if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: $sourcePath not found"
    exit 1
}

# Create resized versions using .NET System.Drawing
Add-Type -AssemblyName System.Drawing

$sourceImage = [System.Drawing.Image]::FromFile($sourcePath)

# Resize to different sizes
$sizes = @{
    "Square44x44Logo.scale-200.png" = 88, 88
    "Square44x44Logo.targetsize-24_altform-unplated.png" = 24, 24
    "Square150x150Logo.scale-200.png" = 300, 300
    "StoreLogo.png" = 50, 50
    "SplashScreen.scale-200.png" = 620, 300
    "Wide310x150Logo.scale-200.png" = 620, 300
    "LockScreenLogo.scale-200.png" = 192, 192
}

foreach ($output in $sizes.Keys) {
    $width = $sizes[$output][0]
    $height = $sizes[$output][1]
    $outputPath = Join-Path $basePath $output
    
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    
    $graphics.DrawImage($sourceImage, 0, 0, $width, $height)
    
    $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graphics.Dispose()
    $bitmap.Dispose()
    
    Write-Host "Created: $outputPath ($width x $height)"
}

$sourceImage.Dispose()
Write-Host "Icon resizing completed!"

