namespace MIFCore.Hangfire.APIETL.Transform
{
    internal interface IApiEndpointTransformPipeline : IHandleResponse, IParseResponse, ITransformModel
    {
    }
}
