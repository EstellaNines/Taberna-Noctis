@echo off
echo ========================================
echo   Taberna Noctis 文档服务器启动脚本
echo ========================================
echo.

REM 检查 Python 是否安装
where python >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [+] 检测到 Python，正在启动服务器...
    echo.
    echo 服务器地址: http://localhost:8000/index.html
    echo.
    echo 按 Ctrl+C 停止服务器
    echo ========================================
    echo.
    python -m http.server 8000
    goto :end
)

REM 检查 Node.js 是否安装
where node >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [+] 检测到 Node.js，正在启动服务器...
    echo.
    echo 服务器地址: http://localhost:8000/index.html
    echo.
    echo 按 Ctrl+C 停止服务器
    echo ========================================
    echo.
    npx -y http-server -p 8000
    goto :end
)

REM 如果都没有安装
echo [!] 错误: 未检测到 Python 或 Node.js
echo.
echo 请安装以下任一工具：
echo   - Python 3: https://www.python.org/downloads/
echo   - Node.js: https://nodejs.org/
echo.
echo 或者手动启动服务器：
echo   Python:  python -m http.server 8000
echo   Node.js: npx http-server -p 8000
echo.
pause

:end

