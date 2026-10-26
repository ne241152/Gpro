using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Bind imported sub-assets by name, so Unity's actual file IDs are serialized.
[InitializeOnLoad]
public static class SmsCannonSpriteBinding
{
    private const string TexturePath = "Assets/Prefabs/SMScanon.png";
    private const string PrefabPath = "Assets/Prefabs/DelayBomb.prefab";

    static SmsCannonSpriteBinding()
    {
        EditorApplication.delayCall += BindWhenReady;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingEditMode) Bind();
        };
    }

    private static void BindWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
            EditorApplication.delayCall += BindWhenReady;
            return;
        }
        if (!EditorApplication.isPlayingOrWillChangePlaymode) Bind();
    }

    [MenuItem("Tools/Gpro/Repair SMS Cannon Sprites")]
    public static void Bind()
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(TexturePath)) {
            if (asset is Sprite sprite) sprites[sprite.name] = sprite;
        }
        var fields = new[] { "flightFrames", "targetFrames", "blastFrames", "clockFrames" };
        var prefixes = new[] { "Flight", "Target", "Blast", "Clock" };
        var counts = new[] { 8, 7, 9, 6 };
        for (int group = 0; group < fields.Length; group++) {
            for (int i = 0; i < counts[group]; i++) {
                string name = $"{prefixes[group]}_{i:00}";
                if (!sprites.ContainsKey(name)) {
                    Debug.LogError($"SMSキャノン: {name} が画像から読み込めません。SMScanon.pngをReimportしてください。");
                    return;
                }
            }
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return;
        DelayBomb cannon = prefab.GetComponent<DelayBomb>();
        if (cannon == null) return;
        var serialized = new SerializedObject(cannon);
        bool changed = false;
        for (int group = 0; group < fields.Length; group++) {
            SerializedProperty array = serialized.FindProperty(fields[group]);
            if (array.arraySize != counts[group]) {
                array.arraySize = counts[group];
                changed = true;
            }
            for (int i = 0; i < counts[group]; i++) {
                Sprite sprite = sprites[$"{prefixes[group]}_{i:00}"];
                SerializedProperty item = array.GetArrayElementAtIndex(i);
                if (item.objectReferenceValue == sprite) continue;
                item.objectReferenceValue = sprite;
                changed = true;
            }
        }
        if (!changed) return;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("SMSキャノン: Unityが読み込んだ30コマをPrefabへ再設定しました。");
    }
}

public class SmsCannonSpritePostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
    {
        foreach (string path in imported) {
            if (path == "Assets/Prefabs/SMScanon.png") {
                EditorApplication.delayCall += SmsCannonSpriteBinding.Bind;
                return;
            }
        }
    }
}
