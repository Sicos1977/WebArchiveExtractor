using System;
using WebArchiveExtractor;

namespace TestTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var extractor = new Extractor();

            using (var stream = Console.OpenStandardOutput())
            {
                extractor.Extract("d:\\E-mail - CentraalBeheer VvE.webarchive", "d:\\test", Extractor.ExtractorOptions.IgnoreJavaScriptFiles, stream);
            }
        }
    }
}
