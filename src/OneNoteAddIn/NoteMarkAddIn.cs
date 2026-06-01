using System.Runtime.InteropServices;
using Extensibility;
using Microsoft.Office.Core;

namespace NoteMark.AddIn;

[ComVisible(true)]
[Guid("B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E")]
[ProgId("NoteMark.AddIn")]
[ClassInterface(ClassInterfaceType.None)]
public class NoteMarkAddIn : IDTExtensibility2, IRibbonExtensibility
{
    static NoteMarkAddIn()
    {
        DiagnosticLog("Static constructor called - .NET Framework 4.8 runtime loaded successfully");
    }

    public NoteMarkAddIn()
    {
        DiagnosticLog("Instance constructor called");
    }

    private static void DiagnosticLog(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteMark", "logs");
            Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n";
            File.AppendAllText(Path.Combine(dir, "startup.log"), line);
        }
        catch
        {
        }
    }

    public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
    {
        DiagnosticLog($"OnConnection called, connectMode={connectMode}");
        DiagnosticLog($"application type = {application?.GetType().Name ?? "null"}");
        DiagnosticLog($"application type full = {application?.GetType().FullName ?? "null"}");
        DiagnosticLog($"application type assembly = {application?.GetType().Assembly.FullName ?? "null"}");
        DiagnosticLog("OnConnection completed (minimal shell)");
    }

    public void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
    {
        DiagnosticLog($"OnDisconnection called, removeMode={removeMode}");
    }

    public void OnAddInsUpdate(ref Array custom)
    {
        DiagnosticLog("OnAddInsUpdate called");
    }

    public void OnStartupComplete(ref Array custom)
    {
        DiagnosticLog("OnStartupComplete called");
    }

    public void OnBeginShutdown(ref Array custom)
    {
        DiagnosticLog("OnBeginShutdown called");
    }

    public string GetCustomUI(string ribbonId)
    {
        DiagnosticLog($"GetCustomUI called, ribbonId={ribbonId}");

        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<customUI xmlns=""http://schemas.microsoft.com/office/2009/07/customui"">
  <ribbon>
    <tabs>
      <tab id=""NoteMarkTab"" label=""NoteMark"">
        <group id=""NoteMarkGroup"" label=""NoteMark"" autoScale=""true"">
          <button id=""btnTest"" label=""Test"" onAction=""OnTest"" />
        </group>
      </tab>
    </tabs>
  </ribbon>
</customUI>";

        DiagnosticLog("GetCustomUI returning minimal ribbon XML");
        return xml;
    }

    public void OnTest(IRibbonControl control)
    {
        DiagnosticLog("OnTest called - button clicked!");
        try
        {
            System.Windows.Forms.MessageBox.Show(
                "NoteMark COM Add-In is working!",
                "NoteMark Test",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            DiagnosticLog($"OnTest MessageBox FAILED: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
