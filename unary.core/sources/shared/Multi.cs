using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Unary.Core
{
    public static class Multi
    {
        private static ParallelOptions _defaultOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        public static ParallelLoopResult Thread(int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
        {
            return Parallel.For(fromInclusive, toExclusive, body);
        }

        public static ParallelLoopResult Thread<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            return Parallel.ForEach(source, _defaultOptions, body);
        }
    }
}
