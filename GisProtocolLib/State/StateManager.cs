using GisProtocolLib.Models;
using Newtonsoft.Json;

namespace GisProtocolLib.State;

public static class StateManager
{
    private const string StateFileName = "state.json";
    
    public static async Task SaveState(FormState state) => 
        await File.WriteAllTextAsync(StateFileName, JsonConvert.SerializeObject(state));

    public static async Task<FormState> RestoreState()
    {
        if (!File.Exists(StateFileName))
            return new FormState();

        var text = await File.ReadAllTextAsync(StateFileName);
        var state = JsonConvert.DeserializeObject<FormState>(text);

        return state ?? new FormState();
    }
}