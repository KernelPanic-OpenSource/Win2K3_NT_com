// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
#ifdef _DEBUG
#define VER_FILEFLAGS		VS_FF_DEBUG
#else
#define VER_FILEFLAGS		VS_FF_SPECIALBUILD
#endif

#define VER_FILETYPE		VFT_DLL
#define VER_INTERNALNAME_STR    "smctest.exe"
#define VER_FILEDESCRIPTION_STR "Common Language Runtime sample compiler\0"
#define VER_ORIGFILENAME_STR    "smctest.exe\0"
