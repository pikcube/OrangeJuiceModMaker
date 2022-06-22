#define public Dependency_NoExampleSetup
#define public UseDotNet60Desktop
#define public UseNetCoreCheck

#include "C:\Users\Michael\source\repos\OrangeJuiceModMaker\OrangeJuiceModMaker\CodeDependencies.iss"

; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "OrangeJuiceModMaker"
#define MyAppVersion "0.8"
#define MyAppPublisher "Pikcube"
#define MyAppURL "https://github.com/pikcube/OrangeJuiceModMaker"
#define MyAppExeName "OrangeJuiceModMaker.exe"
#define MyBasePath = "C:\Users\Michael\source\repos\OrangeJuiceModMaker\OrangeJuiceModMaker\bin\x64\Release\net6.0-windows"
; #define MyCompression "lzma2/max"
; Uncomment to compile in Inno

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{8D226F23-7CED-44AD-9709-B92066518BD9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
; PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=C:\Users\Michael\Desktop\OJModMaker
OutputBaseFilename=OJSetup
;Compression=none
Compression={#MyCompression}
SolidCompression=yes
WizardStyle=modern

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddDotNet60Desktop;

  Result := True;
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\Michael\source\repos\OrangeJuiceModMaker\OrangeJuiceModMaker\netcorecheck.exe"; Flags: dontcopy noencryption
Source: "C:\Users\Michael\source\repos\OrangeJuiceModMaker\OrangeJuiceModMaker\netcorecheck_x64.exe"; Flags: dontcopy noencryption

Source: "{#MyBasePath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\Magick.NET.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\Magick.NET-Q16-AnyCPU.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\OrangeJuiceModMaker.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\OrangeJuiceModMaker.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\OrangeJuiceModMaker.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\OrangeJuiceModMaker.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\ffme.win.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\FFMpeg.AutoGen.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\FFmpeg.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBasePath}\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs

Source: "{#MyBasePath}\oj.version"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
Source: "{#MyBasePath}\7za.dll"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
Source: "{#MyBasePath}\7za.exe"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
Source: "{#MyBasePath}\HyperLookupTable.csv"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
Source: "{#MyBasePath}\FlavorLookUp.csv"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
Source: "{#MyBasePath}\ffmpeg.7z"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
Source: "{#MyBasePath}\csvFiles.7z"; DestDir: "{app}\OrangeJuiceModMaker"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

