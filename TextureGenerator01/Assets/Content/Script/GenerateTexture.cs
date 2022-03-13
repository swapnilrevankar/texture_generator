using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateTexture : MonoBehaviour
{
    public GameObject face;

    private void Start() 
    {
        NormalizeSize(face);
    }

    private static T[] GetAll<T>(GameObject root, bool includeInactiveChildren = false, bool exclude = true) where T : Component
        {
            if (!root)
            {
                return null;
            }

            if (exclude)
            {

                List<T> rends = new List<T>();
                foreach (T component in root.GetComponentsInChildren<T>(includeInactiveChildren))
                {
                    if (component.GetComponent<ExcludeFromBake>() == null)
                    {
                        rends.Add(component);
                    }
                }

                return rends.ToArray();
            }

            return root.GetComponentsInChildren<T>();
        }
    private static Bounds ComputeBounds(GameObject root)
        {
            Quaternion currentRotation = root.transform.rotation;
            root.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);

            Renderer[] rends = GetAll<Renderer>(root);
            foreach (Renderer renderer in rends)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 localCenter = bounds.center - root.transform.position;
            bounds.center = localCenter;
            root.transform.rotation = currentRotation;

            return bounds;
        }
    private static void NormalizeSize(GameObject obj)
        {
            Transform t = obj.transform;
            t.localScale = Vector3.one;
            Bounds bounds = ComputeBounds(obj);
            Vector3 size = bounds.size;
            float scale = 1 / Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            t.localScale *= scale;
        }
}
