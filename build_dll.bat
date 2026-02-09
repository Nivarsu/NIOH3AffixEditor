@echo off
REM Nioh3AffixCore C++ DLL 构建脚本
REM 需要安装 Visual Studio 2022 和 CMake

setlocal

set "PROJECT_DIR=%~dp0Nioh3AffixCore"
set "BUILD_DIR=%PROJECT_DIR%\build"

echo ========================================
echo Building Nioh3AffixCore.dll
echo ========================================

REM 检查 CMake 是否可用
where cmake >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: CMake not found in PATH
    echo Please install CMake and add it to PATH
    exit /b 1
)

REM 创建构建目录
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"

REM 配置 CMake (x64 Release)
echo.
echo Configuring CMake...
cmake -S "%PROJECT_DIR%" -B "%BUILD_DIR%" -G "Visual Studio 17 2022" -A x64
if %errorlevel% neq 0 (
    echo ERROR: CMake configuration failed
    exit /b 1
)

REM 构建 Release
echo.
echo Building Release...
cmake --build "%BUILD_DIR%" --config Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    exit /b 1
)

echo.
echo ========================================
echo Build successful!
echo DLL location: %PROJECT_DIR%\bin\Release\Nioh3AffixCore.dll
echo ========================================

REM 复制 DLL 到 C# 项目输出目录
set "CSHARP_OUTPUT=%~dp0bin\Debug\net9.0-windows"
if exist "%CSHARP_OUTPUT%" (
    echo Copying DLL to C# output directory...
    copy /Y "%PROJECT_DIR%\bin\Release\Nioh3AffixCore.dll" "%CSHARP_OUTPUT%\"
)

set "CSHARP_OUTPUT_RELEASE=%~dp0bin\Release\net9.0-windows"
if exist "%CSHARP_OUTPUT_RELEASE%" (
    copy /Y "%PROJECT_DIR%\bin\Release\Nioh3AffixCore.dll" "%CSHARP_OUTPUT_RELEASE%\"
)

echo Done!
pause
