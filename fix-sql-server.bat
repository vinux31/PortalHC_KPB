@echo off
echo ====================================
echo Restarting SQL Server Express Service
echo ====================================
echo.
net stop "SQL Server (SQLEXPRESS)"
timeout /t 2 /nobreak >nul
net start "SQL Server (SQLEXPRESS)"
echo.
echo ====================================
echo Checking service status...
====================================
sc query "MSSQL$SQLEXPRESS" | find "STATE"
echo.
echo Done! Press any key to close...
pause >nul
