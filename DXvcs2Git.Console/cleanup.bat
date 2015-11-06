@echo off
setlocal EnableDelayedExpansion

set "excludeFile= cleanup.bat DXVcs2Git.Console.exe.config DXVcs2Git.UI.exe.config users.config trackconfig_common_2014.2.config trackconfig_common_2015.1.config trackconfig_common_2015.2.config trackconfig_reportdesigner.config dxvcs2git.core.dll "
set "excludeExt= .bat .exe .txt .dll"

for %%v in (*.*) do (
   if "!excludeFile: %%v =!" equ "%excludeFile%" if "!excludeExt: %%~Xv =!" equ "%excludeExt%" del %%v
)