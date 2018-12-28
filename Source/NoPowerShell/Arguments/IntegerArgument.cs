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
        private int _defaultValue;

        /// <summary>
        /// Create a new integer argument including its default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        public IntegerArgument(string argumentName, int defaultValue) : base(argumentName)
        {
            this._value = defaultValue;
            this._defaultValue = defaultValue;
            this._isOptionalArgument = false;
        }

        /// <summary>
        /// Create a new integer argument including its default value specifying whether it is optional or not.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        /// <param name="defaultValue">Default value of the argument</param>
        /// <param name="optionalArgument">True if the argument is optional; False if not</param>
        public IntegerArgument(string argumentName, int defaultValue, bool optionalArgument) : this(argumentName, defaultValue)
        {
            this._isOptionalArgument = optionalArgument;
        }

        /// <summary>
        /// Create a new integer argument with a null default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter</param>
        public IntegerArgument(string argumentName) : this(argumentName, 0)
        {
        }

        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public bool IsDefaultValue
        {
            get { return _value == _defaultValue; }
        }

        public override string ToString()
        {
            return string.Format("{0} \"{1}\"", _name, _value);
        }
    }
}
