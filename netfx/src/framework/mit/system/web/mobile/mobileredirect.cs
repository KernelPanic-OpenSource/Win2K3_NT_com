//------------------------------------------------------------------------------
// <copyright file="MobileRedirect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Web; 
using System.Web.UI.MobileControls;

namespace System.Web.Mobile
{
    /*
     * Mobile Redirect
     * An internal helper class that provides methods to work around redirection issues with
     * mobile devices.
     *
     * Copyright (c) 2000 Microsoft Corporation
     */
    
    internal class MobileRedirect 
    {
        internal static readonly String QueryStringVariable = "__redir";
        internal static readonly String QueryStringValue = "1";
        internal static readonly String QueryStringAssignment = QueryStringVariable + "=" + QueryStringValue;

        private static readonly String _redirectQueryString = "?" + QueryStringAssignment;
        private static readonly String _redirectQueryStringInline = QueryStringAssignment + "&";

        private static readonly String _disallowRedirectionKey = "_disallowRedirection";

        internal MobileRedirect() 
        {
        }

        internal static void AllowRedirection(HttpContext context)
        {
            context.Items.Remove(_disallowRedirectionKey);
        }

        internal static void DisallowRedirection(HttpContext context)
        {
            context.Items[_disallowRedirectionKey] = 1;
        }

        internal static void CheckForInvalidRedirection(HttpContext context)
        {
            HttpResponse response = context.Response;
            if (response != null && 
                    response.StatusCode == 302 && 
                    context.Items[_disallowRedirectionKey] != null)
            {
                response.ClearHeaders();
                throw new Exception(SR.GetString(SR.MobileRedirect_RedirectNotAllowed));
            }
        }

        internal static void RedirectToUrl(HttpContext context, String url, bool endResponse)
        {
            //do not add __redir=1 if it already exists
            int i = url.IndexOf(QueryStringAssignment);
            if(i == -1)
            {
                i = url.IndexOf('?');
                if (i >= 0)
                {
                    url = url.Insert(i + 1, _redirectQueryStringInline);
                }
                else
                {
                    url = String.Concat(url, _redirectQueryString);
                }
            }
            AllowRedirection(context);
            MobilePage page = context.Handler as MobilePage;
            
            if ( (page != null) && (!page.Device.SupportsRedirectWithCookie) )
            {
                String formsAuthCookieName = Security.FormsAuthentication.FormsCookieName;
                if(formsAuthCookieName != String.Empty)
                {
                    context.Response.Cookies.Remove(formsAuthCookieName);
                }
            } 
            context.Response.Redirect(url, endResponse);
        }
    }
}

