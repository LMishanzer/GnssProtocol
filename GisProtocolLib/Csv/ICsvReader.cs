using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public interface ICsvReader
{
    Task<CsvData> ReadData(string filePath, bool isGlobal);
}