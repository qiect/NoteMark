#define AppName "OneMarkDotNet"
#define AppVersion "1.0.0"
#define AppPublisher "OneMarkDotNet"
#define AppURL "https://github.com/onemarkdotnet"
#define AppExeName "ONENOTE.EXE"
#define ProgId "OneMarkDotNet.AddIn"
#define Clsid "{B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}"

[Setup]
AppId={{B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=output
OutputBaseFilename=OneMarkDotNet-{#AppVersion}-setup
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayName={#AppName}
CloseApplications=force
CloseApplicationsFilter=*.exe

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Office\OneNote\AddIns\{#ProgId}"; ValueType: string; ValueName: ""; ValueData: "{#AppName} - OneNote Markdown Plugin"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Office\OneNote\AddIns\{#ProgId}"; ValueType: dword; ValueName: "LoadBehavior"; ValueData: "3"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Office\OneNote\AddIns\{#ProgId}"; ValueType: string; ValueName: "FriendlyName"; ValueData: "{#AppName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Office\OneNote\AddIns\{#ProgId}"; ValueType: string; ValueName: "Description"; ValueData: "Markdown rendering and export plugin for OneNote"; Flags: uninsdeletekey

[Run]
Filename: "regsvr32.exe"; Parameters: "/s ""{app}\OneNoteAddIn.comhost.dll"""; StatusMsg: "Registering COM add-in..."; Flags: runhidden 32bit

[UninstallRun]
Filename: "regsvr32.exe"; Parameters: "/u /s ""{app}\OneNoteAddIn.comhost.dll"""; Flags: runhidden 32bit

[Code]
function InitializeSetup: Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
end;

function InitializeUninstall: Boolean;
begin
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{userappdata}\OneMarkDotNet');
    if DirExists(DataDir) then
    begin
      if MsgBox('Do you want to remove all OneMarkDotNet settings and logs?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
