using System;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    /// <summary>
    /// Base class for BoolArgument, IntegerArgument and StringArgument.
    /// Represents a PowerShell-style parameter (for example, "-Path").
    /// </summary>
    public abstract class Argument : IEquatable<Argument>
    {
        /// <summary>
        /// Name of the argument, for example "-Path".
        /// </summary>
        protected string _name = string.Empty;

        /// <summary>
        /// True when the argument is optional; false when it is required.
        /// </summary>
        protected bool _isOptionalArgument;

        /// <summary>
        /// Indicates that the dash-and-name part of this argument has been
        /// consumed and the next positional token should be treated as this
        /// argument's value.
        /// </summary>
        protected bool _dashArgumentNameSkipUsed;

        /// <summary>
        /// Indicates that this argument has been explicitly set during parsing.
        /// </summary>
        protected bool _isSet;

        /// <summary>
        /// Protected parameterless constructor for use by derived classes and
        /// serializers. Initializes to a safe default state.
        /// </summary>
        protected Argument()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument"/> class with
        /// the specified name.
        /// </summary>
        /// <param name="name">The argument name (for example, "-Path").</param>
        protected Argument(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument name cannot be null or whitespace.", nameof(name));

            _name = name;
            _dashArgumentNameSkipUsed = false;
            _isSet = false;
        }

        /// <summary>
        /// Determines equality based on prefix matching of the argument name,
        /// using a case-insensitive comparison. This allows shortened forms
        /// such as "-Pa" to match "-Path".
        /// </summary>
        /// <param name="other">The other argument to compare.</param>
        /// <returns>
        /// True if both argument names share the same prefix (up to the length
        /// of the shorter name) ignoring case; otherwise, false.
        /// </returns>
        public bool Equals(Argument other)
        {
            if (other is null)
                return false;

            var leftName = _name ?? string.Empty;
            var rightName = other.Name ?? string.Empty;

            var minLength = Math.Min(leftName.Length, rightName.Length);
            if (minLength == 0)
                return false;

            return leftName.Substring(0, minLength)
                           .Equals(rightName.Substring(0, minLength), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || Equals(obj as Argument);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Hash based on the full name, case-insensitive, so that
            // logically equal arguments produce the same hash code.
            return (_name ?? string.Empty).ToUpperInvariant().GetHashCode();
        }

        /// <summary>
        /// Creates a shallow copy of this argument instance.
        /// Derived classes should override this method to preserve their
        /// own state and return the derived type.
        /// </summary>
        /// <returns>A new <see cref="Argument"/> with the same state.</returns>
        public virtual Argument Clone()
        {
            // MemberwiseClone is safe here because all fields are either
            // value types or immutable (string).
            return (Argument)MemberwiseClone();
        }

        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets a value indicating whether the argument is optional.
        /// </summary>
        public bool IsOptionalArgument => _isOptionalArgument;

        /// <summary>
        /// Gets a value indicating whether the argument currently holds its
        /// default value. Derived classes can override this to provide
        /// type-specific semantics.
        /// </summary>
        public virtual bool IsDefaultValue => false;

        /// <summary>
        /// Gets a value indicating whether this argument has been explicitly
        /// set during parsing.
        /// </summary>
        public bool IsSet => _isSet;

        /// <summary>
        /// Indicates a positional argument for which the dash-and-name part
        /// has been skipped and the next token should be treated as this
        /// argument's value.
        /// </summary>
        public bool DashArgumentNameSkipUsed
        {
            get => _dashArgumentNameSkipUsed;
            set => _dashArgumentNameSkipUsed = value;
        }
    }
}
