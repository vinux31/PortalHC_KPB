# Enable TCP/IP for SQL Server Express
[Reflection.Assembly]::LoadWithPartialName('Microsoft.SqlServer.SqlWmiManagement') | Out-Null

$mc = New-Object Microsoft.SqlServer.Management.Smo.Wmi.ManagedComputer

# Get TCP protocol for SQLEXPRESS
$tcp = $mc.ServerInstances['SQLEXPRESS'].ServerProtocols['Tcp']

if ($tcp.IsEnabled -eq $false) {
    Write-Host "Enabling TCP/IP for SQL Server Express..."
    $tcp.IsEnabled = $true
    $tcp.Alter()
    Write-Host "TCP/IP enabled successfully"
} else {
    Write-Host "TCP/IP already enabled"
}

# Restart SQL Server service
Write-Host "Restarting SQL Server Express service..."
Restart-Service MSSQL$SQLEXPRESS -Force
Write-Host "Done!"
