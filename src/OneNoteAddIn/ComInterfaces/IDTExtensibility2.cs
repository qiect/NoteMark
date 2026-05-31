using System.Runtime.InteropServices;

namespace Extensibility;

[ComImport]
[Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IDTExtensibility2
{
    [DispId(1)]
    void OnConnection(
        [In, MarshalAs(UnmanagedType.IDispatch)] object application,
        [In] ext_ConnectMode connectMode,
        [In, MarshalAs(UnmanagedType.IDispatch)] object addInInst,
        [In] ref Array custom);

    [DispId(2)]
    void OnDisconnection(
        [In] ext_DisconnectMode removeMode,
        [In] ref Array custom);

    [DispId(3)]
    void OnAddInsUpdate(
        [In] ref Array custom);

    [DispId(4)]
    void OnStartupComplete(
        [In] ref Array custom);

    [DispId(5)]
    void OnBeginShutdown(
        [In] ref Array custom);
}

public enum ext_ConnectMode
{
    ext_cm_AfterStartup = 0,
    ext_cm_Startup = 1,
    ext_cm_External = 2,
    ext_cm_CommandLine = 3,
    ext_cm_Solution = 4,
    ext_cm_UISetup = 5
}

public enum ext_DisconnectMode
{
    ext_dm_HostShutdown = 0,
    ext_dm_UserClosed = 1,
    ext_dm_UISetupComplete = 2,
    ext_dm_SolutionClosed = 3
}
