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
#define VER_INTERNALNAME_STR	"JITMGR.EXE"
#define VER_FILEDESCRIPTION_STR "Microsoft COM Runtime JIT Compiler Manager\0"
#define VER_ORIGFILENAME_STR    "jitmgr.exe\0"
