using GisProtocolLib.Csv.Models;

namespace GisProtocolLib.Csv.Reading;

public interface ICsvReader
{
    Task<CsvData> ReadData(string filePath, bool isGlobal, string delimiter = ",");
}