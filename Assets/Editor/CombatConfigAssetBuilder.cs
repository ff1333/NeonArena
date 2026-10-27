using System.IO;
using UnityEditor;
using UnityEngine;

public static class CombatConfigAssetBuilder
{
    private const string Folder = "Assets/Resources/Combat";
    private const string BossPath = Folder + "/BossDecisionConfig.asset";
    private const string DirectorPath = Folder + "/CombatDirectorConfig.asset";

    [MenuItem("Neon Arena/Build Combat Config Assets")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }

        if (!File.Exists(BossPath))
        {
            BossDecisionConfig boss = BossDecisionConfig.CreateDefault();
            AssetDatabase.CreateAsset(boss, BossPath);
        }

        if (!File.Exists(DirectorPath))
        {
            CombatDirectorConfig director = CombatDirectorConfig.CreateDefault();
            AssetDatabase.CreateAsset(director, DirectorPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Neon Arena combat configuration assets are ready.");
    }
}
