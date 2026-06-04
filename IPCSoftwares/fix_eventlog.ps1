# Run this script as Administrator (right-click -> Run with PowerShell as Admin)
# It registers the Event Log sources needed by the Bending apps

$sources = @("Bending-CoreService", "Bending-App", "IPCSoftware", "AOI-CoreService", "AOI-App")

foreach ($source in $sources) {
    try {
        if (-not [System.Diagnostics.EventLog]::SourceExists($source)) {
            New-EventLog -LogName "Application" -Source $source
            Write-Host "✓ Registered: $source" -ForegroundColor Green
        } else {
            Write-Host "✓ Already exists: $source" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "✗ Failed: $source - $_" -ForegroundColor Red
    }
}

Write-Host "`nDone. You can now run the apps normally." -ForegroundColor Cyan
Read-Host "Press Enter to close"
