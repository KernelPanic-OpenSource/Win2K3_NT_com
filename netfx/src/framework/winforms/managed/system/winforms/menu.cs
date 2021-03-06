//------------------------------------------------------------------------------
// <copyright file="Menu.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Windows.Forms {
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System;
    using System.Security.Permissions;    
    using System.Collections;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using System.Drawing;
    using Microsoft.Win32;
    using System.Security;
    using System.Globalization;

    /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu"]/*' />
    /// <devdoc>
    ///     This is the base class for all menu components (MainMenu, MenuItem, and ContextMenu).
    /// </devdoc>
    [
    Designer("Microsoft.VisualStudio.Windows.Forms.MenuDesigner, " + AssemblyRef.MicrosoftVisualStudio),
    ToolboxItemFilter("System.Windows.Forms"),
    ListBindable(false)
    ]
    public abstract class Menu : Component {
        internal const int CHANGE_ITEMS     = 0; // item(s) added or removed
        internal const int CHANGE_VISIBLE   = 1; // item(s) hidden or shown
        internal const int CHANGE_MDI       = 2; // mdi item changed
        internal const int CHANGE_MERGE     = 3; // mergeType or mergeOrder changed

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.FindHandle"]/*' />
        /// <devdoc>
        ///     Used by findMenuItem
        /// </devdoc>
        /// <internalonly/>
        public const int FindHandle = 0;
        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.FindShortcut"]/*' />
        /// <devdoc>
        ///     Used by findMenuItem
        /// </devdoc>
        /// <internalonly/>
        public const int FindShortcut = 1;

        private MenuItemCollection itemsCollection;
        internal MenuItem[] items;
        internal int itemCount;
        internal IntPtr handle;
        internal bool created;
        
        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.Menu"]/*' />
        /// <devdoc>
        ///     This is an abstract class.  Instances cannot be created, so the constructor
        ///     is only called from derived classes.
        /// </devdoc>
        /// <internalonly/>
        protected Menu(MenuItem[] items) {
            if (items != null) {
                MenuItems.AddRange(items);
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.Handle"]/*' />
        /// <devdoc>
        ///     The HMENU handle corresponding to this menu.
        /// </devdoc>
        [
        Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), 
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(SR.ControlHandleDescr)
        ]
        public IntPtr Handle {
            get {
                if (handle == IntPtr.Zero) handle = CreateMenuHandle();
                CreateMenuItems();
                return handle;
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.IsParent"]/*' />
        /// <devdoc>
        ///     Specifies whether this menu contains any items.
        /// </devdoc>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(SR.MenuIsParentDescr)
        ]
        public virtual bool IsParent {
            [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
            get {
                return null != items && itemCount > 0;
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MdiListItem"]/*' />
        /// <devdoc>
        ///     The MenuItem that contains the list of MDI child windows.
        /// </devdoc>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        SRDescription(SR.MenuMDIListItemDescr)
        ]
        public MenuItem MdiListItem {
            get {
                for (int i = 0; i < itemCount; i++) {
                    MenuItem item = items[i];
                    if (item.MdiList)
                        return item;
                    if (item.IsParent) {
                        item = item.MdiListItem;
                        if (item != null) return item;
                    }
                }
                return null;
            }
        }


        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItems"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [
        Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        SRDescription(SR.MenuMenuItemsDescr),
        MergableProperty(false)
        ]
        public MenuItemCollection MenuItems {
            get {
                if (itemsCollection == null) {
                    itemsCollection = new MenuItemCollection(this);
                }
                return itemsCollection;
            }
        }
        
        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.ClearHandles"]/*' />
        /// <devdoc>
        ///     Notifies Menu that someone called Windows.DeleteMenu on its handle.
        /// </devdoc>
        /// <internalonly/>
        internal void ClearHandles() {
            UnsafeNativeMethods.DestroyMenu(new HandleRef(this, handle));
            handle = IntPtr.Zero;
            if (created) {
                for (int i = 0; i < itemCount; i++)
                    items[i].ClearHandles();
                created = false;
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.CloneMenu"]/*' />
        /// <devdoc>
        ///     Sets this menu to be an identical copy of another menu.
        /// </devdoc>
        protected void CloneMenu(Menu menuSrc) {
            MenuItem[] newItems = null;
            if (menuSrc.items != null) {
                int count = menuSrc.MenuItems.Count;
                newItems = new MenuItem[count];
                for (int i = 0; i < count; i++)
                    newItems[i] = menuSrc.MenuItems[i].CloneMenu();
            }
            MenuItems.Clear();
            if (newItems != null) {
                MenuItems.AddRange(newItems);
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.CreateMenuHandle"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        [SecurityPermission(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        protected virtual IntPtr CreateMenuHandle() {
            return UnsafeNativeMethods.CreatePopupMenu();
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.CreateMenuItems"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        internal void CreateMenuItems() {
            if (!created) {
                for (int i = 0; i < itemCount; i++) items[i].CreateMenuItem();
                created = true;
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.DestroyMenuItems"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        internal void DestroyMenuItems() {
            if (created) {
                while (UnsafeNativeMethods.GetMenuItemCount(new HandleRef(this, handle)) > 0) {
                    UnsafeNativeMethods.RemoveMenu(new HandleRef(this, handle), 0, NativeMethods.MF_BYPOSITION);
                }
                created = false;
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.Dispose"]/*' />
        /// <devdoc>
        ///     Disposes of the component.  Call dispose when the component is no longer needed.
        ///     This method removes the component from its container (if the component has a site)
        ///     and triggers the dispose event.
        /// </devdoc>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                while (itemCount > 0) {
                    MenuItem item = items[--itemCount];

                    // remove the item before we dispose it so it still has valid state
                    // for undo/redo
                    //
                    if (item.Site != null && item.Site.Container != null) {
                        item.Site.Container.Remove(item);
                    }

                    item.menu = null;
                    item.Dispose();
                }
                items = null;
            }
            if (handle != IntPtr.Zero) {
                UnsafeNativeMethods.DestroyMenu(new HandleRef(this, handle));
                if (disposing) {
                    ClearHandles();
                }
            }
            base.Dispose(disposing);
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.FindMenuItem"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        public MenuItem FindMenuItem(int type, IntPtr value) {
            Debug.WriteLineIf(IntSecurity.SecurityDemand.TraceVerbose, "ControlFromHandleOrLocation Demanded");
            IntSecurity.ControlFromHandleOrLocation.Demand();
            return FindMenuItemInternal(type, value);
        }

        private MenuItem FindMenuItemInternal(int type, IntPtr value) {
            for (int i = 0; i < itemCount; i++) {
                MenuItem item = items[i];
                switch (type) {
                    case FindHandle:
                        if (item.handle == value) return item;
                        break;
                    case FindShortcut:
                        if (item.Shortcut == (Shortcut)(int)value) return item;
                        break;
                }
                item = item.FindMenuItemInternal(type, value);
                if (item != null) return item;
            }
            return null;
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.FindMergePosition"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        protected int FindMergePosition(int mergeOrder) {
            int iMin, iLim, iT;

            for (iMin = 0, iLim = itemCount; iMin < iLim;) {
                iT = (iMin + iLim) / 2;
                if (items[iT].MergeOrder <= mergeOrder)
                    iMin = iT + 1;
                else
                    iLim = iT;
            }
            return iMin;
        }
        
        //There's a win32 problem that doesn't allow menus to cascade right to left
        //unless we explicitely set the bit on the menu the first time it pops up
        internal void UpdateRtl() {
            foreach (MenuItem item in MenuItems) {
                item.UpdateItemRtl();
                item.UpdateRtl();
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.GetContextMenu"]/*' />
        /// <devdoc>
        ///     Returns the ContextMenu that contains this menu.  The ContextMenu
        ///     is at the top of this menu's parent chain.
        ///     Returns null if this menu is not contained in a ContextMenu.
        ///     This can occur if it's contained in a MainMenu or if it isn't
        ///     currently contained in any menu at all.
        /// </devdoc>
        public ContextMenu GetContextMenu() {
            Menu menuT;
            for (menuT = this; !(menuT is ContextMenu);) {
                if (!(menuT is MenuItem)) return null;
                menuT = ((MenuItem)menuT).menu;
            }
            return(ContextMenu)menuT;

        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.GetMainMenu"]/*' />
        /// <devdoc>
        ///     Returns the MainMenu item that contains this menu.  The MainMenu
        ///     is at the top of this menu's parent chain.
        ///     Returns null if this menu is not contained in a MainMenu.
        ///     This can occur if it's contained in a ContextMenu or if it isn't
        ///     currently contained in any menu at all.
        /// </devdoc>
        public MainMenu GetMainMenu() {
            Menu menuT;
            for (menuT = this; !(menuT is MainMenu);) {
                if (!(menuT is MenuItem)) return null;
                menuT = ((MenuItem)menuT).menu;
            }
            return(MainMenu)menuT;
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.ItemsChanged"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        internal virtual void ItemsChanged(int change) {
            switch (change) {
                case CHANGE_ITEMS:
                case CHANGE_VISIBLE:
                    DestroyMenuItems();
                    break;
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MergeMenu"]/*' />
        /// <devdoc>
        ///     Merges another menu's items with this one's.  Menu items are merged according to their
        ///     mergeType and mergeOrder properties.  This function is typically used to
        ///     merge an MDI container's menu with that of its active MDI child.
        /// </devdoc>
        public virtual void MergeMenu(Menu menuSrc) {
            int i, j;
            MenuItem item;
            MenuItem itemDst;

            if (menuSrc == this)
                throw new ArgumentException(SR.GetString(SR.MenuMergeWithSelf), "menuSrc");

            if (menuSrc.items != null && items == null) {
                MenuItems.Clear();                
            }

            for (i = 0; i < menuSrc.itemCount; i++) {
                item = menuSrc.items[i];

                switch (item.MergeType) {
                    default:
                        continue;
                    case MenuMerge.Add:
                        MenuItems.Add(FindMergePosition(item.MergeOrder), item.MergeMenu());
                        continue;
                    case MenuMerge.Replace:
                    case MenuMerge.MergeItems:
                        break;
                }

                int mergeOrder = item.MergeOrder;
                for (j = FindMergePosition(mergeOrder - 1); ; j++) {
                    if (j >= itemCount) {
                        MenuItems.Add(j, item.MergeMenu());
                        break;
                    }
                    itemDst = items[j];
                    if (itemDst.MergeOrder != mergeOrder) {
                        MenuItems.Add(j, item.MergeMenu());
                        break;
                    }
                    if (itemDst.MergeType != MenuMerge.Add) {
                        if (item.MergeType != MenuMerge.MergeItems
                            || itemDst.MergeType != MenuMerge.MergeItems) {
                            itemDst.Dispose();
                            MenuItems.Add(j, item.MergeMenu());
                        }
                        else {
                            itemDst.MergeMenu(item);
                        }
                        break;
                    }
                }
            }
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.ProcessInitMenuPopup"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        internal virtual bool ProcessInitMenuPopup(IntPtr handle) {
            MenuItem item = FindMenuItemInternal(FindHandle, handle);
            if (item != null) {
                item._OnInitMenuPopup(EventArgs.Empty);
                item.CreateMenuItems();
                return true;
            }
            return false;
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.ProcessCmdKey"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        [
            System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode),
            System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Flags=System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)
        ]
        protected internal virtual bool ProcessCmdKey(ref Message msg, Keys keyData) {
            MenuItem item = FindMenuItemInternal(FindShortcut, (IntPtr)(int)keyData);
            return item != null? item.ShortcutClick(): false;
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.ToString"]/*' />
        /// <devdoc>
        ///     Returns a string representation for this control.
        /// </devdoc>
        /// <internalonly/>
        public override string ToString() {

            string s = base.ToString();
            return s + ", Items.Count: " + itemCount.ToString();
        }

        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.WmMenuChar"]/*' />
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        internal void WmMenuChar(ref Message m) {
            
            // Handles WM_MENUCHAR for owner-drawn menus
            Menu menu;
            
            if (m.LParam == handle) {
                menu = this;
            }
            else {
                menu = FindMenuItemInternal(FindHandle, m.LParam);
            }
            
            if (menu != null) {
                char key = Char.ToUpper((char)((int)m.WParam & 0x0000FFFF), CultureInfo.CurrentCulture);
                
                foreach(MenuItem mi in menu.items) {
                    //First check that out menu item is not null - keep in mind that
                    //we build this array up in increments of 4 (so we can have null items)
                    //
                    if (mi != null && mi.OwnerDraw && mi.Mnemonic == key) {
                        m.Result = (IntPtr)NativeMethods.Util.MAKELONG(mi.MenuIndex, NativeMethods.MNC_EXECUTE);
                        break;
                    }
                }
            }
        }
        
        /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [ListBindable(false)]
        public class MenuItemCollection : IList {
            private Menu owner;

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.MenuItemCollection"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public MenuItemCollection(Menu owner) {
                this.owner = owner;
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.this"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public virtual MenuItem this[int index] {
                get {
                    if (index < 0 || index >= owner.itemCount)
                        throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,
                                                                  "index",
                                                                  (index).ToString()));
                    return owner.items[index];
                }
                // set not supported
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.this"]/*' />
            /// <internalonly/>
            object IList.this[int index] {
                get {
                    return this[index];
                }
                set {
                    throw new NotSupportedException();
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Count"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public int Count {
                get {
                    return owner.itemCount;
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.ICollection.SyncRoot"]/*' />
            /// <internalonly/>
            object ICollection.SyncRoot {
                get {
                    return this;
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.ICollection.IsSynchronized"]/*' />
            /// <internalonly/>
            bool ICollection.IsSynchronized {
                get {
                    return false;
                }
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.IsFixedSize"]/*' />
            /// <internalonly/>
            bool IList.IsFixedSize {
                get {
                    return false;
                }
            }
           
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.IsReadOnly"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool IsReadOnly {
                get {
                    return false;
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Add"]/*' />
            /// <devdoc>
            ///     Adds a new MenuItem to the end of this menu with the specified caption.
            /// </devdoc>
            public virtual MenuItem Add(string caption) {
                MenuItem item = new MenuItem(caption);
                Add(item);
                return item;
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Add1"]/*' />
            /// <devdoc>
            ///     Adds a new MenuItem to the end of this menu with the specified caption,
            ///     and click handler.
            /// </devdoc>
            public virtual MenuItem Add(string caption, EventHandler onClick) {
                MenuItem item = new MenuItem(caption, onClick);
                Add(item);
                return item;
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Add2"]/*' />
            /// <devdoc>
            ///     Adds a new MenuItem to the end of this menu with the specified caption,
            ///     click handler, and items.
            /// </devdoc>
            public virtual MenuItem Add(string caption, MenuItem[] items) {
                MenuItem item = new MenuItem(caption, items);
                Add(item);
                return item;
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Add3"]/*' />
            /// <devdoc>
            ///     Adds a MenuItem to the end of this menu
            ///     MenuItems can only be contained in one menu at a time, and may not be added
            ///     more than once to the same menu.
            /// </devdoc>
            public virtual int Add(MenuItem item) {                
                return Add(owner.itemCount, item);
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Add4"]/*' />
            /// <devdoc>
            ///     Adds a MenuItem to this menu at the specified index.  The item currently at
            ///     that index, and all items after it, will be moved up one slot.
            ///     MenuItems can only be contained in one menu at a time, and may not be added
            ///     more than once to the same menu.
            /// </devdoc>
            public virtual int Add(int index, MenuItem item) {
                // MenuItems can only belong to one menu at a time
                if (item.menu != null){
                    // First check that we're not adding ourself, i.e. walk
                    // the parent chain for equality
                    if (owner is MenuItem) {
                        MenuItem parent = (MenuItem)owner;
                        while (parent != null) {
                            if (parent.Equals(item)) {
                                throw new ArgumentException(SR.GetString(SR.MenuItemAlreadyExists, item.Text), "item");
                            }
                            if (parent.Parent is MenuItem)
                                parent = (MenuItem)parent.Parent;
                            else 
                                break;
                        }
                    }

                    item.menu.MenuItems.Remove(item);
                }

                if (index < 0 || index > owner.itemCount) {
                    throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,"index",(index).ToString()));
                }
                
                if (owner.items == null || owner.items.Length == owner.itemCount) {
                    MenuItem[] newItems = new MenuItem[owner.itemCount < 2? 4: owner.itemCount * 2];
                    if (owner.itemCount > 0) System.Array.Copy(owner.items, 0, newItems, 0, owner.itemCount);
                    owner.items = newItems;
                }
                System.Array.Copy(owner.items, index, owner.items, index + 1, owner.itemCount - index);
                owner.items[index] = item;
                owner.itemCount++;
                item.menu = owner;
                owner.ItemsChanged(CHANGE_ITEMS);
                
                return index;
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.AddRange"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public virtual void AddRange(MenuItem[] items) {
                if (items == null) {
                    throw new ArgumentNullException("items");
                }
                foreach(MenuItem item in items) {
                    Add(item);
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.Add"]/*' />
            /// <internalonly/>
            int IList.Add(object value) {
                if (value is MenuItem) {
                    return Add((MenuItem)value);
                }
                else {  
                    throw new ArgumentException("value");
                }
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Contains"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public bool Contains(MenuItem value) {
                return IndexOf(value) != -1;
            }
        
            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.Contains"]/*' />
            /// <internalonly/>
            bool IList.Contains(object value) {
                if (value is MenuItem) {
                    return Contains((MenuItem)value);
                }
                else {  
                    return false;
                }
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.IndexOf"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public int IndexOf(MenuItem value) {
                for(int index=0; index < Count; ++index) {
                    if (this[index] == value) {
                        return index;
                    } 
                }
                return -1;
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.IndexOf"]/*' />
            /// <internalonly/>
            int IList.IndexOf(object value) {
                if (value is MenuItem) {
                    return IndexOf((MenuItem)value);
                }
                else {  
                    return -1;
                }
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.Insert"]/*' />
            /// <internalonly/>
            void IList.Insert(int index, object value) {
                if (value is MenuItem) {
                    Add(index, (MenuItem)value);                    
                }
                else {  
                    throw new ArgumentException("value");
                }
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Clear"]/*' />
            /// <devdoc>
            ///     Removes all existing MenuItems from this menu
            /// </devdoc>
            public virtual void Clear() {
                if (owner.itemCount > 0) {
                    
                    for (int i = 0; i < owner.itemCount; i++) {
                        owner.items[i].menu = null;
                    }

                    owner.itemCount = 0;
                    owner.items = null;

                    owner.ItemsChanged(CHANGE_ITEMS);

                    if (owner is MenuItem) {
                        ((MenuItem)(owner)).UpdateMenuItem(true);
                    }
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.CopyTo"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public void CopyTo(Array dest, int index) {
                if (owner.itemCount > 0) {
                    System.Array.Copy(owner.items, 0, dest, index, owner.itemCount);
                }
            }
            
            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.GetEnumerator"]/*' />
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public IEnumerator GetEnumerator() {
                return new WindowsFormsUtils.ArraySubsetEnumerator(owner.items, owner.itemCount);
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.RemoveAt"]/*' />
            /// <devdoc>
            ///     Removes the item at the specified index in this menu.  All subsequent
            ///     items are moved up one slot.
            /// </devdoc>
            public virtual void RemoveAt(int index) {
                if (index < 0 || index >= owner.itemCount) {
                    throw new ArgumentOutOfRangeException(SR.GetString(SR.InvalidArgument,"index",(index).ToString()));
                }

                MenuItem item = owner.items[index];
                item.menu = null;
                owner.itemCount--;
                System.Array.Copy(owner.items, index + 1, owner.items, index, owner.itemCount - index);
                owner.items[owner.itemCount] = null;
                owner.ItemsChanged(CHANGE_ITEMS);

                //if the last item was removed, clear the collection
                //
                if (owner.itemCount == 0) {
                    Clear();
                }
            
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="Menu.MenuItemCollection.Remove"]/*' />
            /// <devdoc>
            ///     Removes the specified item from this menu.  All subsequent
            ///     items are moved down one slot.
            /// </devdoc>
            public virtual void Remove(MenuItem item) {
                if (item.menu == owner) {
                    RemoveAt(item.Index);
                }
            }

            /// <include file='doc\Menu.uex' path='docs/doc[@for="MenuItemCollection.IList.Remove"]/*' />
            /// <internalonly/>
            void IList.Remove(object value) {
                if (value is MenuItem) {
                    Remove((MenuItem)value);
                }                
            }

        }
    }
}
