@echo off
rem Compila CapturaFlotante.exe usando el compilador que ya trae Windows (.NET Framework 4).
rem No hace falta instalar nada.
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo No se encontro csc.exe de .NET Framework 4.
  exit /b 1
)
"%CSC%" /nologo /target:winexe /optimize+ /out:"%~dp0CapturaFlotante.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0src\CapturaFlotante.cs"
if errorlevel 1 exit /b 1
echo.
echo Listo: %~dp0CapturaFlotante.exe
