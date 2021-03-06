//------------------------------------------------------------------------------
// <copyright file="IStateManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 * Interface implemented by objects that support state management.
 *
 * Copyright (c) 1999 Microsoft Corporation
 */
namespace System.Web.UI {

/// <include file='doc\IStateManager.uex' path='docs/doc[@for="IStateManager"]/*' />
/// <devdoc>
///    <para>Defines the contract that controls must implement to support state 
///       management.</para>
/// </devdoc>
public interface IStateManager {
    /*
     * Return true if tracking state changes.
     */
    /// <include file='doc\IStateManager.uex' path='docs/doc[@for="IStateManager.IsTrackingViewState"]/*' />
    /// <devdoc>
    ///    <para>Determines if state changes are being tracked.</para>
    ///    </devdoc>
    bool IsTrackingViewState {
        get;
    }
    
    /*
     * Load previously saved state.
     */
    /// <include file='doc\IStateManager.uex' path='docs/doc[@for="IStateManager.LoadViewState"]/*' />
    /// <devdoc>
    ///    <para>Loads the specified control's previously saved state.</para>
    ///    </devdoc>
    void LoadViewState(object state);

    /*
     * Return object containing state changes.
     */
    /// <include file='doc\IStateManager.uex' path='docs/doc[@for="IStateManager.SaveViewState"]/*' />
    /// <devdoc>
    ///    <para>Returns the object that contains the state changes.</para>
    ///    </devdoc>
    object SaveViewState();

    /*
     * Start tracking state changes.
     */
    /// <include file='doc\IStateManager.uex' path='docs/doc[@for="IStateManager.TrackViewState"]/*' />
    /// <devdoc>
    ///    <para>Instructs the control to start tracking changes in state.</para>
    ///    </devdoc>
    void TrackViewState();


}

}
