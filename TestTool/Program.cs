using System;
using WebArchiveExtractor;

namespace TestTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var extractor = new Extractor();
            extractor.Extract("d:\\test.webarchive", "d:\\test");
        }
    }
}
