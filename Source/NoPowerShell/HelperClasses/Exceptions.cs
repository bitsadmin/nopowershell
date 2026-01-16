using System;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class NoPowerShellException : Exception
    {
        public NoPowerShellException()
        {
        }

        public NoPowerShellException(string message) : base(message)
        {
        }

        public NoPowerShellException(string messageFormat, params object[] args) : base(string.Format(messageFormat, args))
        {
        }
    }

    public class CommandNotFoundException : NoPowerShellException
    {
        public string Command { get; set; }
        public override string Message => $"{Command} : The term '{Command}' is not recognized as the name of a cmdlet.";

        public CommandNotFoundException(string command) : base()
        {
            Command = command;
        }
    }

    public class ParameterBindingException : CommandNotFoundException
    {
        public string Parameter { get; set; }
        public override string Message => $"{Command} : A parameter cannot be found that matches parameter name '{Parameter}'.";

        public ParameterBindingException(string command, string parameter) : base(command)
        {
            Parameter = parameter;
        }
    }

    public class DuplicateParameterException : CommandNotFoundException
    {
        public string Parameter { get; set; }
        public override string Message => $"{Command} : Cannot bind parameter because parameter '{Parameter}' is specified more than once. To provide multiple values to parameters that can accept multiple values, use the array syntax. For example, \"-parameter value1, value2, value3\"";

        public DuplicateParameterException(string command, string parameter) : base(command)
        {
            Parameter = parameter;
        }
    }

    public class ItemNotFoundException : NoPowerShellException
    {
        public string Path { get; set; }
        public override string Message => $"Cannot find path '{Path}' because it does not exist.";

        public ItemNotFoundException(string path) : base()
        {
            Path = path;
        }
    }
}
