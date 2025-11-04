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
            return other.Name.Equals(_name.Substring(0, other.Name.Length), StringComparison.InvariantCultureIgnoreCase);
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

        public string Name
        {
            get { return _name; }
        }

        public bool IsOptionalArgument
        {
            get { return this._isOptionalArgument; }
        }

        public virtual bool IsDefaultValue
        {
            get { return false; }
        }

        public bool IsSet
        {
            get { return _isSet; }
        }

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
