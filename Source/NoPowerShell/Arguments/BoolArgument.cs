/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    public class BoolArgument : Argument
    {
        private bool _value;

        public BoolArgument()
        {
        }

        /// <summary>
        /// Create a new boolean argument. Bool arguments are always optional.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public BoolArgument(string argumentName) : base(argumentName)
        {
            _value = false;
            _isOptionalArgument = true;
        }

        public new BoolArgument Clone()
        {
            return new BoolArgument()
            {
                _name = this._name,
                _isOptionalArgument = this._isOptionalArgument,
                _dashArgumentNameSkipUsed = this._dashArgumentNameSkipUsed,
                _isSet = this._isSet,
                _value = this._value
            };
        }

        public bool Value
        {
            get { return _value; }
            set
            {
                _isSet = true;
                _value = value;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _name, _value);
        }
    }
}
