//------------------------------------------------------------------------------
// <copyright file="RegexBoyerMoore.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 * The RegexBoyerMoore object precomputes the Boyer-Moore
 * tables for fast string scanning. These tables allow
 * you to scan for the first occurance of a string within
 * a large body of text without examining every character.
 * The performance of the heuristic depends on the actual
 * string and the text being searched, but usually, the longer
 * the string that is being searched for, the fewer characters
 * need to be examined.
 *
 * Copyright (c) 1999 Microsoft Corporation
 *
 * Revision history
 *      6/05/99 (dbau)      First draft
 */

namespace System.Text.RegularExpressions
{

    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    internal sealed class RegexBoyerMoore {
        internal int[] _positive;
        internal int[] _negativeASCII;
        internal int[][] _negativeUnicode;
        internal String _pattern;
        internal int _lowASCII;
        internal int _highASCII;
        internal bool _rightToLeft;
        internal bool _caseInsensitive;
        internal CultureInfo _culture;

        internal const int infinite = 0x7FFFFFFF;

        /*
         * Constructs a Boyer-Moore state machine for searching for the string
         * pattern. The string must not be zero-length.
         */
        internal RegexBoyerMoore(String pattern, bool caseInsensitive, bool rightToLeft, CultureInfo culture) {
            /*
             * Sorry,  you just can't use Boyer-Moore to find an empty pattern.
             * We're doing this for your own protection. (Really, for speed.)
             */
            Debug.Assert(pattern.Length != 0, "RegexBoyerMoore called with an empty string.  This is bad for perf");

            int beforefirst;
            int last;
            int bump;
            int examine;
            int scan;
            int match;
            char ch;

            
            if (caseInsensitive)
                pattern = pattern.ToLower(culture);

            _pattern = pattern;
            _rightToLeft = rightToLeft;
            _caseInsensitive = caseInsensitive;
            _culture = culture;
            
            if (!rightToLeft) {
                beforefirst = -1;
                last = pattern.Length - 1;
                bump = 1;
            }
            else {
                beforefirst = pattern.Length;
                last = 0;
                bump = -1;
            }

            /*
             * PART I - the good-suffix shift table
             * 
             * compute the positive requirement:
             * if char "i" is the first one from the right that doesn't match,
             * then we know the matcher can advance by _positive[i].
             *
             * Davidgut 8/2001: This algorithm appears to be a simplified variant of the 
             *                  standard Boyer-Moore good suffix calculation.  It could
             *                  be one of D.M. Sunday's variations, but I have not found which one.
             *
             * CONSIDER: Maybe in v2 rewrite this with the real Boyer-Moore algorithm and split it
             *           out into a separate piece of code in the BCL.
             */
            _positive = new int[pattern.Length];

            examine = last;
            ch = pattern[examine];
            _positive[examine] = bump;
            examine -= bump;

            for (;;) {
                // find an internal char (examine) that matches the tail

                for (;;) {
                    if (examine == beforefirst)
                        goto OuterloopBreak;
                    if (pattern[examine] == ch)
                        break;
                    examine -= bump;
                }

                match = last;
                scan = examine;

                // find the length of the match

                for (;;) {
                    if (scan == beforefirst || pattern[match] != pattern[scan]) {
                        // at the end of the match, note the difference in _positive
                        // this is not the length of the match, but the distance from the internal match
                        // to the tail suffix. 
                        if (_positive[match] == 0)
                            _positive[match] = match - scan;

                        // System.Diagnostics.Debug.WriteLine("Set positive[" + match + "] to " + (match - scan));

                        break;
                    }

                    scan -= bump;
                    match -= bump;
                }

                examine -= bump;
            }

            OuterloopBreak:

            match = last - bump;

            // scan for the chars for which there are no shifts that yield a different candidate

            /* 
             *  Davidgut 8/2001: The inside of the if statement used to say 
             *                   "_positive[match] = last - beforefirst;"
             *                   Per bug ASURT 101422, I've changed it to the below code.  This
             *                   is slightly less agressive in how much we skip, but at worst it 
             *                   should mean a little more work rather than skipping a potential
             *                   match.
             */
            while (match != beforefirst) {
                if (_positive[match] == 0)
                    _positive[match] = bump;

                match -= bump;
            }

            //System.Diagnostics.Debug.WriteLine("good suffix shift table:");
            //for (int i=0; i<_positive.Length; i++)
            //    System.Diagnostics.Debug.WriteLine("\t_positive[" + i + "] = " + _positive[i]);
                

            /*
             * PART II - the bad-character shift table
             * 
             * compute the negative requirement:
             * if char "ch" is the reject character when testing position "i",
             * we can slide up by _negative[ch];
             * (_negative[ch] = str.Length - 1 - str.LastIndexOf(ch))
             *
             * the lookup table is divided into ASCII and Unicode portions;
             * only those parts of the Unicode 16-bit code set that actually
             * appear in the string are in the table. (Maximum size with
             * Unicode is 65K; ASCII only case is 512 bytes.)
             */

            _negativeASCII = new int[128];

            for (int i = 0; i < 128; i++)
                _negativeASCII[i] = last - beforefirst;

            _lowASCII = 127;
            _highASCII = 0;

            for (examine = last; examine != beforefirst; examine -= bump) {
                ch = pattern[examine];

                // HACKHACK: chars are signed!!

                if (ch >= 0 && ch < 128) {
                    if (_lowASCII > ch)
                        _lowASCII = ch;

                    if (_highASCII < ch)
                        _highASCII = ch;

                    if (_negativeASCII[ch] == last - beforefirst)
                        _negativeASCII[ch] = last - examine;
                }
                else {
                    int i = ch >> 8;
                    int j = ch & 0xFF;

                    if (_negativeUnicode == null) {
                        _negativeUnicode = new int[256][];

                        // HACKHACK: SMC inits all entries to int[0]s instead of null!

                        for (int k = 0; k < 256; k++)
                            _negativeUnicode[k] = null;
                    }

                    if (_negativeUnicode[i] == null) {
                        int[] newarray = new int[256];

                        for (int k = 0; k < 256; k++)
                            newarray[k] = last - beforefirst;

                        if (i == 0) {
                            System.Array.Copy(_negativeASCII, newarray, 128);
                            _negativeASCII = newarray;
                        }

                        _negativeUnicode[i] = newarray;
                    }

                    if (_negativeUnicode[i][j] == last - beforefirst)
                        _negativeUnicode[i][j] = last - examine;
                }
            }
        }

        /*
         * When a regex is anchored, we can do a quick IsMatch test instead of a Scan
         */
        internal bool IsMatch(String text, int index, int beglimit, int endlimit) {
           
            if (!_rightToLeft) {
                if (index < beglimit || endlimit - index < _pattern.Length)
                    return false;

                return(0 == String.Compare(_pattern, 0, text, index, _pattern.Length, _caseInsensitive, _culture));
            }
            else {
                if (index > endlimit || index - beglimit < _pattern.Length)
                    return false;

                return(0 == String.Compare(_pattern, 0, text, index - _pattern.Length, _pattern.Length, _caseInsensitive, _culture));
            }
        }


        /*
         * Scan uses the Boyer-Moore algorithm to find the first occurrance
         * of the specified string within text, beginning at index, and
         * constrained within beglimit and endlimit.
         *
         * The direction and case-sensitivity of the match is determined
         * by the arguments to the RegexBoyerMoore constructor.
         */
        internal int Scan(String text, int index, int beglimit, int endlimit) {
            int test;
            int test2;
            int match;
            int startmatch;
            int endmatch;
            int advance;
            int defadv;
            int bump;
            char chMatch;
            char chTest;
            int[] unicodeLookup;

            if (!_rightToLeft) {
                defadv = _pattern.Length;
                startmatch = _pattern.Length - 1;
                endmatch = 0;
                test = index + defadv - 1;
                bump = 1;
            }
            else {
                defadv = -_pattern.Length;
                startmatch = 0;
                endmatch = -defadv - 1;
                test = index + defadv;
                bump = -1;
            }

            chMatch = _pattern[startmatch];

            for (;;) {
                if (test >= endlimit || test < beglimit)
                    return -1;

                chTest = text[test];

                if (_caseInsensitive)
                    chTest = Char.ToLower(chTest, _culture);

                if (chTest != chMatch) {
                    // HACKHACK chars should be unsigned (dbau)
                    // so this should just read "if (ch < 128)"

                    if ((chTest & 0xFF80) == 0)
                        advance = _negativeASCII[chTest];
                    else if (null != _negativeUnicode && (null != (unicodeLookup = _negativeUnicode[chTest >> 8])))
                        advance = unicodeLookup[chTest & 0xFF];
                    else
                        advance = defadv;

                    test += advance;
                }
                else { // if (chTest == chMatch)
                    test2 = test;
                    match = startmatch;

                    for (;;) {
                        if (match == endmatch)
                            return(_rightToLeft ? test2 + 1 : test2);

                        match -= bump;
                        test2 -= bump;

                        chTest = text[test2];

                        if (_caseInsensitive)
                            chTest = Char.ToLower(chTest, _culture);

                        if (chTest != _pattern[match]) {
                            advance = _positive[match];
                            if ((chTest & 0xFF80) == 0)
                                test2 = (match - startmatch) + _negativeASCII[chTest];
                            else if (null != _negativeUnicode && (null != (unicodeLookup = _negativeUnicode[chTest >> 8])))
                                test2 = (match - startmatch) + unicodeLookup[chTest & 0xFF];
                            else {
                                test += advance;
                                break;
                            }

                            if (_rightToLeft ? test2 < advance : test2 > advance)
                                advance = test2;

                            test += advance;
                            break;
                        }
                    }
                }
            }
        }

        /*
         * Used when dumping for debugging.
         */
        public override String ToString() {
            return _pattern;
        }

#if DBG
        public String Dump(String indent) {
            StringBuilder sb = new StringBuilder();

            sb.Append(indent + "BM Pattern: " + _pattern + "\n");
            sb.Append(indent + "Positive: ");
            for (int i = 0; i < _positive.Length; i++) {
                sb.Append(_positive[i].ToString() + " ");
            }
            sb.Append("\n");

            if (_negativeASCII != null) {
                sb.Append(indent + "Negative table\n");
                for (int i = 0; i < _negativeASCII.Length; i++) {
                    if (_negativeASCII[i] != _pattern.Length) {
                        sb.Append(indent + "  " + Regex.Escape(Convert.ToString((char)i)) + " " + _negativeASCII[i].ToString() + "\n");
                    }
                }
            }

            return sb.ToString();
        }
#endif
    }
}
