#!/bin/bash

echo "========================================"
echo "  Taberna Noctis 文档服务器启动脚本"
echo "========================================"
echo ""

# 检查 Python 是否安装
if command -v python3 &> /dev/null; then
    echo "[+] 检测到 Python 3，正在启动服务器..."
    echo ""
    echo "服务器地址: http://localhost:8000/index.html"
    echo ""
    echo "按 Ctrl+C 停止服务器"
    echo "========================================"
    echo ""
    python3 -m http.server 8000
    exit 0
elif command -v python &> /dev/null; then
    echo "[+] 检测到 Python，正在启动服务器..."
    echo ""
    echo "服务器地址: http://localhost:8000/index.html"
    echo ""
    echo "按 Ctrl+C 停止服务器"
    echo "========================================"
    echo ""
    python -m http.server 8000
    exit 0
fi

# 检查 Node.js 是否安装
if command -v node &> /dev/null; then
    echo "[+] 检测到 Node.js，正在启动服务器..."
    echo ""
    echo "服务器地址: http://localhost:8000/index.html"
    echo ""
    echo "按 Ctrl+C 停止服务器"
    echo "========================================"
    echo ""
    npx -y http-server -p 8000
    exit 0
fi

# 如果都没有安装
echo "[!] 错误: 未检测到 Python 或 Node.js"
echo ""
echo "请安装以下任一工具："
echo "  - Python 3: https://www.python.org/downloads/"
echo "  - Node.js: https://nodejs.org/"
echo ""
echo "或者手动启动服务器："
echo "  Python:  python3 -m http.server 8000"
echo "  Node.js: npx http-server -p 8000"
echo ""
read -p "按任意键退出..."

