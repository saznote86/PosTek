@echo off
rem ============================================================
rem  POSTEK Caisse - lancement de l'application
rem  Compile si necessaire puis demarre la caisse.
rem  Usage : start.bat [--test-impression [nom-imprimante]]
rem ============================================================
setlocal
cd /d "%~dp0postek\desktop"

rem Deja lancee ? On ne demarre pas deux fois.
tasklist /FI "IMAGENAME eq Postek.Caisse.exe" 2>nul | find /I "Postek.Caisse.exe" >nul
if not errorlevel 1 (
    echo POSTEK est deja lance.
    exit /b 0
)

echo Compilation...
dotnet build Postek.Caisse\Postek.Caisse.csproj -v quiet --nologo
if errorlevel 1 (
    echo Erreur de compilation. Verifiez que le SDK .NET 8 est installe.
    pause
    exit /b 1
)

echo Demarrage de POSTEK...
start "" /D "%~dp0postek\desktop\Postek.Caisse\bin\Debug\net8.0-windows" "Postek.Caisse.exe" %*
endlocal
