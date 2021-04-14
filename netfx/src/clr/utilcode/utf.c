/*++

Copyright (c) 1991-2000,  Microsoft Corporation  All rights reserved.

Module Name:

    utf.c

Abstract:

    This file contains functions that convert UTF strings to Unicode
    strings and Unicode string to UTF strings.

    External Routines found in this file:
      UTFCPInfo
      UTFToUnicode
      UnicodeToUTF

Revision History:

    02-06-96    JulieB    Created.
    03-20-99    SamerA    Surrogate support.
   
This file was copied from the Windows XP code base on 5/01/2002
to ensure the CLR has appropriate UTF-8 surrogate support and can
recognize invalid UTF-8 sequences on all platforms.  -- BrianGru
--*/



//
//  Include Files.
//

#include <ole2.h>
#define NlsStrLenW(x) lstrlenW(x)
#include "utf.h"




//
//  Forward Declarations.
//

int
UTF7ToUnicode(
    LPCSTR lpSrcStr,
    int cchSrc,
    LPWSTR lpDestStr,
    int cchDest);

int
UTF8ToUnicode(
    LPCSTR lpSrcStr,
    int cchSrc,
    LPWSTR lpDestStr,
    int cchDest,
    DWORD dwFlags);

int
UnicodeToUTF7(
    LPCWSTR lpSrcStr,
    int cchSrc,
    LPSTR lpDestStr,
    int cchDest);

int
UnicodeToUTF8(
    LPCWSTR lpSrcStr,
    int cchSrc,
    LPSTR lpDestStr,
    int cchDest);





//-------------------------------------------------------------------------//
//                           EXTERNAL ROUTINES                             //
//-------------------------------------------------------------------------//


////////////////////////////////////////////////////////////////////////////
//
//  UTFCPInfo
//
//  Gets the CPInfo for the given UTF code page.
//
//  10-23-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

BOOL UTFCPInfo(
    UINT CodePage,
    LPCPINFO lpCPInfo,
    BOOL fExVer)
{
    int ctr;


    //
    //  Invalid Parameter Check:
    //     - validate code page
    //     - lpCPInfo is NULL
    //
    if ( (CodePage < CP_UTF7) || (CodePage > CP_UTF8) ||
         (lpCPInfo == NULL) )
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return (0);
    }

    switch (CodePage)
    {
        case ( CP_UTF7 ) :
        {
            lpCPInfo->MaxCharSize = 5;
            break;
        }
        case ( CP_UTF8 ) :
        {
            lpCPInfo->MaxCharSize = 4;
            break;
        }
    }

    (lpCPInfo->DefaultChar)[0] = '?';
    (lpCPInfo->DefaultChar)[1] = (BYTE)0;

    for (ctr = 0; ctr < MAX_LEADBYTES; ctr++)
    {
        (lpCPInfo->LeadByte)[ctr] = (BYTE)0;
    }

    if (fExVer)
    {
        LPCPINFOEXW lpCPInfoEx = (LPCPINFOEXW)lpCPInfo;

        lpCPInfoEx->UnicodeDefaultChar = L'?';
        lpCPInfoEx->CodePage = CodePage;
    }

    return (TRUE);
}


////////////////////////////////////////////////////////////////////////////
//
//  UTFToUnicode
//
//  Maps a UTF character string to its wide character string counterpart.
//
//  02-06-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

int UTFToUnicode(
    UINT CodePage,
    DWORD dwFlags,
    LPCSTR lpMultiByteStr,
    int cbMultiByte,
    LPWSTR lpWideCharStr,
    int cchWideChar)
{
    int rc = 0;


    //
    //  Invalid Parameter Check:
    //     - validate code page
    //     - length of MB string is 0
    //     - wide char buffer size is negative
    //     - MB string is NULL
    //     - length of WC string is NOT zero AND
    //         (WC string is NULL OR src and dest pointers equal)
    //
    if ( (CodePage < CP_UTF7) || (CodePage > CP_UTF8) ||
         (cbMultiByte == 0) || (cchWideChar < 0) ||
         (lpMultiByteStr == NULL) ||
         ((cchWideChar != 0) &&
          ((lpWideCharStr == NULL) ||
           (lpMultiByteStr == (LPSTR)lpWideCharStr))) )
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return (0);
    }

    //
    //  Invalid Flags Check:
    //     - UTF7: flags not 0.
    //     - UTF8: flags not 0 nor MB_ERR_INVALID_CHARS.
    //
    if (CodePage == CP_UTF8) 
    {
        // UTF8        
        if ((dwFlags & ~MB_ERR_INVALID_CHARS) != 0)
        {
            SetLastError(ERROR_INVALID_FLAGS);
            return (0);
        }
    } 
    else if (dwFlags != 0)
    {
        // UTF7
        SetLastError(ERROR_INVALID_FLAGS);
        return (0);
    }

    //
    //  If cbMultiByte is -1, then the string is null terminated and we
    //  need to get the length of the string.  Add one to the length to
    //  include the null termination.  (This will always be at least 1.)
    //
    if (cbMultiByte <= -1)
    {
        cbMultiByte = strlen(lpMultiByteStr) + 1;
    }

    switch (CodePage)
    {
        case ( CP_UTF7 ) :
        {
            rc = UTF7ToUnicode( lpMultiByteStr,
                                cbMultiByte,
                                lpWideCharStr,
                                cchWideChar );
            break;
        }
        case ( CP_UTF8 ) :
        {
            rc = UTF8ToUnicode( lpMultiByteStr,
                                cbMultiByte,
                                lpWideCharStr,
                                cchWideChar,
                                dwFlags);
            break;
        }
    }

    return (rc);
}


////////////////////////////////////////////////////////////////////////////
//
//  UnicodeToUTF
//
//  Maps a Unicode character string to its UTF string counterpart.
//
//  02-06-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

int UnicodeToUTF(
    UINT CodePage,
    DWORD dwFlags,
    LPCWSTR lpWideCharStr,
    int cchWideChar,
    LPSTR lpMultiByteStr,
    int cbMultiByte,
    LPCSTR lpDefaultChar,
    LPBOOL lpUsedDefaultChar)
{
    int rc = 0;


    //
    //  Invalid Parameter Check:
    //     - validate code page
    //     - length of WC string is 0
    //     - multibyte buffer size is negative
    //     - WC string is NULL
    //     - length of WC string is NOT zero AND
    //         (MB string is NULL OR src and dest pointers equal)
    //     - lpDefaultChar and lpUsedDefaultChar not NULL
    //
    if ( (CodePage < CP_UTF7) || (CodePage > CP_UTF8) ||
         (cchWideChar == 0) || (cbMultiByte < 0) ||
         (lpWideCharStr == NULL) ||
         ((cbMultiByte != 0) &&
          ((lpMultiByteStr == NULL) ||
           (lpWideCharStr == (LPWSTR)lpMultiByteStr))) ||
         (lpDefaultChar != NULL) || (lpUsedDefaultChar != NULL) )
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return (0);
    }

    //
    //  Invalid Flags Check:
    //     - flags not 0
    //
    if (dwFlags != 0)
    {
        SetLastError(ERROR_INVALID_FLAGS);
        return (0);
    }

    //
    //  If cchWideChar is -1, then the string is null terminated and we
    //  need to get the length of the string.  Add one to the length to
    //  include the null termination.  (This will always be at least 1.)
    //
    if (cchWideChar <= -1)
    {
        cchWideChar = NlsStrLenW(lpWideCharStr) + 1;
    }

    switch (CodePage)
    {
        case ( CP_UTF7 ) :
        {
            rc = UnicodeToUTF7( lpWideCharStr,
                                cchWideChar,
                                lpMultiByteStr,
                                cbMultiByte );
            break;
        }
        case ( CP_UTF8 ) :
        {
            rc = UnicodeToUTF8( lpWideCharStr,
                                cchWideChar,
                                lpMultiByteStr,
                                cbMultiByte );
            break;
        }
    }

    return (rc);
}




//-------------------------------------------------------------------------//
//                           INTERNAL ROUTINES                             //
//-------------------------------------------------------------------------//


////////////////////////////////////////////////////////////////////////////
//
//  UTF7ToUnicode
//
//  Maps a UTF-7 character string to its wide character string counterpart.
//
//  02-06-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

int UTF7ToUnicode(
    LPCSTR lpSrcStr,
    int cchSrc,
    LPWSTR lpDestStr,
    int cchDest)
{
    //CHAR is signed, so we have to cast lpSrcStr to an unsigned char below.
    BYTE* pUTF7 = (BYTE*)lpSrcStr;    
    BOOL fShift = FALSE;
    DWORD dwBit = 0;              // 32-bit buffer to hold temporary bits
    int iPos = 0;                 // 6-bit position pointer in the buffer
    int cchWC = 0;                // # of Unicode code points generated


    while ((cchSrc--) && ((cchDest == 0) || (cchWC < cchDest)))
    {
        if (*pUTF7 > ASCII)
        {
            //
            //  Error - non ASCII char, so zero extend it.
            //
            if (cchDest)
            {
                lpDestStr[cchWC] = (WCHAR)*pUTF7;
            }
            cchWC++;
            // Terminate the shifted sequence.
            fShift = FALSE;
        }
        else if (!fShift)
        {
            //
            //  Not in shifted sequence.
            //
            if (*pUTF7 == SHIFT_IN)
            {
                if (cchSrc && (pUTF7[1] == SHIFT_OUT))
                {
                    //
                    //  "+-" means "+"
                    //
                    if (cchDest)
                    {
                        lpDestStr[cchWC] = (WCHAR)*pUTF7;
                    }
                    pUTF7++;
                    cchSrc--;
                    cchWC++;
                }
                else
                {
                    //
                    //  Start a new shift sequence.
                    //
                    fShift = TRUE;
                }
            }
            else
            {
                //
                //  No need to shift.
                //
                if (cchDest)
                {
                    lpDestStr[cchWC] = (WCHAR)*pUTF7;
                }
                cchWC++;
            }
        }
        else
        {
            //
            //  Already in shifted sequence.
            //
            if (nBitBase64[*pUTF7] == -1)
            {
                //
                //  Any non Base64 char also ends shift state.
                //
                if (*pUTF7 != SHIFT_OUT)
                {
                    //
                    //  Not "-", so write it to the buffer.
                    //
                    if (cchDest)
                    {
                        lpDestStr[cchWC] = (WCHAR)*pUTF7;
                    }
                    cchWC++;
                }

                //
                //  Reset bits.
                //
                fShift = FALSE;
                dwBit = 0;
                iPos = 0;
            }
            else
            {
                //
                //  Store the bits in the 6-bit buffer and adjust the
                //  position pointer.
                //
                dwBit |= ((DWORD)nBitBase64[*pUTF7]) << (26 - iPos);
                iPos += 6;
            }

            //
            //  Output the 16-bit Unicode value.
            //
            while (iPos >= 16)
            {
                if (cchDest)
                {
                    if (cchWC < cchDest)
                    {
                        lpDestStr[cchWC] = (WCHAR)(dwBit >> 16);
                    }
                    else
                    {
                        break;
                    }
                }
                cchWC++;

                dwBit <<= 16;
                iPos -= 16;
            }
            if (iPos >= 16)
            {
                //
                //  Error - buffer too small.
                //
                cchSrc++;
                break;
            }
        }

        pUTF7++;
    }

    //
    //  Make sure the destination buffer was large enough.
    //
    if (cchDest && (cchSrc >= 0))
    {
        if (cchSrc == 0 && fShift && *(pUTF7--) == SHIFT_OUT)
        {
            //
            // Do nothing here.
            // If we are in shift-in mode previously, and the last byte is a shift-out byte ('-'),
            // we should absorb this byte.  So don't set error.
            //
        } else
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return (0);
        }
    }

    //
    //  Return the number of Unicode characters written.
    //
    return (cchWC);
}

////////////////////////////////////////////////////////////////////////////
//
//  UTF8ToUnicode
//
//  Maps a UTF-8 character string to its wide character string counterpart.
//
//  04-22-2002    ShawnSte  Fix bug 533476 where final characters are broken.
//  02-06-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

int UTF8ToUnicode(
    LPCSTR lpSrcStr,
    int cchSrc,
    LPWSTR lpDestStr,
    int cchDest,
    DWORD dwFlags
    )
{
    int nTB = 0;                   // # trail bytes to follow
    int cchWC = 0;                 // # of Unicode code points generated
    CONST BYTE* pUTF8 = (CONST BYTE*)lpSrcStr;
    DWORD dwUnicodeChar;           // Our character with room for full surrogate char
    BOOL bSurrogatePair = FALSE;   // Indicate we'r collecting a surrogate pair
    BOOL bCheckInvalidBytes = (dwFlags & MB_ERR_INVALID_CHARS);
    BYTE UTF8;

    // Note that we can't test destination buffer length here because we may have to
    // iterate through thousands of broken characters which won't be output, even though
    // the buffer has no more room.
    while (cchSrc--)
    {
        //
        //  See if there are any trail bytes.
        //
        if (BIT7(*pUTF8) == 0)
        {
            //
            //  Found ASCII.
            //
            if (cchDest)
            {
                // In this function always test buffer size before using it
                if (cchWC >= cchDest)
                {
                    // Error: Buffer too small, we didn't process this character
                    SetLastError(ERROR_INSUFFICIENT_BUFFER);
                    return (0);
                }
                lpDestStr[cchWC] = (WCHAR)*pUTF8;
            }
            nTB = bSurrogatePair = 0;
            cchWC++;
        }
        else if (BIT6(*pUTF8) == 0)
        {
            //
            //  Found a trail byte.
            //  Note : Ignore the trail byte if there was no lead byte.
            //
            if (nTB != 0)
            {
                //
                //  Decrement the trail byte counter.
                //
                nTB--;

                // Add room for trail byte and add the trail byte falue
                dwUnicodeChar <<= 6;
                dwUnicodeChar |= LOWER_6_BIT(*pUTF8);

                // If we're done then we may need to store the data
                if (nTB == 0)
                {
                    if (bSurrogatePair)
                    {
                        if (cchDest)
                        {
                            if ((cchWC + 1) >= cchDest)
                            {
                                // Error: Buffer too small, we didn't process this character
                                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                                return (0);
                            }                                

                            lpDestStr[cchWC]   = (WCHAR)
                                                 (((dwUnicodeChar - 0x10000) >> 10) + HIGH_SURROGATE_START);

                            lpDestStr[cchWC+1] = (WCHAR)
                                                 ((dwUnicodeChar - 0x10000)%0x400 + LOW_SURROGATE_START);
                        }

                        //
                        //  End of sequence.  Advance the output counter, turn off surrogateness
                        //
                        cchWC += 2;
                        bSurrogatePair = FALSE;
                    }
                    else
                    {
                        if (cchDest)
                        {
                            
                            if (cchWC >= cchDest)
                            {
                                // Error: Buffer too small, we didn't process this character
                                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                                return (0);
                            }

                            lpDestStr[cchWC] = (WCHAR)dwUnicodeChar;            
                        }

                        //
                        //  End of sequence.  Advance the output counter.
                        //
                        cchWC++;
                    }
                      
                }
                        
            }
            else
            {
                if (bCheckInvalidBytes) 
                {
                    SetLastError(ERROR_NO_UNICODE_TRANSLATION);
                    return (0);
                }
                
                // error - not expecting a trail byte. That is, there is a trailing byte without leading byte.
                bSurrogatePair = FALSE;
            }
        }
        else
        {
            //
            //  Found a lead byte.
            //
            if (nTB > 0)
            {
                // error - A leading byte before the previous sequence is completed.
                if (bCheckInvalidBytes) 
                {
                    SetLastError(ERROR_NO_UNICODE_TRANSLATION);
                    return (0);
                }            
                //
                //  Error - previous sequence not finished.
                //
                nTB = 0;
                bSurrogatePair = FALSE;
                // Put this character back so that we can start over another sequence.
                cchSrc++;
                pUTF8--;
            }
            else
            {
                //
                //  Calculate the number of bytes to follow.
                //  Look for the first 0 from left to right.
                //
                UTF8 = *pUTF8;
                while (BIT7(UTF8) != 0)
                {
                    UTF8 <<= 1;
                    nTB++;
                }

                // Recover the data from the byte
                UTF8 >>= nTB;

                //
                // Check for non-shortest form.
                // 
                switch (nTB)
                {
                    case 1:
                        nTB = 0;
                        break;
                    case 2:
                        // Make sure that bit 8 ~ bit 11 is not all zero.
                        // 110XXXXx 10xxxxxx
                        if ((*pUTF8 & 0x1e) == 0)
                        {
                            nTB = 0;
                        }
                        break;
                    case 3:
                        // Look ahead to check for non-shortest form.
                        // 1110XXXX 10Xxxxxx 10xxxxxx
                        if (cchSrc >= 2)
                        {
                            if (((*pUTF8 & 0x0f) == 0) && (*(pUTF8 + 1) & 0x20) == 0)
                            {
                                nTB = 0;
                            }
                        }
                        break;
                    case 4:                    
                        //
                        // This is a surrogate unicode pair
                        //
                        if (cchSrc >= 3)
                        {
                            WORD word = (((WORD)*pUTF8) << 8) | *(pUTF8 + 1);
                            // Look ahead to check for non-shortest form.
                            // 11110XXX 10XXxxxx 10xxxxxx 10xxxxxx                        
                            // Check if the 5 X bits are all zero.
                            // 0x0730 == 00000111 00110000
                            if ( (word & 0x0730) == 0 ||
                                  // If the 21st bit is 1, we have extra work
                                  ( (word & 0x0400) == 0x0400 &&
                                     // The 21st bit is 1.
                                     // Make sure that the resulting Unicode is within the valid surrogate range.
                                     // The 4 byte code sequence can hold up to 21 bits, and the maximum valid code point range
                                     // that Unicode (with surrogate) could represent are from U+000000 ~ U+10FFFF.
                                     // Therefore, if the 21 bit (the most significant bit) is 1, we should verify that the 17 ~ 20
                                     // bit are all zero.
                                     // I.e., in 11110XXX 10XXxxxx 10xxxxxx 10xxxxxx,
                                     // XXXXX can only be 10000.    
                                     // 0x0330 = 0000 0011 0011 0000
                                    (word & 0x0330) != 0 ) )
                            {
                                // Not shortest form
                                nTB = 0;
                            }                              
                            else
                            { 
                                // A real surrogate pair
                                bSurrogatePair = TRUE;
                            }
                        }                        
                        break;
                    default:                    
                        // 
                        // If the bits is greater than 4, this is an invalid
                        // UTF8 lead byte.
                        //
                        nTB = 0;
                        break;
                }

                if (nTB != 0) 
                {
                    //
                    //  Store the value from the first byte and decrement
                    //  the number of bytes to follow.
                    //
                    dwUnicodeChar = UTF8;
                    nTB--;
                } else 
                {
                    if (bCheckInvalidBytes) 
                    {
                        SetLastError(ERROR_NO_UNICODE_TRANSLATION);
                        return (0);
                    }                 
                }
            }
        }
        pUTF8++;
    }

    if ((bCheckInvalidBytes && nTB != 0) || (cchWC == 0)) 
    {
        // About (cchWC == 0):
        // Because we now throw away non-shortest form, it is possible that we generate 0 chars.
        // In this case, we have to set error to ERROR_NO_UNICODE_TRANSLATION so that we conform
        // to the spec of MultiByteToWideChar.
        SetLastError(ERROR_NO_UNICODE_TRANSLATION);
        return (0);
    }

    //
    //  Return the number of Unicode characters written.
    //
    return (cchWC);
}

////////////////////////////////////////////////////////////////////////////
//
//  UnicodeToUTF7
//
//  Maps a Unicode character string to its UTF-7 string counterpart.
//
//  02-06-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

int UnicodeToUTF7(
    LPCWSTR lpSrcStr,
    int cchSrc,
    LPSTR lpDestStr,
    int cchDest)
{
    LPCWSTR lpWC = lpSrcStr;
    BOOL fShift = FALSE;
    DWORD dwBit = 0;              // 32-bit buffer
    int iPos = 0;                 // 6-bit position in buffer
    int cchU7 = 0;                // # of UTF7 chars generated


    while ((cchSrc--) && ((cchDest == 0) || (cchU7 < cchDest)))
    {
        if ((*lpWC > ASCII) || (fShiftChar[*lpWC]))
        {
            //
            //  Need shift.  Store 16 bits in buffer.
            //
            dwBit |= ((DWORD)*lpWC) << (16 - iPos);
            iPos += 16;

            if (!fShift)
            {
                //
                //  Not in shift state, so add "+".
                //
                if (cchDest)
                {
                    lpDestStr[cchU7] = SHIFT_IN;
                }
                cchU7++;

                //
                //  Go into shift state.
                //
                fShift = TRUE;
            }

            //
            //  Output 6 bits at a time as Base64 chars.
            //
            while (iPos >= 6)
            {
                if (cchDest)
                {
                    if (cchU7 < cchDest)
                    {
                        //
                        //  26 = 32 - 6
                        //
                        lpDestStr[cchU7] = cBase64[(int)(dwBit >> 26)];
                    }
                    else
                    {
                        break;
                    }
                }

                cchU7++;
                dwBit <<= 6;           // remove from bit buffer
                iPos -= 6;             // adjust position pointer
            }
            if (iPos >= 6)
            {
                //
                //  Error - buffer too small.
                //
                cchSrc++;
                break;
            }
        }
        else
        {
            //
            //  No need to shift.
            //
            if (fShift)
            {
                //
                //  End the shift sequence.
                //
                fShift = FALSE;

                if (iPos != 0)
                {
                    //
                    //  Some bits left in dwBit.
                    //
                    if (cchDest)
                    {
                        if ((cchU7 + 1) < cchDest)
                        {
                            lpDestStr[cchU7++] = cBase64[(int)(dwBit >> 26)];
                            lpDestStr[cchU7++] = SHIFT_OUT;
                        }
                        else
                        {
                            //
                            //  Error - buffer too small.
                            //
                            cchSrc++;
                            break;
                        }
                    }
                    else
                    {
                        cchU7 += 2;
                    }

                    dwBit = 0;         // reset bit buffer
                    iPos  = 0;         // reset postion pointer
                }
                else
                {
                    //
                    //  Simply end the shift sequence.
                    //
                    if (cchDest)
                    {
                        lpDestStr[cchU7++] = SHIFT_OUT;
                    }
                    else
                    {
                        cchU7++;
                    }
                }
            }

            //
            //  Write the character to the buffer.
            //  If the character is "+", then write "+-".
            //
            if (cchDest)
            {
                if (cchU7 < cchDest)
                {
                    lpDestStr[cchU7++] = (char)*lpWC;

                    if (*lpWC == SHIFT_IN)
                    {
                        if (cchU7 < cchDest)
                        {
                            lpDestStr[cchU7++] = SHIFT_OUT;
                        }
                        else
                        {
                            //
                            //  Error - buffer too small.
                            //
                            cchSrc++;
                            break;
                        }
                    }
                }
                else
                {
                    //
                    //  Error - buffer too small.
                    //
                    cchSrc++;
                    break;
                }
            }
            else
            {
                cchU7++;

                if (*lpWC == SHIFT_IN)
                {
                    cchU7++;
                }
            }
        }

        lpWC++;
    }

    //
    //  See if we're still in the shift state.
    //
    if (fShift)
    {
        if (iPos != 0)
        {
            //
            //  Some bits left in dwBit.
            //
            if (cchDest)
            {
                if ((cchU7 + 1) < cchDest)
                {
                    lpDestStr[cchU7++] = cBase64[(int)(dwBit >> 26)];
                    lpDestStr[cchU7++] = SHIFT_OUT;
                }
                else
                {
                    //
                    //  Error - buffer too small.
                    //
                    cchSrc++;
                }
            }
            else
            {
                cchU7 += 2;
            }
        }
        else
        {
            //
            //  Simply end the shift sequence.
            //
            if (cchDest)
            {
                if (cchU7 < cchDest) 
                {
                    lpDestStr[cchU7++] = SHIFT_OUT;
                } 
                else 
                {
                    //
                    //  Error - buffer too small.
                    //
                    cchSrc++;
                }
            }
            else
            {
                cchU7++;
            }
        }
    }

    //
    //  Make sure the destination buffer was large enough.
    //
    if (cchDest && (cchSrc >= 0))
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return (0);
    }

    //
    //  Return the number of UTF-7 characters written.
    //
    return (cchU7);
}


////////////////////////////////////////////////////////////////////////////
//
//  UnicodeToUTF8
//
//  Maps a Unicode character string to its UTF-8 string counterpart.
//
//  02-06-96    JulieB    Created.
////////////////////////////////////////////////////////////////////////////

int UnicodeToUTF8(
    LPCWSTR lpSrcStr,
    int cchSrc,
    LPSTR lpDestStr,
    int cchDest)
{
    LPCWSTR lpWC = lpSrcStr;
    int     cchU8 = 0;                // # of UTF8 chars generated
    DWORD   dwSurrogateChar;
    WCHAR   wchHighSurrogate = 0;
    BOOL    bHandled;


    while ((cchSrc--) && ((cchDest == 0) || (cchU8 < cchDest)))
    {
        bHandled = FALSE;

        //
        // Check if high surrogate is available
        //
        if ((*lpWC >= HIGH_SURROGATE_START) && (*lpWC <= HIGH_SURROGATE_END))
        {
            if (cchDest)
            {
                // Another high surrogate, then treat the 1st as normal
                // Unicode character.
                if (wchHighSurrogate)
                {
                    if ((cchU8 + 2) < cchDest)
                    {
                        lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(wchHighSurrogate);
                    }
                    else
                    {
                        // not enough buffer
                        cchSrc++;
                        break;
                    }
                }
            }
            else
            {
                cchU8 += 3;
            }
            wchHighSurrogate = *lpWC;
            bHandled = TRUE;
        }

        if (!bHandled && wchHighSurrogate)
        {
            if ((*lpWC >= LOW_SURROGATE_START) && (*lpWC <= LOW_SURROGATE_END))
            {
                 // wheee, valid surrogate pairs

                 if (cchDest)
                 {
                     if ((cchU8 + 3) < cchDest)
                     {
                         dwSurrogateChar = (((wchHighSurrogate-0xD800) << 10) + (*lpWC - 0xDC00) + 0x10000);

                         lpDestStr[cchU8++] = (UTF8_1ST_OF_4 |
                                               (unsigned char)(dwSurrogateChar >> 18));           // 3 bits from 1st byte

                         lpDestStr[cchU8++] =  (UTF8_TRAIL |
                                                (unsigned char)((dwSurrogateChar >> 12) & 0x3f)); // 6 bits from 2nd byte

                         lpDestStr[cchU8++] = (UTF8_TRAIL |
                                               (unsigned char)((dwSurrogateChar >> 6) & 0x3f));   // 6 bits from 3rd byte

                         lpDestStr[cchU8++] = (UTF8_TRAIL |
                                               (unsigned char)(0x3f & dwSurrogateChar));          // 6 bits from 4th byte
                     }
                     else
                     {
                        // not enough buffer
                        cchSrc++;
                        break;
                     }
                 }
                 else
                 {
                     // we already counted 3 previously (in high surrogate)
                     cchU8 ++;
                 }

                 bHandled = TRUE;
            }
            else
            {
                 // Bad Surrogate pair : ERROR
                 // Just process wchHighSurrogate , and the code below will
                 // process the current code point
                 if (cchDest)
                 {
                     if ((cchU8 + 2) < cchDest)
                     {
                        lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(wchHighSurrogate);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(wchHighSurrogate);
                     }
                     else
                     {
                        // not enough buffer
                        cchSrc++;
                        break;
                     }
                 }
            }

            wchHighSurrogate = 0;
        }

        if (!bHandled)
        {
            if (*lpWC <= ASCII)
            {
                //
                //  Found ASCII.
                //
                if (cchDest)
                {
                    if (cchU8 < cchDest) 
                    {
                        lpDestStr[cchU8] = (char)*lpWC;
                    } 
                    else 
                    {
                        //
                        //  Error - buffer too small.
                        //
                        cchSrc++;
                        break;
                    }
                }
                cchU8++;
            }
            else if (*lpWC <= UTF8_2_MAX)
            {
                //
                //  Found 2 byte sequence if < 0x07ff (11 bits).
                //
                if (cchDest)
                {
                    if ((cchU8 + 1) < cchDest)
                    {
                        //
                        //  Use upper 5 bits in first byte.
                        //  Use lower 6 bits in second byte.
                        //
                        lpDestStr[cchU8++] = UTF8_1ST_OF_2 | (*lpWC >> 6);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(*lpWC);
                    }
                    else
                    {
                        //
                        //  Error - buffer too small.
                        //
                        cchSrc++;
                        break;
                    }
                }
                else
                {
                    cchU8 += 2;
                }
            }
            else
            {
                //
                //  Found 3 byte sequence.
                //
                if (cchDest)
                {
                    if ((cchU8 + 2) < cchDest)
                    {
                        //
                        //  Use upper  4 bits in first byte.
                        //  Use middle 6 bits in second byte.
                        //  Use lower  6 bits in third byte.
                        //
                        lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(*lpWC);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(*lpWC);
                        lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(*lpWC);
                    }
                    else
                    {
                        //
                        //  Error - buffer too small.
                        //
                        cchSrc++;
                        break;
                    }
                }
                else
                {
                    cchU8 += 3;
                }
            }
        }

        lpWC++;
    }

    //
    // If the last character was a high surrogate, then handle it as a normal
    // unicode character.
    //
    if ((cchSrc < 0) && (wchHighSurrogate != 0))
    {
        if (cchDest)
        {
            if ((cchU8 + 2) < cchDest)
            {
                lpDestStr[cchU8++] = UTF8_1ST_OF_3 | HIGHER_6_BIT(wchHighSurrogate);
                lpDestStr[cchU8++] = UTF8_TRAIL    | MIDDLE_6_BIT(wchHighSurrogate);
                lpDestStr[cchU8++] = UTF8_TRAIL    | LOWER_6_BIT(wchHighSurrogate);
            }
            else
            {
                cchSrc++;
            }
        }
    }

    //
    //  Make sure the destination buffer was large enough.
    //
    if (cchDest && (cchSrc >= 0))
    {
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return (0);
    }

    //
    //  Return the number of UTF-8 characters written.
    //
    return (cchU8);
}
