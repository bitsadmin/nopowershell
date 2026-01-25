/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    /// <summary>
    /// Represents an integer argument that can be optional or required and may
    /// have a default value.
    /// </summary>
    public sealed class IntegerArgument : Argument
    {
        private int _value;
        private readonly int _defaultValue;
        private readonly bool _hasDefault;

        /// <summary>
        /// Creates a new optional integer argument including its default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter (for example, "-Count").</param>
        /// <param name="defaultValue">Default value of the argument.</param>
        public IntegerArgument(string argumentName, int defaultValue)
            : base(argumentName)
        {
            _value = defaultValue;
            _defaultValue = defaultValue;
            _hasDefault = true;
            _isOptionalArgument = true;
        }

        /// <summary>
        /// Creates a new required integer argument without a default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter (for example, "-Count").</param>
        public IntegerArgument(string argumentName)
            : base(argumentName)
        {
            _value = 0;
            _defaultValue = 0;
            _hasDefault = false;
            _isOptionalArgument = false;
        }

        /// <summary>
        /// Creates a copy of this <see cref="IntegerArgument"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IntegerArgument"/> with the same state.</returns>
        public override Argument Clone()
        {
            // Preserve whether this argument originally had a default.
            IntegerArgument clone;

            if (_hasDefault)
            {
                clone = new IntegerArgument(_name, _defaultValue);
            }
            else
            {
                // For required arguments without a default, we construct using the name-only
                // constructor and then copy the current value.
                clone = new IntegerArgument(_name);
            }

            clone._isOptionalArgument = _isOptionalArgument;
            clone._dashArgumentNameSkipUsed = _dashArgumentNameSkipUsed;
            clone._isSet = _isSet;
            clone._value = _value;

            return clone;
        }

        /// <summary>
        /// Gets or sets the value of the argument.
        /// Setting the value will mark the argument as explicitly set.
        /// </summary>
        public int Value
        {
            get => _value;
            set
            {
                _isSet = true;
                _value = value;
            }
        }

        /// <summary>
        /// Indicates whether the current value equals the default value (if any).
        /// </summary>
        public override bool IsDefaultValue =>
            _hasDefault && _value == _defaultValue;

        /// <summary>
        /// Returns a string representation suitable for debugging and logging.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} \"{1}\"", _name, _value);
        }
    }
}
