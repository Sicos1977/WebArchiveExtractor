using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebArchiveExtractor.Exceptions;
using WebArchiveExtractor.Helpers;

namespace WebArchiveExtractor
{
    /// <summary>
    /// With this class a Safari webarchive can me extracted to a folder
    /// </summary>
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
        /// <param name="inputFile">The input file</param>
        /// <param name="outputFolder">The folder where to save the extracted web archive</param>
        /// <param name="logStream">When set then logging is written to this stream</param>
        /// <returns></returns>
        /// <exception cref="WAEResourceMissing">Raised when a required resource is not found in the web archive</exception>
        public List<string> Extract(string inputFile, string outputFolder, Stream logStream = null)
        {
            if (logStream != null)
                Logger.LogStream = logStream;

            try
            {
                var reader = new PList.BinaryPlistReader();
                var archive = reader.ReadObject(inputFile);

                if (!archive.Contains(WebMainResource))
                {
                    var message = $"Can't find the resource '{WebMainResource}' in the webarchive";
                    Logger.WriteToLog(message);
                    throw new WAEResourceMissing(message);
                }

                var mainResource = (IDictionary) archive[WebMainResource];
                var webPageFileName = Path.Combine(outputFolder, "webpage.html");
                //Logger.WriteToLog($"Reading main web page from '{WebMainResource}' and writing it to '{webPageFileName}'");

                var webPage = ProcessMainResource(mainResource, out var mainUri);
                File.WriteAllText("d:\\input.html", webPage);

                if (!archive.Contains(WebSubresources))
                    Logger.WriteToLog("Web archive does not contain any sub resources");
                else
                {
                    var subResources = (object[]) archive[WebSubresources];
                    var count = subResources.Length;
                    Logger.WriteToLog($"Web archive has {count} sub resource{(count > 1 ? "s" : string.Empty)}");

                    foreach(IDictionary subResource in subResources)
                        ProcessSubResources(subResource, outputFolder, mainUri, ref webPage);
                }

                File.WriteAllText("d:\\output.html", webPage);
            }
            catch (Exception exception)
            {
                Logger.WriteToLog(ExceptionHelpers.GetInnerException(exception));
                throw;
            }

            return null;
        }
        #endregion

        #region ProcessMainResource

        /// <summary>
        /// Reads the main resource and returns it as a string
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="mainUri"></param>
        private string ProcessMainResource(IDictionary resources, out Uri mainUri)
        {
            byte[] data = null;
            var textEncoding = "UTF-8";
            mainUri = null;

            foreach(DictionaryEntry resource in resources)
            {
                switch (resource.Key)
                {
                    case WebResourceUrl:
                        mainUri = new Uri((string)resource.Value);
                        Logger.WriteToLog($"The webpage has been saved from the url '{mainUri.Host}'");
                        break;

                    case WebResourceData:
                        data = (byte[]) resource.Value;
                        break;

                    case WebResourceTextEncodingName:
                        textEncoding = (string) resource.Value;
                        break;
                }
            }

            var encoding = Encoding.GetEncoding(textEncoding);

            return data == null ? string.Empty : encoding.GetString(data);
        }
        #endregion

        #region ProcessSubResources

        /// <summary>
        /// Reads the sub resource and saves it to the given <paramref name="outputFolder"/>
        /// </summary>
        /// <param name="resources">The sub resource</param>
        /// <param name="outputFolder">The output folder where to save the resource</param>
        /// <param name="mainUri">The main uri of the web page</param>
        /// <param name="webPage">The main web page</param>
        private void ProcessSubResources(
            IDictionary resources, 
            string outputFolder, 
            Uri mainUri,
            ref string webPage)
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

            if (data != null && uri != null && uri.LocalPath.StartsWith("/"))
            {
                var fileInfo = new FileInfo(Path.Combine(outputFolder, uri.LocalPath.TrimStart('/')));

                if (!fileInfo.FullName.EndsWith(@"\"))
                {
                    fileInfo.Directory?.Create();
                    File.WriteAllBytes(fileInfo.FullName, data);

                    var webArchiveUri = uri.ToString();
                    var webArchiveUriWithoutScheme = webArchiveUri.Replace($"{uri.Scheme}:", string.Empty);
                    var fileUri = new Uri(fileInfo.FullName).ToString();


                    if (webPage.Contains(webArchiveUri))
                    {
                        Logger.WriteToLog($"Replacing '{webArchiveUri}' with '{fileUri}'");
                        webPage = webPage.Replace(webArchiveUri, fileUri);
                    }
                    else if (webPage.Contains(webArchiveUriWithoutScheme))
                    {
                        Logger.WriteToLog($"Replacing '{webArchiveUriWithoutScheme}' with '{fileUri}'");
                        webPage = webPage.Replace(webArchiveUriWithoutScheme, $"{fileUri}");
                    }
                    else if (webArchiveUri.Contains(mainUri.Host) && webPage.Contains(uri.PathAndQuery))
                    {
                        Logger.WriteToLog($"Replacing '{uri.PathAndQuery}' with '{fileUri}'");
                        webPage = webPage.Replace(uri.PathAndQuery, $"{fileUri}");
                    }
                    else
                    {
                        Logger.WriteToLog($"Could not find any resources with url '{uri}' in the web page");
                    }
                }
            }
        }
        #endregion
    }
}

