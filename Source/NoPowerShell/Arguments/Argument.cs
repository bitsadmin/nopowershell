using System;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    /// <summary>
    /// Base class for BoolArgument and StringArgument
    /// </summary>
    public class Argument : IEquatable<Argument>
    {
        /// <summary>
        /// Name of the argument, for example "-Path"
        /// </summary>
        protected string _name;
        protected bool _isOptionalArgument;
        protected bool _dashArgumentNameSkipUsed;
        protected bool _isSet;

        public Argument()
        {
        }

        public Argument(string name)
        {
            _name = name;
            _dashArgumentNameSkipUsed = false;
            _isSet = false;
        }

        public bool Equals(Argument other)
        {
            if (other is null)
                return false;

            int minLength = Math.Min(_name?.Length ?? 0, other.Name?.Length ?? 0);
            if (minLength == 0)
                return false;

            return _name.Substring(0, minLength).Equals(other.Name.Substring(0, minLength), StringComparison.InvariantCultureIgnoreCase);
        }

        public Argument Clone()
        {
            return new Argument()
            {
                _name = this._name,
                _isOptionalArgument = this._isOptionalArgument,
                _dashArgumentNameSkipUsed = this._dashArgumentNameSkipUsed,
                _isSet = this._isSet
            };
        }

        public string Name => _name;

        public bool IsOptionalArgument => _isOptionalArgument;

        public virtual bool IsDefaultValue => false;

        public bool IsSet => _isSet;

        /// <summary>
        /// Positional StringArgument which requires a value
        /// </summary>
        public bool DashArgumentNameSkipUsed
        {
            get { return _dashArgumentNameSkipUsed; }
            set { _dashArgumentNameSkipUsed = value; }
        }
    }
}
