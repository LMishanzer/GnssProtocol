using GisProtocolLib.CommonModels;

namespace GisProtocolLib.ImageGeneration;

public interface IImageGenerator
{
    SchematicImage GenerateImage(IEnumerable<Measurement> measurements);
}