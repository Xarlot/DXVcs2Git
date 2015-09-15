@echo off
setlocal EnableDelayedExpansion

set "excludeFile= cleanup.bat DXVcs2Git.Console.exe.config DXVcs2Git.UI.exe.config users.config trackconfig_common.config  "
set "excludeExt= .bat .dll .exe .txt "

for %%v in (*.*) do (
   if "!excludeFile: %%v =!" equ "%excludeFile%" if "!excludeExt: %%~Xv =!" equ "%excludeExt%" del %%v
)