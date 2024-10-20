using System.Text;
using GisProtocolLib.Csv.Models;
using GisProtocolLib.Protocols.Docx;
using GisProtocolLib.Protocols.Text;

namespace GisProtocolLib.Protocols;

public static class ProtocolProcessor
{
    public static async Task<UnreadMeasurements> ProcessProtocol(ProtocolData protocolData)
    {
        var isGlobal = protocolData.IsGlobal();
        var precision = protocolData.GetPrecision();
        var csvReader = protocolData.GetCsvReader();
        
        var csvData = await csvReader.ReadData(protocolData.SourceFilePath, isGlobal, protocolData.CsvDelimiter);
        var measurements = csvData.Measurements;

        var (aggregatedPositions, differences) = PositionsHelper.AggregatePositions(measurements);
        
        var protocolHelper = new TextProtocolMaker(protocolData.FormDetails, precision);

        if (protocolData.OnlyAveragedPoints)
        {
            var textToWrite = protocolHelper.OnlyAveraged(measurements, aggregatedPositions);
            await File.WriteAllTextAsync(protocolData.OutputFilePath, textToWrite, Encoding.UTF8);
        }
        else
        {
            var textToWrite = protocolHelper.CreateProtocol(measurements, aggregatedPositions, differences, protocolData.FitForA4);
            await File.WriteAllTextAsync(protocolData.OutputFilePath, textToWrite, Encoding.UTF8);
            var docxProtocolHelper = new DocxProtocolHelper(protocolData.DocxDetails);
            docxProtocolHelper.CreateProtocol(measurements);
        }

        return csvData.UnreadMeasurements;
    } 
}