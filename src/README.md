# OneMarkDotNet

## 项目简介

OneMarkDotNet 是一个 OneNote Markdown 扩展插件（COM Add-In），支持在 OneNote 中实时渲染 Markdown、将 OneNote 内容导出为 Markdown、主题定制等功能。它基于 .NET Framework 4.8 和 C# 9.0 构建，使用 Markdig 作为 Markdown 解析引擎，WebView2 进行 LaTeX 公式和 Mermaid 图表的离屏渲染，并通过 COM Interop 与 OneNote 深度集成。

## 功能特性

- **实时渲染** — 输入 Markdown 后按 Enter 即可自动渲染为 OneNote 原生格式
- **选区渲染** — 选中部分文本后按 F5 仅渲染选中区域
- **源码模式** — 按 Ctrl+, 进入源码模式查看和编辑原始 Markdown，按 Ctrl+Enter 退出并渲染
- **Markdown 导入** — 从剪贴板或文件导入 Markdown 到当前页面
- **Markdown 导出** — 将当前 OneNote 页面导出为 Markdown 文本
- **主题系统** — 内置多套 CSS 主题（学术风、暗色风等），支持自定义主题
- **代码高亮** — 支持 17 种编程语言的语法高亮渲染
- **LaTeX 公式** — 通过 WebView2 离屏渲染行内公式（`$...$`）和块级公式（`$$...$$`）
- **Mermaid 图表** — 支持流程图、序列图等 Mermaid 图表渲染
- **全局快捷键** — 通过 WH_KEYBOARD_LL 全局键盘钩子实现跨窗口快捷操作

## 系统要求

| 依赖 | 最低版本 |
|------|---------|
| Windows | 10 1809+ |
| .NET Framework | 4.8 |
| OneNote | 2016 / Microsoft 365 |
| WebView2 Runtime | 最新稳定版 |

## 快速开始

### 构建

1. 使用 Visual Studio 2022 打开 `OneMarkDotNet.sln`
2. 还原 NuGet 包
3. 构建 Debug 版本

### 安装

1. 以管理员身份打开命令提示符
2. 导航到构建输出目录
3. 使用 regasm 注册 COM 组件：

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /codebase OneMarkDotNet.dll
```

> RegAsm.exe 随 .NET Framework 自动安装，无需单独下载。如果命令找不到，请使用完整路径。

4. 重启 OneNote，在功能区中找到 OneMark 选项卡

### 卸载

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /u OneMarkDotNet.dll
```

## 项目结构

```
OneMarkDotNet/
├── OneMarkAddIn.cs              # COM Add-In 入口，实现 IDTExtensibility2 接口
├── OneMarkRibbon.cs             # Ribbon 自定义命令按钮
├── Ribbon.xml                   # Ribbon UI 定义
├── MarkdownParser.cs            # Markdown 解析器，基于 Markdig
├── MarkdownDocument.cs          # 文档模型，管理元素树
├── MarkdownRenderer.cs          # HTML / OneNote XML 渲染器
├── MarkdownExtension.cs         # Markdig 扩展（数学公式、Mermaid 图表）
├── MarkdownRenderHandler.cs     # 渲染流程控制器
├── OneNoteXmlConverter.cs       # OneNote XML 双向转换
├── HtmlToOneNoteConverter.cs    # Markdown → OneNote XML 转换
├── OneNoteToMarkdownConverter.cs# OneNote XML → Markdown 转换
├── OneNoteApiWrapper.cs         # OneNote COM API 封装
├── OneNotePageUpdater.cs        # 页面内容更新器
├── OneNoteXmlBuilder.cs         # OneNote XML 构建工具
├── CodeHighlightService.cs      # 17 种语言代码高亮服务
├── LatexRenderService.cs        # LaTeX 公式渲染服务（WebView2 离屏）
├── DiagramRenderService.cs      # Mermaid 图表渲染服务（WebView2 离屏）
├── WebView2Helper.cs            # WebView2 环境初始化辅助
├── ThemeManager.cs              # 主题管理器
├── Theme.cs                     # 主题数据模型
├── KeyboardHook.cs              # 全局键盘钩子（WH_KEYBOARD_LL）
├── AddInSettings.cs             # 用户设置管理
├── AppLogger.cs                 # 异步日志（ConcurrentQueue + Timer）
├── ExportHandler.cs             # 导入导出流程控制器
├── MarkdownExporter.cs          # Markdown 导出实现
├── MarkdownImporter.cs          # Markdown 导入实现
├── ClipboardHelper.cs           # 剪贴板操作辅助
├── FrontMatterParser.cs         # YAML Front Matter 解析器
├── HtmlTemplateGenerator.cs     # HTML 模板生成器
├── CssStyleMapper.cs            # CSS 样式映射
├── CssVariableParser.cs         # CSS 变量解析器
├── ConverterOneNoteStyle.cs     # OneNote 样式转换器
├── OneNoteStyle.cs              # OneNote 样式模型
├── BorderStyle.cs               # 边框样式枚举
├── OutlineElement.cs            # 大纲元素模型
├── RenderResult.cs              # 渲染结果模型
├── MarkdownImage.cs             # 图片元素模型
├── ImageHandler.cs              # 图片处理辅助
├── Elements/                    # 20 个元素模型类
│   ├── BlockElement.cs          #   块级元素基类
│   ├── InlineElement.cs         #   行内元素基类
│   ├── HeadingElement.cs        #   标题元素
│   ├── ParagraphElement.cs      #   段落元素
│   ├── CodeBlockElement.cs      #   代码块元素
│   ├── CodeInlineElement.cs     #   行内代码元素
│   ├── QuoteBlockElement.cs     #   引用块元素
│   ├── ListElement.cs           #   列表元素
│   ├── TaskListElement.cs       #   任务列表元素
│   ├── TableElement.cs          #   表格元素
│   ├── HorizontalRuleElement.cs #   分隔线元素
│   ├── MathBlockElement.cs      #   块级数学公式元素
│   ├── MathInlineElement.cs     #   行内数学公式元素
│   ├── DiagramBlockElement.cs   #   图表块元素
│   ├── BoldElement.cs           #   加粗元素
│   ├── ItalicElement.cs         #   斜体元素
│   ├── StrikethroughElement.cs  #   删除线元素
│   ├── LinkElement.cs           #   链接元素
│   ├── ImageElement.cs          #   图片元素
│   └── TextElement.cs           #   纯文本元素
└── Themes/                      # CSS 主题文件
    ├── __global.css             #   全局基础样式
    ├── academic.css             #   学术风格主题
    └── dark.css                 #   暗色风格主题
```

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| F5 | 渲染选区 Markdown |
| F8 | 导出当前页面为 Markdown |
| Ctrl+, | 进入源码模式 |
| Ctrl+Enter | 退出源码模式并渲染 |
| Enter | 实时渲染（在行尾按 Enter 触发） |
| Tab | 渲染表格 / 列表 |

## 主题系统

主题文件位于 `Themes/` 目录下，使用 CSS 变量驱动样式：

- `__global.css` — 全局基础样式，定义默认 CSS 变量
- `academic.css` — 学术风格主题
- `dark.css` — 暗色风格主题

### 自定义主题

1. 复制现有主题文件作为模板
2. 修改 CSS 变量值（颜色、字号、间距等）
3. 将新主题文件放入 `Themes/` 目录
4. 在设置中选择新主题

主题通过 `ThemeManager` 加载，`CssVariableParser` 解析 CSS 变量，`CssStyleMapper` 将 CSS 样式映射为 OneNote 原生样式属性。

## 许可证

本项目基于 [MIT License](https://opensource.org/licenses/MIT) 开源。
