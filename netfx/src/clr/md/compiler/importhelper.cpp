// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//*****************************************************************************
// MDUtil.cpp
//
// contains utility code to MD directory
//
//*****************************************************************************
#include "stdafx.h"
#include "ImportHelper.h"
#include <MdUtil.h>
#include "RWUtil.h"
#include "MDLog.h"
#include "StrongName.h"

#define COM_RUNTIME_LIBRARY "ComRuntimeLibrary"

//*******************************************************************************
// Find a Method given a parent, name and signature.
//*******************************************************************************
HRESULT ImportHelper::FindMethod(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdTypeDef   td,                     // [IN] parent.
    LPCUTF8     szName,                 // [IN] MethodDef name.
    const COR_SIGNATURE *pSig,          // [IN] Signature.
    ULONG       cbSig,                  // [IN] Size of signature.
    mdMethodDef *pmb,                   // [OUT] Put the MethodDef token here.
    RID         rid /* = 0 */)          // [IN] Optional rid to be ignored.
{
    HRESULT     hr = S_OK;              // A result.
    ULONG       ridStart;               // Start of td's methods.
    ULONG       ridEnd;                 // End of td's methods.
    ULONG       index;                  // Loop control.
    TypeDefRec  *pRec;                  // A TypeDef Record.
    MethodRec   *pMethod;               // A MethodDef Record.
    LPCUTF8     szNameUtf8Tmp;          // A found MethodDef's name.
    PCCOR_SIGNATURE pSigTmp;            // A found MethodDef's signature.
    ULONG       cbSigTmp;               // Size of a found MethodDef's signature.
    PCCOR_SIGNATURE pvSigTemp = pSig;   // For use in parsing a signature.
    CQuickBytes qbSig;                  // Struct to build a non-varargs signature.
    int         rtn;

    if (cbSig)
    {   // check to see if this is a vararg signature
        if ( isCallConv(CorSigUncompressCallingConv(pvSigTemp), IMAGE_CEE_CS_CALLCONV_VARARG) )
        {   // Get the fix part of VARARG signature
            IfFailGo( _GetFixedSigOfVarArg(pSig, cbSig, &qbSig, &cbSig) );
            pSig = (PCCOR_SIGNATURE) qbSig.Ptr();
        }
    }

    *pmb = TokenFromRid(rid, mdtMethodDef); // to know what to ignore
    rtn = pMiniMd->FindMemberDefFromHash(td, szName, pSig, cbSig, pmb);
    if (rtn == CMiniMdRW::Found)
        goto ErrExit;
    else if (rtn == CMiniMdRW::NotFound)
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    _ASSERTE(rtn == CMiniMdRW::NoTable);

    *pmb = mdMethodDefNil;

    // get the range of method rids given a typedef
    pRec = pMiniMd->getTypeDef(RidFromToken(td));
    ridStart = pMiniMd->getMethodListOfTypeDef(pRec);
    ridEnd = pMiniMd->getEndMethodListOfTypeDef(pRec);
    // Iterate over the methods.
    for (index = ridStart; index < ridEnd; index ++ )
    {
		RID	methodRID = pMiniMd->GetMethodRid(index);
        // For the call from Validator ignore the rid passed in.
        if (methodRID != rid)
		{
			// Get the method and its name.
			pMethod = pMiniMd->getMethod( methodRID );
			szNameUtf8Tmp = pMiniMd->getNameOfMethod(pMethod);

			// If name matches what was requested...
			if ( strcmp(szNameUtf8Tmp, szName) == 0 )
			{
                if (cbSig && pSig)
				{
					pSigTmp = pMiniMd->getSignatureOfMethod(pMethod, &cbSigTmp);
					if (cbSigTmp != cbSig || memcmp(pSig, pSigTmp, cbSig))
                        continue;
                }
                // Ignore PrivateScope methods.
                if (IsMdPrivateScope(pMiniMd->getFlagsOfMethod(pMethod)))
                    continue;
                // Found method.
						*pmb = TokenFromRid(methodRID, mdtMethodDef);
						goto ErrExit;
					}
				}
			}

    // record not found
    *pmb = mdMethodDefNil;
    hr = CLDB_E_RECORD_NOTFOUND;

ErrExit:
    return hr;
} // HRESULT ImportHelper::FindMethod()

//*******************************************************************************
// Find a Field given a parent, name and signature.
//*******************************************************************************
HRESULT ImportHelper::FindField(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdTypeDef   td,                     // [IN] parent.
    LPCUTF8     szName,                 // [IN] FieldDef name.
    const COR_SIGNATURE *pSig,          // [IN] Signature.
    ULONG       cbSig,                  // [IN] Size of signature.
    mdFieldDef  *pfd,                   // [OUT] Put the FieldDef token here.
    RID         rid /* = 0 */)          // [IN] Optional rid to be ignored.
{
    HRESULT     hr = S_OK;              // A result.
    ULONG       ridStart;               // Start of td's methods.
    ULONG       ridEnd;                 // End of td's methods.
    ULONG       index;                  // Loop control.
    TypeDefRec  *pRec;                  // A TypeDef Record.
    FieldRec    *pField;                // A FieldDef Record.
    LPCUTF8     szNameUtf8Tmp;          // A found FieldDef's name.
    PCCOR_SIGNATURE pSigTmp;            // A found FieldDef's signature.
    ULONG       cbSigTmp;               // Size of a found FieldDef's signature.
    int         rtn;

    *pfd = TokenFromRid(rid,mdtFieldDef); // to know what to ignore
    rtn = pMiniMd->FindMemberDefFromHash(td, szName, pSig, cbSig, pfd);
    if (rtn == CMiniMdRW::Found)
        goto ErrExit;
    else if (rtn == CMiniMdRW::NotFound)
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    _ASSERTE(rtn == CMiniMdRW::NoTable);

    *pfd = mdFieldDefNil;

    // get the range of method rids given a typedef
    pRec = pMiniMd->getTypeDef(RidFromToken(td));
    ridStart = pMiniMd->getFieldListOfTypeDef(pRec);
    ridEnd = pMiniMd->getEndFieldListOfTypeDef(pRec);

    // Iterate over the methods.
    for (index = ridStart; index < ridEnd; index ++ )
    {
		RID fieldRID = pMiniMd->GetFieldRid(index);
        // For the call from Validator ignore the rid passed in.
        if (fieldRID != rid)
		{
			// Get the field and its name.
			pField = pMiniMd->getField( fieldRID );
			szNameUtf8Tmp = pMiniMd->getNameOfField(pField);

			// If name matches what was requested...
			if ( strcmp(szNameUtf8Tmp, szName) == 0 )
			{
                // Check signature if specified.
                if (cbSig && pSig)
				{
					pSigTmp = pMiniMd->getSignatureOfField(pField, &cbSigTmp);
					if (cbSigTmp != cbSig || memcmp(pSig, pSigTmp, cbSig))
                        continue;
                }
                // Ignore PrivateScope fields.
                if (IsFdPrivateScope(pMiniMd->getFlagsOfField(pField)))
                    continue;
                // Field found.
						*pfd = TokenFromRid(fieldRID, mdtFieldDef);
						goto ErrExit;
					}
				}
			}

    // record not found
    *pfd = mdFieldDefNil;
    hr = CLDB_E_RECORD_NOTFOUND;

ErrExit:
    return hr;
} // HRESULT ImportHelper::FindField()

//*******************************************************************************
// Find a Member given a parent, name and signature.
//*******************************************************************************
HRESULT ImportHelper::FindMember(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    mdTypeDef   td,                         // [IN] parent.
    LPCUTF8     szName,                     // [IN] Member name.
    const COR_SIGNATURE *pSig,              // [IN] Signature.
    ULONG       cbSig,                      // [IN] Size of signature.
    mdToken     *ptk)                       // [OUT] Put the token here.
{
    HRESULT     hr;                         // A result.

    // determine if it is ref to MethodDef or FieldDef
    if ((pSig[0] & IMAGE_CEE_CS_CALLCONV_MASK) != IMAGE_CEE_CS_CALLCONV_FIELD)
        hr = FindMethod(pMiniMd, td, szName, pSig, cbSig, ptk);
    else 
        hr = FindField(pMiniMd, td, szName, pSig, cbSig, ptk);

    if (hr == CLDB_E_RECORD_NOTFOUND)
        *ptk = mdTokenNil;

    return hr;
} // HRESULT ImportHelper::FindMember()


//*******************************************************************************
// Find the memberref given name, sig, and parent
//*******************************************************************************
HRESULT ImportHelper::FindMemberRef(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    mdToken     tkParent,                   // [IN] the parent token
    LPCUTF8     szName,                     // [IN] memberref name
    const COR_SIGNATURE *pbSig,             // [IN] Signature.
    ULONG       cbSig,                      // [IN] Size of signature.
    mdMemberRef *pmr,                       // [OUT] Put the MemberRef token found
    RID         rid /* = 0*/)               // [IN] Optional rid to be ignored.
{
    ULONG       cMemberRefRecs;
    MemberRefRec    *pMemberRefRec;
    LPCUTF8     szNameTmp = 0;
    const COR_SIGNATURE *pbSigTmp;          // Signature.
    ULONG       cbSigTmp;                   // Size of signature.
    mdToken     tkParentTmp;                // the parent token
    HRESULT     hr = NOERROR;
    int         rtn;
    ULONG       i;

    _ASSERTE(szName &&  pmr);

    *pmr = TokenFromRid(rid,mdtMemberRef); // to know what to ignore
    rtn = pMiniMd->FindMemberRefFromHash(tkParent, szName, pbSig, cbSig, pmr);
    if (rtn == CMiniMdRW::Found)
        goto ErrExit;
    else if (rtn == CMiniMdRW::NotFound)
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }
    _ASSERTE(rtn == CMiniMdRW::NoTable);

    *pmr = mdMemberRefNil;

    cMemberRefRecs = pMiniMd->getCountMemberRefs();

    // Search for the MemberRef
    for (i = 1; i <= cMemberRefRecs; i++)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pMemberRefRec = pMiniMd->getMemberRef(i);
        if ( !IsNilToken(tkParent) )
        {
            // given a valid parent
            tkParentTmp = pMiniMd->getClassOfMemberRef(pMemberRefRec);
            if (tkParentTmp != tkParent)
            {
                // if parent is specified and not equal to the current row,
                // try the next row.
                //
                continue;
            }
        }
        if ( szName && *szName )
        {
            // name is specified
            szNameTmp = pMiniMd->getNameOfMemberRef(pMemberRefRec);
            if ( strcmp(szName, szNameTmp) != 0 )
            {
                // Name is not equal. Try next row.
                continue;
            }
        }
        if ( cbSig && pbSig )
        {
            // signature is specifed
            pbSigTmp = pMiniMd->getSignatureOfMemberRef(pMemberRefRec, &cbSigTmp);
            if (cbSigTmp != cbSig)
                continue;
            if (memcmp( pbSig, pbSigTmp, cbSig ) != 0)
                continue;
        }

        // we found a match
        *pmr = TokenFromRid(i, mdtMemberRef);
        return S_OK;
    }
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
} // HRESULT ImportHelper::FindMemberRef()



//*******************************************************************************
// Find duplicate StandAloneSig
//*******************************************************************************
HRESULT ImportHelper::FindStandAloneSig(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    const COR_SIGNATURE *pbSig,             // [IN] Signature.
    ULONG       cbSig,                      // [IN] Size of signature.
    mdSignature *psa)                       // [OUT] Put the StandAloneSig token found
{
    ULONG       cRecs;
    StandAloneSigRec    *pRec;
    const COR_SIGNATURE *pbSigTmp;          // Signature.
    ULONG       cbSigTmp;                   // Size of signature.


    _ASSERTE(cbSig &&  psa);
    *psa = mdSignatureNil;

    cRecs = pMiniMd->getCountStandAloneSigs();

    // Search for the StandAloneSignature
    for (ULONG i = 1; i <= cRecs; i++)
    {
        pRec = pMiniMd->getStandAloneSig(i);
        pbSigTmp = pMiniMd->getSignatureOfStandAloneSig(pRec, &cbSigTmp);
        if (cbSigTmp != cbSig)
            continue;
        if (memcmp( pbSig, pbSigTmp, cbSig ) != 0)
            continue;

        // we found a match
        *psa = TokenFromRid(i, mdtSignature);
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindStandAloneSig()

//*******************************************************************************
// Find duplicate TypeSpec
//*******************************************************************************
HRESULT ImportHelper::FindTypeSpec(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    const COR_SIGNATURE *pbSig,             // [IN] Signature.
    ULONG       cbSig,                      // [IN] Size of signature.
    mdTypeSpec  *ptypespec)                 // [OUT] Put the TypeSpec token found
{
    ULONG       cRecs;
    TypeSpecRec *pRec;
    const COR_SIGNATURE *pbSigTmp;          // Signature.
    ULONG       cbSigTmp;                   // Size of signature.


    _ASSERTE(cbSig &&  ptypespec);
    *ptypespec = mdSignatureNil;

    cRecs = pMiniMd->getCountTypeSpecs();

    // Search for the TypeSpec
    for (ULONG i = 1; i <= cRecs; i++)
    {
        pRec = pMiniMd->getTypeSpec(i);
        pbSigTmp = pMiniMd->getSignatureOfTypeSpec(pRec, &cbSigTmp);
        if (cbSigTmp != cbSig)
            continue;
        if (memcmp( pbSig, pbSigTmp, cbSig ) != 0)
            continue;

        // we found a match
        *ptypespec = TokenFromRid(i, mdtTypeSpec);
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindTypeSpec()


//*******************************************************************************
// Find the MethodImpl
//*******************************************************************************
HRESULT ImportHelper::FindMethodImpl(
    CMiniMdRW   *pMiniMd,                   // [IN] The MiniMd to lookup.
    mdTypeDef   tkClass,                    // [IN] The parent TypeDef token.
    mdMethodDef tkBody,                     // [IN] Method body token.
    mdMethodDef tkDecl,                     // [IN] Method declaration token.
    RID         *pRid)                      // [OUT] Put the MethodImpl rid here
{
    MethodImplRec *pMethodImplRec;          // MethodImpl record.
    ULONG       cMethodImplRecs;            // Count of MethodImpl records.
    mdTypeDef   tkClassTmp;                 // Parent TypeDef token.
    mdToken     tkBodyTmp;                  // Method body token.
    mdToken     tkDeclTmp;                  // Method declaration token.

    _ASSERTE(TypeFromToken(tkClass) == mdtTypeDef);
    _ASSERTE(TypeFromToken(tkBody) == mdtMemberRef || TypeFromToken(tkBody) == mdtMethodDef);
    _ASSERTE(TypeFromToken(tkDecl) == mdtMemberRef || TypeFromToken(tkDecl) == mdtMethodDef);
    _ASSERTE(!IsNilToken(tkClass) && !IsNilToken(tkBody) && !IsNilToken(tkDecl));
    
    if (pRid)
        *pRid = 0;

    cMethodImplRecs = pMiniMd->getCountMethodImpls();

    // Search for the MethodImpl.
    for (ULONG i = 1; i <= cMethodImplRecs; i++)
    {
        pMethodImplRec = pMiniMd->getMethodImpl(i);

        // match the parent column
        tkClassTmp = pMiniMd->getClassOfMethodImpl(pMethodImplRec);
        if (tkClassTmp != tkClass)
            continue;

        // match the method body column
        tkBodyTmp = pMiniMd->getMethodBodyOfMethodImpl(pMethodImplRec);
        if (tkBodyTmp != tkBody)
            continue;

        // match the method declaration column
        tkDeclTmp = pMiniMd->getMethodDeclarationOfMethodImpl(pMethodImplRec);
        if (tkDeclTmp != tkDecl)
            continue;

        // we found a match
        if (pRid)
            *pRid = i;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindMethodImpl()


//*******************************************************************************
// Find the TypeRef given the full qualified name.
//*******************************************************************************
HRESULT ImportHelper::FindTypeRefByName(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdToken     tkResolutionScope,      // [IN] Resolution scope for the TypeRef.
    LPCUTF8     szNamespace,            // [IN] TypeRef Namespace.
    LPCUTF8     szName,                 // [IN] TypeRef Name.
    mdTypeRef   *ptk,                   // [OUT] Put the TypeRef token here.
    RID         rid /* = 0*/)           // [IN] Optional rid to be ignored.
{
    HRESULT     hr=S_OK;                // A result.
    ULONG       cTypeRefRecs;           // Count of TypeRefs to scan.
    TypeRefRec  *pTypeRefRec;           // A TypeRef record.
    LPCUTF8     szNameTmp;              // A TypeRef's Name.
    LPCUTF8     szNamespaceTmp;         // A TypeRef's Namespace.
    mdToken     tkResTmp;               // TypeRef's resolution scope.
    ULONG       i;                      // Loop control.

    _ASSERTE(szName &&  ptk);
    *ptk = mdTypeRefNil;

    // Treat no namespace as empty string.
    if (!szNamespace)
        szNamespace = "";

    if (pMiniMd->m_pNamedItemHash)
    {
        // If hash is build, go through the hash table
        TOKENHASHENTRY *p;              // Hash entry from chain.
        ULONG       iHash;              // Item's hash value.
        int         pos;                // Position in hash chain.

        // Hash the data.
        iHash = pMiniMd->HashNamedItem(0, szName);

        // Go through every entry in the hash chain looking for ours.
        for (p = pMiniMd->m_pNamedItemHash->FindFirst(iHash, pos);
             p;
             p = pMiniMd->m_pNamedItemHash->FindNext(pos))
        {   

            // name hash can hold more than one kind of token
            if (TypeFromToken(p->tok) != (ULONG)mdtTypeRef)
            {
                continue;
            }

            // skip this one if asked
            if (RidFromToken(p->tok) == rid)
                continue;

            pTypeRefRec = pMiniMd->getTypeRef(RidFromToken(p->tok));
            szNamespaceTmp = pMiniMd->getNamespaceOfTypeRef(pTypeRefRec);
            szNameTmp = pMiniMd->getNameOfTypeRef(pTypeRefRec);
            if (strcmp(szName, szNameTmp) || strcmp(szNamespace, szNamespaceTmp))
            {
                // if the name space is not equal, then check the next one.
                continue;
            }
            tkResTmp = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);

            if (tkResTmp == tkResolutionScope ||
                (IsNilToken(tkResTmp) && IsNilToken(tkResolutionScope)))
            {
                // we found a match
                *ptk = p->tok;
                return S_OK;
            }
        }
        hr = CLDB_E_RECORD_NOTFOUND;
    } 
    else
    {
        cTypeRefRecs = pMiniMd->getCountTypeRefs();

        // Search for the TypeRef.
        for (i = 1; i <= cTypeRefRecs; i++)
        {
            // For the call from Validator ignore the rid passed in.
            if (i == rid)
                continue;

            pTypeRefRec = pMiniMd->getTypeRef(i);

            // See if the Resolution scopes match.
            tkResTmp = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
            if (IsNilToken(tkResTmp))
            {
                if (!IsNilToken(tkResolutionScope))
                    continue;
            }
            else if (tkResTmp != tkResolutionScope)
                continue;

            szNamespaceTmp = pMiniMd->getNamespaceOfTypeRef(pTypeRefRec);
            if (strcmp(szNamespace, szNamespaceTmp))
                continue;

            szNameTmp = pMiniMd->getNameOfTypeRef(pTypeRefRec);
            if (! strcmp(szName, szNameTmp))
            {
                *ptk = TokenFromRid(i, mdtTypeRef);
                return S_OK;
            }
        }
        hr = CLDB_E_RECORD_NOTFOUND;
    }
    return hr;
} // HRESULT ImportHelper::FindTypeRefByName()


//*******************************************************************************
// Find the ModuleRef given the name, guid and mvid.
//*******************************************************************************
HRESULT ImportHelper::FindModuleRef(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    LPCUTF8     szUTF8Name,                 // [IN] ModuleRef name.
    mdModuleRef *pmur,                      // [OUT] Put the ModuleRef token here.
    RID         rid /* = 0*/)               // [IN] Optional rid to be ignored.
{
    ModuleRefRec *pModuleRef;
    ULONG       cModuleRefs;
    LPCUTF8     szCurName;
    ULONG       i;

    _ASSERTE(pmur);
    _ASSERTE(szUTF8Name);

    cModuleRefs = pMiniMd->getCountModuleRefs();

    // linear scan through the ModuleRef table
    for (i=1; i <= cModuleRefs; ++i)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pModuleRef = pMiniMd->getModuleRef(i);

        if (szUTF8Name)
        {
            szCurName = pMiniMd->getNameOfModuleRef(pModuleRef);
            if (strcmp(szCurName, szUTF8Name))
                continue;
        }
        //  Matching record found.
        *pmur = TokenFromRid(i, mdtModuleRef);
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindModuleRef()



//*******************************************************************************
// Find the TypeDef given the type and namespace name
//*******************************************************************************
HRESULT ImportHelper::FindTypeDefByName(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    LPCUTF8     szNamespace,                // [IN] Full qualified TypeRef name.
    LPCUTF8     szName,                     // [IN] Full qualified TypeRef name.
    mdToken     tkEnclosingClass,           // [IN] TypeDef/TypeRef for Enclosing class.
    mdTypeDef   *ptk,                       // [OUT] Put the TypeRef token here.
    RID         rid /* = 0 */)              // [IN] Optional rid to be ignored.
{
    ULONG       cTypeDefRecs;
    TypeDefRec  *pTypeDefRec;
    LPCUTF8     szNameTmp;
    LPCUTF8     szNamespaceTmp;
    DWORD       dwFlags;
    HRESULT     hr = S_OK;

    _ASSERTE(szName &&  ptk);
    _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef ||
             TypeFromToken(tkEnclosingClass) == mdtTypeRef ||
             IsNilToken(tkEnclosingClass));
    _ASSERTE(! ns::FindSep(szName));

    *ptk = mdTypeDefNil;

    cTypeDefRecs = pMiniMd->getCountTypeDefs();

    // Treat no namespace as empty string.
    if (!szNamespace)
        szNamespace = "";

    // Search for the TypeDef
    for (ULONG i = 1; i <= cTypeDefRecs; i++)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pTypeDefRec = pMiniMd->getTypeDef(i);

        dwFlags = pMiniMd->getFlagsOfTypeDef(pTypeDefRec);

        if (!IsTdNested(dwFlags) && !IsNilToken(tkEnclosingClass))
        {
            // If the class is not Nested and EnclosingClass passed in is not nil
            continue;
        }
        else if (IsTdNested(dwFlags) && IsNilToken(tkEnclosingClass))
        {
            // If the class is nested and EnclosingClass passed is nil
            continue;
        }
        else if (!IsNilToken(tkEnclosingClass))
        {
            RID         iNestedClassRec;
            NestedClassRec *pNestedClassRec;
            mdTypeDef   tkEnclosingClassTmp;

            // If the EnclosingClass passed in is not nil
            if (TypeFromToken(tkEnclosingClass) == mdtTypeRef)
            {
                // Resolve the TypeRef to a TypeDef.

                TypeRefRec  *pTypeRefRec;
                mdToken     tkResolutionScope;
                LPCUTF8     szTypeRefName;
                LPCUTF8     szTypeRefNamespace;

                pTypeRefRec = pMiniMd->getTypeRef(RidFromToken(tkEnclosingClass));
                tkResolutionScope = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
                szTypeRefName = pMiniMd->getNameOfTypeRef(pTypeRefRec);
                szTypeRefNamespace = pMiniMd->getNamespaceOfTypeRef(pTypeRefRec);

                hr = FindTypeDefByName(pMiniMd, szTypeRefNamespace, szTypeRefName,
                    (TypeFromToken(tkResolutionScope) == mdtTypeRef) ? tkResolutionScope : mdTokenNil,
                    &tkEnclosingClass);
                if (hr == CLDB_E_RECORD_NOTFOUND)
                {
                    continue;
                }
                else if (hr != S_OK)
                    return hr;
            }

            iNestedClassRec = pMiniMd->FindNestedClassHelper(TokenFromRid(i, mdtTypeDef));
            if (InvalidRid(iNestedClassRec))
                continue;
            pNestedClassRec = pMiniMd->getNestedClass(iNestedClassRec);
            tkEnclosingClassTmp = pMiniMd->getEnclosingClassOfNestedClass(pNestedClassRec);
            if (tkEnclosingClass != tkEnclosingClassTmp)
                continue;
        }

        szNameTmp = pMiniMd->getNameOfTypeDef(pTypeDefRec);
        if ( strcmp(szName, szNameTmp) == 0)
        {
            szNamespaceTmp = pMiniMd->getNamespaceOfTypeDef(pTypeDefRec);
            if (strcmp(szNamespace, szNamespaceTmp) == 0)
            {
                *ptk = TokenFromRid(i, mdtTypeDef);
                return S_OK;
            }
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindTypeDefByName()

//*******************************************************************************
// Find the InterfaceImpl given the typedef and implemented interface
//*******************************************************************************
HRESULT ImportHelper::FindInterfaceImpl(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdToken     tkClass,                // [IN] TypeDef of the type
    mdToken     tkInterface,            // [IN] could be typedef/typeref
    mdInterfaceImpl *ptk,               // [OUT] Put the interface token here.
    RID         rid /* = 0*/)           // [IN] Optional rid to be ignored.
{
    ULONG       ridStart, ridEnd;
    ULONG       i;
    InterfaceImplRec    *pInterfaceImplRec;

    _ASSERTE(ptk);
    *ptk = mdInterfaceImplNil;
    if ( pMiniMd->IsSorted(TBL_InterfaceImpl) )
    {
        ridStart = pMiniMd->getInterfaceImplsForTypeDef(RidFromToken(tkClass), &ridEnd);
    }
    else
    {
        ridStart = 1;
        ridEnd = pMiniMd->getCountInterfaceImpls() + 1;
    }

    // Search for the interfaceimpl
    for (i = ridStart; i < ridEnd; i++)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pInterfaceImplRec = pMiniMd->getInterfaceImpl(i);
        if ( tkClass != pMiniMd->getClassOfInterfaceImpl(pInterfaceImplRec) )
            continue;
        if ( tkInterface == pMiniMd->getInterfaceOfInterfaceImpl(pInterfaceImplRec) )
        {
            *ptk = TokenFromRid(i, mdtInterfaceImpl);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindInterfaceImpl()



//*******************************************************************************
// Find the Permission by parent and action
//*******************************************************************************
HRESULT ImportHelper::FindPermission(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdToken     tkParent,               // [IN] Token with the Permission
    USHORT      usAction,               // [IN] The action of the permission
    mdPermission *ppm)                  // [OUT] Put permission token here
{
    HRESULT     hr = NOERROR;
    DeclSecurityRec *pRec;
    ULONG       ridStart, ridEnd;
    ULONG       i;
    mdToken     tkParentTmp;

    _ASSERTE(ppm);

    if ( pMiniMd->IsSorted(TBL_DeclSecurity) )
    {

        ridStart = pMiniMd->getDeclSecurityForToken(tkParent, &ridEnd);
    }
    else
    {
        ridStart = 1;
        ridEnd = pMiniMd->getCountDeclSecuritys() + 1;
    }
    // loop through all permission
    for (i = ridStart; i < ridEnd; i++)
    {
        pRec = pMiniMd->getDeclSecurity(i);
        tkParentTmp = pMiniMd->getParentOfDeclSecurity(pRec);
        if ( tkParentTmp != tkParent )
            continue;
        if (pRec->m_Action == usAction)
        {
            *ppm = TokenFromRid(i, mdtPermission);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindPermission()


//*****************************************************************************
// find a property record
//*****************************************************************************
HRESULT ImportHelper::FindProperty(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    mdToken     tkTypeDef,                  // [IN] typedef token
    LPCUTF8     szName,                     // [IN] name of the property
    const COR_SIGNATURE *pbSig,             // [IN] Signature.
    ULONG       cbSig,                      // [IN] Size of signature.
    mdProperty  *ppr)                       // [OUT] Property token
{
    HRESULT     hr = NOERROR;
    RID         ridPropertyMap;
    PropertyMapRec *pPropertyMapRec;
    PropertyRec *pRec;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    LPCUTF8     szNameTmp;
    PCCOR_SIGNATURE pbSigTmp;
    ULONG       cbSigTmp;
    ULONG       pr;

    ridPropertyMap = pMiniMd->FindPropertyMapFor( RidFromToken(tkTypeDef) );
    if ( !InvalidRid(ridPropertyMap) )
    {
        pPropertyMapRec = pMiniMd->getPropertyMap( ridPropertyMap );
        ridStart = pMiniMd->getPropertyListOfPropertyMap( pPropertyMapRec );
        ridEnd = pMiniMd->getEndPropertyListOfPropertyMap( pPropertyMapRec );

        for (i = ridStart; i < ridEnd; i++)
        {
            // get the property rid
            pr = pMiniMd->GetPropertyRid(i);
            pRec = pMiniMd->getProperty(pr);
            szNameTmp = pMiniMd->getNameOfProperty(pRec);
            pbSigTmp = pMiniMd->getTypeOfProperty( pRec, &cbSigTmp );
            if ( strcmp (szName, szNameTmp) != 0 )                   
                continue;
            if ( cbSig != 0 && (cbSigTmp != cbSig || memcmp(pbSig, pbSigTmp, cbSig) != 0 ) )
                continue;
            *ppr = TokenFromRid( i, mdtProperty );
            return S_OK;
        }
        return CLDB_E_RECORD_NOTFOUND;
    }
    else
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
} // HRESULT ImportHelper::FindProperty()




//*****************************************************************************
// find an Event record
//*****************************************************************************
HRESULT ImportHelper::FindEvent(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    mdToken     tkTypeDef,                  // [IN] typedef token
    LPCUTF8     szName,                     // [IN] name of the event
    mdProperty  *pev)                       // [OUT] Event token
{
    HRESULT     hr = NOERROR;
    RID         ridEventMap;
    EventMapRec *pEventMapRec;
    EventRec    *pRec;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    LPCUTF8     szNameTmp;
    ULONG       ev;

    ridEventMap = pMiniMd->FindEventMapFor( RidFromToken(tkTypeDef) );
    if ( !InvalidRid(ridEventMap) )
    {
        pEventMapRec = pMiniMd->getEventMap( ridEventMap );
        ridStart = pMiniMd->getEventListOfEventMap( pEventMapRec );
        ridEnd = pMiniMd->getEndEventListOfEventMap( pEventMapRec );

        for (i = ridStart; i < ridEnd; i++)
        {
            // get the Event rid
            ev = pMiniMd->GetEventRid(i);

            // get the event row
            pRec = pMiniMd->getEvent(ev);
            szNameTmp = pMiniMd->getNameOfEvent( pRec );
            if ( strcmp (szName, szNameTmp) == 0)
            {
                *pev = TokenFromRid( ev, mdtEvent );
                return S_OK;
            }
        }
        return CLDB_E_RECORD_NOTFOUND;
    }
    else
    {
        return CLDB_E_RECORD_NOTFOUND;
    }
} // HRESULT ImportHelper::FindEvent()



//*****************************************************************************
// find an custom value record given by parent and type token. This will always return
// the first one that is found regardless duplicated.
//*****************************************************************************
HRESULT ImportHelper::FindCustomAttributeByToken(
    CMiniMdRW   *pMiniMd,                   // [IN] the minimd to lookup
    mdToken     tkParent,                   // [IN] the parent that custom value is associated with
    mdToken     tkType,                     // [IN] type of the CustomAttribute
	const void	*pCustBlob,					// [IN] custom attribute blob
	ULONG		cbCustBlob,					// [IN] size of the blob.
    mdCustomAttribute *pcv)                 // [OUT] CustomAttribute token
{
    HRESULT     hr = NOERROR;
    CustomAttributeRec  *pRec;
    ULONG       ridStart, ridEnd;
    ULONG       i;
    mdToken     tkParentTmp;
    mdToken     tkTypeTmp;
	const void	*pCustBlobTmp;
	ULONG		cbCustBlobTmp;

    _ASSERTE(pcv);
    *pcv = mdCustomAttributeNil;
    if ( pMiniMd->IsSorted(TBL_CustomAttribute) )
    {
        *pcv = pMiniMd->FindCustomAttributeFor(
            RidFromToken( tkParent ),
            TypeFromToken( tkParent ),
            tkType);
        if (InvalidRid(*pcv))
            return S_FALSE;
        else if (pCustBlob)
		{
			pRec = pMiniMd->getCustomAttribute(RidFromToken(*pcv));
			pCustBlobTmp = pMiniMd->getValueOfCustomAttribute(pRec, &cbCustBlobTmp);
			if (cbCustBlob == cbCustBlobTmp &&
				!memcmp(pCustBlob, pCustBlobTmp, cbCustBlob))
			{
				return S_OK;
			}
			else
				return S_FALSE;
		}
        else
            return S_OK;
    }
    else
    {
        CLookUpHash *pHashTable = pMiniMd->m_pLookUpHashs[TBL_CustomAttribute];

        if (pHashTable)
        {
            // table is not sorted but hash is built
            // We want to create dynmaic array to hold the dynamic enumerator.
            TOKENHASHENTRY *p;
            ULONG       iHash;
            int         pos;
            mdToken     tkParentTmp;
            mdToken     tkTypeTmp;

            // Hash the data.
            iHash = pMiniMd->HashCustomAttribute(tkParent);

            // Go through every entry in the hash chain looking for ours.
            for (p = pHashTable->FindFirst(iHash, pos);
                 p;
                 p = pHashTable->FindNext(pos))
            {
                pRec = pMiniMd->getCustomAttribute(RidFromToken(p->tok));

                tkParentTmp = pMiniMd->getParentOfCustomAttribute(pRec);
                if (tkParentTmp != tkParent)
					continue;

                tkTypeTmp = pMiniMd->getTypeOfCustomAttribute(pRec);
				if (tkType != tkTypeTmp)
					continue;

				pCustBlobTmp = pMiniMd->getValueOfCustomAttribute(pRec, &cbCustBlobTmp);
				if (cbCustBlob == cbCustBlobTmp &&
					!memcmp(pCustBlob, pCustBlobTmp, cbCustBlob))
                {
                    *pcv = TokenFromRid(p->tok, mdtCustomAttribute);
                    return S_OK;
                }
            }
        }
        else
        {
            // linear scan
            ridStart = 1;
            ridEnd = pMiniMd->getCountCustomAttributes() + 1;

            // loop through all custom values
            for (i = ridStart; i < ridEnd; i++)
            {
                pRec = pMiniMd->getCustomAttribute(i);

                tkParentTmp = pMiniMd->getParentOfCustomAttribute(pRec);
                if ( tkParentTmp != tkParent )
                    continue;

                tkTypeTmp = pMiniMd->getTypeOfCustomAttribute(pRec);
                if (tkType != tkTypeTmp)
					continue;

				pCustBlobTmp = pMiniMd->getValueOfCustomAttribute(pRec, &cbCustBlobTmp);
				if (cbCustBlob == cbCustBlobTmp &&
					!memcmp(pCustBlob, pCustBlobTmp, cbCustBlob))
                {
                    *pcv = TokenFromRid(i, mdtCustomAttribute);
                    return S_OK;
                }
            }
        }
        // fall through
    }
    return S_FALSE;
} // HRESULT ImportHelper::FindCustomAttributeByToken()



//*****************************************************************************
// Helper function to lookup and retrieve a CustomAttribute.
//*****************************************************************************
HRESULT ImportHelper::GetCustomAttributeByName( // S_OK or error.
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup
    mdToken     tkObj,                  // [IN] Object with Custom Attribute.
    LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
	const void	**ppData,				// [OUT] Put pointer to data here.
	ULONG		*pcbData)				// [OUT] Put size of data here.
{
    return pMiniMd->CommonGetCustomAttributeByName(tkObj, szName, ppData, pcbData);
}   // ImportHelper::GetCustomAttributeByName



//*******************************************************************************
// Find an ImplMap record, which is used for p-invoke data.
//*******************************************************************************
HRESULT ImportHelper::FindImplMap(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
    mdToken     tkModuleRef,            // [IN] Parent module ref to look under.
    mdToken     tkMethodDef,            // [IN] Parent methoddef to look under.
    USHORT      usMappingFlags,         // [IN] Flags on how to map the item.
    LPCUTF8     szImportName,           // [IN] Name of import member.
    ULONG       *piRecord)              // [OUT] Record for item if found.
{
    ULONG       cRecs;                  // Count of records.
    ImplMapRec  *pRec;                  // Current record being looked at.

    mdToken     tkTmp;
    LPCUTF8     szImportNameTmp;

    _ASSERTE(piRecord);
    *piRecord = 0;

    cRecs = pMiniMd->getCountImplMaps();

    // Search for the ImplMap record.
    for (ULONG i = 1; i <= cRecs; i++)
    {
        pRec = pMiniMd->getImplMap(i);

        tkTmp = pMiniMd->getMemberForwardedOfImplMap(pRec);
        if (tkTmp != tkMethodDef)
            continue;

        tkTmp = pMiniMd->getImportScopeOfImplMap(pRec);
        if (tkTmp != tkModuleRef)
            continue;

        szImportNameTmp = pMiniMd->getImportNameOfImplMap(pRec);
        if (strcmp(szImportNameTmp, szImportName))
            continue;

        if (pRec->m_MappingFlags != usMappingFlags)
            continue;

        *piRecord = i;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindImplMap()

//*******************************************************************************
// Find an AssemblyRef record given the name.
//*******************************************************************************
HRESULT ImportHelper::FindAssemblyRef(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
    LPCUTF8     szName,                 // [IN] Name.
    LPCUTF8     szLocale,               // [IN] Locale.
    const void  *pbPublicKeyOrToken,    // [IN] Public key or token (based on flags).
    ULONG       cbPublicKeyOrToken,     // [IN] Byte count of public key or token.
    USHORT      usMajorVersion,         // [IN] Major version.
    USHORT      usMinorVersion,         // [IN] Minor version.
    USHORT      usBuildNumber,          // [IN] Build number.
    USHORT      usRevisionNumber,       // [IN] Revision number.
    DWORD       dwFlags,                // [IN] Flags.
    mdAssemblyRef *pmar)                // [OUT] returned AssemblyRef token.
{
    ULONG       cRecs;                  // Count of records.
    AssemblyRefRec *pRec;               // Current record being looked at.
    LPCUTF8     szTmp;                  // Temp string.
    const void  *pbTmp;                 // Temp blob.
    ULONG       cbTmp;                  // Temp byte count.
    DWORD       dwTmp;                  // Temp flags.
    const void  *pbToken = NULL;        // Token version of public key.
    ULONG       cbToken = 0;            // Count of bytes in token.
    const void  *pbTmpToken;            // Token version of public key.
    ULONG       cbTmpToken;             // Count of bytes in token.
    bool        fMatch;                 // Did public key or tokens match?

    // Handle special cases upfront.
    if (!szLocale)
        szLocale = "";
    if (!pbPublicKeyOrToken)
        cbPublicKeyOrToken = 0;

    if (!IsAfPublicKey(dwFlags))
    {
        pbToken = pbPublicKeyOrToken;
        cbToken = cbPublicKeyOrToken;
    }

    _ASSERTE(pMiniMd && szName && pmar);
    *pmar = 0;

    cRecs = pMiniMd->getCountAssemblyRefs();

    // Search for the AssemblyRef record.
    for (ULONG i = 1; i <= cRecs; i++)
    {
        pRec = pMiniMd->getAssemblyRef(i);

        szTmp = pMiniMd->getNameOfAssemblyRef(pRec);
        if (strcmp(szTmp, szName))
            continue;

        szTmp = pMiniMd->getLocaleOfAssemblyRef(pRec);
        if (strcmp(szTmp, szLocale))
            continue;

        if (pRec->m_MajorVersion != usMajorVersion)
            continue;
        if (pRec->m_MinorVersion != usMinorVersion)
            continue;
        if (pRec->m_BuildNumber != usBuildNumber)
            continue;
        if (pRec->m_RevisionNumber != usRevisionNumber)
            continue;

        pbTmp = pMiniMd->getPublicKeyOrTokenOfAssemblyRef(pRec, &cbTmp);

        if ((cbPublicKeyOrToken && !cbTmp) ||
            (!cbPublicKeyOrToken && cbTmp))
            continue;

        if (cbTmp)
        {
            // Either ref may be using either a full public key or a token
            // (determined by the ref flags). Must cope with all variations.
            dwTmp = pMiniMd->getFlagsOfAssemblyRef(pRec);
            if (IsAfPublicKey(dwTmp) == IsAfPublicKey(dwFlags))
            {
                // Easy case, they're both in the same form.
                if (cbTmp != cbPublicKeyOrToken || memcmp(pbTmp, pbPublicKeyOrToken, cbTmp))
                    continue;
            }
            else if (IsAfPublicKey(dwTmp))
            {
                // Need to compress target public key to see if it matches.
                if (!StrongNameTokenFromPublicKey((BYTE*)pbTmp,
                                                  cbTmp,
                                                  (BYTE**)&pbTmpToken,
                                                  &cbTmpToken))
                    return StrongNameErrorInfo();
                fMatch = cbTmpToken == cbPublicKeyOrToken && !memcmp(pbTmpToken, pbPublicKeyOrToken, cbTmpToken);
                StrongNameFreeBuffer((BYTE*)pbTmpToken);
                if (!fMatch)
                    continue;
            }
            else
            {
                // Need to compress out public key to see if it matches. We
                // cache the result of this for further iterations.
                if (!pbToken)
                    if (!StrongNameTokenFromPublicKey((BYTE*)pbPublicKeyOrToken,
                                                      cbPublicKeyOrToken,
                                                      (BYTE**)&pbToken,
                                                      &cbToken))
                        return StrongNameErrorInfo();
                if (cbTmp != cbToken || memcmp(pbTmp, pbToken, cbToken))
                    continue;
            }
        }

        if (pbToken && IsAfPublicKey(dwFlags))
            StrongNameFreeBuffer((BYTE*)pbToken);
        *pmar = TokenFromRid(i, mdtAssemblyRef);
        return S_OK;
    }
    if (pbToken && IsAfPublicKey(dwFlags))
        StrongNameFreeBuffer((BYTE*)pbToken);
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindAssemblyRef()

//*******************************************************************************
// Find a File record given the name.
//*******************************************************************************
HRESULT ImportHelper::FindFile(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
    LPCUTF8     szName,                 // [IN] name for the File.
    mdFile      *pmf,                   // [OUT] returned File token.
    RID         rid /* = 0 */)          // [IN] Optional rid to be ignored.
{
    ULONG       cRecs;                  // Count of records.
    FileRec     *pRec;                  // Current record being looked at.

    LPCUTF8     szNameTmp;

    _ASSERTE(pMiniMd && szName && pmf);
    *pmf = 0;

    cRecs = pMiniMd->getCountFiles();

    // Search for the File record.
    for (ULONG i = 1; i <= cRecs; i++)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pRec = pMiniMd->getFile(i);

        szNameTmp = pMiniMd->getNameOfFile(pRec);
        if (!strcmp(szNameTmp, szName))
        {
            *pmf = TokenFromRid(i, mdtFile);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindFile()

//*******************************************************************************
// Find a ExportedType record given the name.
//*******************************************************************************
HRESULT ImportHelper::FindExportedType(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
    LPCUTF8     szNamespace,            // [IN] namespace for the ExportedType.
    LPCUTF8     szName,                 // [IN] name for the ExportedType.
    mdExportedType   tkEnclosingType,        // [IN] token for the enclosing type.
    mdExportedType   *pmct,                  // [OUT] returned ExportedType token.
    RID         rid /* = 0 */)          // [IN] Optional rid to be ignored.
{
    ULONG       cRecs;                  // Count of records.
    ExportedTypeRec  *pRec;                  // Current record being looked at.
    mdToken     tkImpl;
    LPCUTF8     szNamespaceTmp;
    LPCUTF8     szNameTmp;

    _ASSERTE(pMiniMd && szName && pmct);
    *pmct = 0;

    // Treat no namespace as empty string.
    if (!szNamespace)
        szNamespace = "";

    cRecs = pMiniMd->getCountExportedTypes();

    // Search for the ExportedType record.
    for (ULONG i = 1; i <= cRecs; i++)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pRec = pMiniMd->getExportedType(i);

        // Handle the case of nested vs. non-nested classes.
        tkImpl = pMiniMd->getImplementationOfExportedType(pRec);
        if (TypeFromToken(tkImpl) == mdtExportedType && !IsNilToken(tkImpl))
        {
            // Current ExportedType being looked at is a nested type, so
            // comparing the implementation token.
            if (tkImpl != tkEnclosingType)
                continue;
        }
        else if (TypeFromToken(tkEnclosingType) == mdtExportedType &&
                 !IsNilToken(tkEnclosingType))
        {
            // ExportedType passed in is nested but the current ExportedType is not.
            continue;
        }

        szNamespaceTmp = pMiniMd->getTypeNamespaceOfExportedType(pRec);
        if (strcmp(szNamespaceTmp, szNamespace))
            continue;

        szNameTmp = pMiniMd->getTypeNameOfExportedType(pRec);
        if (!strcmp(szNameTmp, szName))
        {
            *pmct = TokenFromRid(i, mdtExportedType);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindExportedType()

//*******************************************************************************
// Find a ManifestResource record given the name.
//*******************************************************************************
HRESULT ImportHelper::FindManifestResource(
    CMiniMdRW   *pMiniMd,               // [IN] the minimd to lookup.
    LPCUTF8     szName,                 // [IN] name for the ManifestResource.
    mdManifestResource *pmmr,           // [OUT] returned ManifestResource token.
    RID         rid /* = 0 */)          // [IN] Optional rid to be ignored.
{
    ULONG       cRecs;                  // Count of records.
    ManifestResourceRec *pRec;          // Current record being looked at.

    LPCUTF8     szNameTmp;

    _ASSERTE(pMiniMd && szName && pmmr);
    *pmmr = 0;

    cRecs = pMiniMd->getCountManifestResources();

    // Search for the ManifestResource record.
    for (ULONG i = 1; i <= cRecs; i++)
    {
        // For the call from Validator ignore the rid passed in.
        if (i == rid)
            continue;

        pRec = pMiniMd->getManifestResource(i);

        szNameTmp = pMiniMd->getNameOfManifestResource(pRec);
        if (!strcmp(szNameTmp, szName))
        {
            *pmmr = TokenFromRid(i, mdtManifestResource);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT ImportHelper::FindManifestResource()

//****************************************************************************
// Convert tokens contained in an element type
//****************************************************************************
HRESULT ImportHelper::MergeUpdateTokenInFieldSig(       // S_OK or error.
    CMiniMdRW   *pMiniMdAssemEmit,      // [IN] The assembly emit scope.
    CMiniMdRW   *pMiniMdEmit,           // [IN] The emit scope.
    IMetaModelCommon *pCommonAssemImport,// [IN] Assembly scope where the signature is from.
    const void  *pbHashValue,           // [IN] Hash value for the import assembly.
    ULONG       cbHashValue,            // [IN] Size in bytes for the hash value.
    IMetaModelCommon *pCommonImport,    // [IN] The scope to merge into the emit scope.
    PCCOR_SIGNATURE pbSigImp,           // signature from the imported scope
    MDTOKENMAP      *ptkMap,            // Internal OID mapping structure.
    CQuickBytes     *pqkSigEmit,        // [OUT] buffer for translated signature
    ULONG           cbStartEmit,        // [IN] start point of buffer to write to
    ULONG           *pcbImp,            // [OUT] total number of bytes consumed from pbSigImp
    ULONG           *pcbEmit)           // [OUT] total number of bytes write to pqkSigEmit
{

    HRESULT     hr;                     // A result.
    ULONG       cb;                     // count of bytes
    ULONG       cb1;                    // count of bytes
    ULONG       cb2;                    // count of bytes
    ULONG       cbSubTotal;
    ULONG       cbImp;
    ULONG       cbEmit;
    ULONG       cbSrcTotal = 0;         // count of bytes consumed in the imported signature
    ULONG       cbDestTotal = 0;        // count of bytes for the new signature
    ULONG       ulElementType;          // place holder for expanded data
    ULONG       ulData;
    ULONG       ulTemp;
    mdToken     tkRidFrom;              // Original rid
    mdToken     tkRidTo;                // new rid
    int         iData;
    CQuickArray<mdToken> cqaNesters;    // Array of Nester tokens.
    CQuickArray<LPCUTF8> cqaNesterNamespaces;   // Array of Nester Namespaces.
    CQuickArray<LPCUTF8> cqaNesterNames;    // Array of Nester names.

    _ASSERTE(pcbEmit);

    cb = CorSigUncompressData(&pbSigImp[cbSrcTotal], &ulElementType);
    cbSrcTotal += cb;

    // count numbers of modifiers
    while (CorIsModifierElementType((CorElementType) ulElementType))
    {
        cb = CorSigUncompressData(&pbSigImp[cbSrcTotal], &ulElementType);
        cbSrcTotal += cb;
    }

    // copy ELEMENT_TYPE_* over
    cbDestTotal = cbSrcTotal;
    IfFailGo(pqkSigEmit->ReSize(cbStartEmit + cbDestTotal));
    memcpy(((BYTE *)pqkSigEmit->Ptr()) + cbStartEmit, pbSigImp, cbDestTotal);
    switch (ulElementType)
    {
        case ELEMENT_TYPE_VALUEARRAY:
            // syntax for SDARRAY = BaseType <an integer for size>

            // conver the base type for the SDARRAY
            IfFailGo(MergeUpdateTokenInFieldSig(
                pMiniMdAssemEmit,           // The assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                pCommonAssemImport,         // The assembly scope where the signature is from.
                pbHashValue,                // Hash value for the import assembly.
                cbHashValue,                // Size in bytes for the hash value.
                pCommonImport,              // The scope to merge into the emit scope.
                &pbSigImp[cbSrcTotal],      // signature from the imported scope
                ptkMap,                     // Internal OID mapping structure.
                pqkSigEmit,                 // [OUT] buffer for translated signature
                cbStartEmit + cbSrcTotal,   // [IN] start point of buffer to write to
                &cbImp,                     // [OUT] total number of bytes consumed from pbSigImp
                &cbEmit));                  // [OUT] total number of bytes write to pqkSigEmit
            cbSrcTotal += cbImp;
            cbDestTotal += cbEmit;

            // after the base type, it is followed by an integer indicating the size
            // of the array
            //
            cb = CorSigUncompressData(&pbSigImp[cbSrcTotal], &ulData);

            IfFailGo(pqkSigEmit->ReSize(cbStartEmit + cbDestTotal + cb));
            cb1 = CorSigCompressData(ulData, ((BYTE *)pqkSigEmit->Ptr()) + cbStartEmit + cbDestTotal);
            _ASSERTE(cb == cb1);

            cbSrcTotal = cbSrcTotal + cb;
            cbDestTotal = cbDestTotal + cb1;
            break;

        case ELEMENT_TYPE_SZARRAY:
            // syntax : SZARRAY <BaseType>

            // conver the base type for the SZARRAY or GENERICARRAY
            IfFailGo(MergeUpdateTokenInFieldSig(
                pMiniMdAssemEmit,           // The assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                pCommonAssemImport,         // The assembly scope where the signature is from.
                pbHashValue,                // Hash value for the import assembly.
                cbHashValue,                // Size in bytes for the hash value.
                pCommonImport,              // scope to merge into the emit scope.
                &pbSigImp[cbSrcTotal],      // from the imported scope
                ptkMap,                     // OID mapping structure.
                pqkSigEmit,                 // [OUT] buffer for translated signature
                cbStartEmit + cbDestTotal,  // [IN] start point of buffer to write to
                &cbImp,                     // [OUT] total number of bytes consumed from pbSigImp
                &cbEmit));                  // [OUT] total number of bytes write to pqkSigEmit
            cbSrcTotal += cbImp;
            cbDestTotal += cbEmit;
            break;

        case ELEMENT_TYPE_ARRAY:
            // syntax : ARRAY BaseType <rank> [i size_1... size_i] [j lowerbound_1 ... lowerbound_j]

            // conver the base type for the MDARRAY
            IfFailGo(MergeUpdateTokenInFieldSig(
                pMiniMdAssemEmit,           // The assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                pCommonAssemImport,         // The assembly scope where the signature is from.
                pbHashValue,                // Hash value for the import assembly.
                cbHashValue,                // Size in bytes for the hash value.
                pCommonImport,              // The scope to merge into the emit scope.
                &pbSigImp[cbSrcTotal],      // signature from the imported scope
                ptkMap,                     // Internal OID mapping structure.
                pqkSigEmit,                 // [OUT] buffer for translated signature
                cbStartEmit + cbSrcTotal,   // [IN] start point of buffer to write to
                &cbImp,                     // [OUT] total number of bytes consumed from pbSigImp
                &cbEmit));                  // [OUT] total number of bytes write to pqkSigEmit
            cbSrcTotal += cbImp;
            cbDestTotal += cbEmit;

            // Parse for the rank
            cbSubTotal = CorSigUncompressData(&pbSigImp[cbSrcTotal], &ulData);

            // if rank == 0, we are done
            if (ulData != 0)
            {
                // any size of dimension specified?
                cb = CorSigUncompressData(&pbSigImp[cbSrcTotal + cbSubTotal], &ulData);
                cbSubTotal += cb;

                while (ulData--)
                {
                    cb = CorSigUncompressData(&pbSigImp[cbSrcTotal + cbSubTotal], &ulTemp);
                    cbSubTotal += cb;
                }

                // any lower bound specified?
                cb = CorSigUncompressData(&pbSigImp[cbSrcTotal + cbSubTotal], &ulData);
                cbSubTotal += cb;

                while (ulData--)
                {
                    cb = CorSigUncompressSignedInt(&pbSigImp[cbSrcTotal + cbSubTotal], &iData);
                    cbSubTotal += cb;
                }
            }

            // cbSubTotal is now the number of bytes still left to move over
            // cbSrcTotal is where bytes start on the pbSigImp to be copied over
            // cbStartEmit + cbDestTotal is where the destination of copy

            IfFailGo(pqkSigEmit->ReSize(cbStartEmit + cbDestTotal + cbSubTotal));
            memcpy(((BYTE *)pqkSigEmit->Ptr())+cbStartEmit + cbDestTotal, &pbSigImp[cbSrcTotal], cbSubTotal);

            cbSrcTotal = cbSrcTotal + cbSubTotal;
            cbDestTotal = cbDestTotal + cbSubTotal;

            break;
        case ELEMENT_TYPE_FNPTR:
            // function pointer is followed by another complete signature
            IfFailGo(MergeUpdateTokenInSig(
                pMiniMdAssemEmit,           // The assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                pCommonAssemImport,         // The assembly scope where the signature is from.
                pbHashValue,                // Hash value for the import assembly.
                cbHashValue,                // Size in bytes for the hash value.
                pCommonImport,              // The scope to merge into the emit scope.
                &pbSigImp[cbSrcTotal],      // signature from the imported scope
                ptkMap,                     // Internal OID mapping structure.
                pqkSigEmit,                 // [OUT] buffer for translated signature
                cbStartEmit + cbDestTotal,  // [IN] start point of buffer to write to
                &cbImp,                     // [OUT] total number of bytes consumed from pbSigImp
                &cbEmit));                  // [OUT] total number of bytes write to pqkSigEmit
            cbSrcTotal += cbImp;
            cbDestTotal += cbEmit;
            break;
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:

            // syntax for CLASS = ELEMENT_TYPE_CLASS <rid>
            // syntax for VALUE_CLASS = ELEMENT_TYPE_VALUECLASS <rid>

            // now get the embedded typeref token
            cb = CorSigUncompressToken(&pbSigImp[cbSrcTotal], &tkRidFrom);

            // Map the ulRidFrom to ulRidTo
            if (ptkMap)
            {
                // mdtBaseType does not record in the map. It is unique across modules
                if ( TypeFromToken(tkRidFrom) == mdtBaseType )
                {
                    tkRidTo = tkRidFrom;
                }
                else
                {
                    IfFailGo( ptkMap->Remap(tkRidFrom, &tkRidTo) );
                }
            }
            else
            {
                // If the token is a TypeDef or a TypeRef, get/create the
                // ResolutionScope for the outermost TypeRef.
                if (TypeFromToken(tkRidFrom) == mdtTypeDef)
                {
                    IfFailGo(ImportTypeDef(pMiniMdAssemEmit,
                                           pMiniMdEmit,
                                           pCommonAssemImport,
                                           pbHashValue,
                                           cbHashValue,
                                           pCommonImport,
                                           tkRidFrom,
                                           true,    // Optimize to TypeDef if emit and import scopes are identical.
                                           &tkRidTo));
                }
                else if (TypeFromToken(tkRidFrom) == mdtTypeRef)
                {
                    IfFailGo(ImportTypeRef(pMiniMdAssemEmit,
                                           pMiniMdEmit,
                                           pCommonAssemImport,
                                           pbHashValue,
                                           cbHashValue,
                                           pCommonImport,
                                           tkRidFrom,
                                           &tkRidTo));
                }
                else if ( TypeFromToken(tkRidFrom) == mdtTypeSpec )
                {
                    // copy over the TypeSpec
                    PCCOR_SIGNATURE pvTypeSpecSig;
                    ULONG           cbTypeSpecSig;
                    CQuickBytes qkTypeSpecSigEmit;
                    ULONG           cbTypeSpecEmit;

                    pCommonImport->CommonGetTypeSpecProps(tkRidFrom, &pvTypeSpecSig, &cbTypeSpecSig);

                                        // Translate the typespec signature before look up
                    IfFailGo(MergeUpdateTokenInFieldSig(
                        pMiniMdAssemEmit,           // The assembly emit scope.
                        pMiniMdEmit,                // The emit scope.
                        pCommonAssemImport,         // The assembly scope where the signature is from.
                        pbHashValue,                // Hash value for the import assembly.
                        cbHashValue,                // Size in bytes for the hash value.
                        pCommonImport,              // The scope to merge into the emit scope.
                        pvTypeSpecSig,              // signature from the imported scope
                        ptkMap,                     // Internal OID mapping structure.
                        &qkTypeSpecSigEmit,         // [OUT] buffer for translated signature
                        0,                          // start from first byte of TypeSpec signature
                        0,                          // don't care how many bytes are consumed
                        &cbTypeSpecEmit) );         // [OUT] total number of bytes write to pqkSigEmit

                    hr = FindTypeSpec(pMiniMdEmit,
                                      (PCCOR_SIGNATURE) (qkTypeSpecSigEmit.Ptr()),
                                      cbTypeSpecEmit,
                                      &tkRidTo);

                    if ( hr == CLDB_E_RECORD_NOTFOUND )
                    {
                        // Create TypeSpec record.
                        TypeSpecRec     *pRecEmit;

                        IfNullGo(pRecEmit = pMiniMdEmit->AddTypeSpecRecord((ULONG *)&tkRidTo));

                        IfFailGo(pMiniMdEmit->PutBlob(
                            TBL_TypeSpec,
                            TypeSpecRec::COL_Signature,
                            pRecEmit,
                            (PCCOR_SIGNATURE) (qkTypeSpecSigEmit.Ptr()),
                            cbTypeSpecEmit));
                        tkRidTo = TokenFromRid( tkRidTo, mdtTypeSpec );
                        IfFailGo(pMiniMdEmit->UpdateENCLog(tkRidTo));
                    }
                    IfFailGo( hr );
                }
                else
                {
                    _ASSERTE( TypeFromToken(tkRidFrom) == mdtBaseType );

                    // base type is unique across module
                    tkRidTo = tkRidFrom;
                }
            }

            // How many bytes the new rid will consume?
            cb1 = CorSigCompressToken(tkRidTo, &ulData);

            // ensure buffer is big enough
            IfFailGo(pqkSigEmit->ReSize(cbStartEmit + cbDestTotal + cb1));

            // store the new token
            cb2 = CorSigCompressToken(
                    tkRidTo,
                    (ULONG *)( ((BYTE *)pqkSigEmit->Ptr()) + cbStartEmit + cbDestTotal) );

            // inconsistency on CorSigCompressToken and CorSigUncompressToken
            _ASSERTE(cb1 == cb2);

            cbSrcTotal = cbSrcTotal + cb;
            cbDestTotal = cbDestTotal + cb1;

            if ( ulElementType == ELEMENT_TYPE_CMOD_REQD ||
                 ulElementType == ELEMENT_TYPE_CMOD_OPT)
            {
                // need to skip over the base type
                IfFailGo(MergeUpdateTokenInFieldSig(
                    pMiniMdAssemEmit,           // The assembly emit scope.
                    pMiniMdEmit,                // The emit scope.
                    pCommonAssemImport,         // The assembly scope where the signature is from.
                    pbHashValue,                // Hash value for the import assembly.
                    cbHashValue,                // Size in bytes for the hash value.
                    pCommonImport,              // The scope to merge into the emit scope.
                    &pbSigImp[cbSrcTotal],      // signature from the imported scope
                    ptkMap,                     // Internal OID mapping structure.
                    pqkSigEmit,                 // [OUT] buffer for translated signature
                    cbStartEmit + cbDestTotal,  // [IN] start point of buffer to write to
                    &cbImp,                     // [OUT] total number of bytes consumed from pbSigImp
                    &cbEmit));                  // [OUT] total number of bytes write to pqkSigEmit
                cbSrcTotal += cbImp;
                cbDestTotal += cbEmit;
            }

            break;
        default:
            _ASSERTE(ulElementType < ELEMENT_TYPE_MAX);
            _ASSERTE(ulElementType != ELEMENT_TYPE_PTR && ulElementType != ELEMENT_TYPE_BYREF);
            _ASSERTE(cbSrcTotal == cbDestTotal);

            if (ulElementType >= ELEMENT_TYPE_MAX)
                IfFailGo(META_E_BAD_SIGNATURE);

            break;
    }
    if (pcbImp)
        *pcbImp = cbSrcTotal;
    *pcbEmit = cbDestTotal;

ErrExit:
    return hr;
} // HRESULT ImportHelper::MergeUpdateTokenInFieldSig()


//****************************************************************************
// convert tokens contained in a COM+ signature
//****************************************************************************
HRESULT ImportHelper::MergeUpdateTokenInSig(// S_OK or error.
    CMiniMdRW   *pMiniMdAssemEmit,      // [IN] The assembly emit scope.
    CMiniMdRW   *pMiniMdEmit,           // [IN] The emit scope.
    IMetaModelCommon *pCommonAssemImport,// [IN] Assembly scope where the signature is from.
    const void  *pbHashValue,           // [IN] Hash value for the import assembly.
    ULONG       cbHashValue,            // [IN] Size in bytes for the hash value.
    IMetaModelCommon *pCommonImport,    // [IN] The scope to merge into the emit scope.
    PCCOR_SIGNATURE pbSigImp,           // signature from the imported scope
    MDTOKENMAP      *ptkMap,            // Internal OID mapping structure.
    CQuickBytes     *pqkSigEmit,        // [OUT] translated signature
    ULONG           cbStartEmit,        // [IN] start point of buffer to write to
    ULONG           *pcbImp,            // [OUT] total number of bytes consumed from pbSigImp
    ULONG           *pcbEmit)           // [OUT] total number of bytes write to pqkSigEmit
{
    HRESULT     hr = NOERROR;           // A result.
    ULONG       cb;                     // count of bytes
    ULONG       cb1;
    ULONG       cbSrcTotal = 0;         // count of bytes consumed in the imported signature
    ULONG       cbDestTotal = 0;        // count of bytes for the new signature
    ULONG       cbEmit;                 // count of bytes consumed in the imported signature
    ULONG       cbImp;                  // count of bytes for the new signature
    ULONG       cbField = 0;            // count of bytes
    ULONG       cArg = 0;               // count of arguments in the signature
    ULONG       callingconv;

    _ASSERTE(pcbEmit && pqkSigEmit && pbSigImp);

    // calling convention
    cb = CorSigUncompressData(&pbSigImp[cbSrcTotal], &callingconv);
    _ASSERTE((callingconv & IMAGE_CEE_CS_CALLCONV_MASK) < IMAGE_CEE_CS_CALLCONV_MAX);

    // skip over calling convention
    cbSrcTotal += cb;

    if (isCallConv(callingconv, IMAGE_CEE_CS_CALLCONV_FIELD))
    {
        // It is a FieldRef
        cb1 = CorSigCompressData(callingconv, ((BYTE *)pqkSigEmit->Ptr()) + cbStartEmit);

        // compression and uncompression better match
        _ASSERTE(cb == cb1);

        cbDestTotal = cbSrcTotal = cb;
        IfFailGo(MergeUpdateTokenInFieldSig(
            pMiniMdAssemEmit,
            pMiniMdEmit,
            pCommonAssemImport,
            pbHashValue,
            cbHashValue,
            pCommonImport,
            &pbSigImp[cbSrcTotal],
            ptkMap,
            pqkSigEmit,                     // output buffer to hold the new sig for the field
            cbStartEmit + cbDestTotal,      // number of bytes already in pqkSigDest
            &cbImp,                         // number of bytes consumed from imported signature
            &cbEmit));                      // number of bytes write to the new signature
        *pcbEmit = cbDestTotal + cbEmit;
    }
    else
    {

        // It is a MethodRef

        // count of argument
        cb = CorSigUncompressData(&pbSigImp[cbSrcTotal], &cArg);
        cbSrcTotal += cb;

        // move over the calling convention and the count of arguments
        IfFailGo(pqkSigEmit->ReSize(cbStartEmit + cbSrcTotal));
        memcpy(((BYTE *)pqkSigEmit->Ptr()) + cbStartEmit, pbSigImp, cbSrcTotal);
        cbDestTotal = cbSrcTotal;

        if ( !isCallConv(callingconv, IMAGE_CEE_CS_CALLCONV_LOCAL_SIG) )
        {
                // LocalVar sig does not have return type
                // process the return type
                IfFailGo(MergeUpdateTokenInFieldSig(
                pMiniMdAssemEmit,
                pMiniMdEmit,
                pCommonAssemImport,
                pbHashValue,
                cbHashValue,
                pCommonImport,
                &pbSigImp[cbSrcTotal],
                ptkMap,
                pqkSigEmit,                     // output buffer to hold the new sig for the field
                cbStartEmit + cbDestTotal,      // number of bytes already in pqkSigDest
                &cbImp,                         // number of bytes consumed from imported signature
                &cbEmit));                      // number of bytes write to the new signature

            // advance the count
            cbSrcTotal += cbImp;
            cbDestTotal += cbEmit;
        }


        while (cArg)
        {
            // process every argument
            IfFailGo(MergeUpdateTokenInFieldSig(
                pMiniMdAssemEmit,
                pMiniMdEmit,
                pCommonAssemImport,
                pbHashValue,
                cbHashValue,
                pCommonImport,
                &pbSigImp[cbSrcTotal],
                ptkMap,
                pqkSigEmit,                 // output buffer to hold the new sig for the field
                cbStartEmit + cbDestTotal,
                &cbImp,                     // number of bytes consumed from imported signature
                &cbEmit));                  // number of bytes write to the new signature
            cbSrcTotal += cbImp;
            cbDestTotal += cbEmit;
            cArg--;
        }

        // total of number of bytes consumed from imported signature
        if (pcbImp)
            *pcbImp = cbSrcTotal;

        // total number of bytes emitted by this function call to the emitting signature
        *pcbEmit = cbDestTotal;
    }

ErrExit:
    return hr;
} // HRESULT ImportHelper::MergeUpdateTokenInSig()

//****************************************************************************
// Given a TypeDef or a TypeRef, return the Nesting hierarchy.  The first
// element in the returned array always refers to the class token passed and
// the nesting hierarchy expands outwards from there.
//****************************************************************************
HRESULT ImportHelper::GetNesterHierarchy(
    IMetaModelCommon *pCommon,          // Scope in which to find the hierarchy.
    mdToken     tk,                     // TypeDef/TypeRef whose hierarchy is needed.
    CQuickArray<mdToken> &cqaNesters,   // Array of Nesters.
    CQuickArray<LPCUTF8> &cqaNamespaces,    // Names of the nesters.
    CQuickArray<LPCUTF8> &cqaNames)     // Namespaces of the nesters.
{
    _ASSERTE(pCommon &&
             (TypeFromToken(tk) == mdtTypeDef ||
              TypeFromToken(tk) == mdtTypeRef) &&
             !IsNilToken(tk));

    if (TypeFromToken(tk) == mdtTypeDef)
    {
        return GetTDNesterHierarchy(pCommon,
                                    tk,
                                    cqaNesters,
                                    cqaNamespaces,
                                    cqaNames);
    }
    else
    {
        return GetTRNesterHierarchy(pCommon,
                                    tk,
                                    cqaNesters,
                                    cqaNamespaces,
                                    cqaNames);
    }
}   // HRESULT ImportHelper::GetNesterHierarchy()

//****************************************************************************
// Get Nesting hierarchy given a TypeDef.
//****************************************************************************
HRESULT ImportHelper::GetTDNesterHierarchy(
    IMetaModelCommon *pCommon,          // Scope in which to find the hierarchy.
    mdTypeDef       td,                 // TypeDef whose hierarchy is needed.
    CQuickArray<mdTypeDef> &cqaTdNesters,// Array of Nesters.
    CQuickArray<LPCUTF8> &cqaNamespaces,    // Namespaces of the nesters.
    CQuickArray<LPCUTF8> &cqaNames)     // Names of the nesters.
{
    LPCUTF8     szName, szNamespace;
    DWORD       dwFlags;
    mdTypeDef   tdNester;
    ULONG       ulNesters;
    HRESULT     hr = NOERROR;

    _ASSERTE(pCommon &&
             TypeFromToken(td) == mdtTypeDef &&
             !IsNilToken(td));

    // Set current Nester index to 0.
    ulNesters = 0;
    // The first element in the hierarchy is the TypeDef itself.
    tdNester = td;
    // Bogus initialization to kick off the while loop.
    dwFlags = tdNestedPublic;
    // Loop as long as the TypeDef is a Nested TypeDef.
    while (IsTdNested(dwFlags))
    {
        if (InvalidRid(tdNester))
            IfFailGo(CLDB_E_RECORD_NOTFOUND);
        // Get the name and namespace for the TypeDef.
        pCommon->CommonGetTypeDefProps(tdNester,
                                &szNamespace, &szName, &dwFlags);

        // Update the dynamic arrays.
        ulNesters++;

        IfFailGo(cqaTdNesters.ReSize(ulNesters));
        cqaTdNesters[ulNesters-1] = tdNester;

        IfFailGo(cqaNamespaces.ReSize(ulNesters));
        cqaNamespaces[ulNesters-1] = szNamespace;

        IfFailGo(cqaNames.ReSize(ulNesters));
        cqaNames[ulNesters-1] = szName;

        tdNester = pCommon->CommonGetEnclosingClassOfTypeDef(tdNester);
    }
    // Outermost class must have enclosing of Nil.
    _ASSERTE(IsNilToken(tdNester));
ErrExit:
    return hr;
}   // HRESULT ImportHelper::GetTDNesterHierarchy()


//****************************************************************************
// Get Nesting hierarchy given a TypeRef.
//****************************************************************************
HRESULT ImportHelper::GetTRNesterHierarchy(
    IMetaModelCommon *pCommon,          // [IN] Scope in which to find the hierarchy.
    mdTypeRef   tr,                     // [IN] TypeRef whose hierarchy is needed.
    CQuickArray<mdTypeRef> &cqaTrNesters,// [OUT] Array of Nesters.
    CQuickArray<LPCUTF8> &cqaNamespaces,    // [OUT] Namespaces of the nesters.
    CQuickArray<LPCUTF8> &cqaNames)    // [OUT] Names of the nesters.
{
    LPCUTF8     szNamespace;
    LPCUTF8     szName;
    mdTypeRef   trNester;
    mdToken     tkResolutionScope;
    ULONG       ulNesters;
    HRESULT     hr = S_OK;

    _ASSERTE(pCommon &&
             TypeFromToken(tr) == mdtTypeRef &&
             !IsNilToken(tr));

    // Set current Nester index to 0.
    ulNesters = 0;
    // The first element in the hierarchy is the TypeRef itself.
    trNester = tr;
    // Loop as long as the TypeRef is a Nested TypeRef.
    while (TypeFromToken(trNester) == mdtTypeRef && !IsNilToken(trNester))
    {
        // Get the name and namespace for the TypeDef.
        pCommon->CommonGetTypeRefProps(trNester,
                                       &szNamespace,
                                       &szName,
                                       &tkResolutionScope);

        // Update the dynamic arrays.
        ulNesters++;

        IfFailGo(cqaTrNesters.ReSize(ulNesters));
        cqaTrNesters[ulNesters-1] = trNester;

        IfFailGo(cqaNamespaces.ReSize(ulNesters));
        cqaNamespaces[ulNesters-1] = szNamespace;

        IfFailGo(cqaNames.ReSize(ulNesters));
        cqaNames[ulNesters-1] = szName;

        trNester = tkResolutionScope;
    }
ErrExit:
    return hr;
}   // HRESULT ImportHelper::GetTRNesterHierarchy()

//****************************************************************************
// Create the Nesting hierarchy given the array of TypeRef names.  The first
// TypeRef in the array is the innermost TypeRef.
//****************************************************************************
HRESULT ImportHelper::CreateNesterHierarchy(
    CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope to create the Nesters in.
    CQuickArray<LPCUTF8> &cqaNesterNamespaces,   // [IN] Array of Nester namespaces.
    CQuickArray<LPCUTF8> &cqaNesterNames,  // [IN] Array of Nester names.
    mdToken     tkResolutionScope,      // [IN] ResolutionScope for the innermost TypeRef.
    mdTypeRef   *ptr)                   // [OUT] Token for the innermost TypeRef.
{
    TypeRefRec  *pRecEmit;
    ULONG       iRecord;
    LPCUTF8     szName;
    LPCUTF8     szNamespace;
    mdTypeRef   trNester;
    mdTypeRef   trCur;
    ULONG       ulNesters;
    HRESULT     hr = S_OK;

    _ASSERTE(cqaNesterNames.Size() == cqaNesterNamespaces.Size() &&
             cqaNesterNames.Size());

    // Initialize the output parameter.
    *ptr = mdTypeRefNil;

    // Get count of Nesters in the hierarchy.
    ulNesters = (ULONG)cqaNesterNames.Size();

    // For each nester try to find the corresponding TypeRef in the emit scope.
    // For the outermost TypeRef, ResolutionScope is what's passed in.
    if (tkResolutionScope == mdTokenNil)
        trNester = mdTypeRefNil;
    else
        trNester = tkResolutionScope;
    for (ULONG ulCurNester = ulNesters-1; ulCurNester != -1; ulCurNester--)
    {
        hr = FindTypeRefByName(pMiniMdEmit,
                               trNester,
                               cqaNesterNamespaces[ulCurNester],
                               cqaNesterNames[ulCurNester],
                               &trCur);
        if (hr == CLDB_E_RECORD_NOTFOUND)
            break;
        else
            IfFailGo(hr);
        trNester = trCur;
    }
    if (SUCCEEDED(hr))
        *ptr = trNester;
    else if ( hr == CLDB_E_RECORD_NOTFOUND )
    {
        // Create TypeRef records for the part of the hierarchy for which
        // TypeRefs are not already present.
        for (;ulCurNester != -1; ulCurNester--)
        {
            szName = cqaNesterNames[ulCurNester];
            szNamespace = cqaNesterNamespaces[ulCurNester];

            IfNullGo(pRecEmit = pMiniMdEmit->AddTypeRefRecord(&iRecord));
            if (szNamespace && szNamespace[0] != '\0')
            {
                // only put the namespace if it is not an empty string and not NULL
                IfFailGo(pMiniMdEmit->PutString(TBL_TypeRef, TypeRefRec::COL_Namespace,
                                                pRecEmit, szNamespace));
            }
            IfFailGo(pMiniMdEmit->PutString(TBL_TypeRef, TypeRefRec::COL_Name,
                                            pRecEmit, szName));
            IfFailGo(pMiniMdEmit->PutToken(TBL_TypeRef,
                        TypeRefRec::COL_ResolutionScope, pRecEmit, trNester));
            
            trNester = TokenFromRid(iRecord, mdtTypeRef);
            IfFailGo(pMiniMdEmit->UpdateENCLog(trNester));
            
            // Hash the name.
            IfFailGo(pMiniMdEmit->AddNamedItemToHash(TBL_TypeRef, trNester, szName, 0));
        }
        *ptr = trNester;
    }
    else
        IfFailGo(hr);
ErrExit:
    return hr;
}   // HRESULT ImportHelper::CreateNesterHierarchy()

//****************************************************************************
// Given the arrays of names and namespaces for the Nested Type hierarchy,
// find the innermost TypeRef token.  The arrays start with the innermost
// TypeRefs and go outwards.
//****************************************************************************
HRESULT ImportHelper::FindNestedTypeRef(
    CMiniMdRW   *pMiniMd,               // [IN] Scope in which to find the TypeRef.
    CQuickArray<LPCUTF8> &cqaNesterNamespaces,  // [IN] Array of Names.
    CQuickArray<LPCUTF8> &cqaNesterNames,   // [IN] Array of Namespaces.
    mdToken     tkResolutionScope,      // [IN] Resolution scope for the outermost TypeRef.
    mdTypeRef   *ptr)                   // [OUT] Inner most TypeRef token.
{
    ULONG       ulNesters;
    ULONG       ulCurNester;
    HRESULT     hr = S_OK;

    _ASSERTE(cqaNesterNames.Size() == cqaNesterNamespaces.Size() &&
             cqaNesterNames.Size());

    // Set the output parameter to Nil token.
    *ptr = mdTokenNil;

    // Get count in the hierarchy, the give TypeDef included.
    ulNesters = (ULONG)cqaNesterNames.Size();

    // For each nester try to find the corresponding TypeRef in
    // the emit scope.  For the outermost TypeDef enclosing class is Nil.
    for (ulCurNester = ulNesters-1; ulCurNester != -1; ulCurNester--)
    {
        IfFailGo(FindTypeRefByName(pMiniMd,
                                   tkResolutionScope,
                                   cqaNesterNamespaces[ulCurNester],
                                   cqaNesterNames[ulCurNester],
                                   &tkResolutionScope));
    }
    *ptr = tkResolutionScope;
ErrExit:
    return hr;
}   // HRESULT ImportHelper::FindNestedTypeRef()


//****************************************************************************
// Given the arrays of names and namespaces for the Nested Type hierarchy,
// find the innermost TypeDef token.  The arrays start with the innermost
// TypeDef and go outwards.
//****************************************************************************
HRESULT ImportHelper::FindNestedTypeDef(
    CMiniMdRW   *pMiniMd,               // [IN] Scope in which to find the TypeRef.
    CQuickArray<LPCUTF8> &cqaNesterNamespaces,   // [IN] Array of Namespaces.
    CQuickArray<LPCUTF8> &cqaNesterNames,    // [IN] Array of Names.
    mdTypeDef   tdNester,               // [IN] Enclosing class for the Outermost TypeDef.
    mdTypeDef   *ptd)                   // [OUT] Inner most TypeRef token.
{
    ULONG       ulNesters;
    ULONG       ulCurNester;
    HRESULT     hr = S_OK;

    _ASSERTE(cqaNesterNames.Size() == cqaNesterNamespaces.Size() &&
             cqaNesterNames.Size());

    // Set the output parameter to Nil token.
    *ptd = mdTokenNil;

    // Get count in the hierarchy, the give TypeDef included.
    ulNesters = (ULONG)cqaNesterNames.Size();

    // For each nester try to find the corresponding TypeRef in
    // the emit scope.  For the outermost TypeDef enclosing class is Nil.
    for (ulCurNester = ulNesters-1; ulCurNester != -1; ulCurNester--)
    {
        IfFailGo(FindTypeDefByName(pMiniMd,
                                   cqaNesterNamespaces[ulCurNester],
                                   cqaNesterNames[ulCurNester],
                                   tdNester,
                                   &tdNester));
    }
    *ptd = tdNester;
ErrExit:
    return hr;
}   // HRESULT ImportHelper::FindNestedTypeDef()



//****************************************************************************
// Given the TypeDef and the corresponding assembly and module import scopes,
// create a corresponding TypeRef in the given emit scope.
//****************************************************************************
HRESULT ImportHelper::ImportTypeDef(
    CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Assembly emit scope.
    CMiniMdRW   *pMiniMdEmit,           // [IN] Module emit scope.
    IMetaModelCommon *pCommonAssemImport, // [IN] Assembly import scope.
    const void  *pbHashValue,           // [IN] Hash value for import assembly.
    ULONG       cbHashValue,            // [IN] Size in bytes of hash value.
    IMetaModelCommon *pCommonImport,    // [IN] Module import scope.
    mdTypeDef   tdImport,               // [IN] Imported TypeDef.
    bool        bReturnTd,              // [IN] If the import and emit scopes are identical, return the TypeDef.
    mdToken     *ptkType)               // [OUT] Output token for the imported type in the emit scope.
{
    CQuickArray<mdTypeDef>  cqaNesters;
    CQuickArray<LPCUTF8> cqaNesterNames;
    CQuickArray<LPCUTF8> cqaNesterNamespaces;
    GUID        nullguid = GUID_NULL;
    GUID        *pMvidAssemImport = &nullguid;
    GUID        *pMvidAssemEmit = &nullguid;
    GUID        *pMvidImport = &nullguid;
    GUID        *pMvidEmit = &nullguid;
    GUID        GuidImport = GUID_NULL;
    LPCUTF8     szModuleImport;
    mdToken     tkOuterRes = mdTokenNil;
    HRESULT     hr = S_OK;
    BOOL        bBCL = false;

    _ASSERTE(pMiniMdEmit && pCommonImport && ptkType);
    _ASSERTE(TypeFromToken(tdImport) == mdtTypeDef && tdImport != mdTypeDefNil);

    // Get MVIDs for import and emit, assembly and module scopes.
    if (pCommonAssemImport)
        pCommonAssemImport->CommonGetScopeProps(0, &pMvidAssemImport);
    pCommonImport->CommonGetScopeProps(&szModuleImport, &pMvidImport);
    if (pMiniMdAssemEmit)
        static_cast<IMetaModelCommon*>(pMiniMdAssemEmit)->CommonGetScopeProps(0, &pMvidAssemEmit);
    static_cast<IMetaModelCommon*>(pMiniMdEmit)->CommonGetScopeProps(0, &pMvidEmit);

    if (pCommonAssemImport == NULL && strcmp(szModuleImport, COM_RUNTIME_LIBRARY) == 0) 
    {
        HRESULT         hr;                     // A result.
        const BYTE      *pBlob;                 // Blob with dispid.
        ULONG           cbBlob;                 // Length of blob.
        WCHAR           wzBlob[40];             // Wide char format of guid.
        int             ix;                     // Loop control.

        hr = pCommonImport->CommonGetCustomAttributeByName(1, INTEROP_GUID_TYPE, (const void **)&pBlob, &cbBlob);
        if (hr != S_FALSE)
        {
            // Should be in format.  Total length == 41
            // <0x0001><0x24>01234567-0123-0123-0123-001122334455<0x0000>
            if ((cbBlob == 41) || (*(USHORT*)pBlob == 1))
            {
                for (ix=1; ix<=36; ++ix)
                    wzBlob[ix] = pBlob[ix+2];
                wzBlob[0] = '{';
                wzBlob[37] = '}';
                wzBlob[38] = 0;
                hr = IIDFromString(wzBlob, &GuidImport);
            }
        }
        bBCL = (GuidImport == LIBID_ComPlusRuntime);
    }

    // Compute the ResolutionScope for the imported type.
    if (bBCL)
    {
        // This is the case that we are referring to mscorlib.dll but client does not provide the manifest for
        // mscorlib.dll!! Do not generate ModuleRef to the mscorlib.dll. But instead we should just leave the
        // ResolutionScope empty
        tkOuterRes = mdTokenNil;
    }
    else if (*pMvidAssemImport == *pMvidAssemEmit && *pMvidImport == *pMvidEmit)
    {
        // The TypeDef is in the same Assembly and the Same scope.
        if (bReturnTd)
        {
            *ptkType = tdImport;
            goto ErrExit;
        }
        else
            tkOuterRes = TokenFromRid(1, mdtModule);
    }
    else if (*pMvidAssemImport == *pMvidAssemEmit && *pMvidImport != *pMvidEmit)
    {
        // The TypeDef is in the same Assembly but a different module.
        
        // Create a ModuleRef corresponding to the import scope.
        IfFailGo(CreateModuleRefFromScope(pMiniMdEmit, pCommonImport, &tkOuterRes));
    }
    else if (*pMvidAssemImport != *pMvidAssemEmit)
    {
        if (pCommonAssemImport)
        {
            // The TypeDef is from a different Assembly.

            // Import and Emit scopes can't be identical and be from different
            // Assemblies at the same time.
            _ASSERTE(*pMvidImport != *pMvidEmit &&
                     "Import scope can't be identical to the Emit scope and be from a different Assembly at the same time.");

            _ASSERTE(pCommonAssemImport);

            // Create an AssemblyRef corresponding to the import scope.
            IfFailGo(CreateAssemblyRefFromAssembly(pMiniMdAssemEmit,
                                                   pMiniMdEmit,
                                                   pCommonAssemImport,
                                                   pbHashValue,
                                                   cbHashValue,
                                                   &tkOuterRes));
        }
        else
        {
            // @FUTURE: review this fix! We may want to return error in the future.
            // This is to enable smc to reference mscorlib.dll while it does not have the manifest for mscorlib.dll opened.
            // Create a Nil ResolutionScope to the TypeRef.
            tkOuterRes = mdTokenNil;
        }
    }

    // Get the nesting hierarchy for the Type from the import scope and create
    // the corresponding Type hierarchy in the emit scope.  Note that the non-
    // nested class case simply folds into this scheme.

    IfFailGo(GetNesterHierarchy(pCommonImport,
                                tdImport,
                                cqaNesters,
                                cqaNesterNamespaces,
                                cqaNesterNames));

    IfFailGo(CreateNesterHierarchy(pMiniMdEmit,
                                   cqaNesterNamespaces,
                                   cqaNesterNames,
                                   tkOuterRes,
                                   ptkType));
ErrExit:
    return hr;
}   // HRESULT ImportHelper::ImportTypeDef()

//****************************************************************************
// Given the TypeRef and the corresponding assembly and module import scopes,
// return the corresponding token in the given emit scope.
// @FUTURE:  Should we look at visibility flags on ExportedTypes and TypeDefs when
// handling references across Assemblies?
//****************************************************************************
HRESULT ImportHelper::ImportTypeRef(
    CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Assembly emit scope.
    CMiniMdRW   *pMiniMdEmit,           // [IN] Module emit scope.
    IMetaModelCommon *pCommonAssemImport, // [IN] Assembly import scope.
    const void  *pbHashValue,           // [IN] Hash value for import assembly.
    ULONG       cbHashValue,            // [IN] Size in bytes of hash value.
    IMetaModelCommon *pCommonImport,    // [IN] Module import scope.
    mdTypeRef   trImport,               // [IN] Imported TypeRef.
    mdToken     *ptkType)               // [OUT] Output token for the imported type in the emit scope.
{
    CQuickArray<mdTypeDef>  cqaNesters;
    CQuickArray<LPCUTF8> cqaNesterNames;
    CQuickArray<LPCUTF8> cqaNesterNamespaces;
    LPCUTF8     szScopeNameEmit;
    GUID        nullguid = GUID_NULL;
    GUID        *pMvidAssemImport = &nullguid;
    GUID        *pMvidAssemEmit = &nullguid;
    GUID        *pMvidImport = &nullguid;
    GUID        *pMvidEmit = &nullguid;
    mdToken     tkOuterImportRes;               // ResolutionScope for the outermost TypeRef in import scope.
    mdToken     tkOuterEmitRes = mdTokenNil;    // ResolutionScope for outermost TypeRef in emit scope.
    HRESULT     hr = S_OK;
    bool        bAssemblyRefFromAssemScope = false;

    _ASSERTE(pMiniMdEmit && pCommonImport && ptkType);
    _ASSERTE(TypeFromToken(trImport) == mdtTypeRef);

    // Get MVIDs for import and emit, assembly and module scopes.
    if (pCommonAssemImport)
        pCommonAssemImport->CommonGetScopeProps(0, &pMvidAssemImport);
    pCommonImport->CommonGetScopeProps(0, &pMvidImport);
    if (pMiniMdAssemEmit)
    {
        static_cast<IMetaModelCommon*>(pMiniMdAssemEmit)->CommonGetScopeProps(
                                            0, &pMvidAssemEmit);
    }
    static_cast<IMetaModelCommon*>(pMiniMdEmit)->CommonGetScopeProps(
                                            &szScopeNameEmit, &pMvidEmit);

    // Get the outermost resolution scope for the TypeRef being imported.
    IfFailGo(GetNesterHierarchy(pCommonImport,
                                trImport,
                                cqaNesters,
                                cqaNesterNamespaces,
                                cqaNesterNames));
    pCommonImport->CommonGetTypeRefProps(cqaNesters[cqaNesters.Size() - 1],
                                         0, 0, &tkOuterImportRes);

    // Compute the ResolutionScope for the imported type.
    if (*pMvidAssemImport == *pMvidAssemEmit && *pMvidImport == *pMvidEmit)
    {
        *ptkType = trImport;
        goto ErrExit;
    }
    else if (*pMvidAssemImport == *pMvidAssemEmit && *pMvidImport != *pMvidEmit)
    {
        // The TypeRef is in the same Assembly but a different module.

        if (IsNilToken(tkOuterImportRes))
        {
            tkOuterEmitRes = tkOuterImportRes;
        }
        else if (TypeFromToken(tkOuterImportRes) == mdtModule)
        {
            // TypeRef resolved to the import module in which its defined.

            // This is a work around for 1421 integration to enable IJW linker scenario.
            // We are encountering a problem while compiling ISymWrapper where _GUID is locally
            // defined in the ISymWrapper.obj file and CoCreateInstance MemberRef has a reference to it.
            // When IJW comes in and try to do translateSigWithScope, we create a new TypeRef with ModuleRef
            // record which module ref name is to an empty string!! Upon merger, these two TypeRefs do not
            // seem to be identical to Merger!! This breaks our build!
            //
            if (pMiniMdAssemEmit == NULL && pCommonAssemImport == NULL)
            {
                tkOuterEmitRes = TokenFromRid(1, mdtModule);
            }
            else
            {
                // Create a ModuleRef corresponding to the import scope.
                IfFailGo(CreateModuleRefFromScope(pMiniMdEmit,
                                                  pCommonImport,
                                                  &tkOuterEmitRes));
            }
        }
        else if (TypeFromToken(tkOuterImportRes) == mdtAssemblyRef)
        {
            // TypeRef is from a different Assembly.

            // Create a corresponding AssemblyRef in the emit scope.
            IfFailGo(CreateAssemblyRefFromAssemblyRef(pMiniMdAssemEmit,
                                                      pMiniMdEmit,
                                                      pCommonImport,
                                                      tkOuterImportRes,
                                                      &tkOuterEmitRes));
        }
        else if (TypeFromToken(tkOuterImportRes) == mdtModuleRef)
        {
            // Get Name of the ModuleRef.
            LPCUTF8     szMRName;
            pCommonImport->CommonGetModuleRefProps(tkOuterImportRes, &szMRName);

            if (!strcmp(szMRName, szScopeNameEmit))
            {
                // ModuleRef from import scope resolves to the emit scope.
                tkOuterEmitRes = TokenFromRid(1, mdtModule);
            }
            else
            {
                // ModuleRef does not correspond to the emit scope.
                // Create a corresponding ModuleRef.
                IfFailGo(CreateModuleRefFromModuleRef(pMiniMdEmit,
                                                      pCommonImport,
                                                      tkOuterImportRes,
                                                      &tkOuterEmitRes));
            }
        }
    }
    else if (*pMvidAssemImport != *pMvidAssemEmit)
    {
        // The TypeDef is from a different Assembly.

        // Import and Emit scopes can't be identical and be from different
        // Assemblies at the same time.
        _ASSERTE(*pMvidImport != *pMvidEmit &&
                 "Import scope can't be identical to the Emit scope and be from a different Assembly at the same time.");

        mdToken     tkImplementation;       // Implementation token for ExportedType.
        if (IsNilToken(tkOuterImportRes))
        {
            // BUG FIX:: URT 13626
            // Well, before all of the clients generate AR for mscorlib.dll reference, it is not true
            // that tkOuterImportRes == nil will imply that we have to find such an entry in the import manifest!!

            // Look for a ExportedType entry in the import Assembly.  Its an error
            // if we don't find a ExportedType entry.
            mdExportedType   tkExportedType;
            hr = pCommonAssemImport->CommonFindExportedType(
                                    cqaNesterNamespaces[cqaNesters.Size() - 1],
                                    cqaNesterNames[cqaNesters.Size() - 1],
                                    mdTokenNil,
                                    &tkExportedType);
            if (SUCCEEDED(hr))
            {
                pCommonAssemImport->CommonGetExportedTypeProps(tkExportedType, 0, 0, &tkImplementation);
                if (TypeFromToken(tkImplementation) == mdtFile)
                {
                    // Type is from a different Assembly.
                    IfFailGo(CreateAssemblyRefFromAssembly(pMiniMdAssemEmit,
                                                           pMiniMdEmit,
                                                           pCommonAssemImport,
                                                           pbHashValue,
                                                           cbHashValue,
                                                           &tkOuterEmitRes));
                }
                else if (TypeFromToken(tkImplementation) == mdtAssemblyRef)
                {
                    // This folds into the case where the Type is AssemblyRef.  So
                    // let it fall through to that case.         

                    // Remember that this AssemblyRef token is actually from the Manifest scope not
                    // the module scope!!!
                    bAssemblyRefFromAssemScope = true;
                    tkOuterImportRes = tkImplementation;
                }
                else
                    _ASSERTE(!"Unexpected ExportedType implementation token.");
            }
            else
            {
                // In this case, we will just move over the TypeRef with Nil ResolutionScope.
                hr = NOERROR;
                tkOuterEmitRes = mdTokenNil;
            }
        }
        else if (TypeFromToken(tkOuterImportRes) == mdtModule)
        {
            // Type is from a different Assembly.
            IfFailGo(CreateAssemblyRefFromAssembly(pMiniMdAssemEmit,
                                                   pMiniMdEmit,
                                                   pCommonAssemImport,
                                                   pbHashValue,
                                                   cbHashValue,
                                                   &tkOuterEmitRes));
        }
        // Not else if, because mdtModule case above could change
        // tkOuterImportRes to an AssemblyRef.
        if (TypeFromToken(tkOuterImportRes) == mdtAssemblyRef)
        {
            // If there is an emit assembly, see if the import assembly ref points to 
            //  it.  If there is no emit assembly, the import assembly, by definition,
            //  does not point to this one.
            if (pMiniMdAssemEmit == NULL  || !pMiniMdAssemEmit->getCountAssemblys())
                hr = S_FALSE;
            else
            {
                if (bAssemblyRefFromAssemScope)
                {
                    // Check to see if the AssemblyRef resolves to the emit assembly.
                    IfFailGo(CompareAssemblyRefToAssembly(pCommonAssemImport,
                                                          tkOuterImportRes,
                                    static_cast<IMetaModelCommon*>(pMiniMdAssemEmit)));

                }
                else
                {
                    // Check to see if the AssemblyRef resolves to the emit assembly.
                    IfFailGo(CompareAssemblyRefToAssembly(pCommonImport,
                                                          tkOuterImportRes,
                                    static_cast<IMetaModelCommon*>(pMiniMdAssemEmit)));
                }
            }
            if (hr == S_OK)
            {
                // The TypeRef being imported is defined in the current Assembly.

                // Find the ExportedType for the outermost TypeRef in the Emit assembly.
                mdExportedType   tkExportedType;

                hr = FindExportedType(pMiniMdAssemEmit,
                                 cqaNesterNamespaces[cqaNesters.Size() - 1],
                                 cqaNesterNames[cqaNesters.Size() - 1],
                                 mdTokenNil,    // Enclosing ExportedType.
                                 &tkExportedType);
                if (hr == S_OK)
                {
                    // Create a ModuleRef based on the File name for the ExportedType.
                    // If the ModuleRef corresponds to pMiniMdEmit, the function
                    // will return S_FALSE, in which case set tkOuterEmitRes to
                    // the Module token.
                    hr = CreateModuleRefFromExportedType(pMiniMdEmit,
                                                    pMiniMdAssemEmit,
                                                    tkExportedType,
                                                    &tkOuterEmitRes);
                    if (hr == S_FALSE)
                        tkOuterEmitRes = TokenFromRid(1, mdtModule);
                    else
                        IfFailGo(hr);
                }
                else if (hr == CLDB_E_RECORD_NOTFOUND)
                {
                    // Find the Type in the Assembly emit scope to cover the
                    // case where ExportedTypes may be implicitly defined.  Its an
                    // error if we can't find the Type at this point.
                    IfFailGo(FindTypeDefByName(pMiniMdAssemEmit,
                                               cqaNesterNamespaces[cqaNesters.Size() - 1],
                                               cqaNesterNames[cqaNesters.Size() - 1],
                                               mdTokenNil,  // Enclosing Type.
                                               &tkOuterEmitRes));
                    tkOuterEmitRes = TokenFromRid(1, mdtModule);
                }
                else
                {
                    _ASSERTE(FAILED(hr));
                    IfFailGo(hr);
                }
            }
            else if (hr == S_FALSE)
            {
                // The TypeRef being imported is from a different Assembly.

                if (bAssemblyRefFromAssemScope)
                {
                    // Create a corresponding AssemblyRef.
                    IfFailGo(CreateAssemblyRefFromAssemblyRef(pMiniMdAssemEmit,
                                                              pMiniMdEmit,
                                                              pCommonAssemImport,
                                                              tkOuterImportRes,
                                                              &tkOuterEmitRes));
                }
                else
                {
                    // Create a corresponding AssemblyRef.
                    IfFailGo(CreateAssemblyRefFromAssemblyRef(pMiniMdAssemEmit,
                                                              pMiniMdEmit,
                                                              pCommonImport,
                                                              tkOuterImportRes,
                                                              &tkOuterEmitRes));
                }
            }
            else
            {
                _ASSERTE(FAILED(hr));
                IfFailGo(hr);
            }
        }
        else if (TypeFromToken(tkOuterImportRes) == mdtModuleRef)
        {
            // Type is from a different Assembly.
            IfFailGo(CreateAssemblyRefFromAssembly(pMiniMdAssemEmit,
                                                   pMiniMdEmit,
                                                   pCommonAssemImport,
                                                   pbHashValue,
                                                   cbHashValue,
                                                   &tkOuterEmitRes));
        }
    }

    // Try to find the TypeDef in the emit scope. If we cannot find the
    // typedef, we need to introduce a typeref.

    // See if the Nested TypeDef is present in the Emit scope.
    hr = CLDB_E_RECORD_NOTFOUND;
    if (TypeFromToken(tkOuterEmitRes) == mdtModule && !IsNilToken(tkOuterEmitRes))
    {
        hr = FindNestedTypeDef(pMiniMdEmit,
                               cqaNesterNamespaces,
                               cqaNesterNames,
                               mdTokenNil,
                               ptkType);

        // cannot assert now!! Due to the IJW hack!
        // _ASSERTE(SUCCEEDED(hr));
    }

    if (hr == CLDB_E_RECORD_NOTFOUND)
    {
        IfFailGo(CreateNesterHierarchy(pMiniMdEmit,
                                       cqaNesterNamespaces,
                                       cqaNesterNames,
                                       tkOuterEmitRes,
                                       ptkType));
    }
    else
        IfFailGo(hr);
ErrExit:
    return hr;
}   // HRESULT ImportHelper::ImportTypeRef()

//******************************************************************************
// Given import scope, create a corresponding ModuleRef.
//******************************************************************************
HRESULT ImportHelper::CreateModuleRefFromScope( // S_OK or error.
    CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope in which the ModuleRef is to be created.
    IMetaModelCommon *pCommonImport,    // [IN] Import scope.
    mdModuleRef *ptkModuleRef)          // [OUT] Output token for ModuleRef.
{
    HRESULT     hr = S_OK;
    LPCSTR      szName;
    ModuleRefRec *pRecordEmit;
    RID         iRecordEmit;

    // Set output to nil.
    *ptkModuleRef = mdTokenNil;

    // Get name of import scope.
    pCommonImport->CommonGetScopeProps(&szName, 0);

    // See if the ModuleRef exists in the Emit scope.
    hr = FindModuleRef(pMiniMdEmit, szName, ptkModuleRef);

    if (hr == CLDB_E_RECORD_NOTFOUND)
    {
        if (szName[0] == '\0')
        {
            // It the referenced Module does not have a proper name, use the nil token instead.
            LOG((LOGMD, "WARNING!!! MD ImportHelper::CreatemoduleRefFromScope but scope does not have a proper name!!!!"));

            // clear the error
            hr = NOERROR;

            // It is a bug to create an ModuleRef to an empty name!!!
            *ptkModuleRef = mdTokenNil;
        }
        else
        {
            // Create ModuleRef record and set the output parameter.
            IfNullGo(pRecordEmit = pMiniMdEmit->AddModuleRefRecord(&iRecordEmit));
            *ptkModuleRef = TokenFromRid(iRecordEmit, mdtModuleRef);
            IfFailGo(pMiniMdEmit->UpdateENCLog(*ptkModuleRef));

            // It is a bug to create an ModuleRef to mscorlib.dll
            _ASSERTE(strcmp(szName, COM_RUNTIME_LIBRARY) != 0);

            // Set the name of ModuleRef.
            IfFailGo(pMiniMdEmit->PutString(TBL_ModuleRef, ModuleRefRec::COL_Name,
                                                  pRecordEmit, szName));
        }
    }
    else
        IfFailGo(hr);
ErrExit:
    return hr;
}   // HRESULT ImportHelper::CreateModuleRefFromScope()


//******************************************************************************
// Given an import scope and a ModuleRef, create a corresponding ModuleRef in
// the given emit scope.
//******************************************************************************
HRESULT ImportHelper::CreateModuleRefFromModuleRef(    // S_OK or error.
    CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope.
    IMetaModelCommon *pCommon,              // [IN] Import scope.
    mdModuleRef tkModuleRef,            // [IN] ModuleRef token.
    mdModuleRef *ptkModuleRef)          // [OUT] ModuleRef token in the emit scope.
{
    HRESULT     hr = S_OK;
    LPCSTR      szName;
    ModuleRefRec *pRecord;
    RID         iRecord;

    // Set output to Nil.
    *ptkModuleRef = mdTokenNil;

    // Get name of the ModuleRef being imported.
    pCommon->CommonGetModuleRefProps(tkModuleRef, &szName);

    // See if the ModuleRef exist in the Emit scope.
    hr = FindModuleRef(pMiniMdEmit, szName, ptkModuleRef);

    if (hr == CLDB_E_RECORD_NOTFOUND)
    {
        // Create ModuleRef record and set the output parameter.
        IfNullGo(pRecord = pMiniMdEmit->AddModuleRefRecord(&iRecord));
        *ptkModuleRef = TokenFromRid(iRecord, mdtModuleRef);
        IfFailGo(pMiniMdEmit->UpdateENCLog(*ptkModuleRef));

        // Set the name of ModuleRef.
        IfFailGo(pMiniMdEmit->PutString(TBL_ModuleRef, ModuleRefRec::COL_Name,
                                              pRecord, szName));
    }
    else
        IfFailGo(hr);
ErrExit:
    return hr;
}   // HRESULT ImportHelper::CreateModuleRefFromModuleRef()


//******************************************************************************
// Given a ExportedType and the Assembly emit scope, create a corresponding ModuleRef
// in the give emit scope.  The ExportedType being passed in must belong to the
// Assembly passed in.  Function returns S_FALSE if the ExportedType is implemented
// by the emit scope passed in.
//******************************************************************************
HRESULT ImportHelper::CreateModuleRefFromExportedType(  // S_OK or error.
    CMiniMdRW   *pAssemEmit,            // [IN] Import assembly scope.
    CMiniMdRW   *pMiniMdEmit,           // [IN] Emit scope.
    mdExportedType   tkExportedType,              // [IN] ExportedType token in Assembly emit scope.
    mdModuleRef *ptkModuleRef)          // [OUT] ModuleRef token in the emit scope.
{
    mdFile      tkFile;
    LPCUTF8     szFile;
    LPCUTF8     szScope;
    FileRec     *pFileRec;
    HRESULT     hr = S_OK;

    // Set output to nil.
    *ptkModuleRef = mdTokenNil;

    // Get the implementation token for the ExportedType.  It must be a File token
    // since the caller should call this function only on ExportedTypes that resolve
    // to the same Assembly.
    static_cast<IMetaModelCommon*>(pAssemEmit)->
                            CommonGetExportedTypeProps(tkExportedType, 0, 0, &tkFile);
    _ASSERTE(TypeFromToken(tkFile) == mdtFile);

    // Get the name of the file.
    pFileRec = pAssemEmit->getFile(RidFromToken(tkFile));
    szFile = pAssemEmit->getNameOfFile(pFileRec);

    // Get the name of the emit scope.
    static_cast<IMetaModelCommon*>(pMiniMdEmit)->
                            CommonGetScopeProps(&szScope, 0);

    // If the file corresponds to the emit scope, return S_FALSE;
    if (!strcmp(szFile, szScope))
        return S_FALSE;

    // See if a ModuleRef exists with this name.
    hr = FindModuleRef(pMiniMdEmit, szFile, ptkModuleRef);

    if (hr == CLDB_E_RECORD_NOTFOUND)
    {
        // Create ModuleRef record and set the output parameter.

        ModuleRefRec    *pRecord;
        RID             iRecord;

        IfNullGo(pRecord = pMiniMdEmit->AddModuleRefRecord(&iRecord));
        *ptkModuleRef = TokenFromRid(iRecord, mdtModuleRef);
        IfFailGo(pMiniMdEmit->UpdateENCLog(*ptkModuleRef));

        // Set the name of ModuleRef.
        IfFailGo(pMiniMdEmit->PutString(TBL_ModuleRef, ModuleRefRec::COL_Name,
                                              pRecord, szFile));
    }
    else
        IfFailGo(hr);
ErrExit:
    return hr;
}   // HRESULT ImportHelper::CreateModuleRefFromExportedType()


//******************************************************************************
// Given the Assembly Import scope, hash value and execution location, create
// a corresponding AssemblyRef in the given assembly and module emit scope.
// Set the output parameter to the AssemblyRef token emitted in the module emit
// scope.
//******************************************************************************
HRESULT ImportHelper::CreateAssemblyRefFromAssembly( // S_OK or error.
    CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Emit assembly scope.
    CMiniMdRW   *pMiniMdModuleEmit,     // [IN] Emit module scope.
    IMetaModelCommon *pCommonAssemImport, // [IN] Assembly import scope.
    const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
    ULONG           cbHashValue,                // [IN] Count of bytes.
    mdAssemblyRef *ptkAssemblyRef)      // [OUT] AssemblyRef token.
{
    AssemblyRefRec *pRecordEmit;
    CMiniMdRW   *rMiniMdRW[2];
    CMiniMdRW   *pMiniMdEmit;
    RID         iRecordEmit;
    USHORT      usMajorVersion;
    USHORT      usMinorVersion;
    USHORT      usBuildNumber;
    USHORT      usRevisionNumber;
    DWORD       dwFlags;
    const void  *pbPublicKey;
    ULONG       cbPublicKey;
    LPCUTF8     szName;
    LPCUTF8     szLocale;
    mdAssemblyRef tkAssemRef;
    HRESULT     hr = S_OK;
    const void  *pbToken = NULL;
    ULONG       cbToken = 0;
    ULONG       i;

    // Set output to Nil.
    *ptkAssemblyRef = mdTokenNil;

    // Get the Assembly props.
    pCommonAssemImport->CommonGetAssemblyProps(&usMajorVersion, &usMinorVersion,
                                               &usBuildNumber, &usRevisionNumber,
                                               &dwFlags, &pbPublicKey, &cbPublicKey,
                                               &szName, &szLocale);

    // Compress the public key into a token.
    if ((pbPublicKey != NULL) && (cbPublicKey != 0))
    {
        _ASSERTE(IsAfPublicKey(dwFlags));
        dwFlags &= ~afPublicKey;
        if (!StrongNameTokenFromPublicKey((BYTE*)pbPublicKey,
                                          cbPublicKey,
                                          (BYTE**)&pbToken,
                                          &cbToken))
            IfFailGo(StrongNameErrorInfo());
    }
    else
        _ASSERTE(!IsAfPublicKey(dwFlags));

    // Create the AssemblyRef in both the Assembly and Module emit scopes.
    rMiniMdRW[0] = pMiniMdAssemEmit;
    rMiniMdRW[1] = pMiniMdModuleEmit;

    for (i = 0; i < 2; i++)
    {
        pMiniMdEmit = rMiniMdRW[i];

        if (!pMiniMdEmit)
            continue;

        // See if the AssemblyRef already exists in the emit scope.
        hr = FindAssemblyRef(pMiniMdEmit, szName, szLocale, pbToken,
                             cbToken, usMajorVersion, usMinorVersion,
                             usBuildNumber, usRevisionNumber, dwFlags,
                             &tkAssemRef);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            // Create the AssemblyRef record and set the output parameter.
            IfNullGo(pRecordEmit = pMiniMdEmit->AddAssemblyRefRecord(&iRecordEmit));
            tkAssemRef = TokenFromRid(iRecordEmit, mdtAssemblyRef);
            IfFailGo(pMiniMdEmit->UpdateENCLog(tkAssemRef));

            // Set parameters derived from the import Assembly.
            pRecordEmit->m_MajorVersion     = usMajorVersion;
            pRecordEmit->m_MinorVersion     = usMinorVersion;
            pRecordEmit->m_BuildNumber      = usBuildNumber;
            pRecordEmit->m_RevisionNumber   = usRevisionNumber;
            pRecordEmit->m_Flags            = dwFlags;

            IfFailGo(pMiniMdEmit->PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_PublicKeyOrToken,
                                          pRecordEmit, pbToken, cbToken));
            IfFailGo(pMiniMdEmit->PutString(TBL_AssemblyRef, AssemblyRefRec::COL_Name,
                                          pRecordEmit, szName));
            IfFailGo(pMiniMdEmit->PutString(TBL_AssemblyRef, AssemblyRefRec::COL_Locale,
                                          pRecordEmit, szLocale));

            // Set the parameters passed in for the AssemblyRef.
            IfFailGo(pMiniMdEmit->PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_HashValue,
                                          pRecordEmit, pbHashValue, cbHashValue));
        }
        else
            IfFailGo(hr);

        // Set the output parameter for the AssemblyRef emitted in Module emit scope.
        if (i)
            *ptkAssemblyRef = tkAssemRef;
    }
ErrExit:
    if (pbToken)
        StrongNameFreeBuffer((BYTE*)pbToken);
    return hr;
}   // HRESULT ImportHelper::CreateAssemblyRefFromAssembly()


//******************************************************************************
// Given an AssemblyRef and the corresponding scope, compare it to see if it
// refers to the given Assembly.
//******************************************************************************
HRESULT ImportHelper::CompareAssemblyRefToAssembly(    // S_OK, S_FALSE or error.
    IMetaModelCommon *pCommonAssem1,    // [IN] Scope that defines the AssemblyRef.
    mdAssemblyRef tkAssemRef,           // [IN] AssemblyRef.
    IMetaModelCommon *pCommonAssem2)    // [IN] Assembly against which the Ref is compared.
{
    USHORT      usMajorVersion1;
    USHORT      usMinorVersion1;
    USHORT      usBuildNumber1;
    USHORT      usRevisionNumber1;
    const void  *pbPublicKeyOrToken1;
    ULONG       cbPublicKeyOrToken1;
    LPCUTF8     szName1;
    LPCUTF8     szLocale1;
    DWORD       dwFlags1;

    USHORT      usMajorVersion2;
    USHORT      usMinorVersion2;
    USHORT      usBuildNumber2;
    USHORT      usRevisionNumber2;
    const void  *pbPublicKey2;
    ULONG       cbPublicKey2;
    LPCUTF8     szName2;
    LPCUTF8     szLocale2;
    const void  *pbToken = NULL;
    ULONG       cbToken = 0;
    bool        fMatch;

    // Get the AssemblyRef props.
    pCommonAssem1->CommonGetAssemblyRefProps(tkAssemRef, &usMajorVersion1,
                                             &usMinorVersion1, &usBuildNumber1, 
                                             &usRevisionNumber1, &dwFlags1, &pbPublicKeyOrToken1,
                                             &cbPublicKeyOrToken1, &szName1, &szLocale1,
                                             0, 0);
    // Get the Assembly props.
    pCommonAssem2->CommonGetAssemblyProps(&usMajorVersion2, &usMinorVersion2,
                                          &usBuildNumber2, &usRevisionNumber2, 
                                          0, &pbPublicKey2, &cbPublicKey2,
                                          &szName2, &szLocale2);

    // Compare.
    if (usMajorVersion1 != usMajorVersion2 ||
        usMinorVersion1 != usMinorVersion2 ||
        usBuildNumber1 != usBuildNumber2 ||
        usRevisionNumber1 != usRevisionNumber2 ||
        strcmp(szName1, szName2) ||
        strcmp(szLocale1, szLocale2))
    {
        return S_FALSE;
    }

    // Defs always contain a full public key (or no key at all). Refs may have
    // no key, a full public key or a tokenized key.
    if ((cbPublicKeyOrToken1 && !cbPublicKey2) ||
        (!cbPublicKeyOrToken1 && cbPublicKey2))
        return S_FALSE;

    if (cbPublicKeyOrToken1)
    {
        // If ref contains a full public key we can just directly compare.
        if (IsAfPublicKey(dwFlags1) &&
            (cbPublicKeyOrToken1 != cbPublicKey2 ||
             memcmp(pbPublicKeyOrToken1, pbPublicKey2, cbPublicKeyOrToken1)))
            return FALSE;

        // Otherwise we need to compress the def public key into a token.
        if (!StrongNameTokenFromPublicKey((BYTE*)pbPublicKey2,
                                          cbPublicKey2,
                                          (BYTE**)&pbToken,
                                          &cbToken))
            return StrongNameErrorInfo();

        fMatch = cbPublicKeyOrToken1 == cbToken &&
            !memcmp(pbPublicKeyOrToken1, pbToken, cbPublicKeyOrToken1);

        StrongNameFreeBuffer((BYTE*)pbToken);

        if (!fMatch)
            return S_FALSE;
    }

    return S_OK;
}   // HRESULT ImportHelper::CompareAssemblyRefToAssembly()


//******************************************************************************
// Given an AssemblyRef and the corresponding scope, create an AssemblyRef in
// the given Module scope and Assembly scope.
//******************************************************************************
HRESULT ImportHelper::CreateAssemblyRefFromAssemblyRef(
    CMiniMdRW   *pMiniMdAssemEmit,      // [IN] Assembly emit scope.
    CMiniMdRW   *pMiniMdModuleEmit,     // [IN] Module emit scope
    IMetaModelCommon *pCommonImport,    // [IN] Scope to import the assembly ref from.
    mdAssemblyRef tkAssemRef,           // [IN] Assembly ref to be imported.
    mdAssemblyRef *ptkAssemblyRef)      // [OUT] AssemblyRef in the emit scope.
{
    AssemblyRefRec *pRecordEmit;
    CMiniMdRW   *rMiniMdRW[2];
    CMiniMdRW   *pMiniMdEmit;
    RID         iRecordEmit;
    USHORT      usMajorVersion;
    USHORT      usMinorVersion;
    USHORT      usBuildNumber;
    USHORT      usRevisionNumber;
    DWORD       dwFlags;
    const void  *pbPublicKeyOrToken;
    ULONG       cbPublicKeyOrToken;
    LPCUTF8     szName;
    LPCUTF8     szLocale;
    const void  *pbHashValue;
    ULONG       cbHashValue;
    HRESULT     hr = S_OK;

    // Set output to Nil.
    *ptkAssemblyRef = mdTokenNil;

    // Get import AssemblyRef props.
    pCommonImport->CommonGetAssemblyRefProps(tkAssemRef, &usMajorVersion,
                                             &usMinorVersion, &usBuildNumber,
                                             &usRevisionNumber, &dwFlags,
                                       &pbPublicKeyOrToken, &cbPublicKeyOrToken,
                                       &szName, &szLocale,
                                       &pbHashValue,
                                       &cbHashValue);

    // Create the AssemblyRef in both the Assembly and Module emit scopes.
    rMiniMdRW[0] = pMiniMdAssemEmit;
    rMiniMdRW[1] = pMiniMdModuleEmit;

    for (ULONG i = 0; i < 2; i++)
    {
        pMiniMdEmit = rMiniMdRW[i];

        if (!pMiniMdEmit)
            continue;

        // See if the AssemblyRef already exists in the emit scope.
        hr = FindAssemblyRef(pMiniMdEmit, szName, szLocale, pbPublicKeyOrToken,
                             cbPublicKeyOrToken, usMajorVersion, usMinorVersion,
                             usBuildNumber, usRevisionNumber, dwFlags, &tkAssemRef);
        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            // Create the AssemblyRef record and set the output parameter.
            IfNullGo(pRecordEmit = pMiniMdEmit->AddAssemblyRefRecord(&iRecordEmit));
            tkAssemRef = TokenFromRid(iRecordEmit, mdtAssemblyRef);
            IfFailGo(pMiniMdEmit->UpdateENCLog(tkAssemRef));

            // Set parameters derived from the import Assembly.
            pRecordEmit->m_MajorVersion     = usMajorVersion;
            pRecordEmit->m_MinorVersion     = usMinorVersion;
            pRecordEmit->m_BuildNumber      = usBuildNumber;
            pRecordEmit->m_RevisionNumber   = usRevisionNumber;
            pRecordEmit->m_Flags            = dwFlags;

            IfFailGo(pMiniMdEmit->PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_PublicKeyOrToken,
                                          pRecordEmit, pbPublicKeyOrToken, cbPublicKeyOrToken));
            IfFailGo(pMiniMdEmit->PutString(TBL_AssemblyRef, AssemblyRefRec::COL_Name,
                                          pRecordEmit, szName));
            IfFailGo(pMiniMdEmit->PutString(TBL_AssemblyRef, AssemblyRefRec::COL_Locale,
                                          pRecordEmit, szLocale));

            // Set the parameters passed in for the AssemblyRef.
            IfFailGo(pMiniMdEmit->PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_HashValue,
                                          pRecordEmit, pbHashValue, cbHashValue));
        }
        else
            IfFailGo(hr);

        // Set the output parameter for the AssemblyRef emitted in Module emit scope.
        if (i)
            *ptkAssemblyRef = tkAssemRef;
    }
ErrExit:
    return hr;
}   // HRESULT ImportHelper::CreateAssemblyRefFromAssemblyRef()
