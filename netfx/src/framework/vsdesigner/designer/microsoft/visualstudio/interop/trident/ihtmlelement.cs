//------------------------------------------------------------------------------
/// <copyright file="IHTMLElement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//---------------------------------------------------------------------------
// Microsoft.VisualStudio.Interop.Trident.IHTMLElement.cs
//---------------------------------------------------------------------------
// WARNING: this file autogenerated
//---------------------------------------------------------------------------
// Copyright (c) 1999, Microsoft Corporation   All Rights Reserved
// Information Contained Herein Is Proprietary and Confidential.
//---------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Interop.Trident {

    using System;
    using System.Runtime.InteropServices;

    [ComVisible(true),Guid("3050F1FF-98B5-11CF-BB82-00AA00BDCE0B"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    internal interface IHTMLElement {


        void SetAttribute(
            [In,MarshalAs(UnmanagedType.BStr)]
            string strAttributeName,

            Object AttributeValue,

            int lFlags);


        void GetAttribute(
            [In,MarshalAs(UnmanagedType.BStr)]
            string strAttributeName,

            int lFlags,
            [Out,MarshalAs(UnmanagedType.LPArray)]
            Object[] pvars);


        bool RemoveAttribute(
            [In,MarshalAs(UnmanagedType.BStr)]
            string strAttributeName,

            int lFlags);


        void SetClassName(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetClassName();


        void SetId(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetId();

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetTagName();


        Microsoft.VisualStudio.Interop.Trident.IHTMLElement GetParentElement();


        Microsoft.VisualStudio.Interop.Trident.IHTMLStyle GetStyle();


        void SetOnhelp(

            Object p);


        Object GetOnhelp();


        void SetOnclick(

            Object p);


        Object GetOnclick();


        void SetOndblclick(

            Object p);


        Object GetOndblclick();


        void SetOnkeydown(

            Object p);


        Object GetOnkeydown();


        void SetOnkeyup(

            Object p);


        Object GetOnkeyup();


        void SetOnkeypress(

            Object p);


        Object GetOnkeypress();


        void SetOnmouseout(

            Object p);


        Object GetOnmouseout();


        void SetOnmouseover(

            Object p);


        Object GetOnmouseover();


        void SetOnmousemove(

            Object p);


        Object GetOnmousemove();


        void SetOnmousedown(

            Object p);


        Object GetOnmousedown();


        void SetOnmouseup(

            Object p);


        Object GetOnmouseup();

        [return: MarshalAs(UnmanagedType.Interface)]
            IHTMLDocument2 GetDocument();


        void SetTitle(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetTitle();


        void SetLanguage(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetLanguage();


        void SetOnselectstart(

            Object p);


        Object GetOnselectstart();


        void ScrollIntoView(

            Object varargStart);


        bool Contains(

            Microsoft.VisualStudio.Interop.Trident.IHTMLElement pChild);


        int GetSourceIndex();


        Object GetRecordNumber();


        void SetLang(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetLang();


        int GetOffsetLeft();


        int GetOffsetTop();


        int GetOffsetWidth();


        int GetOffsetHeight();


        Microsoft.VisualStudio.Interop.Trident.IHTMLElement GetOffsetParent();


        void SetInnerHTML(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetInnerHTML();


        void SetInnerText(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetInnerText();


        void SetOuterHTML(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetOuterHTML();


        void SetOuterText(
            [In,MarshalAs(UnmanagedType.BStr)]
            string p);

        [return: MarshalAs(UnmanagedType.BStr)]
            string GetOuterText();


        void InsertAdjacentHTML(
            [In,MarshalAs(UnmanagedType.BStr)]
            string where,
            [In,MarshalAs(UnmanagedType.BStr)]
            string html);


        void InsertAdjacentText(
            [In,MarshalAs(UnmanagedType.BStr)]
            string where,
            [In,MarshalAs(UnmanagedType.BStr)]
            string text);


        Microsoft.VisualStudio.Interop.Trident.IHTMLElement GetParentTextEdit();


        bool GetIsTextEdit();


        void Click();

        [return: MarshalAs(UnmanagedType.Interface)]
            object GetFilters();


        void SetOndragstart(

            Object p);


        Object GetOndragstart();

        [return: MarshalAs(UnmanagedType.BStr)]
            string toString();


        void SetOnbeforeupdate(

            Object p);


        Object GetOnbeforeupdate();


        void SetOnafterupdate(

            Object p);


        Object GetOnafterupdate();


        void SetOnerrorupdate(

            Object p);


        Object GetOnerrorupdate();


        void SetOnrowexit(

            Object p);


        Object GetOnrowexit();


        void SetOnrowenter(

            Object p);


        Object GetOnrowenter();


        void SetOndatasetchanged(

            Object p);


        Object GetOndatasetchanged();


        void SetOndataavailable(

            Object p);


        Object GetOndataavailable();


        void SetOndatasetcomplete(

            Object p);


        Object GetOndatasetcomplete();


        void SetOnfilterchange(

            Object p);


        Object GetOnfilterchange();

        [return: MarshalAs(UnmanagedType.Interface)]
            object GetChildren();

        [return: MarshalAs(UnmanagedType.Interface)]
            object GetAll();

    }
}
