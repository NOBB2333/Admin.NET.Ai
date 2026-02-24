---
name: 首选开发栈 (Preferred Development Stack)
description: 使用 mise, dotnet, pnpm/vite/ts (Vue) 和 Python 虚拟环境配置开发环境的指南。
---

# 首选开发栈

本技能定义了用户首选的开发环境设置。

## 环境管理
- **mise**: 用户首选使用 `mise` 来管理语言版本和环境变量。
    - 始终检查 `mise` 是否可用及配置正确。
    - 如果需要执行依赖特定语言版本的操作，请确保在 `mise` 已激活或路径正确的上下文中运行。

## 后端开发 (.NET)
- **dotnet**: 用户使用 .NET 进行后端开发。
    - 如有必要，使用 `mise use dotnet` 确保激活正确的 .NET SDK 版本（尽管 `mise` 通常通过 `.tool-versions` 或 `mise.toml` 自动处理）。
    - 运行 dotnet 命令时，默认使用标准的 `dotnet` CLI。

## 前端开发 (Vue.js)
- **Vue.js 技术栈**: 对于任何前端 Vue 项目，以下技术栈是**必须的**：
    - **包管理器**: `pnpm` (除非明确指示，否则不使用 npm 或 yarn)。
    - **构建工具**: `vite`。
    - **语言**: `TypeScript` (ts)。
- 创建新的 Vue 项目时，请确保选择这些默认设置。
- 在现有的 Vue 项目中工作时，优先使用 `pnpm` 命令（例如：`pnpm install`, `pnpm dev`, `pnpm build`）。

## Python 开发
- **虚拟环境**: 用户使用标准的 Python 虚拟环境 (venv) 进行 Python 开发。
    - **全局虚拟环境路径**: `/Users/wong/Code/PythonLang/ENV_DIR`
    - **激活方式**:
        - **fish shell**: `source /Users/wong/Code/PythonLang/ENV_DIR/bin/activate.fish`
        - **bash/zsh**: `source /Users/wong/Code/PythonLang/ENV_DIR/bin/activate`
    - 在执行 Python 相关命令前，请确保虚拟环境已激活，或使用虚拟环境中的完整路径执行（如 `/Users/wong/Code/PythonLang/ENV_DIR/bin/python`）。
    - 安装依赖时使用 `pip install` 命令。
