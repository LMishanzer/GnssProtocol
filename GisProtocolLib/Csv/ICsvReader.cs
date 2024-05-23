using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public interface ICsvReader
{
    Task<List<Measurement>> ReadData(string filePath, bool isGlobal);
}