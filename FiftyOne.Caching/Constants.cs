using System;
using System.Collections.Generic;
using System.Text;

namespace FiftyOne.Caching
{
    public class Constants
    {
        public static readonly int DEFAULT_CONCURRENCY = Environment.ProcessorCount;

        public const int DEFAULT_DICTIONARY_SIZE = 50000;
    }
}
