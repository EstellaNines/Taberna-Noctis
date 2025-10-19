# 📚 Taberna Noctis - 开发文档网站

> 这是《夜之小酒馆》游戏项目的在线文档中心

## 🌐 在线访问

部署后，团队成员可以通过浏览器访问此文档网站，无需安装任何软件。

## 📁 文件说明

- `index.html` - 主页面（单页应用）
- `*.md` - Markdown 格式的文档文件
- `部署指南.md` - 详细的服务器部署说明

## 🚀 快速部署

### 本地预览（开发测试）

```bash
# 方法1：Python
python -m http.server 8000

# 方法2：Node.js
npx http-server -p 8000

# 然后访问：http://localhost:8000
```

### 部署到服务器

选择以下任一方式：

#### 1️⃣ 最简单：Netlify（推荐新手）

1. 访问 [netlify.com](https://netlify.com)
2. 注册账号
3. 将整个 `Web` 文件夹拖拽到上传区
4. 完成！获得一个 `https://xxx.netlify.app` 地址

#### 2️⃣ 免费托管：GitHub Pages

```bash
git init
git add .
git commit -m "Deploy docs"
git remote add origin https://github.com/your-username/repo.git
git push -u origin main
# 在仓库设置中启用 GitHub Pages
```

#### 3️⃣ 云服务器：Nginx

```bash
# 上传文件到服务器
scp -r * root@your-server-ip:/var/www/html/docs/

# 配置 Nginx 并启动服务
```

## 📖 详细说明

完整的部署指南请查看 [部署指南.md](./部署指南.md)

## 🔧 技术栈

- **纯前端**：HTML + CSS + JavaScript
- **Markdown 解析**：Marked.js
- **图表渲染**：Mermaid.js
- **代码高亮**：Highlight.js
- **零依赖**：无需 Node.js/Python 运行时

## ✨ 功能特性

- 📱 响应式设计 - 支持手机/平板访问
- 🔍 实时搜索 - 快速查找文档
- 🎨 深色主题 - 护眼设计
- 📊 图表支持 - 自动渲染 Mermaid 流程图
- ⚡ 快速加载 - 单页应用，无刷新切换

## 📞 支持

如有问题，请联系开发团队或查看 `部署指南.md`

---

**项目**：Taberna Noctis（夜之小酒馆）  
**版本**：1.0  
**更新**：2025-10-19
