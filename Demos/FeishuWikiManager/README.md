# 飞书个人知识库管理系统

基于飞书开放平台的个人知识库管理系统，提供知识空间的创建、管理和文档的浏览、编辑等功能。

## ✨ 功能特性

- 🔐 **飞书 OAuth 认证** - 安全的用户身份验证
- 📁 **知识空间管理** - 创建和管理个人知识空间
- 🌳 **文档树浏览** - 可视化文档层级结构
- 📝 **文档操作** - 创建、重命名、移动文档
- ⭐ **收藏功能** - 快速收藏常用文档
- 🔍 **全文搜索** - 在知识库中搜索文档
- 🔄 **Token 自动刷新** - 无感知的会话保持

## 🛠 技术栈

### 后端
- .NET 10 Minimal API
- Entity Framework Core + SQLite
- JWT 认证
- 飞书开放平台 SDK

### 前端
- Vue 3 + TypeScript
- Vite
- Pinia 状态管理
- Vue Router
- Element Plus UI

## 📁 项目结构

```
FeishuWikiManager/
├── backend/                    # 后端项目
│   ├── Controllers/           # API 控制器
│   ├── Services/              # 业务服务
│   ├── Models/                # 数据模型
│   ├── Data/                  # 数据库上下文
│   └── Program.cs             # 应用入口
├── frontend/                   # 前端项目
│   ├── src/
│   │   ├── api/               # API 请求
│   │   ├── components/        # Vue 组件
│   │   ├── composables/       # 组合式函数
│   │   ├── router/            # 路由配置
│   │   ├── stores/            # Pinia 状态
│   │   ├── types/             # TypeScript 类型
│   │   └── views/             # 页面视图
│   └── package.json
├── build-backend.bat/ps1      # 后端编译脚本
├── run-backend.bat/ps1        # 后端运行脚本
├── build-frontend.bat/ps1     # 前端编译脚本
├── run-frontend.bat/ps1       # 前端运行脚本
├── build-all.bat/ps1          # 全部编译脚本
├── start-dev.bat/ps1          # 开发启动脚本
└── SCRIPTS.md                 # 脚本说明文档
```

## 🚀 快速开始

### 环境要求

- .NET 10 SDK
- Node.js 16+ (推荐 18+)
- 飞书开放平台应用

### 配置飞书应用

1. 在 [飞书开放平台](https://open.feishu.cn/) 创建企业自建应用
2. 配置应用权限：
   - `contact:user.base:readonly` - 获取用户基本信息
   - `wiki:wiki:readonly` - 读取知识库
   - `wiki:wiki` - 管理知识库
   - `docs:doc:readonly` - 读取文档
   - `docs:doc` - 编辑文档
3. 配置重定向 URL

### 配置项目

1. 复制后端配置模板：
```bash
cd backend
cp appsettings.json appsettings.local.json
```

2. 编辑 `appsettings.local.json`，填入飞书应用信息：
```json
{
  "Feishu": {
    "AppId": "your_app_id",
    "AppSecret": "your_app_secret"
  },
  "OAuth": {
    "RedirectUri": "http://localhost:5173/auth/feishu/callback"
  },
  "Jwt": {
    "SecretKey": "your_jwt_secret_key_at_least_32_characters",
    "Issuer": "FeishuWikiManager",
    "Audience": "FeishuWikiManager",
    "ExpirationHours": 24
  }
}
```

### 启动开发环境

**Windows:**
```cmd
start-dev.bat
```

**PowerShell:**
```powershell
.\start-dev.ps1
```

### 访问应用

- 前端: http://localhost:5173
- 后端 API: http://localhost:5000

## 📖 API 文档

### 认证接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/oauth/feishu/url` | 获取飞书授权 URL |
| POST | `/api/oauth/feishu/callback` | 处理 OAuth 回调 |
| GET | `/api/oauth/me` | 获取当前用户信息 |
| GET | `/api/oauth/status` | 获取 Token 状态 |
| POST | `/api/oauth/refresh` | 刷新 Token |
| POST | `/api/oauth/logout` | 登出 |

### 知识空间接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/wiki/spaces` | 获取知识空间列表 |
| GET | `/api/wiki/spaces/{spaceId}` | 获取空间详情 |
| POST | `/api/wiki/spaces` | 创建知识空间 |

### 文档节点接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/wiki/nodes/tree/{spaceId}` | 获取节点树 |
| GET | `/api/wiki/nodes/{token}` | 获取节点信息 |
| POST | `/api/wiki/nodes/{spaceId}` | 创建节点 |
| PUT | `/api/wiki/nodes/{spaceId}/{nodeToken}/title` | 更新标题 |
| POST | `/api/wiki/nodes/{spaceId}/{nodeToken}/move` | 移动节点 |
| POST | `/api/wiki/nodes/search` | 搜索文档 |

### 收藏接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/wiki/nodes/favorites` | 获取收藏列表 |
| POST | `/api/wiki/nodes/favorites` | 添加收藏 |
| DELETE | `/api/wiki/nodes/favorites/{nodeToken}` | 取消收藏 |

## 🔧 开发脚本

| 脚本 | 说明 |
|------|------|
| `build-backend.bat/ps1` | 编译后端 |
| `run-backend.bat/ps1` | 运行后端 |
| `build-frontend.bat/ps1` | 编译前端 |
| `run-frontend.bat/ps1` | 运行前端 |
| `build-all.bat/ps1` | 编译全部 |
| `start-dev.bat/ps1` | 启动开发环境 |

详细说明请参阅 [SCRIPTS.md](./SCRIPTS.md)

## 🏗️ 生产部署

### 编译

```bash
# 编译全部
build-all.bat

# 或
.\build-all.ps1
```

### 后端部署

```bash
cd backend
dotnet publish -c Release -o ./publish
```

### 前端部署

```bash
cd frontend
npm run build
# 静态文件在 dist/ 目录
```

## 📝 开发进度

- [x] 飞书 OAuth 认证
- [x] Token 自动刷新
- [x] 知识空间管理
- [x] 文档树浏览
- [x] 文档详情查看
- [x] 文档创建/重命名/移动
- [x] 文档收藏
- [x] 全文搜索
- [ ] 文档编辑
- [ ] 自动化测试
- [ ] Docker 部署

## 📄 许可证

MIT License

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！
