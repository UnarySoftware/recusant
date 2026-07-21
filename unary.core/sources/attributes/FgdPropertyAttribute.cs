
using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class FgdProperty : Attribute
    {
        public string Description { get; set; }

        public FgdProperty()
        {
            Description = string.Empty;
        }

        public FgdProperty(string description)
        {
            Description = description;
        }
    }
}
