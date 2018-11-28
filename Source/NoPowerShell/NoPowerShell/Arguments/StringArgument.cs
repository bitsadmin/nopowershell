/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    public class StringArgument : Argument
    {
        private string _value;
        private string _defaultValue;

        /// <summary>
        /// Create a new string argument including its default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        public StringArgument(string argumentName, string defaultValue) : base(argumentName)
        {
            this._value = defaultValue;
            this._defaultValue = defaultValue;
            this._isOptionalArgument = false;
        }

        /// <summary>
        /// Create a new string argument including its default value specifying whether it is optional or not.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        /// <param name="optionalArgument">True if the argument is optional; False if not</param>
        public StringArgument(string argumentName, string defaultValue, bool optionalArgument) : this(argumentName, defaultValue)
        {
            this._isOptionalArgument = optionalArgument;
        }

        /// <summary>
        /// Create a new string argument with a null default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public StringArgument(string argumentName) : this(argumentName, null)
        {
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public bool IsDefaultValue
        {
            get { return string.Equals(_value, _defaultValue, System.StringComparison.InvariantCultureIgnoreCase); }
        }

        public override string ToString()
        {
            return string.Format("{0} \"{1}\"", _name, _value);
        }
    }
}
