namespace GisProtocolLib.Csv.Writing;

public class EmlidCsvWriter : BaseCsvWriter
{
    protected override string[] GetHeaders(bool isGlobal) => isGlobal ? ["Name", "Longitude", "Latitude", "Ellipsoidal height", "Code"] : ["Name", "Easting", "Northing", "Elevation", "Code"];
}