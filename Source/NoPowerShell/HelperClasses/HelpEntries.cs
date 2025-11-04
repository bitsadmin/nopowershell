using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class ExampleEntries : List<ExampleEntry>
    {
    }

    public class ExampleEntry
    {
        private string _description;
        private List<string> _examples;

        public ExampleEntry(string description, string example)
        {
            _description = description;
            _examples = new List<string>(1) { example };
        }

        public ExampleEntry(string description, List<string> examples)
        {
            _description = description;
            _examples = examples;
        }

        public string Description
        {
            get { return _description; }
        }

        public List<string> Examples
        {
            get { return _examples; }
        }
    }
}
