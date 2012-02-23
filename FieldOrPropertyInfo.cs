using System;
using System.Reflection;

namespace Ironworks
{
    /// <summary>
    /// Class that encapsulates an instance of either
    /// <see cref="FieldInfo"/> or <see cref="PropertyInfo"/>.
    /// This allows continguous arrays of this class
    /// to represent fields OR properties of a type.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{FieldOrPropertyType} {Name}")]
    public class FieldOrPropertyInfo : MemberInfo
    {

        #region Fields

        #region Delegates

        Action<object, object> _setValue = null;
        Func<object, object> _getValue = null;
        Func<bool, object[]> _getCustomAttribs = null;
        Func<Type, bool, object[]> _getCustomAttribsOfType = null;
        Func<Type, bool, bool> _isDefined = null;

        #endregion Delegates

        #region Data

        string _name = null;
        MemberTypes _types;
        Type _declaring = null;
        Type _reflected = null;
        bool _isIndexed = false;

        #endregion Data

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fi"><see cref="FieldInfo"/> to proxy</param>
        public FieldOrPropertyInfo(FieldInfo fi)
        {
            _setValue = fi.SetValue;
            _getValue = fi.GetValue;
            _getCustomAttribs = fi.GetCustomAttributes;
            _getCustomAttribsOfType = fi.GetCustomAttributes;
            _name = fi.Name;
            _types = fi.MemberType;
            _declaring = fi.DeclaringType;
            _reflected = fi.ReflectedType;
            _isDefined = fi.IsDefined;
            this.FieldOrPropertyType = fi.FieldType;
            _isIndexed = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pi"><see cref="PropertyInfo"/> to proxy</param>
        public FieldOrPropertyInfo(PropertyInfo pi)
        {
            _setValue = (target, value) => pi.SetValue(target, value, null);
            _getValue = target => pi.GetValue(target, null);
            _getCustomAttribs = pi.GetCustomAttributes;
            _getCustomAttribsOfType = pi.GetCustomAttributes;
            _name = pi.Name;
            _types = pi.MemberType;
            _declaring = pi.DeclaringType;
            _reflected = pi.ReflectedType;
            _isDefined = pi.IsDefined;
            this.FieldOrPropertyType = pi.PropertyType;
            _isIndexed = pi.GetIndexParameters().Length > 0;
        }

        #endregion Constructors

        #region Static Methods

        /// <summary>
        /// Gets all the fields and properties from an object
        /// </summary>
        /// <param name="o">Object to get fields and properties from</param>
        /// <returns>Array of <see cref="FieldOrPropertyInfo"/></returns>
        public static FieldOrPropertyInfo[] GetFieldsAndPropertiesFromObject(object o)
        {
            System.Collections.Generic.List<FieldOrPropertyInfo> retVal =
                new System.Collections.Generic.List<FieldOrPropertyInfo>();

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (PropertyInfo pi in o.GetType().GetProperties(flags))
            {
                retVal.Add(new FieldOrPropertyInfo(pi));
            }

            foreach (FieldInfo fi in o.GetType().GetFields(flags))
            {
                retVal.Add(new FieldOrPropertyInfo(fi));
            }

            return retVal.ToArray();
        }

        #endregion Static Methods

        #region Public Methods & Properties

        /// <summary>
        /// Gets a custom attribute from this field or property
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <param name="inherit">
        /// Specifies whether to search this member's inheritance
        /// chain to find the attribute</param>
        /// <returns>Instance of the requested attribute, or <c>null</c></returns>
        public T GetCustomAttribute<T>(bool inherit) where T : Attribute
        {
            T retVal = null;

            object[] o = GetCustomAttributes(typeof(T), inherit);
            if (o.Length > 0)
            {
                retVal = o[0] as T;
            }

            return retVal;
        }

        /// <summary>
        /// Gets the value of this field or property from an object
        /// </summary>
        /// <typeparam name="T">Type of the field or property to retrieve</typeparam>
        /// <param name="target">Object to get the field or property from</param>
        /// <returns>Value of field or property from the object</returns>
        /// <exception cref="NotImplementedException">
        /// Thrown if the property that is being fetched
        /// is indexed.
        /// </exception>
        public T GetValue<T>(object target)
        {
            if (_isIndexed)
            {
                throw new NotImplementedException("Cannot get indexed values");
            }

            object retVal = _getValue(target);
            if (retVal != null)
            {
                return (T)target;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Sets the value of this field or property.
        /// </summary>
        /// <param name="target">Object to set the value on</param>
        /// <param name="value">Value to set</param>
        /// <exception cref="NotImplementedException">
        /// Thrown if the property that is being saved
        /// is indexed.
        /// </exception>
        public void SetValue(object target, object value)
        {
            if (_isIndexed)
            {
                throw new NotImplementedException("Cannot set indexed values");
            }

            _setValue(target, value);
        }

        /// <summary>
        /// Gets the type of this field or property object
        /// </summary>
        public Type FieldOrPropertyType
        {
            get;
            private set;
        }

        #region MemberInfo Members

        /// <summary>
        /// Gets the class that declares this member.
        /// </summary>
        public override Type DeclaringType
        {
            get { return _declaring; }
        }

        /// <summary>
        /// Returns an array containing all the custom attributes.
        /// </summary>
        /// <param name="attributeType">
        /// The type of attribute to search for. Only attributes 
        /// that are assignable to this type are returned.
        /// </param>
        /// <param name="inherit">
        /// Specifies whether to search this member's inheritance 
        /// chain to find the attributes.
        /// </param>
        /// <returns>
        /// An array that contains all the custom attributes, 
        /// or an array with zero elements if no attributes 
        /// are defined.
        /// </returns>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return _getCustomAttribsOfType(attributeType, inherit);
        }

        /// <summary>
        /// Returns an array containing all the custom attributes.
        /// </summary>
        /// <param name="attributeType"></param>
        /// <param name="inherit">
        /// Specifies whether to search this member's inheritance 
        /// chain to find the attributes.
        /// </param>
        /// <returns>
        /// An array that contains all the custom attributes, 
        /// or an array with zero elements if no attributes 
        /// are defined.
        /// </returns>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return _getCustomAttribs(inherit);
        }

        /// <summary>
        /// Indicates whether one or more instance of 
        /// attributeType is applied to this member.
        /// </summary>
        /// <param name="attributeType"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return _isDefined(attributeType, inherit);
        }

        /// <summary>
        /// Gets a System.Reflection.MemberTypes value indicating 
        /// the type of the member — method, constructor, event, 
        /// and so on.
        /// </summary>
        public override MemberTypes MemberType
        {
            get { return _types; }
        }

        /// <summary>
        /// Gets the name of the current member.
        /// </summary>
        public override string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the class object that was used to obtain this instance of MemberInfo.
        /// </summary>
        public override Type ReflectedType
        {
            get { return _reflected; }
        }

        #endregion MemberInfo Members

        #endregion Public Methods & Properties

    }
}
