using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "BeatCitySave.json"); //pray this works, should write to correct location on all platforms

    public static void Save(SaveData data)
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath)) return new SaveData();
        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        return data ?? new SaveData();
    }

    public static void Delete()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}