namespace ITA.Common.RestHelp.Interfaces
{    
    public class HelpPageExtension
    {        
        public string OfflineFileName { get; set; }

        public string RelativeUrl { get; set; }

        public string Title { get; set; }

        public string MenuCaption { get; set; }

        public string HtmlContent { get; set; }

        public HelpPageExtensionType Type { get; set; }

        public int Order { get; set; }
    }
}