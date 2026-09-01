using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileMeshCombiner))]
public class TileMeshCombinerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TileMeshCombiner combiner =
            (TileMeshCombiner)target;

        DrawDefaultInspector();

        GUILayout.Space(15);

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button(
            "GENERATE / REBUILD COMBINED MESH",
            GUILayout.Height(40)))
        {
            Undo.RegisterFullObjectHierarchyUndo(
                combiner.gameObject,
                "Combine Tile Meshes"
            );

            combiner.Combine();

            EditorUtility.SetDirty(combiner);
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        if (GUILayout.Button(
            "RESTORE ORIGINAL CUBES",
            GUILayout.Height(30)))
        {
            combiner.RestoreOriginals();

            EditorUtility.SetDirty(combiner);
        }

        if (GUILayout.Button(
            "DELETE COMBINED MESH",
            GUILayout.Height(30)))
        {
            combiner.DeleteCombined();

            EditorUtility.SetDirty(combiner);
        }
    }
}