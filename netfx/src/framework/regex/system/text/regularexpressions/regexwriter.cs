//------------------------------------------------------------------------------
// <copyright file="RegexWriter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 * This RegexWriter class is internal to the Regex package.
 * It builds a block of regular expression codes (RegexCode)
 * from a RegexTree parse tree.
 *
 * Copyright (c) 1999 Microsoft Corporation
 *
 * Revision history
 *      4/26/99 (dbau)      First draft
 *      5/11/99 (dbau)      Added comments
 */

/*
 * Implementation notes:
 * 
 * This step is as simple as walking the tree and emitting
 * sequences of codes.
 *
 * CONSIDER: perhaps we should generate MSIL directly from a tree walk?
 */
#define ECMA

namespace System.Text.RegularExpressions {

    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    
    internal sealed class RegexWriter {
        internal int[]       _intStack;
        internal int         _depth;
        internal int[]       _emitted;
        internal int         _curpos;
        internal IDictionary   _stringhash;
        internal ArrayList   _stringtable;
        // not used! internal int         _stringcount;
        internal bool     _counting;
        internal int         _count;
        internal int         _trackcount;
        internal Hashtable   _caps;

        internal const int BeforeChild = 64;
        internal const int AfterChild = 128;
        internal const int infinite = RegexCode.infinite;

        /*
         * This is the only function that should be called from outside.
         * It takes a RegexTree and creates a corresponding RegexCode.
         */
        internal static RegexCode Write(RegexTree t) {
            RegexWriter w = new RegexWriter();
            RegexCode retval = w.RegexCodeFromRegexTree(t);
#if DBG
            if (t.Debug) {
                retval.Dump();
            }
#endif
            return retval;
        }

        /*
         * private constructor; can't be created outside
         */
        private RegexWriter() {
            _intStack = new int[32];
            _emitted = new int[32];
            _stringhash = new HybridDictionary();
            _stringtable = new ArrayList();
        }

        /*
         * To avoid recursion, we use a simple integer stack.
         * This is the push.
         */
        internal void PushInt(int I) {
            if (_depth >= _intStack.Length) {
                int [] expanded = new int[_depth * 2];

                System.Array.Copy(_intStack, 0, expanded, 0, _depth);

                _intStack = expanded;
            }

            _intStack[_depth++] = I;
        }

        /*
         * True if the stack is empty.
         */
        internal bool EmptyStack() {
            return _depth == 0;
        }

        /*
         * This is the pop.
         */
        internal int PopInt() {
            return _intStack[--_depth];
        }

        /*
         * Returns the current position in the emitted code.
         */
        internal int CurPos() {
            return _curpos;
        }

        /*
         * Fixes up a jump instruction at the specified offset
         * so that it jumps to the specified jumpDest.
         */
        internal void PatchJump(int Offset, int jumpDest) {
            _emitted[Offset + 1] = jumpDest;
        }

        /*
         * Emits a zero-argument operation. Note that the emit
         * functions all run in two modes: they can emit code, or
         * they can just count the size of the code.
         */
        internal void Emit(int op) {
            if (_counting) {
                _count += 1;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
        }

        /*
         * Emits a one-argument operation.
         */
        internal void Emit(int op, int opd1) {
            if (_counting) {
                _count += 2;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
            _emitted[_curpos++] = opd1;
        }

        /*
         * Emits a two-argument operation.
         */
        internal void Emit(int op, int opd1, int opd2) {
            if (_counting) {
                _count += 3;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
            _emitted[_curpos++] = opd1;
            _emitted[_curpos++] = opd2;
        }

        /*
         * Emits a three-argument operation.
         */
        internal void Emit(int op, int opd1, int opd2, int opd3) {
            if (_counting) {
                _count += 4;
                if (RegexCode.OpcodeBacktracks(op))
                    _trackcount += 1;
                return;
            }
            _emitted[_curpos++] = op;
            _emitted[_curpos++] = opd1;
            _emitted[_curpos++] = opd2;
            _emitted[_curpos++] = opd3;
        }

        /*
         * Returns an index in the string table for a string;
         * uses a hashtable to eliminate duplicates.
         */
        internal int StringCode(String str) {
            Int32 i;

            if (_counting)
                return 0;

            if (str == null)
                str = String.Empty;
            
            if (_stringhash.Contains(str)) {
                i = (Int32)_stringhash[str];
            }
            else {
                i = _stringtable.Count;
                _stringhash[str] = i;
                _stringtable.Add(str);
            }

            return i;
        }

        /*
         * Just returns an exception; should be dead code
         */
        internal ArgumentException MakeException(String message) {
            return new ArgumentException(message);
        }

        /*  This is code for ASURT 78559
            Use this in EmitFragment, 'case RegexNode.Capture | AfterChild' when we emit the string
        internal string MapGroupString(string groupstring) {
            if (groupstring == null)
                return null;
            
            char[] ret = new char[groupstring.Length];

            for (int i=0; i<groupstring.Length; i++) 
                ret[i] = (char) MapCapnum((int) groupstring[i]);

            return new String(ret);
        }
        */
        
        /*
         * When generating code on a regex that uses a sparse set
         * of capture slots, we hash them to a dense set of indices
         * for an array of capture slots. Instead of doing the hash
         * at match time, it's done at compile time, here.
         */
        internal int MapCapnum(int capnum) {
            if (capnum == -1)
                return -1;

            if (_caps != null)
                return(Int32)_caps[capnum];
            else
                return capnum;
        }

        /*
         * The top level RegexCode generator. It does a depth-first walk
         * through the tree and calls EmitFragment to emits code before
         * and after each child of an interior node, and at each leaf.
         *
         * It runs two passes, first to count the size of the generated
         * code, and second to generate the code.
         *
         * CONSIDER: we need to time it against the alternative, which is
         * to just generate the code and grow the array as we go.
         */
        internal RegexCode RegexCodeFromRegexTree(RegexTree tree) {
            RegexNode curNode;
            int curChild;
            int capsize;
            RegexPrefix fcPrefix;
            RegexPrefix scPrefix;
            RegexPrefix prefix;
            int anchors;
            RegexBoyerMoore bmPrefix;
            bool rtl;

            // construct sparse capnum mapping if some numbers are unused

            if (tree._capnumlist == null || tree._captop == tree._capnumlist.Length) {
                capsize = tree._captop;
                _caps = null;
            }
            else {
                capsize = tree._capnumlist.Length;
                _caps = tree._caps;
                for (int i = 0; i < tree._capnumlist.Length; i++)
                    _caps[tree._capnumlist[i]] = i;
            }

            _counting = true;

            for (;;) {
                if (!_counting)
                    _emitted = new int[_count];

                curNode = tree._root;
                curChild = 0;

                Emit(RegexCode.Lazybranch, 0);

                for (;;) {
                    if (curNode._children == null) {
                        EmitFragment(curNode._type, curNode, 0);
                    }
                    else if (curChild < curNode._children.Count) {
                        EmitFragment(curNode._type | BeforeChild, curNode, curChild);

                        curNode = (RegexNode)curNode._children[curChild];
                        PushInt(curChild);
                        curChild = 0;
                        continue;
                    }

                    if (EmptyStack())
                        break;

                    curChild = PopInt();
                    curNode = curNode._next;

                    EmitFragment(curNode._type | AfterChild, curNode, curChild);
                    curChild++;
                }

                PatchJump(0, CurPos());
                Emit(RegexCode.Stop);

                if (!_counting)
                    break;

                _counting = false;
            }

            // if the set of possible first chars is very large,
            // don't bother scanning for it (common case: . == [^\n])

            fcPrefix = RegexFCD.FirstChars(tree);

	    // REVIEW : ChrisAn/DavidGut, 11/21/2000 - Huh... this code used to 
	    //        : read "> 0XFFF", note the CAPITAL X... everything is golden,
	    //        : except that this really evaluates to 0 in the C# compiler.
	    //        :
	    //        : However! begining in CSC 9055 0XFFF will attempted to be
	    //        : evaluated as a float, causing a compiler error. So switching
	    //        : the constant to "0xFFF", note the lowercase x, causes
	    //        : everything to fail.
	    //        :
	    //        : What is this code really supposed to do???!
	    //
            if (fcPrefix != null && RegexCharClass.SetSize(fcPrefix.Prefix) > 0)
                fcPrefix = null;

            // REVIEW: is this even used anywhere? Can we use it somehow?
            scPrefix = null; //RegexFCD.ScanChars(tree);
            prefix = RegexFCD.Prefix(tree);
            rtl = ((tree._options & RegexOptions.RightToLeft) != 0);

            CultureInfo culture = (tree._options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            if (prefix != null && prefix.Prefix.Length > 0)
                bmPrefix = new RegexBoyerMoore(prefix.Prefix, prefix.CaseInsensitive, rtl, culture);
            else
                bmPrefix = null;

            anchors = RegexFCD.Anchors(tree);

            return new RegexCode(_emitted, _stringtable, _trackcount, _caps, capsize, bmPrefix, fcPrefix, scPrefix, anchors, rtl);
        }

        /*
         * The main RegexCode generator. It does a depth-first walk
         * through the tree and calls EmitFragment to emits code before
         * and after each child of an interior node, and at each leaf.
         */
        internal void EmitFragment(int nodetype, RegexNode node, int CurIndex) {
            int bits = 0;

            if (nodetype <= RegexNode.Ref) {
                if (node.UseOptionR())
                    bits |= RegexCode.Rtl;
                if ((node._options & RegexOptions.IgnoreCase) != 0)
                    bits |= RegexCode.Ci;
            }

            switch (nodetype) {
                case RegexNode.Concatenate | BeforeChild:
                case RegexNode.Concatenate | AfterChild:
                case RegexNode.Empty:
                    break;

                case RegexNode.Alternate | BeforeChild:
                    if (CurIndex < node._children.Count - 1) {
                        PushInt(CurPos());
                        Emit(RegexCode.Lazybranch, 0);
                    }
                    break;

                case RegexNode.Alternate | AfterChild: {

                        if (CurIndex < node._children.Count - 1) {
                            int LBPos = PopInt();
                            PushInt(CurPos());
                            Emit(RegexCode.Goto, 0);
                            PatchJump(LBPos, CurPos());
                        }
                        else {
                            int I;
                            for (I = 0; I < CurIndex; I++) {
                                PatchJump(PopInt(), CurPos());
                            }
                        }
                        break;
                    }

                case RegexNode.Testref | BeforeChild:
                    switch (CurIndex) {
                        case 0:
                            Emit(RegexCode.Setjump);
                            PushInt(CurPos());
                            Emit(RegexCode.Lazybranch, 0);
                            Emit(RegexCode.Testref, MapCapnum(node._m));
                            Emit(RegexCode.Forejump);
                            break;
                    }
                    break;

                case RegexNode.Testref | AfterChild:
                    switch (CurIndex) {
                        case 0: {
                                int Branchpos = PopInt();
                                PushInt(CurPos());
                                Emit(RegexCode.Goto, 0);
                                PatchJump(Branchpos, CurPos());
                                Emit(RegexCode.Forejump);
                                if (node._children.Count > 1)
                                    break;
                                // else fallthrough
                                goto case 1;
                            }
                        case 1:
                            PatchJump(PopInt(), CurPos());
                            break;
                    }
                    break;

                case RegexNode.Testgroup | BeforeChild:
                    switch (CurIndex) {
                        case 0:
                            Emit(RegexCode.Setjump);
                            Emit(RegexCode.Setmark);
                            PushInt(CurPos());
                            Emit(RegexCode.Lazybranch, 0);
                            break;
                    }
                    break;

                case RegexNode.Testgroup | AfterChild:
                    switch (CurIndex) {
                        case 0:
                            Emit(RegexCode.Getmark);
                            Emit(RegexCode.Forejump);
                            break;
                        case 1: 
                            int Branchpos = PopInt();
                            PushInt(CurPos());
                            Emit(RegexCode.Goto, 0);
                            PatchJump(Branchpos, CurPos());
                            Emit(RegexCode.Getmark);
                            Emit(RegexCode.Forejump);

                            if (node._children.Count > 2)
                                break;
                            // else fallthrough
                            goto case 2;
                        case 2:
                            PatchJump(PopInt(), CurPos());
                            break;
                    }
                    break;

                case RegexNode.Loop | BeforeChild:
                case RegexNode.Lazyloop | BeforeChild:

                    if (node._n < infinite || node._m > 1)
                        Emit(node._m == 0 ? RegexCode.Nullcount : RegexCode.Setcount, node._m == 0 ? 0 : 1 - node._m);
                    else
                        Emit(node._m == 0 ? RegexCode.Nullmark : RegexCode.Setmark);

                    if (node._m == 0) {
                        PushInt(CurPos());
                        Emit(RegexCode.Goto, 0);
                    }
                    PushInt(CurPos());
                    break;

                case RegexNode.Loop | AfterChild:
                case RegexNode.Lazyloop | AfterChild: {
                        int StartJumpPos = CurPos();
                        int Lazy = (nodetype - (RegexNode.Loop | AfterChild));

                        if (node._n < infinite || node._m > 1)
                            Emit(RegexCode.Branchcount + Lazy, PopInt(), node._n == infinite ? infinite : node._n - node._m);
                        else
                            Emit(RegexCode.Branchmark + Lazy, PopInt());

                        if (node._m == 0)
                            PatchJump(PopInt(), StartJumpPos);
                    }
                    break;

                case RegexNode.Group | BeforeChild:
                case RegexNode.Group | AfterChild:
                    break;

                case RegexNode.Capture | BeforeChild:
                    Emit(RegexCode.Setmark);
                    break;

                case RegexNode.Capture | AfterChild:
                    Emit(RegexCode.Capturemark, MapCapnum(node._m), MapCapnum(node._n));
                    break;

                case RegexNode.Require | BeforeChild:
                    // NOTE: the following line causes lookahead/lookbehind to be
                    // NON-BACKTRACKING. It can be commented out with (*)
                    Emit(RegexCode.Setjump);


                    Emit(RegexCode.Setmark);
                    break;

                case RegexNode.Require | AfterChild:
                    Emit(RegexCode.Getmark);

                    // NOTE: the following line causes lookahead/lookbehind to be
                    // NON-BACKTRACKING. It can be commented out with (*)
                    Emit(RegexCode.Forejump);

                    break;

                case RegexNode.Prevent | BeforeChild:
                    Emit(RegexCode.Setjump);
                    PushInt(CurPos());
                    Emit(RegexCode.Lazybranch, 0);
                    break;

                case RegexNode.Prevent | AfterChild:
                    Emit(RegexCode.Backjump);
                    PatchJump(PopInt(), CurPos());
                    Emit(RegexCode.Forejump);
                    break;

                case RegexNode.Greedy | BeforeChild:
                    Emit(RegexCode.Setjump);
                    break;

                case RegexNode.Greedy | AfterChild:
                    Emit(RegexCode.Forejump);
                    break;

                case RegexNode.One:
                case RegexNode.Notone:
                    Emit(node._type | bits, (int)node._ch);
                    break;

                case RegexNode.Notoneloop:
                case RegexNode.Notonelazy:
                case RegexNode.Oneloop:
                case RegexNode.Onelazy:
                    if (node._m > 0)
                        Emit(((node._type == RegexNode.Oneloop || node._type == RegexNode.Onelazy) ?
                              RegexCode.Onerep : RegexCode.Notonerep) | bits, (int)node._ch, node._m);
                    if (node._n > node._m)
                        Emit(node._type | bits, (int)node._ch, node._n == infinite ?
                             infinite : node._n - node._m);
                    break;

                case RegexNode.Setloop:
                case RegexNode.Setlazy:
                    if (node._m > 0)
                        Emit(RegexCode.Setrep | bits, StringCode(node._str), StringCode(node._str2), node._m);
                    if (node._n > node._m)
                        Emit(node._type | bits, StringCode(node._str), StringCode(node._str2), 
                             (node._n == infinite) ? infinite : node._n - node._m);
                    break;

                case RegexNode.Multi:
                    Emit(node._type | bits, StringCode(node._str));
                    break;

                case RegexNode.Set:
                    Emit(node._type | bits, StringCode(node._str), StringCode(node._str2));
                    break;

                case RegexNode.Ref:
                    Emit(node._type | bits, MapCapnum(node._m));
                    break;

                case RegexNode.Nothing:
                case RegexNode.Bol:
                case RegexNode.Eol:
                case RegexNode.Boundary:
                case RegexNode.Nonboundary:
#if ECMA
                case RegexNode.ECMABoundary:
                case RegexNode.NonECMABoundary:
#endif
                case RegexNode.Beginning:
                case RegexNode.Start:
                case RegexNode.EndZ:
                case RegexNode.End:
                    Emit(node._type);
                    break;

                default:
                    throw MakeException(SR.GetString(SR.UnexpectedOpcode, nodetype.ToString()));
            }
        }
    }
}
