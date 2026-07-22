using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed class CredentialStore
{
    private readonly string _filePath;

    public CredentialStore()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CAPETF");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "capital.credentials");
    }

    public bool HasSavedCredentials => File.Exists(_filePath);

    public ApiCredentials? Load()
    {
        if (!File.Exists(_filePath)) return null;
        var protectedBytes = File.ReadAllBytes(_filePath);
        var json = Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser));
        return JsonSerializer.Deserialize<ApiCredentials>(json);
    }

    public void Save(ApiCredentials credentials)
    {
        var json = JsonSerializer.Serialize(credentials);
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, protectedBytes);
    }

    public void Clear()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }
}
