## What is WebArchiveExtractor?

WebArchiveExtractor is a 100% managed C# .NETStandard 2.0 library and .NET Core 3.0 console application that can be used to read Safari webarchives and extract its content to a folder

## Why did I make this?

I needed a tool to save the content of a webarchive to disk so that I could convert it to PDF with my other project [ChromeHtmlToPdf](https://github.com/Sicos1977/ChromeHtmlToPdf)

## Keep in mind

This is a tool to extract the content of an Safari web archive, it is no guarantee that the web page is viewable. Most web pages work fine but complex pages with a lott of javascript will sometimes not work because they are looking for resources that can't be found in the folder where you extracted the web archive. If a page uses a lott of javascript then try to use the option ```IgnoreJavaScriptFiles```

## License Information

WebArchiveReader is Copyright (C)2020 Kees van Spelde and is licensed under the MIT license:

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.

## Installing via NuGet

The easiest way to install WebArchiveReader is via NuGet.

In Visual Studio's Package Manager Console, simply enter the following command:

    Install-Package WebArchiveExtractor 

### Extracting a webarchive

```csharp
using (var extractor = new WebArchiveExtractor())
{
    extractor.Extract(<inputfile>, "<the path where to save the content of the webarchive>", <extraction option>);
}
```

Core Team
=========
    Sicos1977 (Kees van Spelde)

Support
=======
If you like my work then please consider a donation as a thank you.

<a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=NS92EXB2RDPYA" target="_blank"><img src="https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif" /></a>

## Reporting Bugs

Have a bug or a feature request? [Please open a new issue](https://github.com/Sicos1977/WebArchiveExtractor/issues).

Before opening a new issue, please search for existing issues to avoid submitting duplicates.
