// Put this file in any folder named "Editor" (e.g. Assets/Editor/MissingScriptFinder.cs)
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Missing Scripts/Find In All Prefabs")]
    private static void FindInPrefabs()
    {
        int found = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing == 0) continue;

                found += missing;
                Debug.LogWarning($"[Missing Script] {missing}x on '{GetPath(t)}' in prefab: {path}", prefab);
            }
        }

        Debug.Log($"[Missing Script] Prefab scan done. Found {found}.");
    }

    [MenuItem("Tools/Missing Scripts/Find In Open Scenes")]
    private static void FindInOpenScenes()
    {
        int found = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (missing == 0) continue;

                    found += missing;
                    Debug.LogWarning($"[Missing Script] {missing}x on '{GetPath(t)}' in scene: {scene.name}", t.gameObject);
                }
            }
        }

        Debug.Log($"[Missing Script] Scene scan done. Found {found}.");
    }

    // Removes ALL missing-script components from the objects currently selected (and their children).
    [MenuItem("Tools/Missing Scripts/Remove From Selection")]
    private static void RemoveFromSelection()
    {
        int removed = 0;

        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove Missing Scripts");
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        Debug.Log($"[Missing Script] Removed {removed} missing component(s).");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}