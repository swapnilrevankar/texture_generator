using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BNB;

public class GenerateTexture : MonoBehaviour
{
    public GameObject banubaFaceGameobject;
    public GameObject savedFaceGameobject;
    public GameObject[] jointNames;
    public Mesh bakedMesh;
    public GameObject userFace;
    public float scaleOffset;

    #region private varaibles
    private static GameObject cloneGameobject;
    private static int resolution = 512;
    private static RenderTextureReadWrite rtColorSpace;
    private Camera activeCamera;
    private Material material;
    private static readonly int _Color = Shader.PropertyToID("_Color");
    private static Texture2D originMap;
    private static Texture2D directionMap;
    private Vector3[] myVertices;

    #endregion

    private void Awake()
    {
        rtColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Default;
        SetupMaterial();
    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.L))
        {
            CreateBanubaMeshCopy(banubaFaceGameobject);
            //CreateSavedMeshCopy(savedFaceGameobject, scaleOffset);
            //CreateOriginAndDirectionMap();
        }
    }

    private void SetupMaterial()
    {
        material = new Material(Shader.Find("Unlit/Transparent"));
        material.name = "M_Material";
        material.SetColor(_Color, new Color(0.47f, 0.58f, 1, 0.1f));

    }

    private static void DestroyRT(RenderTexture renderTexture)
    {
        renderTexture.Release();
        DestroyImmediate(renderTexture);
    }


    private static Texture2D RenderTexture2Texture2D(RenderTexture renderTexture)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = currentRT;
        return tex;
    }



    private void CreateOriginAndDirectionMap()
    {
        int renderLayer = 25;
        RenderTexture renderTexture;
        renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat, rtColorSpace);

        // Create instance camera
        //if (activeCamera != null) DestroyImmediate(activeCamera.gameObject);
        activeCamera = new GameObject("ActiveCamera").AddComponent<Camera>();
        activeCamera.transform.position = cloneGameobject.transform.position + Vector3.forward * 5f;
        activeCamera.transform.rotation = Quaternion.Euler(0, 180, 0);
        activeCamera.orthographic = true;
        activeCamera.orthographicSize = 0.5f;
        activeCamera.cullingMask = 1 << renderLayer;
        activeCamera.clearFlags = CameraClearFlags.Color;
        activeCamera.backgroundColor = new Color(0, 0, 0, 0);
        activeCamera.targetTexture = renderTexture;

        Material curentMaterial = material;
        Shader shader = curentMaterial.shader;

        // Origin map
        originMap = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
        curentMaterial.shader = Shader.Find("TB/UV2WorldPos");
        activeCamera.Render();
        originMap = RenderTexture2Texture2D(renderTexture);

        // Direction map
        directionMap = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
        curentMaterial.shader = Shader.Find("TB/UV2Normal");
        activeCamera.Render();
        directionMap = RenderTexture2Texture2D(renderTexture);

        curentMaterial.shader = shader;
        activeCamera.targetTexture = null;
        DestroyRT(renderTexture);

    }

    private void CreateSavedMeshCopy(GameObject gameObject, float scaleOffset)
    {
        /*=========================================================================

            Mesh H
            Creates a copy of Saved static mesh generated by Banuba and normalizes the mesh

        =========================================================================*/

        /*
        Bakup code
        GameObject cloneGameobject = Instantiate(gameObject);
        NormalizeSize(cloneGameobject);
        Vector3 scaleChange = new Vector3(scaleOffset, scaleOffset, scaleOffset);
        cloneGameobject.transform.localScale += scaleChange;
        */



    }
    private void CreateBanubaMeshCopy(GameObject gameObject)
    {
        /*=========================================================================

            Mesh H
            Creates a copy of face mesh generated by Banuba and normalizes the mesh

        =========================================================================*/

        // Banuba components
        MeshRenderer banubaMeshrenderer = gameObject.GetComponent<MeshRenderer>();
        Material banubaMaterial = banubaMeshrenderer.material;
        Texture banubaTexture = banubaMaterial.mainTexture;

        // Clone mesh
        cloneGameobject = Instantiate(gameObject);
        cloneGameobject.name = "H";
        cloneGameobject.layer = 25;
        MeshFilter cloneMeshFilter = cloneGameobject.GetComponent<MeshFilter>();
        cloneMeshFilter.mesh = cloneMeshFilter.mesh;

        // Disable Banuba scripts
        FaceMeshController cloneFaceMeshControllerScript = cloneGameobject.GetComponent<FaceMeshController>();
        cloneFaceMeshControllerScript.enabled = false;

        // Duplicate Banuba texture
        Texture cloneTexture = new Texture2D(banubaTexture.width, banubaTexture.height, TextureFormat.ARGB32, false);
        Graphics.CopyTexture(banubaTexture, cloneTexture);

        // Duplicate material
        Material cloneMaterial = Instantiate(banubaMaterial);
        cloneMaterial.SetTexture("_MainTex", cloneTexture);

        // Set material matrix like in Banuba
        cloneMaterial.SetMatrix("_TextureMVP", banubaMaterial.GetMatrix("_TextureMVP"));
        cloneMaterial.SetInt("_TextureRotate", banubaMaterial.GetInt("_TextureRotate"));
        cloneMaterial.SetInt("_TextureYFlip", banubaMaterial.GetInt("_TextureYFlip"));

        // Assing material to cloned mesh
        MeshRenderer cloneMeshRenderer = cloneGameobject.GetComponent<MeshRenderer>();
        cloneMeshRenderer.material = cloneMaterial;

        // Add mesh collider
        MeshCollider meshCollider = cloneGameobject.AddComponent<MeshCollider>();

        // Normalize mesh
        NormalizeSize(cloneGameobject);

        // Move mesh to uv tile
        cloneGameobject.transform.position = new Vector3(0.5f, 0.5f, 0);
        cloneGameobject.transform.rotation = Quaternion.Euler(0, 180, 0);


        myVertices = cloneGameobject.GetComponent<MeshFilter>().mesh.vertices;
        for (int i = 0; i < myVertices.Length; i++)
        {
            jointNames[i].transform.position = transform.TransformPoint(myVertices[i]);
        }

        SkinnedMeshRenderer skinnedMeshRenderer = savedFaceGameobject.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.BakeMesh(bakedMesh);

        MeshFilter userMeshFilter = userFace.GetComponent<MeshFilter>();
        userMeshFilter.mesh = bakedMesh;
        NormalizeSize(userFace);
        Vector3 scaleChange = new Vector3(scaleOffset, scaleOffset, scaleOffset);
        userFace.transform.localScale += scaleChange;

        // Move mesh to uv tile
        userFace.transform.position = new Vector3(0.5f, 0.5f, 0);
        userFace.transform.rotation = Quaternion.Euler(0, 180, 0);

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
