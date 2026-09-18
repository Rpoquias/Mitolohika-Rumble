using UnityEditor;
using UnityEngine;

public static class ArenaTileColliderTool
{
    [MenuItem("Tools/Arena/Add Box Colliders To Tiles")]
    private static void AddBoxCollidersToTiles()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning(
                "[ARENA] Select an arena/environment parent first."
            );

            return;
        }

        MeshFilter[] meshFilters =
            selectedObject.GetComponentsInChildren<MeshFilter>(true);

        int addedCount = 0;
        int skippedCount = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            GameObject tile = meshFilter.gameObject;

            MeshRenderer renderer =
                tile.GetComponent<MeshRenderer>();

            if (renderer == null)
                continue;

            BoxCollider boxCollider =
                tile.GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                skippedCount++;
                continue;
            }

            Undo.AddComponent<BoxCollider>(tile);

            addedCount++;
        }

        Debug.Log(
            $"[ARENA] Added {addedCount} BoxColliders. " +
            $"Skipped {skippedCount} existing colliders."
        );
    }

    [MenuItem("Tools/Arena/Enable Tile Renderers")]
    private static void EnableTileRenderers()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning(
                "[ARENA] Select an arena/environment parent first."
            );

            return;
        }

        MeshFilter[] meshFilters =
            selectedObject.GetComponentsInChildren<MeshFilter>(true);

        int enabledCount = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            GameObject tile = meshFilter.gameObject;

            MeshRenderer renderer =
                tile.GetComponent<MeshRenderer>();

            if (renderer == null)
                continue;

            if (!renderer.enabled)
            {
                Undo.RecordObject(renderer, "Enable Tile Renderer");

                renderer.enabled = true;

                enabledCount++;
            }
        }

        Debug.Log(
            $"[ARENA] Enabled {enabledCount} tile renderers."
        );
    }

    [MenuItem("Tools/Arena/Setup Arena Tiles")]
    private static void SetupArenaTiles()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning(
                "[ARENA] Select an arena/environment parent first."
            );

            return;
        }

        MeshFilter[] meshFilters =
            selectedObject.GetComponentsInChildren<MeshFilter>(true);

        int collidersAdded = 0;
        int renderersEnabled = 0;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            GameObject tile = meshFilter.gameObject;

            MeshRenderer renderer =
                tile.GetComponent<MeshRenderer>();

            if (renderer == null)
                continue;

            // Enable renderer
            if (!renderer.enabled)
            {
                Undo.RecordObject(renderer, "Enable Tile Renderer");

                renderer.enabled = true;

                renderersEnabled++;
            }

            // Add collider
            BoxCollider boxCollider =
                tile.GetComponent<BoxCollider>();

            if (boxCollider == null)
            {
                Undo.AddComponent<BoxCollider>(tile);

                collidersAdded++;
            }
        }

        Debug.Log(
            $"[ARENA] Setup complete. " +
            $"Renderers enabled: {renderersEnabled}. " +
            $"BoxColliders added: {collidersAdded}."
        );
    }
}