//------------------------------------------------------------------------------
// <copyright file="CompareValidator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.Drawing;
using System.Web;
using System.Web.UI;
using System.Web.UI.Design.WebControls;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using WebCntrls = System.Web.UI.WebControls;
using System.Security.Permissions;

namespace System.Web.UI.MobileControls
{
    /*
     * Mobile CompareValidator class.
     * The CompareValidator compares the value of the associated input control
     * with a constant value or another input control.  A data type property
     * specifies how the values being compared should be interpreted: strings,
     * integers, dates, etc.  A comparison operator specifies the nature of the
     * comparison; greater than, less than, etc.
     *
     * Copyright (c) 2000 Microsoft Corporation
     */
    [
        ToolboxData("<{0}:CompareValidator runat=\"server\" ErrorMessage=\"CompareValidator\"></{0}:CompareValidator>"),
        ToolboxItem("System.Web.UI.Design.WebControlToolboxItem, " + AssemblyRef.SystemDesign)
    ]    
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public class CompareValidator : BaseValidator
    {
        private WebCntrls.CompareValidator _webCompareValidator;

        protected override WebCntrls.BaseValidator CreateWebValidator()
        {
            _webCompareValidator = new WebCntrls.CompareValidator();
            return _webCompareValidator;
        }

        ////////////////////////////////////////////////////////////////////////
        // Mimic the properties exposed in the original CompareValidator.
        // The properties are got and set directly from the original CompareValidator.
        ////////////////////////////////////////////////////////////////////////

        [
            Bindable(false),
            DefaultValue(""),
            MobileCategory(SR.Category_Behavior),
            MobileSysDescription(SR.CompareValidator_ControlToCompare),
            TypeConverter(typeof(System.Web.UI.Design.MobileControls.Converters.ValidatedMobileControlConverter))
        ]
        public String ControlToCompare
        {
            get
            {
                return _webCompareValidator.ControlToCompare;
            }
            set
            {
                _webCompareValidator.ControlToCompare = value;
            }
        }

        [
            Bindable(false),
            DefaultValue(ValidationCompareOperator.Equal),
            MobileCategory(SR.Category_Behavior),
            MobileSysDescription(SR.CompareValidator_Operator)
        ]
        public ValidationCompareOperator Operator
        {
            get
            {
                return _webCompareValidator.Operator;
            }
            set
            {
                _webCompareValidator.Operator = value;
            }
        }

        [
            Bindable(false),
            DefaultValue(ValidationDataType.String),
            MobileCategory(SR.Category_Behavior),
            MobileSysDescription(SR.CompareValidator_Type)
        ]
        public ValidationDataType Type
        {
            get
            {
                return _webCompareValidator.Type;
            }
            set
            {
                _webCompareValidator.Type = value;
            }
        }

        [
            Bindable(true),
            DefaultValue(""),
            MobileCategory(SR.Category_Behavior),
            MobileSysDescription(SR.CompareValidator_ValueToCompare)
        ]
        public String ValueToCompare
        {
            get
            {
                return _webCompareValidator.ValueToCompare;
            }
            set
            {
                _webCompareValidator.ValueToCompare = value;
            }
        }

        protected override bool EvaluateIsValid()
        {
            return EvaluateIsValidInternal();
        }

        /////////////////////////////////////////////////////////////////////
        // Helper function adopted from WebForms CompareValidator
        /////////////////////////////////////////////////////////////////////

        protected override bool ControlPropertiesValid()
        {
            // Check the control id references 
            if (ControlToCompare.Length > 0)
            {
                CheckControlValidationProperty(ControlToCompare, "ControlToCompare");
                if (String.Compare(ControlToValidate, ControlToCompare, true, CultureInfo.InvariantCulture) == 0)
                {
                    throw new ArgumentException(SR.GetString(
                        SR.CompareValidator_BadCompareControl, ID, ControlToCompare));
                }
            }   
            else
            {
                // Check Values
                if (Operator != ValidationCompareOperator.DataTypeCheck && 
                    !WebCntrls.BaseCompareValidator.CanConvert(ValueToCompare, Type))
                {
                    throw new ArgumentException(SR.GetString(
                        SR.Validator_ValueBadType,
                        ValueToCompare,
                        "ValueToCompare",
                        ID,
                        PropertyConverter.EnumToString(
                            typeof(ValidationDataType), Type)
                    ));
                }
            }
            return base.ControlPropertiesValid();
        }
    }
}
