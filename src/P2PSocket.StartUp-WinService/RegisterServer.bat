@echo off
set b=%cd%
echo %b%
echo %b%\P2PSocket.StartUp-WinService
sc create P2PSocket binPath="C:\Users\xiejm\Desktop\我的程序\P2PSocket - 代理测试\P2PSocket.StartUp-WinService.exe" start=auto displayname= "wireboy内网穿透"
pause