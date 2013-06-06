; Installer script file for VPEx software

;--------------------------------

!include "MUI.nsh"
!include "StrFunc.nsh"
!include "FileFunc.nsh"
!include "FileAssociation.nsh"
;!include "fileassoc.nsh"

!define MAINDIR "$PROGRAMFILES\XOware"
!define TAP "tap0901"
!define TAPDRV "${TAP}.sys"
!ifndef SF_SELECTED
!define SF_SELECTED 1
!endif

; my own funcs
Function un.DeleteDirIfEmpty
  FindFirst $R0 $R1 "$0\*.*"
  strcmp $R1 "." 0 NoDelete
   FindNext $R0 $R1
   strcmp $R1 ".." 0 NoDelete
    ClearErrors
    FindNext $R0 $R1
    IfErrors 0 NoDelete
     FindClose $R0
     Sleep 1000
     RMDir "$0"
  NoDelete:
   FindClose $R0
FunctionEnd

; The name of the installer
Name "VPEx"
Caption "XOware VPEx Software"
Icon "vpex.ico"
OutFile "VPExConnectionManager.exe"
ShowInstDetails "show"
ShowUninstDetails "show"

; no compression because it really bogs down the cpu
SetCompress off

; The branding image
;AddBrandingImage left 100
;SetBrandingImage "vpex.jpg"

; The default installation directory
InstallDir "${MAINDIR}\VPEx"
InstallDirRegKey HKLM "Software\XOware\VPEx" "Install_Dir"

; Request application privileges for Windows Vista
; ("admin" access needed if you need to install system-y components
; like DLLs, etc.; otherwise can be "user")
RequestExecutionLevel admin

; Display the license
LicenseText "You must read and agree to the Software License Agreement in order to install this software."
LicenseData "gpl.txt"

;--------------------------------

; Set up what installer pages you want to display

Page license			; read and accept the license to proceed
;Page components		; uncomment to allow user to choose optional
				; components to install (e.g. "Program,"
				; "Libraries," "Sample Configs," etc.)s
Page directory			; allow user to change install directory
Page instfiles			; the actual "meat" of the installer

; Uninstall workflow.  Only needed if you are doing an uninstaller.

UninstPage uninstConfirm	; "Do you really want to do this?" page
UninstPage instfiles		; the actual "meat" of the uninstaller

;--------------------------------

; Install Types
;!ifndef NOINSTTYPES ; only if not defined
  ;InstType "Most"
  ;InstType "Full"
  ;InstType "More"
  ;InstType "Base"
  ;InstType /NOCUSTOM
  ;InstType /COMPONENTSONLYONCUSTOM
;!endif

;AutoCloseWindow false
;ShowInstDetails show

;--------------------------------

; The stuff to install
;
; If you want multiple installable components that the user can enable
; or disable (e.g. "Program files," "Sample config files," "Help files,"
; etc.), then each section gets its own "Section" with the name as how
; you want that section to appear in the "choose stuff to install" screen.
;
; If not, then the section name is not important (it can even be left blank)

Section ""
  ; Install main app
  DetailPrint "Installing VPEx application..."
  SetOutPath $INSTDIR
  File "VPExConnectionManager.exe"
  File "stopvpex.bat"
  File "splash.png"
  File "connect.wav"
  File "disconnect.wav"
  File "openvpn/bin/openvpn.exe"
  File "openvpn/bin/openvpnserv.exe"
  File "libeay32.dll"
  File "libpkcs11-helper-1.dll"
  File "libssl32.dll"
  ; XXX these are candidates for deletion
  File "vpex.ico"

  ; DLLS go in system
  DetailPrint "Installing shared libraries..."
  SetOutPath $SYSDIR
  File "mingwm10.dll"
  File "wxbase28*_gcc_custom.dll"
  File "wxbase28*_net_gcc_custom.dll"
  File "wxbase28*_xml_gcc_custom.dll"
  File "wxmsw28*_adv_gcc_custom.dll"
  File "wxmsw28*_aui_gcc_custom.dll"
  File "wxmsw28*_core_gcc_custom.dll"
  File "wxmsw28*_html_gcc_custom.dll"
  File "wxmsw28*_qa_gcc_custom.dll"
  File "wxmsw28*_richtext_gcc_custom.dll"
  File "wxmsw28*_xrc_gcc_custom.dll"
  #RegDLL "example_dll.dll"

  # Set up TAP driver
  DetailPrint "Installing TAP driver..."

  ; Check if we are running on a 64 bit system.
  System::Call "kernel32::GetCurrentProcess() i .s"
  System::Call "kernel32::IsWow64Process(i s, *i .r0)"
  IntCmp $0 0 tap-32bit

; tap-64bit:
  DetailPrint "We are running on a 64-bit system."
  SetOutPath "$INSTDIR\${TAP}"
  File "driver64/tapinstall.exe"
  File "driver64/OemWin2k.inf"
  File "driver64/${TAPDRV}"
  # Don't try to install TAP driver signature if it does not exist.
  File /nonfatal "driver64/${TAP}.cat"
  goto tapend

tap-32bit:
  DetailPrint "We are running on a 32-bit system."
  SetOutPath "$INSTDIR\${TAP}"
  File "driver32/tapinstall.exe"
  File "driver32/OemWin2k.inf"
  File "driver32/${TAPDRV}"
  # Don't try to install TAP driver signature if it does not exist.
  File /nonfatal "driver32/${TAP}.cat"

  tapend:
  DetailPrint "Executing TAP installer..."
  nsExec::ExecToLog '"$INSTDIR\${TAP}\tapinstall.exe" install "$INSTDIR\${TAP}\OemWin2k.inf" ${TAP}'

  ; register .vpex file
  DetailPrint "Registering .VPEX file type..."
  ${registerExtension} "$INSTDIR\VPExConnectionManager.exe" ".vpex" "VPEx Configuration"
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
  ;!insertmacro APP_ASSOCIATE "vpex" "vpex.vpexfile" "VPEx Configuration" "$INSTDIR\VPExConnectionManager.exe,1" \
  ;  "Open" "$INSTDIR\myapp.exe $\"%1$\""
  ;!insertmacro UPDATEFILEASSOC
 
  ; set up the start menu links
  DetailPrint "Setting up Desktop and Start Menu items..."
  SetOutPath $INSTDIR
  CreateDirectory "$SMPROGRAMS\XOware\VPEx"
  createShortCut "$SMPROGRAMS\XOware\VPEx\VPEx Connection Manager.lnk" "$INSTDIR\VPExConnectionManager.exe" "" "$INSTDIR\VPExConnectionManager.exe" 0 "" "" "Starts and stops the VPEx connection"
  createShortCut "$SMPROGRAMS\XOware\VPEx\Uninstall VPEx.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0 "" "" "Uninstall VPEx Software"
  createShortCut "$DESKTOP\VPEx Connection Manager.lnk" "$INSTDIR\VPExConnectionManager.exe" "" "$INSTDIR\VPExConnectionManager.exe" 0 "" "" "Starts and stops the VPEx connection"

  ; Set up the uninstaller
  DetailPrint "Writing software installation record to the registry..."
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VPEx" "DisplayName" "XOware VPEx"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VPEx" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VPEx" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VPEx" "NoRepair" 1
  ${GetSize} "${MAINDIR}" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\VPEx" "EstimatedSize" "$0"
  WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

;--------------------------------

; If you want an uninstaller, the uninstall commands must appear in their
; own section titled "Uninstall"

Section "Uninstall"
  DetailPrint "Removing registry keys..."
  DeleteRegKey "HKLM" "Software\Microsoft\Windows\CurrentVersion\Uninstall\VPEx"

  DetailPrint "Unregistering .VPEX file type..."
  ${unregisterExtension} ".vpex" "VPEx Configuration"
  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) v (0x08000000, 0, 0, 0)'
  ;!insertmacro APP_UNASSOCIATE "vpex" "vpex.vpexfile"
  ;!insertmacro UPDATEFILEASSOC

  ; uninstall and remove tap
  DetailPrint "Uninstalling and removing TAP driver..."
  nsExec::ExecToLog '"$INSTDIR\${TAP}\tapinstall.exe" remove ${TAP}'
  Delete "$INSTDIR\${TAP}\tapinstall.exe"
  Delete "$INSTDIR\${TAP}\OemWin2k.inf"
  Delete "$INSTDIR\${TAP}\${TAPDRV}"
  Delete "$INSTDIR\${TAP}\${TAP}.cat"

  ; unregister and remove the dlls
  DetailPrint "Removing shared libraries..."
  #UnRegDLL "$SYSDIR\example_dll.dll"
  Delete "$SYSDIR\example_dll.dll"
  Delete "$SYSDIR\mingwm10.dll"
  Delete "$SYSDIR\wxbase28_gcc_custom.dll"
  Delete "$SYSDIR\wxbase28_net_gcc_custom.dll"
  Delete "$SYSDIR\wxbase28_xml_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_adv_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_aui_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_core_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_html_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_qa_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_richtext_gcc_custom.dll"
  Delete "$SYSDIR\wxmsw28_xrc_gcc_custom.dll"

  ; remove the app and it's directory
  DetailPrint "Removing VPEx application..."
  Delete "$INSTDIR\VPExConnectionManager.exe"
  Delete "$INSTDIR\stopvpex.bat"
  Delete "$INSTDIR\splash.png"
  Delete "$INSTDIR\connect.wav"
  Delete "$INSTDIR\disconnect.wav"
  Delete "$INSTDIR\openvpn.exe"
  Delete "$INSTDIR\openvpnserv.exe"
  ; XXX these are candidates for deletion
  Delete "$INSTDIR\vpex.ico"

  ; remove desktop and start menu shortcuts
  Delete "$SMPROGRAMS\XOware\VPEx\VPEx Connection Manager.lnk"
  Delete "$SMPROGRAMS\XOware\VPEx\Uninstall VPEx.lnk"
  Delete "$DESKTOP\VPEx Connection Manager.lnk"

  RMDir /r "$SMPROGRAMS\XOware\VPEx"
  StrCpy $0 "$SMPROGRAMS\XOware"
  Call un.DeleteDirIfEmpty

  StrCpy $0 "$APPDATA\VPExConnectionManager"
  Call un.DeleteDirIfEmpty

  ; remove the uninstaller
  Delete "$INSTDIR\uninstall.exe"

  ; blow away directories if empty
  RMDir /r "$INSTDIR"
  StrCpy $0 "${MAINDIR}"
  Call un.DeleteDirIfEmpty
SectionEnd
