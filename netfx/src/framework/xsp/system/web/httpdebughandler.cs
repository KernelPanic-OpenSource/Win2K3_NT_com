//------------------------------------------------------------------------------
// <copyright file="HttpDebugHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 * Handler for DEBUG verb
 * 
 * Copyright (c) 2000 Microsoft Corporation
 */

namespace System.Web {

    using System.Web.Util;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Globalization;
    
/*
This is just a temporary code to get some timings for the Http Debug request.  To enable it, add the following
to the app's "web.config" file:

<configuration>

        <configSections>
            <section name="httpDebug" type="System.Configuration.DictionarySectionHandler,System" />
        </configSections>

        <httpDebug>
           <add key="enable" value="1" />
           <add key="path" value="c:\dir\file.log" />
        </httpDebug>

</configuration>

*/
    using System.Collections;

    internal class HttpDebugHandlerTimeLog {
        private static int initialTick = 0;
        private static int isEnabled = -1;
        private static StreamWriter writer = null;
        private static FileStream fstream = null;

        internal HttpDebugHandlerTimeLog() {
        }

        private static bool IsEnabled {
            get {
                if (isEnabled == -1) {
                    // Else check to see if config settings enables it
                    Hashtable h = (Hashtable) HttpContext.GetAppConfig("httpDebug");
                    if (h != null) {
                        string s = (string) h["enable"];
                        if ((s != null) && (s == "1")) {
                            // Open file handle if needed
                            try {
                                string filename = (string) h["path"];
                                if (filename != null) {
                                    fstream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                                    writer = new StreamWriter(fstream);
                                    isEnabled = 1;
                                }
                            }
                            catch (Exception) {
                                isEnabled = 0;
                                Close();
                            }
                        }
                    }
                    
                    // If still undecided here, then it's off
                    if (isEnabled == -1) {
                        isEnabled = 0;
                    }
                }

                return (isEnabled == 1) ? true : false;
            }
        }

        internal static void Close() {
            if (writer != null) {
                writer.Flush();
                writer.Close();
                writer = null;
            }
            if (fstream != null) {
                fstream.Close();
                fstream = null;
            }
        }
        
        internal static void PrintTickDelta(string s) {
            if (! IsEnabled) {
                return;
            }
            if (initialTick == 0) {
                initialTick = Environment.TickCount;
                SafeNativeMethods.OutputDebugString(s + " : " + initialTick + " (HttpDebugHandler + 0)\n");
                if (writer != null) {
                    writer.WriteLine(s + " : " + initialTick + " (HttpDebugHandler + 0)");
                }
            }
            else {
                int curTick = Environment.TickCount;
                SafeNativeMethods.OutputDebugString(s + " : " + curTick + " (HttpDebugHandler + " + (curTick - initialTick) + ")\n");
                if (writer != null) {
                    writer.WriteLine(s + " : " + curTick + " (HttpDebugHandler + " + (curTick - initialTick) + ")");
                }
            }
        }
    }

    /// <include file='doc\HttpDebugHandler.uex' path='docs/doc[@for="HttpDebugHandler"]/*' />
    /// <devdoc>
    ///    <para>Handler for debugging operations.</para>
    /// </devdoc>
    internal class HttpDebugHandler : IHttpHandler {

        private static string[] validClsIds = {"{70f65411-fe8c-4248-bcff-701c8b2f4529}",
                                               "{62a78ac2-7d9a-4377-b97e-6965919fdd02}",
                                               "{cc23651f-4574-438f-b4aa-bcb28b6b3ecf}",
                                               "{dbfdb1d0-04a4-4315-b15c-f874f6b6e90b}",
                                               "{a4fcb474-2687-4924-b0ad-7caf331db826}",
                                               "{beb261f6-d5f0-43ba-baf4-8b79785fffaf}",
                                               "{8e2f5e28-d4e2-44c0-aa02-f8c5beb70cac}",
                                               "{08100915-0f41-4ccf-9564-ebaa5d49446c}"};

        internal HttpDebugHandler () {
        }
        
        internal /*public*/ static bool IsDebuggingRequest(HttpContext context) {
            return (context.Request.HttpMethod == "DEBUG");
        }

        /// <include file='doc\HttpDebugHandler.uex' path='docs/doc[@for="HttpDebugHandler.ProcessRequest"]/*' />
        /// <devdoc>
        ///    <para>Drives web processing execution.</para>
        /// </devdoc>
        public void ProcessRequest(HttpContext context) {
            // Debugging must be enabled

            try {
                HttpDebugHandlerTimeLog.PrintTickDelta("Entered HttpDebugHandler");
                
                if (!HttpRuntime.DebuggingEnabled) {
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Debugging_forbidden, context.Request.Path));
                    context.Response.StatusCode = 403;
                    return;
                }

                // Check to see if it's a valid debug command.
                string command = context.Request.Headers["Command"];

                if (command == null) {
                    Debug.Trace("AutoAttach", "No debug command!!");
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Invalid_Debug_Request));
                    context.Response.StatusCode = 500;
                    return;
                }

                Debug.Trace("AutoAttach", "Debug command: " + command);

                if (string.Compare(command, "stop-debug", true, CultureInfo.InvariantCulture) == 0) {
                    context.Response.Write("OK");
                    return;
                }

                if (string.Compare(command, "start-debug", true, CultureInfo.InvariantCulture) != 0) {
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Invalid_Debug_Request));
                    context.Response.StatusCode = 500;
                    return;
                }

                // Request must be NTLM authenticated
                string authType = context.WorkerRequest.GetServerVariable("AUTH_TYPE"); // go the metal
                string logonUser  = context.WorkerRequest.GetServerVariable("LOGON_USER");

                Debug.Trace("AutoAttach", "Authentication type string: " + ((authType != null) ? authType : "NULL"));
                Debug.Trace("AutoAttach", "Logon user string: " + ((logonUser != null) ? logonUser : "NULL"));

                if (logonUser == null || logonUser.Length == 0 || authType == null || authType.Length == 0 || String.Compare(authType, "basic", true, CultureInfo.InvariantCulture) == 0) 
                {
                    Debug.Trace("AutoAttach", "Invalid logon_user or auth_type string.");
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Debug_Access_Denied, context.Request.Path));
                    context.Response.StatusCode = 401;
                    return;
                }

                // Get the session ID
                String sessId = context.Request.Form["DebugSessionID"];

                Debug.Trace("AutoAttach", "DebugSessionID: " + ((sessId != null) ? sessId : "NULL"));

                if (sessId == null || sessId.Length == 0) {
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Invalid_Debug_ID));
                    context.Response.StatusCode = 500;
                    return;
                }

                string s = sessId.Replace(';', '&');

                HttpValueCollection valCol = new HttpValueCollection(s, true, true, Encoding.UTF8);
                string clsId = (string) valCol["autoattachclsid"];

                // Verify clsId
                bool isClsIdOk = false;
                if (clsId != null) {
                    for (int i = 0; i < validClsIds.Length; i++) {
                        if (clsId.ToLower(System.Globalization.CultureInfo.InvariantCulture) == validClsIds[i]) {
                            isClsIdOk = true;
                            break;
                        }
                    }
                }
                if (isClsIdOk == false) {
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Debug_Access_Denied, context.Request.Path));
                    context.Response.StatusCode = 401;
                    Debug.Trace("AutoAttach", "Debug attach not attempted because of invalid CLSID.");
                    return;
                }

                // Attach the debugger
                HttpDebugHandlerTimeLog.PrintTickDelta("About to call into MDM");

                int rc = UnsafeNativeMethods.AttachDebugger(clsId, sessId, context.WorkerRequest.GetUserToken());

                HttpDebugHandlerTimeLog.PrintTickDelta("Returned from call to MDM");

                // If it's not S_OK, then we got a problem
                if (rc != 0) {
                    Debug.Trace("AutoAttach", "Debug attach failed! Return code: " + rc);
                    context.Response.Write(HttpRuntime.FormatResourceString(SR.Error_Attaching_with_MDM, "0x" + rc.ToString("X8")));
                    context.Response.StatusCode = 500;
                    return;
                }

                Debug.Trace("AutoAttach", "Debug attach successful!");

                // Everything ok -- increment counter, return something (not 204)
                PerfCounters.IncrementCounter(AppPerfCounter.DEBUGGING_REQUESTS);
                context.Response.Write("OK");

                // Set global flag for us
                HttpRuntime.VSDebugAttach = true;
            }
            finally {
                Debug.Trace("AutoAttach","Http Debug attach done!");

                HttpDebugHandlerTimeLog.PrintTickDelta("Leaving HttpDebugHandler");
                HttpDebugHandlerTimeLog.Close();
            }
        }

        /// <include file='doc\HttpDebugHandler.uex' path='docs/doc[@for="HttpDebugHandler.IsReusable"]/*' />
        /// <devdoc>
        ///    <para>Indicates whether an HttpDebugHandler instance can be recycled and used for 
        ///       another request.</para>
        /// </devdoc>
        public bool IsReusable {
            get { return true; }
        }
    }

}

