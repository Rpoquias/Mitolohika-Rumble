using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneObjectReplacer : MonoBehaviour
{
    [Header("OLD Object")]
    [Tooltip("Drag ONE of the old objects here.")]
    public GameObject oldObject;

    [Header("NEW Object")]
    [Tooltip("Drag your new version here.")]
    public GameObject newObject;

    [Header("Objects Parent")]
    [Tooltip("Usually 'inner circle base'.")]
    public Transform objectsParent;

    [Header("Random Y Rotation")]
    public bool randomizeYRotation = true;

#if UNITY_EDITOR

    public void ReplaceAllObjects()
    {
        if (oldObject == null)
        {
            Debug.LogError("Please assign the OLD object.");
            return;
        }

        if (newObject == null)
        {
            Debug.LogError("Please assign the NEW object.");
            return;
        }

        if (objectsParent == null)
        {
            Debug.LogError("Please assign the Objects Parent.");
            return;
        }

        string oldName = oldObject.name;

        // Store objects first so we don't modify the hierarchy while searching
        Transform[] allObjects =
            objectsParent.GetComponentsInChildren<Transform>(true);

        int replacedCount = 0;

        foreach (Transform current in allObjects)
        {
            // Don't replace the new object itself
            if (current.gameObject == newObject)
                continue;

            // Only replace objects with the same name as the OLD object
            if (current.name != oldName)
                continue;

            // Save transform information
            Vector3 position = current.position;
            Quaternion rotation = current.rotation;
            Vector3 scale = current.lossyScale;

            Transform parent = current.parent;
            int siblingIndex = current.GetSiblingIndex();

            // Create a copy of the NEW object
            GameObject replacement =
                Instantiate(newObject, parent);

            // Preserve exact position
            replacement.transform.position = position;

            // Randomize Y rotation
            if (randomizeYRotation)
            {
                int[] possibleRotations =
                {
                    0,
                    90,
                    180,
                    270,
                    360
                };

                int randomIndex =
                    Random.Range(0, possibleRotations.Length);

                float randomY =
                    possibleRotations[randomIndex];

                // Keep the new object's X and Z rotation
                Vector3 newRotation =
                    newObject.transform.eulerAngles;

                newRotation.y = randomY;

                replacement.transform.rotation =
                    Quaternion.Euler(newRotation);
            }
            else
            {
                // Use the original replacement rotation
                replacement.transform.rotation = rotation;
            }

            // Keep the new object's scale
            replacement.transform.localScale =
                newObject.transform.localScale;

            // Preserve hierarchy position
            replacement.transform.SetSiblingIndex(siblingIndex);

            // Register replacement for Undo
            Undo.RegisterCreatedObjectUndo(
                replacement,
                "Replace Scene Object"
            );

            // Delete OLD object
            Undo.DestroyObjectImmediate(current.gameObject);

            replacedCount++;
        }

        Debug.Log(
            "Replacement complete! Replaced "
            + replacedCount +
            " objects named '" + oldName + "'."
        );
    }

#endif
}


#if UNITY_EDITOR

[CustomEditor(typeof(SceneObjectReplacer))]
public class SceneObjectReplacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);

        SceneObjectReplacer replacer =
            (SceneObjectReplacer)target;

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button(
            "REPLACE ALL OLD OBJECTS",
            GUILayout.Height(45)))
        {
            replacer.ReplaceAllObjects();
        }

        GUI.backgroundColor = Color.white;
    }
}

#endif