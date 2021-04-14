//+-------------------------------------------------------------------------
//
//  Microsoft Windows
//  Copyright (C) Microsoft Corporation, 1997.
//
//  File:       security.hxx
//
//  Contents:
//
//--------------------------------------------------------------------------

#pragma once

class CSecDescriptor;

BOOL
CheckForAccess(
    IN  CToken *                pToken,
    IN  SECURITY_DESCRIPTOR *   pSD
    );

HANDLE
GetRunAsToken(
    DWORD   clsctx,
    WCHAR   *pwszAppID,
    WCHAR   *pwszRunAsDomainName,
    WCHAR   *pwszRunAsUserName,
    BOOL    fForLaunch
    );

BOOL
DuplicateTokenAsPrimary(
    HANDLE hUserToken,
    PSID psidUserSid,
    HANDLE *hPrimaryToken
    );


BOOL
DuplicateTokenForSessionUse(
    HANDLE hUserToken,
    HANDLE *hDuplicate
    );

PSID
GetUserSid(
    HANDLE hUserToken
    );

HANDLE
GetUserTokenForSession(
    ULONG ulSessionId
    );

CSecDescriptor*
GetDefaultLaunchPermissions();

void
SetDefaultLaunchPermissions(CSecDescriptor* pNewLaunchPerms);

//
// Small class to add refcount semantics around a security descriptor.
//
class CSecDescriptor
{
public:
    
CSecDescriptor(SECURITY_DESCRIPTOR*);
~CSecDescriptor();

void IncRefCount();
void DecRefCount();

SECURITY_DESCRIPTOR* GetSD();

private:

SECURITY_DESCRIPTOR* _pSD;
LONG _lRefs;

};
