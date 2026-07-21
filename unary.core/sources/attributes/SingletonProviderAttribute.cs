
using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class SingletonProviderAttribute(string singletonPath, int genericIndex = -1) : Attribute
    {
        public string SingletonPath { get; private set; } = singletonPath;
        public int GenericIndex { get; private set; } = genericIndex;
    }
}
