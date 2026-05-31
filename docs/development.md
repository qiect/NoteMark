# 开发指南

## 环境要求

- **操作系统**: Windows 10 1809 或更高版本
- **SDK**: .NET 8.0 SDK
- **IDE**: Visual Studio 2022 (17.9+) 或 JetBrains Rider
- **OneNote**: Microsoft OneNote 2016 / Microsoft 365（用于调试 COM Add-In）
- **WebView2**: Microsoft Edge WebView2 Runtime

## 获取源码

```bash
git clone <repository-url>
cd OneMarkDotNet
```

## 构建项目

```bash
# 还原 NuGet 包
dotnet restore

# Debug 构建
dotnet build

# Release 构建
dotnet build -c Release
```

构建产物位于各项目的 `bin/Debug/net8.0` 或 `bin/Release/net8.0` 目录下。

## 项目结构

```
src/
├── OneNoteAddIn/              # COM Add-In（net8.0-windows）
├── MarkdownEngine/            # Markdown 引擎（net8.0）
├── OneNoteConverter/          # OneNote 转换器（net8.0-windows）
├── RenderingServices/         # 渲染服务（net8.0-windows）
├── ImportExport/              # 导入导出（net8.0）
└── ThemeManager/              # 主题管理（net8.0）
```

其中 `OneNoteAddIn`、`OneNoteConverter`、`RenderingServices` 目标框架为 `net8.0-windows`，因为它们依赖 Windows Forms 或 OneNote COM 互操作。`MarkdownEngine`、`ImportExport`、`ThemeManager` 为纯 .NET 8.0 类库，理论上可跨平台使用。

## NuGet 依赖

| 包 | 版本 | 使用项目 |
|----|------|----------|
| Markdig | 0.38.0 | MarkdownEngine, ImportExport |
| Interop.Microsoft.Office.Interop.OneNote | 1.1.0.2 | OneNoteAddIn, OneNoteConverter |
| Microsoft.Web.WebView2 | 1.0.2903.40 | RenderingServices |

## 编码规范

### 命名约定

- 类名使用 PascalCase：`MarkdownParser`、`CodeHighlightService`
- 方法名使用 PascalCase：`Parse()`、`ConvertToMarkdown()`
- 私有字段使用 camelCase + 下划线前缀：`_pipeline`、`_themeManager`
- 常量使用 PascalCase：`OneNoteNamespace`、`MaxThemeCount`
- 命名空间遵循 `OneMarkDotNet.<Module>` 模式

### 代码风格

- 启用 `Nullable` 引用类型
- 启用 `ImplicitUsings`
- 使用 `LangVersion` latest（C# 12）
- 使用 `file` 作用域命名空间
- 优先使用 `sealed` 类（除非设计为可继承）
- 优先使用 `record` 类型定义不可变数据模型
- 使用 `required` 关键字标记必须初始化的属性
- 使用源生成器正则表达式 `[GeneratedRegex]` 替代运行时编译

### 错误处理

- COM 互操作调用必须包裹在 try-catch 中
- 日志记录使用 `AppLogger.Instance`，不要使用 `Console.WriteLine` 或 `Debug.WriteLine`
- 对外公共 API 使用异常，内部使用 `Result` 模式或返回 null

### 异步编程

- 异步方法返回 `Task` 或 `ValueTask`
- 使用 `await using` 处理 `IAsyncDisposable` 资源
- 避免在 COM Add-In 事件处理中使用 `async void`，使用 `GetAwaiter().GetResult()` 同步等待

## 调试 COM Add-In

1. 构建 Debug 版本
2. 使用 `regsvr32` 注册 COM Add-In DLL：
   ```cmd
   regsvr32 bin\Debug\net8.0\OneNoteAddIn.comhost.dll
   ```
3. 在 Visual Studio 中配置调试：
   - 启动外部程序：OneNote.exe 路径
   - 附加到进程
4. 在代码中设置断点进行调试

**注意**: 修改代码后需要先注销再重新注册 COM DLL：
```cmd
regsvr32 /u bin\Debug\net8.0\OneNoteAddIn.comhost.dll
```

## 日志

运行时日志位于 `%APPDATA%\OneMarkDotNet\logs\` 目录，文件名格式为 `onemark_yyyyMMdd.log`。

日志自动清理超过 30 天的旧文件。

## 添加新的 Markdown 扩展

1. 在 `MarkdownEngine/MarkdownExtension.cs` 中创建新的 `IMarkdownExtension` 实现
2. 在 `OneMarkExtension` 类的 `_extensions` 数组中注册新扩展
3. 如需新的元素类型，在 `Elements/` 目录下添加
4. 在 `MarkdownDocument.ConvertBlock` / `ConvertInline` 中添加转换逻辑
5. 在 `HtmlMarkdownRenderer` 和 `OneNoteXmlRenderer` 中添加渲染逻辑

## 添加新的代码语言高亮

1. 在 `RenderingServices/CodeHighlightService.cs` 的 `SupportedLanguages` 数组中添加语言名称
2. 如有别名，在 `LanguageAliases` 字典中添加映射
3. 在 `InitializeRuleSets()` 方法中添加该语言的 `LanguageRuleSet`，定义关键词、字符串、注释等正则规则

## 添加新的主题变量

1. 在 `ThemeManager/CssVariableParser.cs` 的 `SupportedVariables` 数组中添加变量名
2. 在 `ThemeManager/CssStyleMapper.cs` 的 `MapCssToOneNoteStyle` 方法中添加映射逻辑
3. 在 `OneNoteConverter/OneNoteXmlBuilder.cs` 中使用新变量控制渲染输出
