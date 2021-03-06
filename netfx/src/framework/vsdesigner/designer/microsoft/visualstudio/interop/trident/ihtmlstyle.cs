//------------------------------------------------------------------------------
/// <copyright file="IHTMLStyle.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// Microsoft.VisualStudio.Interop.Trident.IHTMLStyle.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Trident {

    using System;
    using System.Runtime.InteropServices;

    [ComVisible(true),Guid("3050F25E-98B5-11CF-BB82-00AA00BDCE0B"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    internal interface IHTMLStyle {

        void SetFontFamily(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetFontFamily();


        void SetFontStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetFontStyle();


        void SetFontObject(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetFontObject();


        void SetFontWeight(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetFontWeight();


        void SetFontSize(object p);

        object GetFontSize();

        void SetFont(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetFont();


        void SetColor(object p);

        object GetColor();

        void SetBackground(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBackground();


        void SetBackgroundColor(object p);

        object GetBackgroundColor();


        void SetBackgroundImage(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBackgroundImage();


        void SetBackgroundRepeat(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBackgroundRepeat();


        void SetBackgroundAttachment(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBackgroundAttachment();


        void SetBackgroundPosition(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBackgroundPosition();


        void SetBackgroundPositionX(object p);

        object GetBackgroundPositionX();


        void SetBackgroundPositionY(object p);

        object GetBackgroundPositionY();

        void SetWordSpacing(object p);

        object GetWordSpacing();

        void SetLetterSpacing(object p);

        object GetLetterSpacing();

        void SetTextDecoration(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetTextDecoration();


        void SetTextDecorationNone(bool p);

        bool GetTextDecorationNone();

        void SetTextDecorationUnderline(bool p);

        bool GetTextDecorationUnderline();

        void SetTextDecorationOverline(bool p);

        bool GetTextDecorationOverline();

        void SetTextDecorationLineThrough(bool p);

        bool GetTextDecorationLineThrough();

        void SetTextDecorationBlink(bool p);

        bool GetTextDecorationBlink();

        void SetVerticalAlign(object p);

        object GetVerticalAlign();

        void SetTextTransform(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetTextTransform();


        void SetTextAlign(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetTextAlign();


        void SetTextIndent(object p);

        object GetTextIndent();

        void SetLineHeight(object p);

        object GetLineHeight();

        void SetMarginTop(object p);

        object GetMarginTop();

        void SetMarginRight(object p);

        object GetMarginRight();

        void SetMarginBottom(object p);

        object GetMarginBottom();

        void SetMarginLeft(object p);

        object GetMarginLeft();

        void SetMargin(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetMargin();


        void SetPaddingTop(object p);

        object GetPaddingTop();

        void SetPaddingRight(object p);

        object GetPaddingRight();

        void SetPaddingBottom(object p);

        object GetPaddingBottom();

        void SetPaddingLeft(object p);

        object GetPaddingLeft();

        void SetPadding(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetPadding();


        void SetBorder(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorder();


        void SetBorderTop(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderTop();


        void SetBorderRight(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderRight();


        void SetBorderBottom(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderBottom();


        void SetBorderLeft(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderLeft();


        void SetBorderColor(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderColor();


        void SetBorderTopColor(object p);

        object GetBorderTopColor();

        void SetBorderRightColor(object p);

        object GetBorderRightColor();

        void SetBorderBottomColor(object p);

        object GetBorderBottomColor();

        void SetBorderLeftColor(object p);

        object GetBorderLeftColor();

        void SetBorderWidth(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderWidth();


        void SetBorderTopWidth(object p);

        object GetBorderTopWidth();

        void SetBorderRightWidth(object p);

        object GetBorderRightWidth();

        void SetBorderBottomWidth(object p);

        object GetBorderBottomWidth();

        void SetBorderLeftWidth(object p);

        object GetBorderLeftWidth();

        void SetBorderStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderStyle();


        void SetBorderTopStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderTopStyle();


        void SetBorderRightStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderRightStyle();


        void SetBorderBottomStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderBottomStyle();


        void SetBorderLeftStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetBorderLeftStyle();


        void SetWidth(object p);

        object GetWidth();

        void SetHeight(object p);

        object GetHeight();

        void SetStyleFloat(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetStyleFloat();


        void SetClear(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetClear();


        void SetDisplay(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetDisplay();


        void SetVisibility(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetVisibility();


        void SetListStyleType(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetListStyleType();


        void SetListStylePosition(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetListStylePosition();


        void SetListStyleImage(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetListStyleImage();


        void SetListStyle(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetListStyle();


        void SetWhiteSpace(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetWhiteSpace();


        void SetTop(object p);

        object GetTop();

        void SetLeft(object p);

        object GetLeft();

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetPosition();


        void SetZIndex(object p);

        object GetZIndex();

        void SetOverflow(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetOverflow();


        void SetPageBreakBefore(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetPageBreakBefore();


        void SetPageBreakAfter(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetPageBreakAfter();


        void SetCssText(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetCssText();


        void SetPixelTop(int p);

        int GetPixelTop();

        void SetPixelLeft(int p);

        int GetPixelLeft();

        void SetPixelWidth(int p);

        int GetPixelWidth();

        void SetPixelHeight(int p);

        int GetPixelHeight();

        void SetPosTop(float p);

        float GetPosTop();

        void SetPosLeft(float p);

        float GetPosLeft();

        void SetPosWidth(float p);

        float GetPosWidth();

        void SetPosHeight(float p);

        float GetPosHeight();

        void SetCursor(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetCursor();


        void SetClip(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetClip();


        void SetFilter(
            [MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetFilter();


        void SetAttribute(
            [MarshalAs(UnmanagedType.BStr)]
            string strAttributeName,
            object AttributeValue,
            int lFlags);

        object GetAttribute(
            [MarshalAs(UnmanagedType.BStr)]
            string strAttributeName,
            int lFlags);

        bool RemoveAttribute(
            [MarshalAs(UnmanagedType.BStr)]
            string strAttributeName,
            int lFlags);

    }
}
