#!/bin/bash

# ============================================
# HeMaCupAICheck 编译脚本
# 自包含单文件 + 压缩 (无需安装 .NET 即可运行)
# ============================================

dotnet publish HeMaCupAICheck/HeMaCupAICheck.csproj \
  -c Release \
  -r osx-arm64 \
  -o ./publish \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false

echo ""
echo "✅ 编译完成! 输出目录: ./publish"
echo "📦 可执行文件: ./publish/HeMaCupAICheck"