/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    /// <summary>
    /// Represents an optional boolean switch argument.
    /// Bool arguments are always optional in this model.
    /// </summary>
    public sealed class BoolArgument : Argument
    {
        private bool _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolArgument"/> class.
        /// Bool arguments are always optional and default to <c>false</c>.
        /// </summary>
        /// <param name="argumentName">Name of the parameter (for example, "-Recurse").</param>
        public BoolArgument(string argumentName)
            : base(argumentName)
        {
            _value = false;
            _isOptionalArgument = true;
        }

        /// <summary>
        /// Creates a copy of this <see cref="BoolArgument"/> instance.
        /// </summary>
        /// <returns>A new <see cref="BoolArgument"/> with the same state.</returns>
        public override Argument Clone()
        {
            var clone = new BoolArgument(_name)
            {
                _isOptionalArgument = _isOptionalArgument,
                _dashArgumentNameSkipUsed = _dashArgumentNameSkipUsed,
                _isSet = _isSet,
                _value = _value
            };

            return clone;
        }

        /// <summary>
        /// Gets or sets the value of the argument.
        /// Setting the value will mark the argument as explicitly set.
        /// </summary>
        public bool Value
        {
            get => _value;
            set
            {
                _isSet = true;
                _value = value;
            }
        }

        /// <summary>
        /// Returns a string representation suitable for debugging and logging.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}: {1}", _name, _value);
        }
    }
}
