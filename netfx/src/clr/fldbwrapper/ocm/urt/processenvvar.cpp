// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/////////////////////////////////////////////////////////////////////////////
// Module Name: processenvvar.cpp
//
// Abstract:
//    implementation of CProcessEnvVar methods (class for handling the environment variable changes)
//
// Author: a-mshn
//
// Notes:
//

#include "processenvvar.h"

// implementation of CProcessEnvVar methods
CProcessEnvVar::CProcessEnvVar( const TCHAR* pwz ) : m_strEnvVar(pwz), m_bEnvVarChanged(false)
{
    DWORD dwRet = ::GetEnvironmentVariable( m_strEnvVar.c_str(), NULL, 0 );
    
    UINT cBufSize = dwRet+1;
    TCHAR* pBuffer = new TCHAR[cBufSize];
    ::ZeroMemory( pBuffer, cBufSize * sizeof(TCHAR) );
    
    dwRet = ::GetEnvironmentVariable( m_strEnvVar.c_str(), pBuffer, cBufSize );
    assert( dwRet < cBufSize );
    
    m_strOrigData = pBuffer;
    m_strCurrentData = pBuffer;
    
    delete [] pBuffer;
}

// copy constructor
CProcessEnvVar::CProcessEnvVar( const CProcessEnvVar& procEnvVar )
{
    m_strOrigData = procEnvVar.m_strOrigData;
    m_strCurrentData = procEnvVar.m_strCurrentData;
    m_strEnvVar = procEnvVar.m_strEnvVar;
    m_bEnvVarChanged = procEnvVar.m_bEnvVarChanged;
}

// append the EnvVar
CProcessEnvVar& CProcessEnvVar::operator+=( const TCHAR* pwz )
{
    return this->Append(pwz);
}

// append: appends the EnvVar (the same as +=)
CProcessEnvVar& CProcessEnvVar::Append( const TCHAR* pwz )
{
    m_strCurrentData += pwz;
    SetEnvVar( m_strCurrentData.c_str() );
    return *this;
}

// prepends the EnvVar
CProcessEnvVar& CProcessEnvVar::Prepend( const TCHAR* pwz )
{
    m_strCurrentData.insert( 0, _T( ";" ) );
    m_strCurrentData.insert( 0, pwz );
    SetEnvVar( m_strCurrentData.c_str() );
    return *this;
}

// returns current EnvVar
const tstring& CProcessEnvVar::GetData( VOID ) const
{
    return m_strCurrentData;
}

// restores original EnvVar
BOOL CProcessEnvVar::RestoreOrigData( VOID )
{
    if (m_bEnvVarChanged)
    {
        return ::SetEnvironmentVariable( m_strEnvVar.c_str(), m_strOrigData.c_str() );
    }
    else
    {
        // path has not changed, nothing should be done
        return true;
    }
}

// set environment variable (helper method)
BOOL CProcessEnvVar::SetEnvVar( const TCHAR* pwz )
{
    m_bEnvVarChanged = true;
    return ::SetEnvironmentVariable( m_strEnvVar.c_str(), pwz );
}
