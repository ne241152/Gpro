using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterSelectManager))]
public class CharacterSelectManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CharacterSelectManager manager = (CharacterSelectManager)target;
        if (DrawDefaultInspector()) ShowPreview(manager, manager.CurrentIndex);
        if (manager.characters == null || manager.characters.Length == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("表示プレビュー", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("名前・説明・おすすめ文はキャラクターの設定データで編集できます。配置・サイズ・色は各 UI パーツで編集できます。", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < manager.characters.Length; i++)
            {
                string label = manager.characters[i].characterName;
                if (GUILayout.Button(string.IsNullOrEmpty(label) ? (i + 1).ToString() : label))
                    ShowPreview(manager, i);
            }
        }
    }

    private static void ShowPreview(CharacterSelectManager manager, int index)
    {
        if (!Application.isPlaying)
        {
            Object[] parts = { manager.characterImageDisplay, manager.nameText,
                manager.descriptionText, manager.recommendationText,
                manager.previousButton, manager.nextButton, manager.confirmButton };
            foreach (Object part in parts)
                if (part != null) Undo.RecordObject(part, "キャラクター表示プレビュー");
        }
        manager.SelectCharacter(index);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
