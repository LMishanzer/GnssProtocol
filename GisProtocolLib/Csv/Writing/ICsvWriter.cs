using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Csv.Writing;

public interface ICsvWriter
{
    Task WriteData(string outputFilePath, IEnumerable<ReducedMeasurement> reducedMeasurements, bool isGlobal, string delimiter = ",");
}