//------------------------------------------------------------------------------
// <copyright file="SessionViewState.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Web.Util;
using System.Web.UI;
using System.Security.Permissions;

namespace System.Web.UI.MobileControls
{
    /*
     * Session-based view state.
     *
     * When saving view state on the server as session data, some critical problems
     * arise. The core issue behind most of these is how to handle the user 
     * clicking the Back button. When the user does this, there is no corresponding
     * notification to the server, and the client and server session state are thrown
     * out of sync. 
     *
     * This class attempts to alleviate this by storing a small history of view states
     * in session data. 
     *
     * To save session view state, construct a new object, set the ViewState and ActiveForm
     * properties, and call Save. You'll get back a reference that contains the 
     * state reference to write out.
     *
     * To load session view state, construct a new object, and call Load. The class will
     * attempt to construct the view state from its history.
     *
     * Copyright (c) 2000 Microsoft Corporation
     */

    internal class SessionViewState
    {
        private static readonly String ViewStateKey = "ViewState";
        private Object _state;

        internal SessionViewState() {
        }

        internal /*public*/ Object ViewState
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        internal /*public*/ String Save(MobilePage page)
        {
            SessionViewStateHistory history = (SessionViewStateHistory)page.Session[ViewStateKey];
            if (history == null)
            {
                history = new SessionViewStateHistory(HttpContext.Current);
                page.Session[ViewStateKey] = history;
            }

            SessionViewStateHistoryItem historyItem = new SessionViewStateHistoryItem();
            SaveTo(historyItem);
            #if TRACE
            historyItem.Url = page.Request.FilePath;
            #endif
            return history.Push(historyItem);
        }

        internal /*public*/ void Load(MobilePage page, String id)
        {
            _state = null;

            SessionViewStateHistory history = (SessionViewStateHistory)page.Session[ViewStateKey];
            if (history != null)
            {
                SessionViewStateHistoryItem historyItem = history.Find(id);
                if (historyItem != null)
                {
                    LoadFrom(historyItem);
                }
            }
        }

        private void SaveTo(SessionViewStateHistoryItem historyItem)
        {
            historyItem.ViewState = _state;
        }

        private void LoadFrom(SessionViewStateHistoryItem historyItem)
        {
            _state = historyItem.ViewState;
        }

        #if TRACE
        internal /*public*/ void Dump(MobilePage page, out ArrayList arr)
        {
            SessionViewStateHistory history;
            if ((page is IRequiresSessionState) && !(page is IReadOnlySessionState))
            {
                history = (SessionViewStateHistory)page.Session[ViewStateKey];
            }
            else
            {
                history = null;
            }

            if (history != null)
            {
                history.Dump(out arr);
            }
            else
            {
                arr = new ArrayList();
            }
        }
        #endif

        [
            Serializable
        ]
        private class SessionViewStateHistoryItem : ISerializable
        {
            #if TRACE
            public String Url;
            public String Id;
            #endif
            public Object ViewState;

            public SessionViewStateHistoryItem()
            {
            }

            public SessionViewStateHistoryItem(SerializationInfo info, StreamingContext context)
            {
                String s = (String)info.GetString("s");
                if (s.Length > 0)
                {
                    ViewState = new LosFormatter().Deserialize(s);
                }
                else
                {
                    ViewState = null;
                }
            }

            [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (ViewState != null)
                {
                    StringWriter s = new StringWriter();
                    new LosFormatter().Serialize(s, ViewState);
                    info.AddValue("s", s.ToString());
                }
                else
                {
                    info.AddValue("s", String.Empty);
                }
            }
        }

        // Session view state history. This is the history record kept in each session.

        [
            Serializable
        ]
        private class SessionViewStateHistory
        {
            private int _historySize;
            private SessionViewStateHistoryItem[] _history;
            private int _currentHistoryIndex = 0;
            private int _historyUsed = 0;
            private String _sessionUniqueID = Int64ToString(DateTime.Now.Ticks);
            private int _currentHistoryID = 0;

            public SessionViewStateHistory(HttpContext context)
            {
                _historySize = ControlsConfig.GetFromContext(context).SessionStateHistorySize;
                if (_historySize < 1)
                {
                    throw new Exception(
                        SR.GetString(SR.SessionViewState_InvalidSessionStateHistory));
                }

                _history = new SessionViewStateHistoryItem[_historySize];
            }

            public String Push(SessionViewStateHistoryItem item)
            {
                String id = _sessionUniqueID + "," + _currentHistoryID.ToString();
                _currentHistoryID++;

                _history[_currentHistoryIndex] = item;
                _currentHistoryIndex = (_currentHistoryIndex + 1) % _historySize;
                if (_historyUsed < _historySize)
                {
                    _historyUsed++;
                }

                #if TRACE
                item.Id = _currentHistoryID.ToString();
                #endif
                
                return id;
            }

            public SessionViewStateHistoryItem Find(String id)
            {
                // First make sure that the page is from the current session.
                int separator = id.IndexOf(',');
                if (separator != _sessionUniqueID.Length || 
                    separator == id.Length - 1 ||
                    String.Compare(_sessionUniqueID, 0, id, 0, separator) != 0)
                {
                    return null;
                }

                // Now check if we actually still have it.
                int historyID = Int32.Parse(id.Substring(separator + 1));
                int distance = _currentHistoryID - historyID;

                if (distance <= 0)
                {
                    // Shouldn't happen, but this would be a forward jump.
                    return null;
                }
                else if (distance > _historyUsed)
                {
                    // Gone way back. Empty history, but return null.
                    _historyUsed = 0;
                    return null;
                }
                else
                {
                    int foundIndex = (_currentHistoryIndex + _historySize - distance) % 
                                     _historySize;
                    // Make the found item the top of the stack.
                    _currentHistoryIndex = (foundIndex + 1) % _historySize;
                    _currentHistoryID = historyID + 1;
                    _historyUsed -= distance - 1;
                    return _history[foundIndex];
                }
            }

            private static String Int64ToString(Int64 number)
            {
                // Since each Unicode char is 16 bits, for the best encoding
                // to String we take 2^16 - 1 for the base.  The reason of -1
                // is to eliminate the case of having 0x0000 as the encoded
                // value since it is a String terminator.
                const int EncodingBase = 65535;
                StringBuilder stringBuilder = new StringBuilder();
                Int64 quotient = number;
                char remainder;

                while (quotient != 0)
                {
                    // REVIEW: Is there any better algorithm on doing such
                    // encoding?

                    // +1 is to eliminate the case of having remainder as 0
                    // (String terminator)
                    remainder = (char) ((quotient % EncodingBase) + 1);
                    stringBuilder.Append(remainder);
                    quotient = quotient / EncodingBase;
                }

                return stringBuilder.ToString();
            }

            #if TRACE
            public void Dump(out ArrayList arr)
            {
                arr = new ArrayList();
                int n = _currentHistoryIndex;
                for (int i = 0; i < _historyUsed; i++)
                {
                    n = n - 1;
                    if (n == -1)
                    {
                        n = _history.Length - 1;
                    }

                    SessionViewStateHistoryItem item = _history[n];
                    if (item != null)
                    {
                        arr.Add(String.Format("{0}({1})", item.Url, item.Id));
                    }
                    else
                    {
                        arr.Add("(null)");
                    }
                }
            }
            #endif
        }

    }
}
