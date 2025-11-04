/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    public class StringArgument : Argument
    {
        private string _value;
        private readonly string _defaultValue;

        public StringArgument()
        {
        }

        /// <summary>
        /// Create a new optional string argument including its default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        public StringArgument(string argumentName, string defaultValue) : base(argumentName)
        {
            _value = defaultValue;
            _defaultValue = defaultValue;
            _isOptionalArgument = true;
        }

        /// <summary>
        /// Create a new string argument with a null default value specifying whether it is optional or not.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="optionalArgument">True if the argument is optional; False if not</param>
        public StringArgument(string argumentName, bool optionalArgument) : base(argumentName)
        {
            _defaultValue = null;
            _isOptionalArgument = optionalArgument;
        }

        /// <summary>
        /// Create a new string argument including its default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        /// <param name="optionalArgument">True if the argument is optional; False if not</param>
        public StringArgument(string argumentName, string defaultValue, bool optionalArgument) : base(argumentName)
        {
            _value = defaultValue;
            _defaultValue = defaultValue;
            _isOptionalArgument = optionalArgument;
        }

        /// <summary>
        /// Create a new string argument with a null default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public StringArgument(string argumentName) : base(argumentName)
        {
            _isOptionalArgument = false;
        }

        public new StringArgument Clone()
        {
            return new StringArgument(this._name, this._defaultValue, this._isOptionalArgument)
            {
                _dashArgumentNameSkipUsed = this._dashArgumentNameSkipUsed,
                _isSet = this._isSet,
                _value = this._value
            };
        }

        public string Value
        {
            get { return _value; }
            set
            {
                _isSet = true;
                _value = value;
            }
        }

        public override bool IsDefaultValue
        {
            get { return string.Equals(_value, _defaultValue, System.StringComparison.InvariantCultureIgnoreCase); }
        }

        public override string ToString()
        {
            return string.Format("{0} \"{1}\"", _name, _value);
        }
    }
}
