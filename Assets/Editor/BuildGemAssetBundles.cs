using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildGemAssetBundles
{
    private const string GemBundleName = "icons/gems";

    [MenuItem("Tools/Asset Bundles/Assign Selected To Gem Bundle")]
    private static void AssignSelectedToGemBundle()
    {
        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

        if (selectedAssets.Length == 0)
        {
            Debug.LogWarning("Select one or more assets in the Project window first.");
            return;
        }

        foreach (Object asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
            {
                continue;
            }

            importer.SetAssetBundleNameAndVariant(GemBundleName, string.Empty);
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        Debug.Log($"Assigned {selectedAssets.Length} asset(s) to AssetBundle '{GemBundleName}'.");
    }

    [MenuItem("Tools/Asset Bundles/Build Gem Bundles")]
    private static void BuildGemBundles()
    {
        string outputPath = Path.Combine(Application.dataPath, "StreamingAssets");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        BuildPipeline.BuildAssetBundles(
            outputPath,
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.Refresh();
        Debug.Log($"Built AssetBundles to: {outputPath}");
    }
}
