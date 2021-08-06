namespace ITA.Common.RestHelp.Examples
{
    public enum HelpExampleType
    {
        Default = 0,

        Input = 1,

        Output = 2,

        Custom = 100,

        FailureHttp400 = 101,

        FailureHttp401 = 102,

        FailureHttp403 = 103,

        FailureHttp409 = 104,

        FailureHttp500 = 105,
    }
}