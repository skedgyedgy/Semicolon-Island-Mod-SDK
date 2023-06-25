using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class BuildButtons {
    [MenuItem ("Assets/Build Map", validate = true)]
    private static bool ValidateBuildAddessableGroup () {
        // Return false if no transform is selected.
        return Selection.activeObject != null && Selection.activeObject.GetType () == typeof (AddressableAssetGroup);
    }

    private static readonly string EXPORT_DIR = Path.Combine (Application.dataPath, "../ModExports");


    [MenuItem ("Assets/Build Map", priority = -100)]
    private static void Build () {
        // if (Directory.Exists (EXPORT_DIR)) Directory.Delete (EXPORT_DIR, true);
        string group = Selection.activeObject.name;
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (AddressableAssetGroup sg in settings.groups) {
            if (sg.ReadOnly) continue;

            BundledAssetGroupSchema groupSchema = sg.GetSchema<BundledAssetGroupSchema>();
            if (groupSchema != null) groupSchema.IncludeInBuild = sg.Name == group;
            EditorUtility.SetDirty (sg);
        }
        settings.ShaderBundleCustomNaming = Random.Range (0, int.MaxValue).ToString () + "_" + Random.Range (0, int.MaxValue); // the fact i have to unironically do this shit is so stupid i hate unity

        AddressableAssetSettings.BuildPlayerContent ();

        /*foreach (string filepath in Directory.GetFiles (Path.Combine (Application.dataPath, Path.Combine ("../ModExports", EditorUserBuildSettings.activeBuildTarget.ToString ())))) {
            FileInfo fileInfo = new (filepath);
            if (!fileInfo.Name.Contains ("catalog") || fileInfo.Extension != ".json") continue;
            Debug.Log ($"getting rid of the stupid fucking dependencies i hate unity so god damn much fuck jesus fucking christ i've been trying to fix this annoying ass shit for 12 hours now unity fix your shit oh my god...");

            StreamReader reader = new (filepath);
            string contentCatalog = reader.ReadToEnd ();
            reader.Close ();

            contentCatalog = Regex.Replace (contentCatalog, "\\,\\\"\\{UnityEngine\\.AddressableAssets\\.Addressables\\.RuntimePath\\}\\\\\\\\StandaloneWindows64\\\\\\\\hotgaysexwithmario_thesearetheshadersdoNOTdeletethisgenuinely_unitybuiltinshaders_.{32}\\.bundle\\\"", "");
            // i'm so fucking pissed at this shit you wouldn't even begin to understand

            StreamWriter writer = new (filepath);
            writer.Write (contentCatalog);
            writer.Close ();

            Debug.Log ("okay done i'm gonna kms now");

            break;
        }*/
        // _ = EditorUtility.DisplayDialog ("Build", group + " AddressableGroup has successfully built.", "Ok");
        EditorUtility.RevealInFinder (Path.GetFullPath (EXPORT_DIR));
    }
}