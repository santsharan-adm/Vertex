
# Monitor TCP connections to remote port 501
$portToMonitor = 501
$interval = 5  # seconds

Write-Host "Monitoring TCP connections to port $portToMonitor every $interval seconds..."
Write-Host "Press Ctrl+C to stop."

while ($true) {
    $connections = Get-NetTCPConnection | Where-Object { $_.RemotePort -eq $portToMonitor -and $_.State -eq 'Established' }
    
    if ($connections) {
        foreach ($conn in $connections) {
            Write-Host ("[{0}] Local: {1}:{2} -> Remote: {3}:{4} State: {5}" -f (Get-Date), $conn.LocalAddress, $conn.LocalPort, $conn.RemoteAddress, $conn.RemotePort, $conn.State)
        }
    } else {
        Write-Host ("[{0}] No active connections to port $portToMonitor" -f (Get-Date))
    }
    
    Start-Sleep -Seconds $interval
}
