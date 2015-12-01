 call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"
 set MSBUILD4=msbuild
%MSBUILD4% /nologo /t:rebuild /clp:errorsonly /property:Configuration=Release;BuildingInsideVisualStudio=true DXVcs2Git.sln >> build.log
Lib\AtomFeed\AtomfeedCore.exe %CD%\bin\Release\DXVcs2Git.GitTools.vsix