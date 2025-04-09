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

        /// <summary>
        /// Create a new boolean argument. Bool arguments are always optional.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public BoolArgument(string argumentName) : base(argumentName)
        {
            this._value = false;
            this._isOptionalArgument = true;
        }

        public bool Value
        {
            get { return this._value; }
            set { this._value = value; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _name, _value);
        }
    }
}
