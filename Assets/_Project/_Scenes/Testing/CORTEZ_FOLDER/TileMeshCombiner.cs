using UnityEngine;

public class TileMeshCombiner : MonoBehaviour
{
    [Header("Combine Settings")]
    public bool disableOriginalRenderers = true;
    public bool addMeshCollider = false;

    [HideInInspector]
    public GameObject combinedObject;

    public void Combine()
    {
        // Remove previous combined mesh
        if (combinedObject != null)
        {
            if (Application.isPlaying)
                Destroy(combinedObject);
            else
                DestroyImmediate(combinedObject);

            combinedObject = null;
        }

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("No MeshFilters found under " + gameObject.name);
            return;
        }

        // Ignore the previously generated combined object
        System.Collections.Generic.List<MeshFilter> validFilters =
            new System.Collections.Generic.List<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.transform == transform)
                continue;

            if (mf.gameObject == combinedObject)
                continue;

            if (mf.sharedMesh == null)
                continue;

            validFilters.Add(mf);
        }

        if (validFilters.Count == 0)
        {
            Debug.LogWarning("No valid meshes found under " + gameObject.name);
            return;
        }

        // Make sure all meshes use the same material
        MeshRenderer firstRenderer =
            validFilters[0].GetComponent<MeshRenderer>();

        if (firstRenderer == null)
        {
            Debug.LogWarning("First mesh has no MeshRenderer.");
            return;
        }

        Material sharedMaterial = firstRenderer.sharedMaterial;

        foreach (MeshFilter mf in validFilters)
        {
            MeshRenderer renderer = mf.GetComponent<MeshRenderer>();

            if (renderer == null)
                continue;

            if (renderer.sharedMaterial != sharedMaterial)
            {
                Debug.LogWarning(
                    "Different materials detected. " +
                    "All meshes should use the same material."
                );

                return;
            }
        }

        CombineInstance[] combineInstances =
            new CombineInstance[validFilters.Count];

        for (int i = 0; i < validFilters.Count; i++)
        {
            MeshFilter mf = validFilters[i];

            combineInstances[i] = new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform =
                    transform.worldToLocalMatrix *
                    mf.transform.localToWorldMatrix
            };
        }

        // Create combined object
        combinedObject = new GameObject("Combined Mesh");
        combinedObject.transform.SetParent(transform, false);

        MeshFilter combinedMeshFilter =
            combinedObject.AddComponent<MeshFilter>();

        MeshRenderer combinedRenderer =
            combinedObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh();
        combinedMesh.name = gameObject.name + "_Combined";

        combinedMesh.indexFormat =
            UnityEngine.Rendering.IndexFormat.UInt32;

        combinedMesh.CombineMeshes(
            combineInstances,
            true,
            true
        );

        combinedMeshFilter.sharedMesh = combinedMesh;
        combinedRenderer.sharedMaterial = sharedMaterial;

        // Optional Mesh Collider
        if (addMeshCollider)
        {
            MeshCollider collider =
                combinedObject.AddComponent<MeshCollider>();

            collider.sharedMesh = combinedMesh;
        }

        // Disable original renderers
        if (disableOriginalRenderers)
        {
            foreach (MeshFilter mf in validFilters)
            {
                MeshRenderer renderer =
                    mf.GetComponent<MeshRenderer>();

                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        Debug.Log(
            "Successfully combined " +
            validFilters.Count +
            " meshes in " +
            gameObject.name
        );
    }

    public void RestoreOriginals()
    {
        MeshFilter[] meshFilters =
            GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.transform == transform)
                continue;

            if (mf.gameObject == combinedObject)
                continue;

            MeshRenderer renderer =
                mf.GetComponent<MeshRenderer>();

            if (renderer != null)
                renderer.enabled = true;
        }
    }

    public void DeleteCombined()
    {
        if (combinedObject != null)
        {
            DestroyImmediate(combinedObject);
            combinedObject = null;
        }

        RestoreOriginals();
    }
}