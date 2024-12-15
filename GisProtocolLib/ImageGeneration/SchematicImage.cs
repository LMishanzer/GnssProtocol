namespace GisProtocolLib.ImageGeneration;

public class SchematicImage : IDisposable
{
    public SchematicImage(MemoryStream imageStream)
    {
        ImageStream = imageStream;
        ImageStream.Seek(0, SeekOrigin.Begin);
    }

    public MemoryStream ImageStream { get; }

    public void Dispose()
    {
        ImageStream.Dispose();
        GC.SuppressFinalize(this);
    }
}