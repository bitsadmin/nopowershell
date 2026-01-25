/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    /// <summary>
    /// Represents a string-based command-line argument.
    ///
    /// This implementation:
    /// - Is sealed to keep the cloning and default-value behavior predictable.
    /// - Explicitly tracks whether a default value exists via <see cref="_hasDefault"/>.
    /// - Treats the presence of a default separately from whether the argument is optional.
    /// - Implements <see cref="Argument.Clone"/> polymorphically instead of using "new Clone()".
    /// </summary>
    public sealed class StringArgument : Argument
    {
        private string _value;
        private readonly string _defaultValue;
        private readonly bool _hasDefault;

        /// <summary>
        /// Creates a new optional string argument with a default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter.</param>
        /// <param name="defaultValue">Default value of the argument.</param>
        public StringArgument(string argumentName, string defaultValue)
            : base(argumentName)
        {
            _defaultValue = defaultValue;
            _hasDefault = true;

            // Optional argument with default: treated as having a value,
            // but not "set" by the user until explicitly assigned.
            _value = defaultValue;
            _isOptionalArgument = true;
        }

        /// <summary>
        /// Creates a new string argument with an explicit default value and optionality.
        /// </summary>
        /// <param name="argumentName">Name of the parameter.</param>
        /// <param name="defaultValue">Default value of the argument.</param>
        /// <param name="optionalArgument">True if the argument is optional; false if it is mandatory.</param>
        public StringArgument(string argumentName, string defaultValue, bool optionalArgument)
            : base(argumentName)
        {
            _defaultValue = defaultValue;
            _hasDefault = true;

            _value = defaultValue;
            _isOptionalArgument = optionalArgument;
        }

        /// <summary>
        /// Creates a new string argument with no default value and explicit optionality.
        /// </summary>
        /// <param name="argumentName">Name of the parameter.</param>
        /// <param name="optionalArgument">True if the argument is optional; false if it is mandatory.</param>
        public StringArgument(string argumentName, bool optionalArgument)
            : base(argumentName)
        {
            _defaultValue = null;
            _hasDefault = false;

            _value = null;
            _isOptionalArgument = optionalArgument;
        }

        /// <summary>
        /// Creates a new mandatory string argument with no default value.
        /// </summary>
        /// <param name="argumentName">Name of the parameter.</param>
        public StringArgument(string argumentName)
            : base(argumentName)
        {
            _defaultValue = null;
            _hasDefault = false;

            _value = null;
            _isOptionalArgument = false;
        }

        /// <summary>
        /// Polymorphic clone implementation used by the argument parsing pipeline.
        ///
        /// This creates a new <see cref="StringArgument"/> that preserves:
        /// - Name
        /// - Default value and has-default flag
        /// - Optionality
        /// - DashArgumentNameSkipUsed
        /// - IsSet flag
        /// - Current Value
        /// </summary>
        public override Argument Clone()
        {
            StringArgument clone;

            if (_hasDefault)
            {
                // Use the most expressive ctor when we know the default
                clone = new StringArgument(_name, _defaultValue, _isOptionalArgument);
            }
            else
            {
                // No default defined
                clone = new StringArgument(_name, _isOptionalArgument);
            }

            // Preserve current runtime state
            clone._value = _value;
            clone._isSet = _isSet;
            clone._dashArgumentNameSkipUsed = _dashArgumentNameSkipUsed;

            return clone;
        }

        /// <summary>
        /// Gets or sets the current value of the argument.
        /// Setting this property marks the argument as explicitly set by the user.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set
            {
                _isSet = true;
                _value = value;
            }
        }

        /// <summary>
        /// Indicates whether the current value is the default value.
        ///
        /// Returns true only if:
        /// - A default value exists, and
        /// - The current value equals that default (case-insensitive).
        /// </summary>
        public override bool IsDefaultValue
        {
            get
            {
                if (!_hasDefault)
                    return false;

                if (_defaultValue == null && _value == null)
                    return true;

                if (_defaultValue == null || _value == null)
                    return false;

                return string.Equals(
                    _value,
                    _defaultValue,
                    System.StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Returns a string representation of the argument as it would appear on the command line.
        /// </summary>
        public override string ToString()
        {
            string displayValue = _value ?? string.Empty;
            return string.Format("{0} \"{1}\"", _name, displayValue);
        }
    }
}
