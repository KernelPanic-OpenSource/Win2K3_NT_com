//------------------------------------------------------------------------------
// <copyright file="ReflectPropertyDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.ComponentModel {
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Reflection;
    using Microsoft.Win32;
    using System.Security;
    using System.Security.Permissions;
    using System.ComponentModel.Design;
    using System.ComponentModel;

    /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor"]/*' />
    /// <internalonly/>
    /// <devdoc>
    ///    <para>
    ///       ReflectPropertyDescriptor defines a property. Properties are the main way that a user can
    ///       set up the state of a component.
    ///       The ReflectPropertyDescriptor class takes a component class that the property lives on,
    ///       a property name, the type of the property, and various attributes for the
    ///       property.
    ///       For a property named XXX of type YYY, the associated component class is
    ///       required to implement two methods of the following
    ///       form:
    ///    </para>
    ///    <code>
    /// public YYY GetXXX();
    ///     public void SetXXX(YYY value);
    ///    </code>
    ///    The component class can optionally implement two additional methods of
    ///    the following form:
    ///    <code>
    /// public boolean ShouldSerializeXXX();
    ///     public void ResetXXX();
    ///    </code>
    ///    These methods deal with a property's default value. The ShouldSerializeXXX()
    ///    method returns true if the current value of the XXX property is different
    ///    than it's default value, so that it should be persisted out. The ResetXXX()
    ///    method resets the XXX property to its default value. If the ReflectPropertyDescriptor
    ///    includes the default value of the property (using the DefaultValueAttribute),
    ///    the ShouldSerializeXXX() and ResetXXX() methods are ignored.
    ///    If the ReflectPropertyDescriptor includes a reference to an editor
    ///    then that value editor will be used to
    ///    edit the property. Otherwise, a system-provided editor will be used.
    ///    Various attributes can be passed to the ReflectPropertyDescriptor, as are described in
    ///    Attribute.
    ///    ReflectPropertyDescriptors can be obtained by a user programmatically through the
    ///    ComponentManager.
    /// </devdoc>
    internal sealed class ReflectPropertyDescriptor : PropertyDescriptor {

        private static readonly Type[] argsNone = new Type[0];
        private static readonly object  noValue = new object();
        
        private static TraceSwitch PropDescCreateSwitch = new TraceSwitch("PropDescCreate", "ReflectPropertyDescriptor: Dump errors when creating property info");
        private static TraceSwitch PropDescUsageSwitch  = new TraceSwitch("PropDescUsage", "ReflectPropertyDescriptor: Debug propertydescriptor usage");
        private static TraceSwitch PropDescSwitch       = new TraceSwitch("PropDesc", "ReflectPropertyDescriptor: Debug property descriptor");
        
        private static readonly int BitDefaultValueQueried      = BitVector32.CreateMask();
        private static readonly int BitGetQueried               = BitVector32.CreateMask(BitDefaultValueQueried);
        private static readonly int BitSetQueried               = BitVector32.CreateMask(BitGetQueried);
        private static readonly int BitShouldSerializeQueried   = BitVector32.CreateMask(BitSetQueried);
        private static readonly int BitResetQueried             = BitVector32.CreateMask(BitShouldSerializeQueried);
        private static readonly int BitChangedQueried           = BitVector32.CreateMask(BitResetQueried);
        private static readonly int BitReadOnlyChecked          = BitVector32.CreateMask(BitChangedQueried);
        private static readonly int BitAmbientValueQueried      = BitVector32.CreateMask(BitReadOnlyChecked);

        BitVector32  state = new BitVector32();  // Contains the state bits for this proeprty descriptor.
        Type         componentClass;             // used to determine if we should all on us or on the designer
        Type         type;                       // the data type of the property
        
        object       defaultValue;               // the default value of the property (or noValue)
        object       ambientValue;               // the ambient value of the property (or noValue)
        
        PropertyInfo propInfo;                   // the property info
        MethodInfo   getMethod;                  // the property get method
        MethodInfo   setMethod;                  // the property set method
        
        MethodInfo   shouldSerializeMethod;      // the should serialize method
        MethodInfo   resetMethod;                // the reset property method
        
        EventInfo    realChangedEventInfo;       // Changed event handler on object
        
        Type         receiverType;               // Only set if we are an extender

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ReflectPropertyDescriptor"]/*' />
        /// <devdoc>
        ///     The main constructor for ReflectPropertyDescriptors.
        /// </devdoc>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type,
                                         Attribute[] attributes)
        : base(name, attributes) {
        
            Debug.WriteLineIf(PropDescCreateSwitch.TraceVerbose, "Creating ReflectPropertyDescriptor for " + componentClass.FullName + "." + name);
            
            try {
                if (type == null) {
                    Debug.WriteLineIf(PropDescCreateSwitch.TraceVerbose, "type == null, name == " + name);
                    throw new ArgumentException(SR.GetString(SR.ErrorInvalidPropertyType, name));
                }
                if (componentClass == null) {
                    Debug.WriteLineIf(PropDescCreateSwitch.TraceVerbose, "componentClass == null, name == " + name);
                    throw new ArgumentException(SR.GetString(SR.InvalidNullArgument, "componentClass"));
                }
                this.type = type;
                this.componentClass = componentClass;
            }
            catch (Exception t) {
                Debug.Fail("Property '" + name + "' on component " + componentClass.FullName + " failed to init.");
                Debug.Fail(t.ToString());
                throw t;
            }
        }
        
        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ReflectPropertyDescriptor1"]/*' />
        /// <devdoc>
        ///     A constructor for ReflectPropertyDescriptors that have no attributes.
        /// </devdoc>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type) : this(componentClass, name, type, (Attribute[])null) {
        }
        
        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ReflectPropertyDescriptor2"]/*' />
        /// <devdoc>
        ///     A constructor for ReflectPropertyDescriptors that have no attributes.
        /// </devdoc>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type, PropertyInfo propInfo, MethodInfo getMethod, MethodInfo setMethod, Attribute[] attrs) : this(componentClass, name, type, attrs) {
            this.propInfo = propInfo;
            this.getMethod = getMethod;
            this.setMethod = setMethod;
            state[BitGetQueried | BitSetQueried] = true;
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ReflectPropertyDescriptor3"]/*' />
        /// <devdoc>
        ///     A constructor for ReflectPropertyDescriptors that creates an extender property.
        /// </devdoc>
        public ReflectPropertyDescriptor(Type componentClass, string name, Type type, Type receiverType, MethodInfo getMethod, MethodInfo setMethod, Attribute[] attrs) : this(componentClass, name, type, attrs) {
            this.receiverType = receiverType;
            this.getMethod = getMethod;
            this.setMethod = setMethod;
            state[BitGetQueried | BitSetQueried] = true;
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ReflectPropertyDescriptor4"]/*' />
        /// <devdoc>
        ///     This constructor takes an existing ReflectPropertyDescriptor and modifies it by merging in the
        ///     passed-in attributes.
        /// </devdoc>
        public ReflectPropertyDescriptor(Type componentClass, PropertyDescriptor oldReflectPropertyDescriptor, Attribute[] attributes)
        : base(oldReflectPropertyDescriptor, attributes) {
        
            this.componentClass = componentClass;
            this.type = oldReflectPropertyDescriptor.PropertyType;

            if (componentClass == null) {
                throw new ArgumentException(SR.GetString(SR.InvalidNullArgument, "componentClass"));
            }

            // If the classes are the same, we can potentially optimize the method fetch because
            // the old property descriptor may already have it.
            //
            if (oldReflectPropertyDescriptor is ReflectPropertyDescriptor) {
                ReflectPropertyDescriptor oldProp = (ReflectPropertyDescriptor)oldReflectPropertyDescriptor;
                
                if (oldProp.ComponentType == componentClass) {
                    propInfo = oldProp.propInfo;
                    getMethod = oldProp.getMethod;
                    setMethod = oldProp.setMethod;
                    shouldSerializeMethod = oldProp.shouldSerializeMethod;
                    resetMethod = oldProp.resetMethod;
                    defaultValue = oldProp.defaultValue;
                    ambientValue = oldProp.ambientValue;
                    state = oldProp.state;
                }
                
                // Now we must figure out what to do with our default value.  First, check to see
                // if the caller has provided an new default value attribute.  If so, use it.  Otherwise,
                // just let it be and it will be picked up on demand.
                //
                if (attributes != null) {
                    foreach(Attribute a in attributes) {
                        if (a is DefaultValueAttribute) {
                            defaultValue = ((DefaultValueAttribute)a).Value;
                            state[BitDefaultValueQueried] = true;
                        }
                        else if (a is AmbientValueAttribute) {
                            ambientValue = ((AmbientValueAttribute)a).Value;
                            state[BitAmbientValueQueried] = true;
                        }
                    }
                }
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.AmbientValue"]/*' />
        /// <devdoc>
        ///      Retrieves the ambient value for this property.
        /// </devdoc>
        private object AmbientValue {
            get {
                if (!state[BitAmbientValueQueried]) {
                    state[BitAmbientValueQueried] = true;
                    Attribute a = Attributes[typeof(AmbientValueAttribute)];
                    if (a != null) {
                        ambientValue = ((AmbientValueAttribute)a).Value;
                    }
                    else {
                        ambientValue = noValue;
                    }
                }
                return ambientValue;
            }
        }

        /// <devdoc>
        ///     The EventInfo for the changed event on the component, or null if there isn't one for this property.
        /// </devdoc>
        private EventInfo ChangedEventValue { 
            get {
                if (!state[BitChangedQueried]) {
                    state[BitChangedQueried] = true;
                    realChangedEventInfo = ComponentType.GetEvent(Name + "Changed", BindingFlags.Public | BindingFlags.Instance);
                }
                return realChangedEventInfo;
            }
            set {
                realChangedEventInfo = value;
                state[BitChangedQueried] = true;
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ComponentType"]/*' />
        /// <devdoc>
        ///     Retrieves the type of the component this PropertyDescriptor is bound to.
        /// </devdoc>
        public override Type ComponentType {
            get {
                return componentClass;
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.DefaultValue"]/*' />
        /// <devdoc>
        ///      Retrieves the default value for this property.
        /// </devdoc>
        private object DefaultValue {
            get {
                if (!state[BitDefaultValueQueried]) {
                    state[BitDefaultValueQueried] = true;
                    Attribute a = Attributes[typeof(DefaultValueAttribute)];
                    if (a != null) {
                        defaultValue = ((DefaultValueAttribute)a).Value;
                    }
                    else {
                        defaultValue = noValue;
                    }
                }
                return defaultValue;
            }
        }

        /// <devdoc>
        ///     The GetMethod for this property
        /// </devdoc>
        private MethodInfo GetMethodValue {
            get {
                if (!state[BitGetQueried]) {
                    state[BitGetQueried] = true;
                    
                    if (receiverType == null) {
                        if (propInfo == null) {
                            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;
                            propInfo = componentClass.GetProperty(Name, bindingFlags, null, PropertyType, new Type[0], new ParameterModifier[0]);
                        }
                        if (propInfo != null) {
                            getMethod = propInfo.GetGetMethod(true);
                        }
                        if (getMethod == null) {
                            throw new InvalidOperationException(SR.GetString(SR.ErrorMissingPropertyAccessors, componentClass.FullName + "." + Name));
                        }
                    }
                    else {
                        getMethod = FindMethod(componentClass, "Get" + Name, new Type[] {receiverType}, type);
                        if (getMethod == null) {
                            throw new ArgumentException(SR.GetString(SR.ErrorMissingPropertyAccessors, Name));
                        }
                    }
                }
                return getMethod;
            }
            set {
                state[BitGetQueried] = true;
                getMethod = value; 
            } 
        }
        
        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.IsExtender"]/*' />
        /// <devdoc>
        ///     Determines if this property is an extender property.
        /// </devdoc>
        private bool IsExtender {
            get {
                return (receiverType != null);
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.IsReadOnly"]/*' />
        /// <devdoc>
        ///     Indicates whether this property is read only.
        /// </devdoc>
        public override bool IsReadOnly {
            get {
                return SetMethodValue == null || ((ReadOnlyAttribute)Attributes[typeof(ReadOnlyAttribute)]).IsReadOnly;
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.PropertyType"]/*' />
        /// <devdoc>
        ///     Retrieves the type of the property.
        /// </devdoc>
        public override Type PropertyType {
            get {
                return type;
            }
        }

        /// <devdoc>
        ///     Access to the reset method, if one exists for this property.
        /// </devdoc>
        private MethodInfo ResetMethodValue {
            get {
                if (!state[BitResetQueried]) {
                    state[BitResetQueried] = true;
                    
                    Type[] args;
                    
                    if (receiverType == null) {
                        args = argsNone;
                    }
                    else {
                        args = new Type[] {receiverType};
                    }
                    
                    IntSecurity.FullReflection.Assert();
                    try {
                        resetMethod = FindMethod(componentClass, "Reset" + Name, args, typeof(void), /* publicOnly= */ false);
                    }
                    finally {
                        CodeAccessPermission.RevertAssert();
                    }
                }
                return resetMethod; 
            } 
            set {
                state[BitResetQueried] = true;
                resetMethod = value; 
            } 
        }

        /// <devdoc>
        ///     Accessor for the set method
        /// </devdoc>
        private MethodInfo SetMethodValue {
            get {
                if (!state[BitSetQueried]) {
                    state[BitSetQueried] = true;
                    
                    if (receiverType == null) {
                        if (propInfo == null) {
                            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;
                            propInfo = componentClass.GetProperty(Name, bindingFlags, null, PropertyType, new Type[0], new ParameterModifier[0]);
                        }
                        if (propInfo != null) {
                            setMethod = propInfo.GetSetMethod(true);
                        }
                    }
                    else {
                        setMethod = FindMethod(componentClass, "Set" + Name,
                                               new Type[] { receiverType, type}, typeof(void));
                    }
                }
                return setMethod; 
            }
            set {
                state[BitSetQueried] = true;
                setMethod = value; 
            } 
        }
        
        /// <devdoc>
        ///     Accessor for the ShouldSerialize method.
        /// </devdoc>
        private MethodInfo ShouldSerializeMethodValue {
            get {
                if (!state[BitShouldSerializeQueried]) {
                    state[BitShouldSerializeQueried] = true;
                    
                    Type[] args;
                    
                    if (receiverType == null) {
                        args = argsNone;
                    }
                    else {
                        args = new Type[] {receiverType};
                    }
                    
                    IntSecurity.FullReflection.Assert();
                    try {
                        shouldSerializeMethod = FindMethod(componentClass, "ShouldSerialize" + Name,
                                                         args, typeof(Boolean), /* publicOnly= */ false);
                    }
                    finally {
                        CodeAccessPermission.RevertAssert();
                    }
                }
                return shouldSerializeMethod;
            }
            set {
                state[BitShouldSerializeQueried] = true;
                shouldSerializeMethod = value; 
            }
        }
        
        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.AddValueChanged"]/*' />
        /// <devdoc>
        ///     Allows interested objects to be notified when this property changes.
        /// </devdoc>
        public override void AddValueChanged(object component, EventHandler handler) {
            if (component == null) throw new ArgumentNullException("component");
            if (handler == null) throw new ArgumentNullException("handler");
            
            EventInfo changedEvent = ChangedEventValue;
            if (changedEvent != null) {
                changedEvent.AddEventHandler(component, handler);
            }
            else {
                base.AddValueChanged(component, handler);
            }
        }

        internal bool ExtenderCanResetValue(IExtenderProvider provider, object component) {
            if (DefaultValue != noValue) {
                return !object.Equals(ExtenderGetValue(provider, component),defaultValue);
            }
            
            MethodInfo reset = ResetMethodValue;
            if (reset != null) {
                MethodInfo shouldSerialize = ShouldSerializeMethodValue;
                if (shouldSerialize != null) {
                    try {
                        provider = (IExtenderProvider)GetInvokee(componentClass, provider);
                        return (bool)shouldSerialize.Invoke(provider, new object[] { component});
                    }
                    catch (Exception) {
                    }
                }
            }
            else {
                return true;
            }
            return false;
        }

        internal Type ExtenderGetReceiverType() {
            return receiverType;
        }

        internal Type ExtenderGetType(IExtenderProvider provider) {
            return PropertyType;
        }

        internal object ExtenderGetValue(IExtenderProvider provider, object component) {
            if (provider != null) {
                provider = (IExtenderProvider)GetInvokee(componentClass, provider);
                return GetMethodValue.Invoke(provider, new object[] { component});
            }
            return null;
        }

        internal void ExtenderResetValue(IExtenderProvider provider, object component, PropertyDescriptor notifyDesc) {
            if (DefaultValue != noValue) {
                ExtenderSetValue(provider, component, DefaultValue, notifyDesc);
            }
            else if (AmbientValue != noValue) {
                ExtenderSetValue(provider, component, AmbientValue, notifyDesc);
            }
            else if (ResetMethodValue != null) {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;
                object newValue;

                // Announce that we are about to change this component
                //
                if (site != null) {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                    Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                }

                // Make sure that it is ok to send the onchange events
                //
                if (changeService != null) {
                    oldValue = ExtenderGetValue(provider, component);
                    try {
                        changeService.OnComponentChanging(component, notifyDesc);
                    }
                    catch (CheckoutException coEx) {
                        if (coEx == CheckoutException.Canceled) {
                            return;
                        }
                        throw coEx;
                    }
                }

                provider = (IExtenderProvider)GetInvokee(componentClass, provider);
                if (ResetMethodValue != null) {
                    ResetMethodValue.Invoke(provider, new object[] { component});

                    // Now notify the change service that the change was successful.
                    //
                    if (changeService != null) {
                        newValue = ExtenderGetValue(provider, component);
                        changeService.OnComponentChanged(component, notifyDesc, oldValue, newValue);
                    }
                }
            }
        }

        internal void ExtenderSetValue(IExtenderProvider provider, object component, object value, PropertyDescriptor notifyDesc) {
            if (provider != null) {

                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;

                // Announce that we are about to change this component
                //
                if (site != null) {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                    Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                }

                // Make sure that it is ok to send the onchange events
                //
                if (changeService != null) {
                    oldValue = ExtenderGetValue(provider, component);
                    try {
                        changeService.OnComponentChanging(component, notifyDesc);
                    }
                    catch (CheckoutException coEx) {
                        if (coEx == CheckoutException.Canceled) {
                            return;
                        }
                        throw coEx;
                    }
                }

                provider = (IExtenderProvider)GetInvokee(componentClass, provider);

                if (SetMethodValue != null) {
                    SetMethodValue.Invoke(provider, new object[] { component, value});

                    // Now notify the change service that the change was successful.
                    //
                    if (changeService != null) {
                        changeService.OnComponentChanged(component, notifyDesc, oldValue, value);
                    }
                }
            }
        }

        internal bool ExtenderShouldSerializeValue(IExtenderProvider provider, object component) {
        

            provider = (IExtenderProvider)GetInvokee(componentClass, provider);

            if (IsReadOnly) {
                if (ShouldSerializeMethodValue != null) {
                    try {
                        return (bool)ShouldSerializeMethodValue.Invoke(provider, new object[] {component});
                    }
                    catch (Exception) {
                    }
                }
                return Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
            }
            else if (DefaultValue == noValue) {
                if (ShouldSerializeMethodValue != null) {
                    try {
                        return (bool)ShouldSerializeMethodValue.Invoke(provider, new object[] {component});
                    }
                    catch (Exception) {
                    }
                }
                return true;
            }
            return !object.Equals(DefaultValue, ExtenderGetValue(provider, component));
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.CanResetValue"]/*' />
        /// <devdoc>
        ///     Indicates whether reset will change the value of the component.  If there
        ///     is a DefaultValueAttribute, then this will return true if getValue returns
        ///     something different than the default value.  If there is a reset method and
        ///     a ShouldSerialize method, this will return what ShouldSerialize returns.
        ///     If there is just a reset method, this always returns true.  If none of these
        ///     cases apply, this returns false.
        /// </devdoc>
        public override bool CanResetValue(object component) {
            if (IsExtender) {
                return false;
            }

            if (DefaultValue != noValue) {
                return !object.Equals(GetValue(component),DefaultValue);
            }
            
            if (ResetMethodValue != null) {
                if (ShouldSerializeMethodValue != null) {
                    component = GetInvokee(componentClass, component);
                    try {
                        return (bool)ShouldSerializeMethodValue.Invoke(component, null);
                    }
                    catch (Exception) {
                    }
                }
                return true;
            }
            
            if (AmbientValue != noValue) {
                return ShouldSerializeValue(component);
            }
            
            return false;
        }

        protected override void FillAttributes(IList attributes) {
            Debug.Assert(componentClass != null, "Must have a component class for FillAttributes");

            //
            // The order that we fill in attributes is critical.  The list of attributes will be
            // filtered so that matching attributes at the end of the list replace earlier matches
            // (last one in wins).  Therefore, the three categories of attributes we add must be
            // added as follows:
            //
            // 1.  Attributes of the property type.  These are the lowest level and should be
            //     overwritten by any newer attributes.
            //
            // 2.  Attributes of the property itself, from base class to most derived.  This way
            //     derived class attributes replace base class attributes.
            //
            // 3.  Attributes from our base MemberDescriptor.  While this seems opposite of what
            //     we want, MemberDescriptor only has attributes if someone passed in a new
            //     set in the constructor.  Therefore, these attributes always
            //     supercede existing values.
            //
            
            
            // We need to include attributes from the type of the property.
            //
            foreach (Attribute typeAttr in TypeDescriptor.GetAttributes(PropertyType)) {
                attributes.Add(typeAttr);
            }
        
            // NOTE : Must look at method OR property, to handle the case of Extender properties...
            //
            // Note : Because we are using BindingFlags.DeclaredOnly it is more effcient to re-aquire
            //      : the property info, rather than use the one we have cached.  The one we have cached
            //      : may ave come from a base class, meaning we will request custom metadata for this
            //      : class twice.

            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
            Type currentReflectType = componentClass;
            int depth = 0;
            
            // First, calculate the depth of the object hierarchy.  We do this so we can do a single
            // object create for an array of attributes.
            //
            while(currentReflectType != null && currentReflectType != typeof(object)) {
                depth++;
                currentReflectType = currentReflectType.BaseType;
            }
            
            // Now build up an array in reverse order
            //
            if (depth > 0) {
                currentReflectType = componentClass;
                object[][] attributeStack = new object[depth][];
                
                while(currentReflectType != null && currentReflectType != typeof(object)) {
                
                    MemberInfo memberInfo = null;
                    
                    // Fill in our member info so we can get at the custom attributes.
                    //
                    if (IsExtender) {
                        memberInfo = currentReflectType.GetMethod("Get" + Name, bindingFlags);
                    }
                    else {
                        memberInfo = currentReflectType.GetProperty(Name, bindingFlags, null, PropertyType, new Type[0], new ParameterModifier[0]);
                    }
                    
                    // Get custom attributes for the member info.
                    //
                    if (memberInfo != null) {
                        attributeStack[--depth] = TypeDescriptor.GetCustomAttributes(memberInfo);
                    }
                    
                    // Ready for the next loop iteration.
                    //
                    currentReflectType = currentReflectType.BaseType;
                }
                   
                // Now trawl the attribute stack so that we add attributes
                // from base class to most derived.
                //
                foreach(object[] attributeArray in attributeStack) {
                    if (attributeArray != null) {
                        foreach(object attr in attributeArray) {
                            if (attr is Attribute) {
                                attributes.Add(attr);
                            }
                        }
                    }
                }
            }
            
            // Include the base attributes.  These override all attributes on the actual
            // property, so we want to add them last.
            //
            base.FillAttributes(attributes);
            
            // Finally, override any form of ReadOnlyAttribute.  
            //
            if (!state[BitReadOnlyChecked]) {
                state[BitReadOnlyChecked] = true;
                if (SetMethodValue == null) {
                    attributes.Add(ReadOnlyAttribute.Yes);
                }
            }
        }
        
        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.GetValue"]/*' />
        /// <devdoc>
        ///     Retrieves the current value of the property on component,
        ///     invoking the getXXX method.  An exception in the getXXX
        ///     method will pass through.
        /// </devdoc>
        public override object GetValue(object component) {
#if DEBUG
            if (PropDescUsageSwitch.TraceVerbose) {
                string compName = "(null)";
                if (component != null)
                    compName = component.ToString();

                Debug.WriteLine("[" + Name + "]: GetValue(" + compName + ")");
            }
#endif

            if (IsExtender) {
                Debug.WriteLineIf(PropDescUsageSwitch.TraceVerbose, "[" + Name + "]:   ---> returning: null");
                return null;
            }

            Debug.Assert(component != null, "GetValue must be given a component");

            if (component != null) {
                component = GetInvokee(componentClass, component);
                

                try {
                    return GetMethodValue.Invoke(component, null);
                }
                catch (Exception t) {
                    
                    string name = null;
                    if (component is IComponent) {

                        ISite site = ((IComponent)component).Site;
                        if (site != null && site.Name != null) {
                            name = site.Name;
                        }
                    }
                    
                    if (name == null) {
                        name = component.GetType().FullName;
                    }
                    
                    if (t is TargetInvocationException) {
                        t = t.InnerException;
                    }
                    
                    string message = t.Message;
                    if (message == null) {
                        message = t.GetType().Name;
                    }
                    
                    throw new TargetInvocationException(SR.GetString(SR.ErrorPropertyAccessorException, Name, name, message), t);
                }
            }
            Debug.WriteLineIf(PropDescUsageSwitch.TraceVerbose, "[" + Name + "]:   ---> returning: null");
            return null;
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.OnValueChanged"]/*' />
        /// <devdoc>
        ///     This should be called by your property descriptor implementation
        ///     when the property value has changed.
        /// </devdoc>
        protected override void OnValueChanged(object component, EventArgs e) {
            if (state[BitChangedQueried] && realChangedEventInfo == null) {
                base.OnValueChanged(component, e);
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.RemoveValueChanged"]/*' />
        /// <devdoc>
        ///     Allows interested objects to be notified when this property changes.
        /// </devdoc>
        public override void RemoveValueChanged(object component, EventHandler handler) {
            if (component == null) throw new ArgumentNullException("component");
            if (handler == null) throw new ArgumentNullException("handler");
            
            EventInfo changedEvent = ChangedEventValue;
            if (changedEvent != null) {
                changedEvent.RemoveEventHandler(component, handler);
            }
            else {
                base.RemoveValueChanged(component, handler);
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ResetValue"]/*' />
        /// <devdoc>
        ///     Will reset the default value for this property on the component.  If
        ///     there was a default value passed in as a DefaultValueAttribute, that
        ///     value will be set as the value of the property on the component.  If
        ///     there was no default value passed in, a ResetXXX method will be looked
        ///     for.  If one is found, it will be invoked.  If one is not found, this
        ///     is a nop.
        /// </devdoc>
        public override void ResetValue(object component) {
            object invokee = GetInvokee(componentClass, component);

            if (DefaultValue != noValue) {
                SetValue(component, DefaultValue);
            }
            else if (AmbientValue != noValue) {
                SetValue(component, AmbientValue);
            }
            else if (ResetMethodValue != null) {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;
                object newValue;

                // Announce that we are about to change this component
                //
                if (site != null) {
                    changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                    Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                }

                // Make sure that it is ok to send the onchange events
                //
                if (changeService != null) {
                    oldValue = GetMethodValue.Invoke(invokee, (object[])null);
                    try {
                        changeService.OnComponentChanging(component, this);
                    }
                    catch (CheckoutException coEx) {
                        if (coEx == CheckoutException.Canceled) {
                            return;
                        }
                        throw coEx;
                    }

                }

                if (ResetMethodValue != null) {
                    ResetMethodValue.Invoke(invokee, (object[])null);

                    // Now notify the change service that the change was successful.
                    //
                    if (changeService != null) {
                        newValue = GetMethodValue.Invoke(invokee, (object[])null);
                        changeService.OnComponentChanged(component, this, oldValue, newValue);
                    }
                }
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.SetValue"]/*' />
        /// <devdoc>
        ///     This will set value to be the new value of this property on the
        ///     component by invoking the setXXX method on the component.  If the
        ///     value specified is invalid, the component should throw an exception
        ///     which will be passed up.  The component designer should design the
        ///     property so that getXXX following a setXXX should return the value
        ///     passed in if no exception was thrown in the setXXX call.
        /// </devdoc>
        public override void SetValue(object component, object value) {
#if DEBUG
            if (PropDescUsageSwitch.TraceVerbose) {
                string compName = "(null)";
                string valName  = "(null)";

                if (component != null)
                    compName = component.ToString();
                if (value != null)
                    valName = value.ToString();

                Debug.WriteLine("[" + Name + "]: SetValue(" + compName + ", " + valName + ")");
            }
#endif
            if (component != null) {
                ISite site = GetSite(component);
                IComponentChangeService changeService = null;
                object oldValue = null;

                object invokee = GetInvokee(componentClass, component);

                Debug.Assert(!IsReadOnly, "SetValue attempted on read-only property [" + Name + "]");
                if (!IsReadOnly) {

                    // Announce that we are about to change this component
                    //
                    if (site != null) {
                        changeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
                        Debug.Assert(!CompModSwitches.CommonDesignerServices.Enabled || changeService != null, "IComponentChangeService not found");
                    }


                    // Make sure that it is ok to send the onchange events
                    //
                    if (changeService != null) {
                        oldValue = GetMethodValue.Invoke(invokee, null);
                        try {
                            changeService.OnComponentChanging(component, this);
                        }
                        catch (CheckoutException coEx) {
                            if (coEx == CheckoutException.Canceled) {
                                return;
                            }
                            throw coEx;
                        }
                    }

                    try {
                        try {
                            SetMethodValue.Invoke(invokee, new object[]{value});
                            OnValueChanged(invokee, EventArgs.Empty);
                        }
                        catch (Exception t) {
                            // Give ourselves a chance to unwind properly before rethrowing the exception (bug# 20221).
                            //
                            value = oldValue;
                            
                            // If there was a problem setting the controls property then we get:
                            // ArgumentException (from properties set method)
                            // ==> Becomes inner exception of TargetInvocationException
                            // ==> caught here

                            if (t is TargetInvocationException && t.InnerException != null) {
                                // Propagate the original exception up
                                throw t.InnerException;
                            }
                            else {
                                throw t;
                            }
                        }
                    }
                    finally {
                        // Now notify the change service that the change was successful.
                        //
                        if (changeService != null) {
                            changeService.OnComponentChanged(component, this, oldValue, value);
                        }
                    }
                }
            }
        }

        /// <include file='doc\ReflectPropertyDescriptor.uex' path='docs/doc[@for="ReflectPropertyDescriptor.ShouldSerializeValue"]/*' />
        /// <devdoc>
        ///     Indicates whether the value of this property needs to be persisted. In
        ///     other words, it indicates whether the state of the property is distinct
        ///     from when the component is first instantiated. If there is a default
        ///     value specified in this ReflectPropertyDescriptor, it will be compared against the
        ///     property's current value to determine this.  If there is't, the
        ///     ShouldSerializeXXX method is looked for and invoked if found.  If both
        ///     these routes fail, true will be returned.
        ///
        ///     If this returns false, a tool should not persist this property's value.
        /// </devdoc>
        public override bool ShouldSerializeValue(object component) {

            component = GetInvokee(componentClass, component);

            if (IsReadOnly) {
                if (ShouldSerializeMethodValue != null) {
                    try {
                        return (bool)ShouldSerializeMethodValue.Invoke(component, null);
                    }
                    catch (Exception) {
                    }
                }
                return Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
            }
            else if (DefaultValue == noValue) {
                if (ShouldSerializeMethodValue != null) {
                    try {
                        return (bool)ShouldSerializeMethodValue.Invoke(component, null);
                    }
                    catch (Exception) {
                    }
                }
                return true;
            }
            return !object.Equals(DefaultValue, GetValue(component));
        }
    }
}

