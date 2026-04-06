using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class InitializeExplicitAttribute(params Type[] dependencies) : Attribute
    {
        public Type[] Dependencies { get; private set; } = dependencies;
    }
}
