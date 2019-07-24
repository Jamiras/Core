@echo off

set PACKAGE=https://github.com/Jamiras/Core/wiki/files/nUnit.3.11.zip
set MOQ_VERSION=4.3
set NUNIT_VERSION=3.11
set DOTNET_VERSION=40

set LIB_DIR=%1
if "%LIB_DIR%" == "" set LIB_DIR=../lib

rem ==== Check parallel folder first ====
set LIBS_EXIST=1
set MOQ_LIB=%LIB_DIR%/../../libs/moq-%MOQ_VERSION%/net%DOTNET_VERSION%/Moq.dll
set NUNIT_LIB=%LIB_DIR%/../../libs/nUnit-%NUNIT_VERSION%/net%DOTNET_VERSION%/nunit.framework.dll
if not exist %MOQ_LIB% set LIBS_EXIST=0
if not exist %NUNIT_LIB% set LIBS_EXIST=0
if "%LIBS_EXIST%" == "1" goto done

rem ==== Check nested folder ====
set LIBS_EXIST=1
set MOQ_LIB=%LIB_DIR%/moq-%MOQ_VERSION%/net%DOTNET_VERSION%/Moq.dll
set NUNIT_LIB=%LIB_DIR%/nUnit-%NUNIT_VERSION%/net%DOTNET_VERSION%/nunit.framework.dll
if not exist %MOQ_LIB% set LIBS_EXIST=0
if not exist %NUNIT_LIB% set LIBS_EXIST=0
if "%LIBS_EXIST%" == "1" goto done

rem ==== Download package ====
if not exist package.zip (
    echo Downloading %PACKAGE%...
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest %PACKAGE% -OutFile package.zip"
    if not exist package.zip (
        echo Could not download %PACKAGE%
        goto done
    )
)

rem ==== Extract package ====
echo Extracting to %LIB_DIR%...
powershell -Command "Expand-Archive 'package.zip' '%LIB_DIR%'"

rem ==== Validate package ====
set LIBS_EXIST=1
if not exist %MOQ_LIB% set LIBS_EXIST=0
if not exist %NUNIT_LIB% set LIBS_EXIST=0

if "%LIBS_EXIST%" == "0" (
    echo Failed to extract libs
    goto done
)

rem ==== Cleanup ====
del package.zip

echo Libs installed
:done
