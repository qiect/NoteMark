# 架构设计

## 概述

OneMarkDotNet 采用分层模块化架构，由 6 个项目组成，各项目职责明确，依赖关系清晰。核心设计思路是将 Markdown 处理、OneNote 交互、渲染服务和主题管理解耦，通过 OneNoteAddIn 项目作为宿主进行协调。

## 模块依赖关系

```
┌─────────────────────────────────────────────────────────┐
│                     OneNoteAddIn                        │
│            (COM Add-In 宿主 / Ribbon UI)                │
├─────────┬──────────┬──────────┬──────────┬─────────────┤
│         │          │          │          │             │
│         ▼          ▼          ▼          ▼             │
│  MarkdownEngine  OneNoteConverter  RenderingServices   │
│                  ImportExport       ThemeManager        │
│         ▲          ▲                    ▲              │
│         │          │                    │              │
│         │    ┌─────┘                    │              │
│         │    │                          │              │
└─────────┼────┼──────────────────────────┼──────────────┘
          │    │                          │
          ▼    ▼                          │
   MarkdownEngine ◄──────────────────────┘
```

## 项目详解

### OneNoteAddIn

COM Add-In 入口项目，负责与 OneNote 进程集成。

| 类 | 职责 |
|---|------|
| `OneMarkAddIn` | Add-In 主入口，实现 `IDTExtensibility2` 和 `IRibbonExtensibility`，协调各组件初始化和生命周期管理 |
| `MarkdownRenderHandler` | Markdown 渲染处理器，处理实时渲染、F5 渲染和源码模式切换 |
| `ExportHandler` | 导入导出处理器，处理 F8 剪贴板导出、文件导入和文件导出 |
| `OneNoteEventHandler` | OneNote 事件处理器，监听页面切换事件 |
| `KeyboardHook` | 全局键盘钩子，拦截快捷键并分发事件 |
| `AddInSettings` | 插件设置管理（单例），持久化到 JSON 文件 |
| `AppLogger` | 异步日志记录器（单例），队列缓冲 + 定时刷新 |
| `OneMarkRibbon` | Ribbon UI 自定义，定义按钮和动态主题菜单 |

**依赖**: MarkdownEngine, RenderingServices, ThemeManager, OneNoteConverter, ImportExport

### MarkdownEngine

Markdown 解析和渲染引擎，是整个系统的核心模块。

| 类 | 职责 |
|---|------|
| `MarkdownParser` | 解析入口，配置 Markdig 管道并调用自定义扩展 |
| `MarkdownDocument` | 文档模型，包含从 Markdig AST 到自定义元素模型的转换逻辑 |
| `MarkdownRenderer` | 渲染器基类，包含 `HtmlMarkdownRenderer`（HTML 输出）和 `OneNoteXmlRenderer`（OneNote XML 输出） |
| `MarkdownExtension` | Markdig 扩展集合，包含数学公式、图表和引用块标题图标三个扩展 |
| `MarkdownImage` | 图片数据模型 |

**元素模型**:

```
BlockElement (抽象基类)
├── ParagraphElement       段落
├── HeadingElement         标题 (h1-h6)
├── CodeBlockElement       代码块
├── DiagramBlockElement    图表块 (Mermaid/Flow/Sequence/Mindmap)
├── QuoteBlockElement      引用块
├── TableElement           表格
├── ListElement            有序/无序列表
├── TaskListElement        任务列表
├── MathBlockElement       数学公式块
└── HorizontalRuleElement  水平线

InlineElement (抽象基类)
├── TextElement            纯文本
├── BoldElement            加粗
├── ItalicElement          斜体
├── StrikethroughElement   删除线
├── CodeInlineElement      行内代码
├── LinkElement            链接
├── ImageElement           图片
└── MathInlineElement      行内数学公式
```

**依赖**: Markdig (NuGet)

### OneNoteConverter

OneNote XML 与 Markdown 的双向转换层。

| 类 | 职责 |
|---|------|
| `OneNoteXmlConverter` | 转换协调器，整合 HTML 转换器和 Markdown 转换器，支持双向转换和主题应用 |
| `HtmlToOneNoteConverter` | HTML → OneNote XML 转换器，处理标题、段落、列表、表格、引用、代码、图片等元素 |
| `OneNoteToMarkdownConverter` | OneNote XML → Markdown 转换器，解析 OneNote 页面结构并提取样式信息 |
| `OneNoteXmlBuilder` | OneNote XML 构建器，生成页面、大纲、表格、图片等 XML 元素 |
| `OneNoteApiWrapper` | OneNote COM API 封装，提供页面内容读写和层级查询 |
| `OneNotePageUpdater` | 页面更新器，支持替换选中文本、追加内容和替换整页 |
| `OutlineElement` | 大纲元素模型，包含类型、内容、样式和嵌套子元素 |
| `OneNoteStyle` | OneNote 样式模型（record 类型），包含字体、颜色、大小等属性 |

**依赖**: MarkdownEngine, Interop.Microsoft.Office.Interop.OneNote (NuGet)

### RenderingServices

高级渲染服务，负责代码高亮、LaTeX 公式和图表的渲染。

| 类 | 职责 |
|---|------|
| `CodeHighlightService` | 代码语法高亮，内置 17 种语言的词法规则，基于正则分词实现 |
| `LatexRenderService` | LaTeX 渲染服务，通过 WebView2 + KaTeX 将公式渲染为图片或文本 |
| `DiagramRenderService` | 图表渲染服务，通过 WebView2 渲染 Mermaid、流程图、时序图和思维导图 |
| `HtmlTemplateGenerator` | HTML 模板生成器，为各类渲染生成包含 CDN 脚本的 HTML 页面 |
| `WebView2Helper` | WebView2 辅助类，封装控件创建、脚本执行、截图和消息通信 |
| `RenderResult` | 渲染结果模型，包含成功/失败状态、图片数据和渲染耗时 |

**依赖**: MarkdownEngine, Microsoft.Web.WebView2 (NuGet)

### ImportExport

Markdown 文件导入导出和剪贴板操作。

| 类 | 职责 |
|---|------|
| `MarkdownImporter` | 导入器，支持从文件、剪贴板和文本导入 Markdown 并解析 Front Matter |
| `MarkdownExporter` | 导出器，支持导出到文件、剪贴板以及从 OneNote 页面导出 |
| `FrontMatterParser` | YAML Front Matter 解析器，支持解析和序列化元数据 |
| `ImageHandler` | 图片处理器，支持图片提取、Base64/本地文件互转和远程图片下载 |
| `ClipboardHelper` | Windows 剪贴板操作，支持纯文本和 HTML 格式 |

**依赖**: MarkdownEngine, Markdig (NuGet)

### ThemeManager

CSS 主题管理系统。

| 类 | 职责 |
|---|------|
| `ThemeManager` | 主题管理器，从 CSS 文件加载主题，支持主题列表查询和重新加载 |
| `CssVariableParser` | CSS 变量解析器，从 `:root` 块中提取支持的主题变量 |
| `CssStyleMapper` | CSS → OneNote 样式映射器，将 CSS 变量映射为 OneNote 支持的样式属性 |
| `Theme` | 主题数据模型 |
| `OneNoteStyle` | OneNote 样式模型（ThemeManager 层），包含字体、颜色、行高等属性 |
| `BorderStyle` | 边框样式模型 |

**依赖**: 无外部依赖

## 数据流

### 渲染流程

```
用户输入 Markdown → KeyboardHook 捕获按键 → MarkdownRenderHandler
    → OneNoteApiWrapper 获取当前页面内容
    → MarkdownParser 解析为 MarkdownDocument
    → OneNoteXmlConverter 转换为 OneNote XML 元素
    → ThemeManager 应用主题样式
    → OneNotePageUpdater 更新页面内容
```

### 导出流程

```
F8 按键 → ExportHandler
    → OneNoteApiWrapper 获取页面 XML
    → OneNoteToMarkdownConverter 转换为 Markdown
    → ClipboardHelper 复制到剪贴板
```

### 导入流程

```
Ribbon 按钮点击 → ExportHandler
    → MarkdownImporter 从文件读取 Markdown
    → FrontMatterParser 解析元数据
    → ImageHandler 处理图片
    → OneNotePageUpdater 追加内容到页面
```

## 关键设计决策

1. **Markdig 作为解析基础** — 利用 Markdig 成熟的 AST 和管道扩展机制，通过自定义扩展实现数学公式和图表的特殊处理
2. **双样式模型** — `OneNoteConverter.OneNoteStyle` 使用 record 类型支持不可变修改（`with` 表达式），`ThemeManager.OneNoteStyle` 使用 init-only 属性适配主题映射
3. **WebView2 离屏渲染** — 通过隐藏的 WebView2 控件加载 HTML 模板，利用 KaTeX/Mermaid 等 JS 库进行渲染后截图
4. **异步日志** — 使用 `ConcurrentQueue` + `Timer` 实现低开销的异步日志写入，避免阻塞 UI 线程
5. **COM 互操作** — 手动定义 `IDTExtensibility2` 和 `IRibbonExtensibility` 接口，避免依赖 Visual Studio VSTO 工具链
