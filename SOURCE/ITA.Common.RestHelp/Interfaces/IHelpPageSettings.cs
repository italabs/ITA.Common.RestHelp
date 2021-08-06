namespace ITA.Common.RestHelp.Interfaces
{
    public interface IHelpPageSettings
    {
        bool Enabled { get; set; }

        string BaseHelpUri { get; }
    }
}