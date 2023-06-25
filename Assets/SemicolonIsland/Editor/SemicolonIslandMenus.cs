using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class SemicolonIslandMenus {
    /*
    private static readonly string ADDRESSABLES_EDITOR_DLL_PATH = Path.Combine (Application.dataPath, "../ModExports");

    [MenuItem ("Semicolon Island/Show Addressable Groups")]
    public static void ShowAddressableGroups () {
        Assembly assmebly = null;
        foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies ()) {
            Debug.Log (ass.FullName);
            if (ass.FullName.Contains ("Addressables.Editor")) {
                assmebly = ass;
            }
        }
        if (assmebly == null) return;
        Type t = assmebly.GetType ("AddressableAssetsWindow");
        MethodInfo m = t.GetMethod ("Init");
        _ = m.Invoke (null, new object[0]);
        // internal classes /neg
    }*/
}