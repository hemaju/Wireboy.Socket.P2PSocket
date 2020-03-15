@echo off
cd /d %~dp0
cd ../
echo %cd%\P2PSocket.StartUp-WinService.exe
sc create P2PSocket binPath="%cd%\P2PSocket.StartUp-WinService.exe -ws" start=auto displayname="wireboyÄÚÍø´©Í¸"
pause