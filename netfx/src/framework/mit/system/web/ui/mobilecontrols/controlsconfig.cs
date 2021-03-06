//------------------------------------------------------------------------------
// <copyright file="ControlsConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------


using System;
using System.Xml;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
using System.Collections.Specialized;

namespace System.Web.UI.MobileControls
{
    // Mobile Controls Configuration class.
    // Includes Mobile Web Forms-specific settings, including a set
    // of device configurations, that can be used to decide what set of
    // adapters to use for a given device.

    internal class ControlsConfig
    {
        private readonly ControlsConfig _parent;

        private readonly StringDictionary _settings = new StringDictionary();
        private readonly ListDictionary _deviceConfigs = new ListDictionary();
        private FactoryGenerator _factoryGenerator;

        internal static ControlsConfig GetFromContext(HttpContext context)
        {
            return (ControlsConfig)context.GetConfig ("system.web/mobileControls");
        }

        internal ControlsConfig() : this(null)
        {
        }

        internal ControlsConfig(ControlsConfig parent)
        {
            _parent = parent;
        }

        // Return false if a device of the same name has already been added.
        internal /*public*/ bool AddDeviceConfig(String configName, IndividualDeviceConfig deviceConfig)
        {
            // Note that GetDeviceConfig also walks the parents configs
            if (GetDeviceConfig(configName) != null)
            {
                return false;
            }
            else
            {
                _deviceConfigs[configName] = deviceConfig;
                return true;
            }
        }

        internal /*public*/ IndividualDeviceConfig GetDeviceConfig(HttpContext context)
        {
            IndividualDeviceConfig deviceConfig = null;

            #if DEBUG
            if (context.Session != null)
            {
                String var = "AdapterOverride";
                bool saveInSession = true;
                String adapterOverride = (String)context.Session[var];
                if (adapterOverride == null)
                {
                    saveInSession = false;
                    adapterOverride = (String)context.Request.QueryString[var];
                }
                if (adapterOverride != null && 
                    (deviceConfig = GetDeviceConfig(adapterOverride)) != null)
                {
                    if (saveInSession)
                    {
                        context.Session[var] = adapterOverride;
                    }
                    return deviceConfig;
                }
            }
            #endif

            foreach (IndividualDeviceConfig candidate in _deviceConfigs.Values) 
            {
                if (candidate.DeviceQualifies(context))
                {
                    deviceConfig = candidate;
                    break;
                }
            }

            if (deviceConfig == null && _parent != null)
            {
                deviceConfig = _parent.GetDeviceConfig (context);
            }

            if (deviceConfig == null)
            {
                throw new Exception(
                    SR.GetString(SR.ControlsConfig_NoDeviceConfigRegistered,
                                 context.Request.UserAgent));
            }

            return deviceConfig;
        }

        internal /*public*/ IndividualDeviceConfig GetDeviceConfig(String configName)
        {
            IndividualDeviceConfig deviceConfig = (IndividualDeviceConfig)_deviceConfigs[configName];
            if (deviceConfig == null && _parent != null)
            {
                deviceConfig = _parent.GetDeviceConfig (configName);
            }
            return deviceConfig;
        }

        // Call this after all the device configs have been entered.  This will
        // resolve the names of the parent classes to inherit from actual
        // classes, and flag an error if there isn't one.  This is done as a
        // second-pass because devices earlier in web.config may inherit from
        // items later in the web.config.  That flexibility is required to get
        // the right behavior for device predicates being evaluated in the order
        // they appear.
        internal void FixupDeviceConfigInheritance(XmlNode configNode)
        {
            foreach (IndividualDeviceConfig config in _deviceConfigs.Values)
            {
                config.FixupInheritance(null, configNode);
            }
        }

        internal /*public*/ String this[String key]
        {
            get
            {
                String s = _settings[key];
                if (s == null && _parent != null)
                {
                    s = _parent[key];
                }
                return s;
            }

            set
            {
                _settings[key] = value;
            }
        }

        internal /*public*/ int SessionStateHistorySize
        {
            get
            {
                String sizeString = this["sessionStateHistorySize"];
                int size = Constants.DefaultSessionsStateHistorySize;
                
                if (sizeString != null)
                {
                    // Enclose in case a numerical value wasn't provided.  In
                    // which case just return the default.
                    try
                    {
                        size = Int32.Parse(sizeString);
                    }
                    catch (Exception)
                    {
                    }
                }

                return size;
            }
        }

        internal /*public*/ Type CookielessDataDictionaryType
        {
            get
            {
                Type cookielessDataType = null;
                String typeString = this["cookielessDataDictionaryType"];
                if((typeString != null) && (typeString != String.Empty))
                {
                    cookielessDataType = Type.GetType(typeString);
                }
                return cookielessDataType;
            }
        }

        internal /*public*/ bool AllowCustomAttributes
        {
            get
            {
                String allow = this["allowCustomAttributes"];
                return String.Compare(allow, "true", true, CultureInfo.InvariantCulture) == 0;
            }
        }

        internal FactoryGenerator AdapterFactoryGenerator
        {
            get
            {
                if (_parent != null)
                {
                    return _parent.AdapterFactoryGenerator;
                }
                else
                {
                    if (_factoryGenerator == null)
                    {
                        _factoryGenerator = new FactoryGenerator(typeof(IControlAdapter), 
                                                                 typeof(IControlAdapterFactory));
                    }
                    return _factoryGenerator;
                }
            }
        }

    }
}
