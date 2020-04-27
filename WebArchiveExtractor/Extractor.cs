using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
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
        // ReSharper disable UnusedMember.Local
        private const string WebMainResource = "WebMainResource";
        private const string WebSubresources = "WebSubresources";
        private const string WebResourceUrl = "WebResourceURL";
        private const string WebResourceResponse = "WebResourceResponse";
        private const string WebResourceData = "WebResourceData";
        private const string WebResourceMimeType = "WebResourceMIMEType";
        private const string WebResourceTextEncodingName = "WebResourceTextEncodingName";
        private const string WebResourceFrameName = "WebResourceFrameName";
        private const string WebSubframeArchives = "WebSubframeArchives";
        // ReSharper restore UnusedMember.Local
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

                Logger.WriteToLog($"Getting main web page from '{WebMainResource}'");
                var webPage = ProcessMainResource(mainResource, out var mainUri);

                // TODO: Remove before release
                File.WriteAllText(webPageFileName, webPage);

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

                if (!archive.Contains(WebSubframeArchives))
                    Logger.WriteToLog("Web archive does not contain any sub frame archives");
                else
                {
                    var subFrameResources = (object[])archive[WebSubframeArchives];
                    var subFrameResourcesCount = subFrameResources.Length;

                    Logger.WriteToLog($"Web archive has {subFrameResourcesCount} sub frame resource{(subFrameResourcesCount > 1 ? "s" : string.Empty)}");

                    var i = 1;

                    foreach (IDictionary subFrameResource in subFrameResources)
                    {
                        var subFrameMainResource = (IDictionary) subFrameResource[WebMainResource];

                        Logger.WriteToLog($"Getting web page from sub frame resource '{WebMainResource}'");
                        var subFrameResourceWebPage = ProcessSubFrameMainResource(subFrameMainResource, out var frameName, out var subFrameMainUri);
                        var subFrameOutputFolder = Path.Combine(outputFolder, $"subframe_{i}");

                        Logger.WriteToLog($"Creating folder '{subFrameOutputFolder}' for iframe '{frameName}' content");
                        Directory.CreateDirectory(subFrameOutputFolder);
                        i += 1;

                        var subFrameSubResources = (object[]) subFrameResource[WebSubresources];

                        if (subFrameSubResources == null)
                        {
                            Logger.WriteToLog("Web archive sub frame does not contain any sub resources");
                        }
                        else
                        {
                            var subFrameSubResourcesCount = subFrameSubResources.Length;
                            Logger.WriteToLog($"Web archive sub frame has {subFrameSubResourcesCount} sub resource{(subFrameSubResourcesCount > 1 ? "s" : string.Empty)}");

                            foreach (IDictionary subFrameSubResource in subFrameSubResources)
                                ProcessSubResources(subFrameSubResource, subFrameOutputFolder, subFrameMainUri,
                                    ref subFrameResourceWebPage);
                        }

                        var subFrameWebPageFileName = Path.Combine(subFrameOutputFolder, "webpage.html");
                        var subFrameWebPageRelativeUri = $"subframe_{i}/webpage.html";
                        var subFrameUri = subFrameMainUri.ToString();
                        var subFrameUriWithoutScheme = subFrameUri.Replace($"{subFrameMainUri.Scheme}:", string.Empty);
                        var subFrameUriWithoutMainUri = subFrameUri.Replace($"{mainUri.Scheme}://{mainUri.Host}{mainUri.AbsolutePath}", string.Empty);

                        if (webPage.Contains(subFrameUri))
                        {
                            Logger.WriteToLog($"Replacing '{subFrameUri}' with '{subFrameWebPageRelativeUri}'");
                            webPage = webPage.Replace(subFrameUri, subFrameWebPageRelativeUri);
                        }
                        else if (webPage.Contains(subFrameUriWithoutScheme))
                        {
                            Logger.WriteToLog($"Replacing '{subFrameUriWithoutScheme}' with '{subFrameWebPageRelativeUri}'");
                            webPage = webPage.Replace(subFrameUriWithoutScheme, $"{subFrameWebPageRelativeUri}");
                        }
                        else if (webPage.Contains(subFrameUriWithoutMainUri))
                        {
                            Logger.WriteToLog($"Replacing '{subFrameUriWithoutMainUri}' with '{subFrameWebPageRelativeUri}'");
                            webPage = webPage.Replace(subFrameUriWithoutMainUri, $"{subFrameWebPageRelativeUri}");
                        }
                        else
                        {
                            Logger.WriteToLog($"Could not find any resources with url '{subFrameUri}' in the web page");
                        }
                        
                        File.WriteAllText(subFrameWebPageFileName, subFrameResourceWebPage);
                    }
                }

                File.WriteAllText(webPageFileName, webPage);

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
                        Logger.WriteToLog($"The webpage has been saved for the url '{mainUri.Host}'");
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
                var fileRelativeUri = uri.LocalPath.Replace(mainUri.AbsolutePath, string.Empty).TrimStart('/');
                var path = Path.Combine(outputFolder, fileRelativeUri);
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists || File.Exists(fileInfo.DirectoryName) || Directory.Exists(fileInfo.FullName))
                    path = Path.Combine(outputFolder, Guid.NewGuid().ToString());

                fileInfo = new FileInfo(path);

                if (!fileInfo.FullName.EndsWith(@"\"))
                {
                    fileInfo.Directory?.Create();
                    File.WriteAllBytes(fileInfo.FullName, data);

                    var webArchiveUri = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}{HttpUtility.HtmlEncode(uri.Query)}";
                    var webArchiveUriWithoutScheme = webArchiveUri.Replace($"{uri.Scheme}:", string.Empty);
                    var webArchiveUriWithoutMainUriHost = webArchiveUri.Replace($"{mainUri.Scheme}://{mainUri.Host}", string.Empty);
                    var webArchiveUriWithoutMainUriHostAbsolutePath = webArchiveUri.Replace($"{mainUri.Scheme}://{mainUri.Host}{mainUri.AbsolutePath}", string.Empty);
                    
                    if (webPage.Contains(webArchiveUri))
                    {
                        Logger.WriteToLog($"Replacing '{webArchiveUri}' with '{fileRelativeUri}'");
                        webPage = webPage.Replace(webArchiveUri, fileRelativeUri);
                    }
                    else if (webPage.Contains(webArchiveUriWithoutScheme))
                    {
                        Logger.WriteToLog($"Replacing '{webArchiveUriWithoutScheme}' with '{fileRelativeUri}'");
                        webPage = webPage.Replace(webArchiveUriWithoutScheme, $"{fileRelativeUri}");
                    }
                    else if (webPage.Contains(webArchiveUriWithoutMainUriHost))
                    {
                        Logger.WriteToLog($"Replacing '{webArchiveUriWithoutMainUriHost}' with '{fileRelativeUri}'");
                        webPage = webPage.Replace(webArchiveUriWithoutMainUriHost, $"{fileRelativeUri}");
                    }
                    else if (webPage.Contains(webArchiveUriWithoutMainUriHostAbsolutePath))
                    {
                        Logger.WriteToLog($"Replacing '{webArchiveUriWithoutMainUriHostAbsolutePath}' with '{fileRelativeUri}'");
                        webPage = webPage.Replace(webArchiveUriWithoutMainUriHostAbsolutePath, $"{fileRelativeUri}");
                    }
                    else if (webArchiveUri.Contains(mainUri.Host) && webPage.Contains(uri.PathAndQuery))
                    {
                        Logger.WriteToLog($"Replacing '{uri.PathAndQuery}' with '{fileRelativeUri}'");
                        webPage = webPage.Replace(uri.PathAndQuery, $"{fileRelativeUri}");
                    }
                    else
                    {
                        Logger.WriteToLog($"Could not find any resources with url '{uri}' in the web page");
                    }
                }
            }
            else
            {
                Logger.WriteToLog($"Ignoring url '{uri}'");
            }
        }
        #endregion

        #region ProcessSubFrameMainResource
        /// <summary>
        /// Reads the main resource and returns it as a string
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="frameName"></param>
        /// <param name="mainUri"></param>
        private string ProcessSubFrameMainResource(IDictionary resources, out string frameName, out Uri mainUri)
        {
            byte[] data = null;
            var textEncoding = "UTF-8";
            mainUri = null;
            frameName = string.Empty;

            foreach (DictionaryEntry resource in resources)
            {
                switch (resource.Key)
                {
                    case WebResourceUrl:
                        mainUri = new Uri((string)resource.Value);
                        Logger.WriteToLog($"The webpage has been saved from the url '{mainUri.Host}'");
                        break;

                    case WebResourceData:
                        data = (byte[])resource.Value;
                        break;

                    case WebResourceTextEncodingName:
                        textEncoding = (string)resource.Value;
                        break;

                    case WebResourceFrameName:
                        frameName = (string)resource.Value;
                        break;
                }
            }

            var encoding = Encoding.GetEncoding(textEncoding);

            return data == null ? string.Empty : encoding.GetString(data);
        }
        #endregion
    }
}

