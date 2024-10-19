; Globals
!define APPNAME "OracleOfDereth"
!define GUID "{D86E2BAB-A1F9-4060-0B98-AB74EB41A58A}"
!define SURROGATE "{71A69713-6593-47EC-0002-0000000DECA1}"
!define DLL "OracleOfDereth.dll"

; Settings
OutFile "OracleOfDereth_v1.0.0.exe"
InstallDir "$PROGRAMFILES\${APPNAME}"

; Pages
page directory
page instfiles

; Installer section
Section
 
SetOutPath $INSTDIR
File "..\bin\net48\${DLL}"
WriteUninstaller $INSTDIR\Uninstall.exe
 
; Registry
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "" "${APPNAME}"
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Assembly" "${DLL}"
WriteRegDWORD HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Enabled" 0x01
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Object" "${APPNAME}.PluginCore"
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Path" "$INSTDIR"
WriteRegStr HKLM "SOFTWARE\Decal\Plugins\${GUID}" "Surrogate" "${SURROGATE}"

SectionEnd


; Uninstaller section
Section "un.Uninstall"
 
delete $INSTDIR\Uninstall.exe
delete "$INSTDIR\${DLL}"
rmDir $INSTDIR
DeleteRegKey HKLM "SOFTWARE\Decal\Plugins\${GUID}"

SectionEnd