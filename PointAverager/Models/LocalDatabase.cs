using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PointAverager.Models;

public class LocalDatabase
{
    private Dictionary<string, List<string>> _ciselnikOkresy = new();
    private Dictionary<string, string> _ciselnikUzemi = new();
    
    public async Task Init()
    {
            var fileContent = await File.ReadAllTextAsync(Path.Combine("Resources", "ciselnik.json"));
        _ciselnikOkresy = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(fileContent) ?? throw new Exception("Není možné načíst číselník.");

        foreach (var (okres, uzemiList) in _ciselnikOkresy)
        {
            foreach (var uzemi in uzemiList)
            {
                _ciselnikUzemi.Add(uzemi, okres);
            }
        }
    }

    public List<string> GetOkresyList() => _ciselnikOkresy.Keys.Order().ToList();

    public List<string> GetAllUzemiList() => _ciselnikUzemi.Keys.Order().ToList();

    public string GetOkresByUzemi(string uzemi)
    {
        var success =_ciselnikUzemi.TryGetValue(uzemi, out var okres);

        return success ? okres! : string.Empty;
    }

    public List<string> GetUzemiByOkres(string okres)
    {
        var success =_ciselnikOkresy.TryGetValue(okres, out var uzemi);

        return success ? uzemi! : [];
    }
}