using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp.Views
{
    internal class FileHelpView : BaseHelpView
    {
        public FileHelpView(ServiceEndpoint endpoint, IUriHelper uriHelper)
            : base(null, endpoint, uriHelper)
        {
        }

        private static Dictionary<string, string> ContentTypes = new Dictionary<string, string>
        {
            {".gif", Resources.HelpPageImageGif},
            {".css", Resources.HelpPageCss},
            {".js", Resources.HelpPageJavascript}
        };

        public override Message Show(UriTemplate template, Uri uri)
        {
            var match = template.Match(Endpoint.Address.Uri, uri);
            var imageKey = match.BoundVariables[HelpPageBehavior.FILE_URI_PARAM];
            var extension = Path.GetExtension(imageKey);
            var contentType = ContentTypes[extension];
            var filename = Path.GetFileNameWithoutExtension(imageKey);

            var memoryStream = new MemoryStream();
            {
                switch (extension)
                {
                    case ".gif":
                        {
                            var bitmap = (Bitmap)Resources.ResourceManager.GetObject(filename, Resources.Culture);
                            bitmap.Save(memoryStream, ImageFormat.Gif);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            return WebOperationContext.Current.CreateStreamResponse(memoryStream, contentType);
                        }
                    case ".css":
                    case ".js":
                        {
                            var text = Resources.ResourceManager.GetObject(filename, Resources.Culture).ToString();
                            return WebOperationContext.Current.CreateTextResponse(text, contentType);
                        }
                    default:
                        return null;
                }
            }
        }
    }
}