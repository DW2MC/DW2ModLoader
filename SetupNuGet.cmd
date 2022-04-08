@echo off & cd /d %~dp0
setlocal
set /p U=GitHub User:
set /p P=GitHub Token:
dotnet nuget add source -n Tyler-IN -u "%U%" -p "%P%" --store-password-in-clear-text --configfile "%AppData%\NuGet\NuGet.Config"