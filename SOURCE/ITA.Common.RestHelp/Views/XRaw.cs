using System.Xml.Linq;

namespace ITA.Common.RestHelp.Views
{
    internal class XRaw : XText
    {
        public XRaw(string text) : base(text) { }

        public override void WriteTo(System.Xml.XmlWriter writer)
        {            
            writer.WriteRaw(this.Value);
        }
    }
}