@echo off
rem ============================================================
rem  POSTEK Caisse - lancement de l'application
rem  Compile si necessaire puis demarre la caisse.
rem  Le premier demarrage ouvre l'ecran de creation administrateur/login.
rem  Usage : start.bat [--test-impression [nom-imprimante]]
rem ============================================================
setlocal
set "RACINE=%~dp0"
set "PROJET=%RACINE%desktop\Postek.Caisse\Postec.Caisse.csproj"
set "SORTIE=%RACINE%desktop\Postek.Caisse\bin\Debug\net8.0-windows"
set "EXECUTABLE=%SORTIE%\Postec.Caisse.exe"

if not exist "%PROJET%" (
    echo Projet POSTEK introuvable : %PROJET%
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo Le SDK .NET est introuvable. Installez .NET 8 puis relancez start.bat.
    pause
    exit /b 1
)

cd /d "%RACINE%desktop"

rem Deja lancee ? On ne demarre pas deux fois.
tasklist /FI "IMAGENAME eq Postec.Caisse.exe" 2>nul | find /I "Postec.Caisse.exe" >nul
if not errorlevel 1 (
    echo POSTEK est deja lance.
    exit /b 0
)

echo Compilation...
dotnet build "%PROJET%" -v quiet --nologo
if errorlevel 1 (
    echo Erreur de compilation. Verifiez que le SDK .NET 8 est installe.
    pause
    exit /b 1
)

if not exist "%EXECUTABLE%" (
    echo Executable introuvable apres la compilation : %EXECUTABLE%
    pause
    exit /b 1
)

echo Demarrage de POSTEK...
start "POSTEK Caisse" /D "%SORTIE%" "%EXECUTABLE%" %*
endlocal
