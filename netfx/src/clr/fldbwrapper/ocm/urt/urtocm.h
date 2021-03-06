// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/////////////////////////////////////////////////////////////////////////////
// Module Name: urtocm.h
//
// Abstract:
//    class declarations for setup object
//
// Author: JoeA
//
// Notes:
//

#if !defined( CURTOCMSETUP_H )
#define CURTOCMSETUP_H

#include "globals.h"
#include "infhelpers.h"

extern BOOL g_bIsAdmin;
extern BOOL g_bInstallOK;
extern BOOL g_bInstallComponent;


const DWORD UNRECOGNIZED = 0;
const DWORD DEFAULT_RETURN = 0;

//for use in ChangeSelectionState calls
const UINT  NOT_SELECTED = 0;
const UINT  SELECTED     = 1;

//for communication with eHome and TabletPC
//
#define NOTIFY_NDPINSTALL               OC_PRIVATE_BASE + 2
const UINT NDP_INSTALL   = 1;
const UINT NDP_NOINSTALL = 0;



class CUrtOcmSetup
{
public:
    CUrtOcmSetup();

    DWORD OcmSetupProc( LPCTSTR ComponentId,
                        LPCTSTR SubcomponentId,
                        UINT    Function,
                        UINT    Param1,
                        PVOID   Param2 );

private:
    //handler methods
    //
    DWORD OnPreInitialize( UINT uiCharWidth );
    DWORD InitializeComponent( PSETUP_INIT_COMPONENT pSetupInitComponent );
    DWORD OnSetLanguage( UINT uiLangID );
    DWORD OnQueryImage( UINT uiImage, DWORD dwImageSize);
    DWORD OnRequestPages( PSETUP_REQUEST_PAGES prpPages );
    DWORD OnQuerySkipPage( OcManagerPage ocmpPage );
    DWORD OnQueryChangeSelectionState( UINT uiNewState, PVOID pvFlags, LPCTSTR szComp );
    DWORD OnCalculateDiskSpace( UINT uiAdd, HDSKSPC hdSpace, LPCTSTR szComp );
    DWORD OnQueueFileOperations( LPCTSTR szComp, HSPFILEQ pvHFile );
    DWORD OnNeedMedia( VOID );
    DWORD OnQueryStepCount( LPCTSTR szSubCompId );
    DWORD OnAboutToCommitQueue( VOID );
    DWORD OnCompleteInstallation( LPCTSTR szComp );
    DWORD OnCleanup( VOID );
    DWORD OnNotificationFromQueue( VOID );
    DWORD OnFileBusy( VOID );
    DWORD OnQueryError( VOID );
    DWORD OnPrivateBase( VOID );
    DWORD OnQueryState( UINT uiState );
    DWORD OnWizardCreated( VOID );
    DWORD OnExtraRoutines( VOID );
    DWORD OnNdpInstall( LPCTSTR szSubcomponentId, UINT uiParam1, PVOID pvParam2 );

    //helper methods
    //
    BOOL StateChanged( LPCTSTR szCompName, BOOL* pfChanged );
    VOID RegTypeLibrary( const WCHAR* wzFilename, const WCHAR* wzHelpDir );
    VOID SetVariableDirs( VOID );
    
    // update HKLM,software\microsoft\windows\currentversion\sharedlls
    // registry values, for all files that we copy
    VOID UpdateSharedDllsRegistryValues(LPCTSTR szInstallSection);
    VOID UpdateRegistryValue( HKEY &hKey, const WCHAR* szFullFileName );

    VOID GetAndRegisterTypeLibs( const WCHAR* szTLibSection, BOOL fInstall );
    VOID GetAndRunCustomActions( const WCHAR* szSection, BOOL fInstall );

    // delete files from the [temp_files_delete] section
    VOID DeleteTempFiles( VOID );

    // write a string to the logFile (m_csLogFileName) with the date and time stamps
    VOID LogInfo( LPCTSTR szInfo );

    // CreateProcess and execute the CA
    // implementation is in QuetExec.cpp
    UINT QuietExec( const WCHAR* const szInstallArg );

    // Bind files from the [BindImage_files] section
    VOID BindImageFiles( const WCHAR* szSection );

    //Parse input args
    // expecting something like
    // "exe-file and arguments, unused, path to add as temp env. var"
    // parameters:
    // [in/out] pszString: will contain everything before first comma
    // [out] pPath:        will contain everything after last comma
    VOID ParseArgument( WCHAR *pszString, WCHAR*& pPath );

    // breaks pszString to applicationName (exe-file) and command-line (exefile and arguments)
    // encloses exe-name in quotes (for commandLine only), if it is not quoted already 
    // removes quotes from applicationName if exe-name was quoted
    // returns false if caString is in wrong format (contains one quote only, has no exe-name, etc)
    // Parameters:
    //          [in] pszString - string containing exe-name and arguments
    //                           "my.exe" arg1, arg2
    //                            
    //          [out] pszApplicationName - will contain exe-name
    //          [out] pszCommandLine - same as caString with exe-name qouted
    
    // for example if pszString = "my.exe" arg1 arg2 (OR pszString = my.exe arg1 arg2)
    // then 
    //       pszApplicationName = my.exe 
    //       pszCommandLine = "my.exe" arg1 arg2
    BOOL GetApplicationName( const WCHAR* pszString, 
                             WCHAR* pszApplicationName, 
                             WCHAR* pszCommandLine );

    // helper function:
    // breaks command-line to applicationName and arguments 
    // for path that begins with quote (pszString = "my.exe" arg1 arg2)
    BOOL GetApplicationNameFromQuotedString( const WCHAR* pszString, 
                                             WCHAR* pszApplicationName, 
                                             WCHAR* pszCommandLine );
    // helper function:
    // breaks command-line to applicationName and arguments 
    // for path that does NOT begin with quote (pszString = my.exe arg1 arg2)
    BOOL GetApplicationNameFromNonQuotedString( const WCHAR* pszString, 
                                                WCHAR* pszApplicationName, 
                                                WCHAR* pszCommandLine );
    // helper function:
    BOOL IsEverettInstalled();

    // helper function:
    // return TRUE if last 4 characters before pBlank are ".exe"
    // return FALSE otherwise
    BOOL IsExeExtention(const WCHAR* pszString, WCHAR *pBlank);

    // helper function:
    // Fix for DCR BUG#563183
    // Copies .config.orig file to .config files at the install location if it does not exist
    // Deletes the .config.orig file.

    VOID ProcessConfigFiles();


    //data
    //
    WORD m_wLang;

    SETUP_INIT_COMPONENT m_InitComponent;

    WCHAR m_csLogFileName[MAX_PATH+1];


}; //class CUrtOcmSetup




#endif  //CURTOCMSETUP_H