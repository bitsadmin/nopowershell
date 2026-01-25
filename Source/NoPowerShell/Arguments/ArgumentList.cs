using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    /// <summary>
    /// Represents a collection of <see cref="Argument"/> instances for a command.
    /// </summary>
    public class ArgumentList : List<Argument>
    {
        /// <summary>
        /// Gets the argument of the specified type and name, or null if not found.
        /// Type comparison is exact (no subclasses).
        /// </summary>
        /// <typeparam name="T">The concrete <see cref="Argument"/> type.</typeparam>
        /// <param name="argumentName">The name of the argument (for example, "-Path").</param>
        /// <returns>
        /// The matching argument instance if found; otherwise, null.
        /// </returns>
        public T Get<T>(string argumentName) where T : Argument
        {
            if (string.IsNullOrWhiteSpace(argumentName))
                throw new ArgumentException("Argument name cannot be null or whitespace.", nameof(argumentName));

            foreach (var arg in this)
            {
                if (arg is T typed &&
                    string.Equals(typed.Name, argumentName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return typed;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to get the argument of the specified type and name.
        /// </summary>
        /// <typeparam name="T">The concrete <see cref="Argument"/> type.</typeparam>
        /// <param name="argumentName">The name of the argument (for example, "-Path").</param>
        /// <param name="result">
        /// When this method returns, contains the matching argument instance if found;
        /// otherwise, null.
        /// </param>
        /// <returns>
        /// True if a matching argument was found; otherwise, false.
        /// </returns>
        public bool TryGet<T>(string argumentName, out T result) where T : Argument
        {
            result = Get<T>(argumentName);
            return result != null;
        }
    }
}
