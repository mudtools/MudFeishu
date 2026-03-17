# FeishuWikiManager 脚本说明

## 脚本列表

| 脚本文件 | 用途 | 说明 |
|---------|------|------|
| `build-backend.bat/ps1` | 后端编译 | 编译后端项目（Debug + Release） |
| `run-backend.bat/ps1` | 后端运行 | 启动后端开发服务器 (端口 5000) |
| `build-frontend.bat/ps1` | 前端编译 | 编译前端项目 |
| `run-frontend.bat/ps1` | 前端运行 | 启动前端开发服务器 (端口 5173) |
| `build-all.bat/ps1` | 全部编译 | 同时编译前后端 |
| `start-dev.bat/ps1` | 开发启动 | 同时启动前后端开发服务器 |

## 使用方法

### Windows 批处理脚本 (.bat)

```cmd
# 编译后端
build-backend.bat

# 运行后端
run-backend.bat

# 编译前端
build-frontend.bat

# 运行前端
run-frontend.bat

# 编译全部
build-all.bat

# 启动开发环境（同时启动前后端）
start-dev.bat
```

### PowerShell 脚本 (.ps1)

```powershell
# 编译后端
.\build-backend.ps1

# 运行后端
.\run-backend.ps1

# 编译前端
.\build-frontend.ps1

# 运行前端
.\run-frontend.ps1

# 编译全部
.\build-all.ps1

# 启动开发环境
.\start-dev.ps1
```

## 端口配置

| 服务 | 端口 | URL |
|------|------|-----|
| 后端 API | 5000 | http://localhost:5000 |
| 前端 Dev Server | 5173 | http://localhost:5173 |

## 前置要求

- **后端**: .NET 10 SDK
- **前端**: Node.js 16+ (推荐 18+)

## 注意事项

1. 首次运行前端脚本会自动安装依赖
2. `start-dev` 会在新窗口中启动服务
3. 生产环境部署请使用 `build-all` 编译后使用
