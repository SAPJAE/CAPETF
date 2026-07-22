using System.IO;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed class WorkspaceStore
{
    private readonly string _filePath;

    public WorkspaceStore()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CAPETF");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "workspace.json");
    }

    public WorkspaceState Load()
    {
        if (!File.Exists(_filePath)) return new WorkspaceState();
        try
        {
            return JsonSerializer.Deserialize<WorkspaceState>(File.ReadAllText(_filePath)) ?? new WorkspaceState();
        }
        catch
        {
            return new WorkspaceState();
        }
    }

    public void Save(WorkspaceState state)
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class WorkspaceState
{
    public List<string> WatchlistEpics { get; set; } = [];
    public Dictionary<string, decimal> Alerts { get; set; } = [];
}
