using System.Text;

namespace Unary.Core
{
    public static class StringBuilderExtensions
    {
        public static void Prepend(this StringBuilder source, string target)
        {
            source.Insert(0, target);
        }
    }
}
