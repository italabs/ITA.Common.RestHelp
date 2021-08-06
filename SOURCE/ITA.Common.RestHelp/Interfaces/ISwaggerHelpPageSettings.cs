namespace ITA.Common.RestHelp.Interfaces
{
    public interface ISwaggerHelpPageSettings : IHelpPageSettings
    {
        SwaggerVersion SwaggerHelpVersion { get; set; }
    }
}
