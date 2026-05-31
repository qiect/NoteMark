using System.Runtime.InteropServices;

namespace Microsoft.Office.Core;

[ComImport]
[Guid("000C0396-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IRibbonExtensibility
{
    [DispId(1)]
    string GetCustomUI(string ribbonId);
}

[ComImport]
[Guid("000C03A5-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IRibbonControl
{
    [DispId(1)]
    string Id { get; }

    [DispId(2)]
    object Context { get; }

    [DispId(3)]
    string Tag { get; }
}

[ComImport]
[Guid("C3AF8585-252C-4E3C-9B1A-30B5F2E8E0FA")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IRibbonUI
{
    [DispId(1)]
    void Invalidate();

    [DispId(2)]
    void InvalidateControl(string controlId);
}
