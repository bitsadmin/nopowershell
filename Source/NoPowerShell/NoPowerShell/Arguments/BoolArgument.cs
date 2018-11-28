/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    public class BoolArgument : Argument
    {
        private bool _value;

        /// <summary>
        /// Create a new boolean argument including its default value. Bool arguments are always optional.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        public BoolArgument(string argumentName, bool defaultValue) : base(argumentName)
        {
            this._value = defaultValue;
            this._isOptionalArgument = true;
        }

        /// <summary>
        /// Create new boolean argument with false as its default value. Bool arguments are always optional.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public BoolArgument(string argumentName) : this(argumentName, false)
        {
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
