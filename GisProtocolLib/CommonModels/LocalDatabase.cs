using Newtonsoft.Json;

namespace GisProtocolLib.CommonModels;

public class LocalDatabase
{
    private readonly Dictionary<string, string> _ciselnikUzemi = new();
    
    public async Task Init()
    {
        var fileContent = await File.ReadAllTextAsync(Path.Combine("Resources", "ciselnik.json"));
        var ciselnikOkresy = 
            JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(fileContent) ?? throw new Exception("Není možné načíst číselník.");

        foreach (var (okres, uzemiList) in ciselnikOkresy)
        {
            foreach (var uzemi in uzemiList)
            {
                _ciselnikUzemi.Add(uzemi, okres);
            }
        }
    }
    
    public List<string> FilterUzemi(string filter) => _ciselnikUzemi.Keys
        .Where(c => c.StartsWith(filter, StringComparison.InvariantCultureIgnoreCase))
        .Order()
        .ToList();

    public string GetOkresByUzemi(string uzemi)
    {
        var success =_ciselnikUzemi.TryGetValue(uzemi, out var okres);

        return success ? okres! : string.Empty;
    }
}