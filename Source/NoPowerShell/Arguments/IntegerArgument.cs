/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    public class IntegerArgument : Argument
    {
        private int _value;
        private readonly int _defaultValue;

        /// <summary>
        /// Create a new optional integer argument including its default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        public IntegerArgument(string argumentName, int defaultValue) : base(argumentName)
        {
            _value = defaultValue;
            _defaultValue = defaultValue;
            _isOptionalArgument = true;
        }

        /// <summary>
        /// Create a new required integer argument
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public IntegerArgument(string argumentName) : base(argumentName)
        {
            _isOptionalArgument = false;
        }

        public new IntegerArgument Clone()
        {
            return new IntegerArgument(this._name, this._defaultValue)
            {
                _dashArgumentNameSkipUsed = this._dashArgumentNameSkipUsed,
                _isSet = this._isSet,
                _value = this._value
            };
        }

        public int Value
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
            get { return _value == _defaultValue; }
        }

        public override string ToString()
        {
            return string.Format("{0} \"{1}\"", _name, _value);
        }
    }
}
