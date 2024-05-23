using Newtonsoft.Json;

namespace GisProtocolLib.Models;

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

    public List<string> GetAllUzemiList() => _ciselnikUzemi.Keys.Order().ToList();

    public string GetOkresByUzemi(string uzemi)
    {
        var success =_ciselnikUzemi.TryGetValue(uzemi, out var okres);

        return success ? okres! : string.Empty;
    }
}