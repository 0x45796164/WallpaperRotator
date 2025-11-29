; Wallpaper Rotator Installer Script for Inno Setup

#define MyAppName "Wallpaper Rotator"
;#define MyAppVersion "1.0.0" ; Defined via command line
#define MyAppPublisher "Wallpaper Rotator Project"
#define MyAppExeName "WallpaperRotator.exe"
#define MyAppPublishDir "ReleaseOutput\Standard"

[Setup]
; Basic app information
AppId={{A8F3C9D1-2E4B-4C7A-9F1D-3E5B7C8A9D2F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\{#MyAppName}

DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
OutputDir=ReleaseOutput
OutputBaseFilename=WallpaperRotator-Setup-v{#MyAppVersion}
; Icon with multiple sizes (16x16, 32x32, 48x48, 256x256)
SetupIconFile=icon-light.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

; UI customization (optional images)
;WizardImageFile=installer-banner.bmp
;WizardSmallImageFile=installer-icon.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startuprun"; Description: "Start with Windows"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
; Main application files from publish folder (optimized, single-file build)
Source: "{#MyAppPublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppPublishDir}\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#MyAppPublishDir}\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "icon-light.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "icon-dark.ico"; DestDir: "{app}"; Flags: ignoreversion

; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "TROUBLESHOOTING.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon-light.ico"
Name: "{group}\README"; Filename: "{app}\README.md"
Name: "{group}\Troubleshooting"; Filename: "{app}\TROUBLESHOOTING.md"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\icon-light.ico"

[Registry]
; Add to startup if user selected the task
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "WallpaperRotator"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startuprun

[Run]
; Offer to run application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check for .NET 7.0 Desktop Runtime
function IsDotNet7Installed(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if dotnet CLI is available and .NET 7.0 is installed
  Result := (Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) = True) and (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  ErrorCode: Integer;
begin
  Result := True;
  
  // Check for .NET 7.0 Desktop Runtime
  if not IsDotNet7Installed() then
  begin
    if MsgBox('.NET 7.0 Desktop Runtime is required to run this application.' + #13#10#13#10 +
              'Would you like to download and install it now?' + #13#10#13#10 +
              'The installer will open your browser to the Microsoft download page.', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('', 'https://dotnet.microsoft.com/download/dotnet/7.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
      Result := False; // Cancel installation, user needs to install .NET first
      MsgBox('Please install .NET 7.0 Desktop Runtime and run this installer again.', mbInformation, MB_OK);
    end
    else
    begin
      Result := False; // User chose not to install .NET, cancel installation
    end;
  end;
end;

[UninstallDelete]
; Clean up app data on uninstall (optional - ask user)
Type: filesandordirs; Name: "{%APPDATA}\WallpaperRotator"

[Messages]
WelcomeLabel1=Welcome to [name] Setup
WelcomeLabel2=This will install [name/ver] on your computer.%n%nThis application manages multi-monitor wallpapers with support for images and videos.
FinishedLabel=Setup has finished installing [name] on your computer.%n%nThe application can be launched from the Start Menu or Desktop shortcut.
