using System;

namespace Unary.Core
{
    public static class ObjectExtensions
    {
        public static string ResolvePrintType(this object source)
        {
            if (source is Type type)
            {
                return type.FullName;
            }
            else if (source is string stringType)
            {
                return stringType;
            }
            else
            {
                return source.GetType().FullName;
            }
        }
    }
}
