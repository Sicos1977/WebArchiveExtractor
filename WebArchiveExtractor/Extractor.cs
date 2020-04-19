using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace WebArchiveExtractor
{
    public class Extractor
    {
        #region Consts
        private const string WebMainResource = "WebMainResource";
        private const string WebSubresources = "WebSubresources";
        private const string WebResourceUrl = "WebResourceURL";
        private const string WebResourceResponse = "WebResourceResponse";
        private const string WebResourceData = "WebResourceData";
        private const string WebResourceMimeType = "WebResourceMIMEType";
        private const string WebResourceTextEncodingName = "WebResourceTextEncodingName";
        private const string WebResourceFrameName = "WebResourceFrameName";
        private const string WebSubframeArchives = "WebSubframeArchives";

        #endregion

        #region Extract
        /// <summary>
        /// Extract the given <paramref name="inputFile"/> to the given <paramref name="outputFolder"/>
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
        public List<string> Extract(string inputFile, string outputFolder)
        {
            return null;
        }
        #endregion

        #region ProcessMainResources
        /// <summary>
        /// Reads the main resource and saves it to the given <paramref name="outputFileName"/>
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="outputFileName">The name for the webpage</param>
        private void ProcessMainResources(IDictionary resources, string outputFileName)
        {
            Uri uri = null;
            byte[] data = null;

            foreach(DictionaryEntry resource in resources)
            {
                switch (resource.Key)
                {
                    case WebMainResource:
                        uri = new Uri((string) resource.Value);
                        break;

                    case WebResourceData:
                        data = (byte[]) resource.Value;
                        break;
                }
            }

            if (data != null)
                File.WriteAllBytes(outputFileName, data);
        }
        #endregion

        #region ProcessSubResources
        /// <summary>
        /// Reads the sub resource and saves it to the given <paramref name="outputFolder"/>
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="outputFolder"></param>
        private void ProcessSubResources(IDictionary resources, string outputFolder)
        {
            Uri uri = null;
            byte[] data = null;

            foreach(DictionaryEntry resource in resources)
            {
                switch (resource.Key)
                {
                    case WebResourceUrl:
                        uri = new Uri((string) resource.Value);
                        break;

                    case WebResourceData:
                        data = (byte[]) resource.Value;
                        break;
                }
            }

            if (data != null && 
                uri != null && uri.LocalPath.StartsWith("/"))
            {
                var fileInfo = new FileInfo(Path.Combine(outputFolder, uri.LocalPath.TrimStart('/')));

                if (!fileInfo.FullName.EndsWith(@"\"))
                {
                    fileInfo.Directory?.Create();
                    File.WriteAllBytes(fileInfo.FullName, data);
                }
            }
        }
        #endregion
    }
}

