; ============================================================
; DACDT 2026 - Inno Setup Installer Script
; ============================================================

#define MyAppName "DACDT 2026"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AML"
#define MyAppExeName "DACDT_2026.exe"
#define MyAppURL ""
#define RepoRoot ".."
#define BuildDir RepoRoot + "\src\DACDT_2026.App\bin\x86\Release"

[Setup]
AppId={{7FF72565-D079-48E1-B124-5D6D4AB5F1B7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#RepoRoot}\artifacts\installer
OutputBaseFilename=DACDT_2026_Setup_v{#MyAppVersion}
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main executable and config
Source: "{#BuildDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion

; All DLLs (both managed and native)
Source: "{#BuildDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; UI web assets (Web SCADA)
Source: "{#RepoRoot}\docs\index.html"; DestDir: "{app}\ui"; Flags: ignoreversion
Source: "{#RepoRoot}\src\DACDT_2026.App\error_codes.js"; DestDir: "{app}\ui"; Flags: ignoreversion
Source: "{#RepoRoot}\src\DACDT_2026.App\error_codes.json"; DestDir: "{app}\ui"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if .NET Framework 4.8 or later is installed
function IsDotNet48Installed(): Boolean;
var
  release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) then
  begin
    // 528040 = .NET Framework 4.8 on Windows 10 May 2019 Update and later
    Result := (release >= 528040);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  // Check .NET 4.8
  if not IsDotNet48Installed() then
  begin
    MsgBox('This application requires .NET Framework 4.8 or later.' + #13#10 +
           'Please install it from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run this installer again.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;
end;

// Check if MX Component COM is registered (ActUtlTypeLib)
procedure CurStepChanged(CurStep: TSetupStep);
var
  classRoot: String;
begin
  if CurStep = ssPostInstall then
  begin
    if not RegKeyExists(HKCR, 'TypeLib\{D217E54E-4A26-4A76-B0AB-57166B90F9AF}') then
    begin
      MsgBox('Note: Mitsubishi MX Component is not detected on this machine.' + #13#10#13#10 +
             'The application requires MX Component (ActUtlType) to communicate with Mitsubishi PLCs.' + #13#10 +
             'PLC connectivity will not work until MX Component is installed.',
             mbInformation, MB_OK);
    end;
  end;
end;
