// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using WbemClient_v1;

namespace System.Management
{

	/// <summary>
	/// <para>Represents the method that will handle the <see cref='E:System.Management.ManagementOperationObserver.ObjectReady'/> event.</para>
	/// </summary>
	public delegate void ObjectReadyEventHandler(object sender, ObjectReadyEventArgs e);

	/// <summary>
	/// <para>Represents the method that will handle the <see cref='E:System.Management.ManagementOperationObserver.Completed'/> event.</para>
	/// </summary>
	public delegate void CompletedEventHandler (object sender, CompletedEventArgs e);

	/// <summary>
	/// <para>Represents the method that will handle the <see cref='E:System.Management.ManagementOperationObserver.Progress'/> event.</para>
	/// </summary>
	public delegate void ProgressEventHandler (object sender, ProgressEventArgs e);

	/// <summary>
	/// <para>Represents the method that will handle the <see cref='E:System.Management.ManagementOperationObserver.ObjectPut'/> event.</para>
	/// </summary>
	public delegate void ObjectPutEventHandler(object sender, ObjectPutEventArgs e);

	/// <summary>
	///    <para>Used to manage asynchronous operations and handle management information and events received asynchronously.</para>
	/// </summary>
	/// <example>
	///    <code lang='C#'>using System; 
	/// using System.Management; 
	/// 
	/// // This sample demonstrates how to read a ManagementObject asychronously 
	/// // using the ManagementOperationObserver object. 
	/// 
	/// class Sample_ManagementOperationObserver {
	///     public static int Main(string[] args) {
	/// 
	///         //Set up a handler for the asynchronous callback
	///         ManagementOperationObserver observer = new ManagementOperationObserver(); 
	///         MyHandler completionHandler = new MyHandler(); 
	///         observer.Completed += new CompletedEventHandler(completionHandler.Done); 
	/// 
	///         //Invoke the asynchronous read of the object
	///         ManagementObject disk = new ManagementObject("Win32_logicaldisk='C:'"); 
	///         disk.Get(observer); 
	/// 
	///         //For the purpose of this sample, we keep the main 
	///         // thread alive until the asynchronous operation is completed. 
	/// 
	///         while (!completionHandler.IsComplete) { 
	///             System.Threading.Thread.Sleep(500); 
	///         } 
	/// 
	///         Console.WriteLine("Size= " + disk["Size"] + " bytes."); 
	/// 
	///         return 0; 
	///     } 
	/// 
	///     public class MyHandler 
	///     {
	///         private bool isComplete = false;
	///  
	///         public void Done(object sender, CompletedEventArgs e) { 
	///             isComplete = true;
	///         }
	/// 
	///         public bool IsComplete {
	///             get {
	///                 return isComplete;
	///             }
	///         }
	///     }
	/// }
	///    </code>
	///    <code lang='VB'>Imports System
	/// Imports System.Management
	/// 
	/// ' This sample demonstrates how to read a ManagementObject asychronously
	/// ' using the ManagementOperationObserver object.
	/// 
	/// Class Sample_ManagementOperationObserver
	///     Overloads Public Shared Function Main(args() As String) As Integer
	/// 
	///         'Set up a handler for the asynchronous callback
	///         Dim observer As New ManagementOperationObserver()
	///         Dim completionHandler As New MyHandler()
	///         AddHandler observer.Completed, AddressOf completionHandler.Done
	///       
	///         ' Invoke the object read asynchronously
	///         Dim disk As New ManagementObject("Win32_logicaldisk='C:'")
	///         disk.Get(observer)
	/// 
	///         ' For the purpose of this sample, we keep the main
	///         ' thread alive until the asynchronous operation is finished.
	///         While Not completionHandler.IsComplete Then
	///             System.Threading.Thread.Sleep(500)
	///         End While
	///        
	///         Console.WriteLine("Size = " + disk("Size").ToString() &amp; " bytes")
	///       
	///         Return 0
	///     End Function
	/// 
	///     Public Class MyHandler
	///         Private _isComplete As Boolean = False
	///    
	///         Public Sub Done(sender As Object, e As CompletedEventArgs)
	///             _isComplete = True
	///         End Sub 'Done
	/// 
	///         Public ReadOnly Property IsComplete() As Boolean
	///             Get
	///                 Return _isComplete
	///             End Get
	///         End Property
	///     End Class
	/// End Class
	///    </code>
	/// </example>
	public class ManagementOperationObserver 
	{
		private Hashtable m_sinkCollection;
		private WmiDelegateInvoker delegateInvoker;
		
		/// <summary>
		///    <para> Occurs when a new object is available.</para>
		/// </summary>
		public event ObjectReadyEventHandler		ObjectReady;

		/// <summary>
		///    <para> Occurs when an operation has completed.</para>
		/// </summary>
		public event CompletedEventHandler			Completed;

		/// <summary>
		///    <para> Occurs to indicate the progress of an ongoing operation.</para>
		/// </summary>
		public event ProgressEventHandler			Progress;

		/// <summary>
		///    <para> Occurs when an object has been successfully committed.</para>
		/// </summary>
		public event ObjectPutEventHandler			ObjectPut;

		/// <summary>
		/// <para>Initializes a new instance of the <see cref='System.Management.ManagementOperationObserver'/> class. This is the default constructor.</para>
		/// </summary>
		public ManagementOperationObserver () 
		{
			// We make our sink collection synchronized
			m_sinkCollection = new Hashtable ();
			delegateInvoker = new WmiDelegateInvoker (this);
		}

		/// <summary>
		///    <para> Cancels all outstanding operations.</para>
		/// </summary>
		public void Cancel () 
		{
			// Cancel all the sinks we have - make a copy to avoid things
			// changing under our feet
			Hashtable copiedSinkTable =  new Hashtable ();

			lock (m_sinkCollection) {
				// TODO - the MemberwiseClone method should be used here
				IDictionaryEnumerator sinkEnum = m_sinkCollection.GetEnumerator();

				try {
					sinkEnum.Reset ();
					
					while (sinkEnum.MoveNext ()) {
						DictionaryEntry entry = (DictionaryEntry) sinkEnum.Current;
						copiedSinkTable.Add (entry.Key, entry.Value);
					}
				} catch {}
			}

			// Now step through the copy and cancel everything
			try {
				IDictionaryEnumerator copiedSinkEnum = copiedSinkTable.GetEnumerator();
				copiedSinkEnum.Reset ();

				while (copiedSinkEnum.MoveNext ())
				{
					DictionaryEntry entry = (DictionaryEntry) copiedSinkEnum.Current;
					WmiEventSink eventSink = (WmiEventSink) entry.Value;

					try {
						eventSink.Cancel ();
					} catch {}
				}
			} catch {}
		}

		internal WmiEventSink GetNewSink (
						ManagementScope scope, 
						object context) 
		{
			try {
				WmiEventSink eventSink = WmiEventSink.GetWmiEventSink(this, context, scope, null, null);
		
				// Add it to our collection
				lock (m_sinkCollection) {
					m_sinkCollection.Add (eventSink.GetHashCode(), eventSink);
				}

				return eventSink;
			} catch {
				return null;
			}
		}

		internal bool HaveListenersForProgress 
		{
			get 
			{
				bool result = false;

				try {
					if (Progress != null)
						result = ((Progress.GetInvocationList ()).Length > 0);
				} catch (Exception) { }

				return result;
			}
		}
		internal WmiEventSink GetNewPutSink (
						ManagementScope scope, 
						object context,
						string path,
						string className) 
		{
			try {
				WmiEventSink eventSink = WmiEventSink.GetWmiEventSink(this, context, scope, path, className);
		
				// Add it to our collection
				lock (m_sinkCollection) {
					m_sinkCollection.Add (eventSink.GetHashCode(), eventSink);
				}

				return eventSink;
			} catch {
				return null;
			}
		}

		internal WmiGetEventSink GetNewGetSink (
							ManagementScope scope,
							object context,
							ManagementObject managementObject)
		{
			try {
				WmiGetEventSink eventSink = WmiGetEventSink.GetWmiGetEventSink(this, 
									context, scope, managementObject);
		
				// Add it to our collection
				lock (m_sinkCollection) {
					m_sinkCollection.Add (eventSink.GetHashCode(), eventSink);
				}

				return eventSink;
			} catch {
				return null;
			}
		}

		internal void RemoveSink (WmiEventSink eventSink)
		{
			try {
				lock (m_sinkCollection) {
					m_sinkCollection.Remove (eventSink.GetHashCode ());
				}

				// Release the stub as we are now disconnected
				eventSink.ReleaseStub ();
			} catch {}
		}

		/// <summary>
		/// Fires the ObjectReady event to whomsoever is listening
		/// </summary>
		/// <param name="args"> </param>
		internal void FireObjectReady (ObjectReadyEventArgs args)
		{
			try {
				delegateInvoker.FireEventToDelegates (ObjectReady, args);
			} catch {}
		}

		internal void FireCompleted (CompletedEventArgs args)
		{
			try {
				delegateInvoker.FireEventToDelegates (Completed, args);
			} catch {}
		}

		internal void FireProgress (ProgressEventArgs args)
		{
			try {
				delegateInvoker.FireEventToDelegates (Progress, args);
			} catch {}
		}

		internal void FireObjectPut (ObjectPutEventArgs args)
		{
			try {
				delegateInvoker.FireEventToDelegates (ObjectPut, args);
			} catch {}
		}
	}

	internal class WmiEventState 
	{
		private Delegate d;
		private ManagementEventArgs args;
		private AutoResetEvent h;

		internal WmiEventState (Delegate d, ManagementEventArgs args, AutoResetEvent h)
		{
			this.d = d;
			this.args = args;
			this.h = h;
		}

		public Delegate Delegate {
			get { return d; }
		}

		public ManagementEventArgs Args {
			get { return args; }
		}

		public AutoResetEvent AutoResetEvent {
			get { return h; }
		}
	}

	/// <summary>
	/// This class handles the posting of events to delegates. For each event
	/// it queues a set of requests (one per target delegate) to the thread pool
	/// to handle the event. It ensures that no single delegate can throw
	/// an exception that prevents the event from reaching any other delegates.
	/// It also ensures that the sender does not signal the processing of the
	/// WMI event as "done" until all target delegates have signalled that they are
	/// done.
	/// </summary>
	internal class WmiDelegateInvoker
	{
		internal object sender;

		internal WmiDelegateInvoker (object sender)
		{
			this.sender = sender;
		}

		/// <summary>
		/// Custom handler for firing a WMI event to a list of delegates. We use
		/// the process thread pool to handle the firing.
		/// </summary>
		/// <param name="md">The MulticastDelegate representing the collection
		/// of targets for the event</param>
		/// <param name="args">The accompanying event arguments</param>
		internal void FireEventToDelegates (MulticastDelegate md, ManagementEventArgs args)
		{
			try 
			{
				if (null != md)
				{
	#if USEPOOL
					Delegate[] delegateList = md.GetInvocationList ();

					if (null != delegateList)
					{
						int numDelegates = delegateList.Length;
						AutoResetEvent[] waitHandles = new AutoResetEvent [numDelegates];

						/*
						 * For each target delegate, queue a request to the 
						 * thread pool to handle the POST. We pass as state the 
						 *  1) Target delegate
						 *  2) Event args
						 *  3) AutoResetEvent that the thread should signal to 
						 *     indicate that it is done.
						 */
						for (int i = 0; i < numDelegates; i++)
						{
							waitHandles [i] = new AutoResetEvent (false);
							ThreadPool.QueueUserWorkItem (
									new WaitCallback (this.FireEventToDelegate),
									new WmiEventState (delegateList[i], args, waitHandles[i]));
						}

						/*
						 * We wait for all the delegates to signal that they are done.
						 * This is because if we return from the IWbemObjectSink callback
						 * before they are all done, it is possible that a delegate will
						 * begin to process the next callback before the current callback
						 * processing is done.
						 */
						WaitHandle.WaitAll (waitHandles, 10000, true);
						}
					}
	#endif
					foreach (Delegate d in md.GetInvocationList())
					{
						try {
							d.DynamicInvoke (new object [] {this.sender, args});	
						} catch {}
					}
				}
			}
			catch {}
		}

	#if USE_POOL
		/// <summary>
		/// This is the WaitCallback for firing an event from the thread pool
		/// </summary>
		/// <param name="state">Represents a WmiEventState object</param>
		internal void FireEventToDelegate (object state)
		{
			if (state is WmiEventState)
			{
				WmiEventState oState = (WmiEventState) state;

				try {
					oState.Delegate.DynamicInvoke (new object [] {this.sender, oState.Args});	
				} catch {}
				finally {
					// Signal that we are done
					oState.AutoResetEvent.Set();
				}
			}
		}
	#endif
	}

}
