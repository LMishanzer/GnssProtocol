using System.Text;
using GisProtocolLib.Csv.Models;
using GisProtocolLib.Protocols.Docx;
using GisProtocolLib.Protocols.Docx.StandardProtocol;
using GisProtocolLib.Protocols.Docx.TechnickaZprava;
using GisProtocolLib.Protocols.Text;

namespace GisProtocolLib.Protocols;

public static class ProtocolProcessor<T> where T : class, IDocxDetails
{
    public static async Task<UnreadMeasurements> ProcessProtocol(ProtocolData<T> protocolData)
    {
        var isGlobal = protocolData.IsGlobal();
        var precision = protocolData.GetPrecision();
        var csvReader = protocolData.GetCsvReader();
        
        var csvData = await csvReader.ReadData(protocolData.SourceFilePath, isGlobal, protocolData.CsvDelimiter);
        var measurements = csvData.Measurements;

        var (aggregatedPositions, differences) = PositionsHelper.AggregatePositions(measurements);
        
        var protocolHelper = new TextProtocolMaker(protocolData.FormDetails, precision);

        switch (protocolData.ProtocolType)
        {
            case ProtocolType.TechnickaZprava:
            {
                if (protocolData is ProtocolData<TechnickaZpravaDetails> protocolDocx)
                {
                    var docxProtocolHelper = new TechnickaZpravaMaker(protocolDocx.ProtocolDocxDetails);
                    docxProtocolHelper.CreateProtocol(measurements, aggregatedPositions);
                }

                break;
            }
            case ProtocolType.OnlyAveragedPoints:
            {
                var textToWrite = protocolHelper.OnlyAveraged(aggregatedPositions);
                await File.WriteAllTextAsync(protocolData.OutputFilePath, textToWrite, Encoding.UTF8);
                break;
            }
            case ProtocolType.RegularProtocol:
            {
                if (protocolData is ProtocolData<ProtocolDocxDetails> protocolDocx)
                {
                    var textToWrite = protocolHelper.CreateProtocol(measurements, aggregatedPositions, differences, protocolData.FitForA4);
                    await File.WriteAllTextAsync(protocolData.OutputFilePath, textToWrite, Encoding.UTF8);
                    var docxProtocolHelper = new StandardProtocolMaker(protocolDocx.ProtocolDocxDetails);
                    docxProtocolHelper.CreateProtocol(measurements, aggregatedPositions);
                }

                break;
            }
        }
        
        return csvData.UnreadMeasurements;
    } 
}