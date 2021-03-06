//------------------------------------------------------------------------------
// <copyright file="ObjectListCommandCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Security.Permissions;

namespace System.Web.UI.MobileControls
{
    /*
     * Object List Command Collection class.
     *
     * Copyright (c) 2000 Microsoft Corporation
     */

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class ObjectListCommandCollection : ArrayListCollectionBase, IStateManager
    {
        // ObjectListCommandCollection has a special form of viewstate management.
        // In normal operation, if you add, remove or modify commands in the 
        // collection, the results are saved as part of view state.
        //
        // However, when showing the commands for an item, the ObjectList control
        // raises an event that allows the application to alter the commands
        // specifically for the item. These changes are considered local to the
        // item, and are not saved.
        //
        // To do this, the class uses a private state, declared in the MarkState
        // enumeration below. The following sequence of events are used:
        //
        // 1) Class is created. MarkState is set to MarkState.NotMarked.
        // 2) Class is initialized from persistent values.
        // 3) Framework calls TrackViewState. MarkState is set to MarkState.MArked.
        // 4) Properties can be changed. Changes are reflected in viewstate.
        // 5) Just before raising the ShowItemCommands event, the ObjectList control
        //    calls GlobalStateSet. PreSaveViewState is called to create a 
        //    snapshot of viewstate, and MarkState is set to MarkState.PostMarked.
        // 6) If any changes are made after this, they will be ignored for view state
        //    purposes.
        // 7) The framework calls SaveViewState. Instead of the current view state
        //    (which would include the changes made in Step 6), the snapshot view state
        //     from step 5 is returned.

        private enum MarkState
        {
            NotMarked,
            Marked,
            PostMarked,
        }

        private MarkState _markState = MarkState.NotMarked;
        private bool _dirty;
        private String[] _savedState;

        internal ObjectListCommandCollection()
        {
        }

        public ObjectListCommand this[int index]
        {
            get
            {
                return (ObjectListCommand)Items[index];
            }
        }

        public void Clear()
        {
            foreach (ObjectListCommand command in Items)
            {
                command.Owner = null;
            }
            Items.Clear();
            SetDirty();
        }

        public void Add(ObjectListCommand command)
        {
            AddAt(-1, command);
        }

        public void AddAt(int index, ObjectListCommand command)
        {
            if (index == -1)
            {
                Items.Add(command);
            }
            else
            {
                Items.Insert(index, command);
            }
            command.Owner = this;
            SetDirty();
        }

        public void Remove(String s)
        {
            RemoveAt(IndexOf(s));
        }    

        public void RemoveAt(int index)
        {
            (this[index]).Owner = null;
            Items.RemoveAt(index);
            SetDirty();
        }

        public int IndexOf(String s)
        {
            int index = 0;
            foreach (ObjectListCommand command in Items)
            {
                if (String.Compare(command.Name, s, true, CultureInfo.InvariantCulture) == 0)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        internal void GlobalStateSet()
        {
            // Base commands have been set. From here, commands will only be
            // modified on a per-item, non-persistent basis.

            PreSaveViewState();
            _markState = MarkState.PostMarked;
        }

        internal void SetDirty()
        {
            if (_markState == MarkState.Marked)
            {
                _dirty = true;
            }
        }

        /////////////////////////////////////////////////////////////////////////
        //  STATE MANAGEMENT
        /////////////////////////////////////////////////////////////////////////

        private void PreSaveViewState()
        {
            if (_dirty)
            {
                ArrayList commands = Items;
                _savedState = new String[commands.Count * 2];
                int i = 0;
                foreach (ObjectListCommand command in commands)
                {
                    _savedState[i] = command.Name;
                    _savedState[i + 1] = command.Text;
                    i += 2;
                }
            }
            else
            {
                _savedState = null;
            }
        }

        bool IStateManager.IsTrackingViewState
        {
            get
            {
                return _markState != MarkState.NotMarked;
            }
        }

        void IStateManager.TrackViewState() 
        {
            _markState = MarkState.Marked;
        }

        void IStateManager.LoadViewState(Object state) 
        {
            if (state != null)
            {
                String[] commandStates = (String[])state;
                int count = commandStates.Length;
                Clear();
                for (int i = 0; i < count; i += 2)
                {
                    Add(new ObjectListCommand(commandStates[i], commandStates[i + 1]));
                }
            }
        }

        Object IStateManager.SaveViewState() 
        {
            if (_markState == MarkState.Marked)
            {
                PreSaveViewState();
            }
            return _savedState;
        }
    }
}


