# OneMarkDotNet

OneMarkDotNet 是一个 OneNote Markdown 插件，通过 COM Add-In 方式集成到 Microsoft OneNote 中，为 OneNote 提供 Markdown 实时渲染、导入导出和主题定制功能。

## 功能特性

- **实时 Markdown 渲染** — 在 OneNote 中输入 Markdown 语法，按 Enter 即可自动渲染为富文本
- **全页/选中渲染** — 按 F5 渲染当前行或选中文本，支持整页 Markdown 内容转换
- **源码模式切换** — 按 Ctrl+, 在渲染视图和源码视图之间切换，方便编辑原始 Markdown
- **Markdown 导入导出** — 支持将 OneNote 页面导出为 `.md` 文件，或将 `.md` 文件导入到 OneNote 页面
- **剪贴板导出** — 按 F8 将当前页面内容以 Markdown 格式复制到剪贴板
- **代码语法高亮** — 内置 17 种编程语言的语法高亮，支持行号显示
- **LaTeX 数学公式** — 通过 KaTeX 渲染数学公式，支持行内和块级公式
- **图表渲染** — 支持 Mermaid、流程图、时序图和思维导图
- **主题系统** — 通过 CSS 文件自定义渲染样式，支持多主题切换
- **YAML Front Matter** — 支持解析和序列化 Markdown 文档的元数据
- **图片处理** — 支持本地图片、Base64 编码图片和远程图片的转换与嵌入

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Enter` | 实时渲染当前行（需启用实时渲染） |
| `Ctrl+Enter` | 退出块源码模式并渲染 |
| `Ctrl+,` | 切换源码/渲染模式 |
| `F5` | 渲染当前行或选中文本 |
| `F8` | 导出当前页面为 Markdown 到剪贴板 |
| `Tab` | 在表格或列表中触发渲染 |

## 项目结构

```
OneMarkDotNet/
├── Directory.Build.props          # 解决方案级别 MSBuild 属性（默认 netstandard2.0）
├── OneMarkDotNet.sln              # Visual Studio 解决方案文件
├── src/
│   ├── OneNoteAddIn/              # COM Add-In 入口（.NET Framework 4.8）
│   │   ├── OneMarkAddIn.cs        # Add-In 主类，IDTExtensibility2 + IRibbonExtensibility
│   │   ├── OneNoteApiWrapper.cs   # OneNote COM API 封装
│   │   ├── OneNotePageUpdater.cs  # 页面更新器
│   │   ├── WebView2Helper.cs      # WebView2 辅助类
│   │   ├── DiagramRenderService.cs# 图表渲染服务
│   │   ├── LatexRenderService.cs  # LaTeX 渲染服务
│   │   └── ...
│   ├── MarkdownEngine/            # Markdown 解析、文档模型和渲染引擎（.NET Standard 2.0）
│   ├── OneNoteConverter/          # OneNote XML 与 Markdown 双向转换（.NET Standard 2.0）
│   ├── RenderingServices/         # 代码高亮和 HTML 模板生成（.NET Standard 2.0）
│   ├── ImportExport/              # Markdown 文件导入导出和剪贴板操作（.NET Standard 2.0）
│   ├── ThemeManager/              # CSS 主题加载、解析和应用（.NET Standard 2.0）
│   └── Shared/                    # 共享代码（Polyfills 等）
└── docs/                          # 项目文档
```

## 系统要求

- Windows 10 1809 或更高版本
- Microsoft OneNote 2016 / Microsoft 365
- .NET Framework 4.8（Windows 预装）
- Microsoft Edge WebView2 Runtime

## 构建项目

```bash
# 还原依赖
dotnet restore

# 构建解决方案
dotnet build

# 构建 Release 版本
dotnet build -c Release
```

## 安装

1. 构建 Release 版本
2. 使用 `RegAsm.exe` 注册 COM Add-In：
   ```cmd
   "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" bin\Release\net48\OneNoteAddIn.dll /codebase
   ```
3. 添加 OneNote AddIn 注册表项：
   ```reg
   [HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn]
   FriendlyName=OneMark
   Description=Markdown Plugin
   LoadBehavior=3
   ```
4. 重启 OneNote

或使用项目提供的 `register.bat` 脚本一键注册。

## 卸载

```cmd
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" bin\Release\net48\OneNoteAddIn.dll /unregister
reg delete "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" /f
```

或使用 `unregister.bat` 脚本。

## 主题定制

主题以 CSS 文件形式存储在 `%APPDATA%\OneMark\themes\` 目录下。每个 CSS 文件定义一组 CSS 变量来控制渲染样式。

支持的变量：

```css
:root {
  --font-family: "Calibri";
  --bg-color: #ffffff;
  --line-height: 1.6;
  --paragraph-margin: 8px;
  --monospace: "Consolas";
  --select-text-bg-color: #f5f5f5;
  --select-text-font-color: #333333;
  --blockquote-heading-icons: true;
  --enable-heading-in-blockquote: true;
  --enable-code-line-number: false;
  --enable-latex-to-image: true;
  --block-width-margin: #e0e0e0;
}
```

详细说明请参阅 [主题定制指南](docs/主题定制指南.md)。

## 配置

插件配置文件位于 `%APPDATA%\OneMark\settings.json`：

```json
{
  "isRealtimeRenderEnabled": true,
  "currentThemeName": "default",
  "isLineNumberEnabled": false,
  "isLatexToImage": true,
  "isSourceModeDefault": false
}
```

## 文档

- [架构设计](docs/架构设计.md) — 项目架构和模块关系
- [开发指南](docs/开发指南.md) — 开发环境搭建和编码规范
- [快捷键参考](docs/快捷键参考.md) — 完整快捷键列表
- [主题定制](docs/主题定制指南.md) — 主题创建和自定义

## 技术栈

- **.NET Framework 4.8** — AddIn 宿主框架
- **.NET Standard 2.0** — 业务层类库
- **Markdig** — Markdown 解析库
- **Microsoft.Office.Interop.OneNote** — OneNote COM 互操作
- **Microsoft.Web.WebView2** — WebView2 渲染引擎
- **KaTeX** — LaTeX 公式渲染
- **Mermaid** — 图表渲染

## 许可证

[Apache License 2.0](LICENSE)
