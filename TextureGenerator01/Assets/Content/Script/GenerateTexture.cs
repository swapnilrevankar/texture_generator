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
    private static int resolution = 256;
    private static RenderTextureReadWrite rtColorSpace;
    private Camera activeCamera;
    private Material material;
    private static readonly int _Color = Shader.PropertyToID("_Color");
    private static Texture2D originMap;
    private static Texture2D directionMap;
    private static Texture2D outputTex;
    private Vector3[] myVertices;
    private RaycastHit hit;

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
            CreateOriginAndDirectionMap();
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

    public class WorldPoint
    {
        public Vector3 point; //world space point
        public Vector3 normal; //world space normal
        public bool mapped; //this will be false if the ray from cage to highpoly does not hit any surface

        public WorldPoint(Vector3 p, Vector3 n, bool m)
        {
            point = p;
            normal = n;
            mapped = m;
        }
    }

    private WorldPoint UvToWorld(int resolution, Vector2 uv, Texture2D originMap, Texture2D directionMap)
    {
        bool isMapped;
        int x = 1 - (int)(uv.x * resolution);
        int y = (int)(uv.y * resolution);
        Color c;
        c = originMap.GetPixel(x, y);
        isMapped = !Mathf.Approximately(c.a, 0);
        Vector3 worldPos = new Vector3(c.r, c.g, c.b);
        c = directionMap.GetPixel(x, y);
        Vector3 normal = new Vector3(c.r * 2 - 1, c.g * 2 - 1, c.b * 2 - 1);
        return new WorldPoint(worldPos, normal, isMapped);
    }



    private void CreateOriginAndDirectionMap()
    {
        int renderLayer = 25;

        RenderTexture rt;
        rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat, rtColorSpace);

        // Create camera
        activeCamera = new GameObject("Swap_Camera").AddComponent<Camera>();
        activeCamera.transform.position = new Vector3(0.5f, 0.5f, -2.0f);
        activeCamera.orthographic = true;
        activeCamera.orthographicSize = 0.5f;
        activeCamera.cullingMask = 1 << renderLayer;
        activeCamera.targetTexture = rt;
        activeCamera.clearFlags = CameraClearFlags.Color;
        activeCamera.backgroundColor = new Color(0, 0, 0, 0);

        Material currentMat = cloneGameobject.GetComponent<MeshRenderer>().material;
        Matrix4x4 currentMatrix = currentMat.GetMatrix("_TextureMVP");
        int currentIntRotate = currentMat.GetInt("_TextureRotate");
        int currentIntFlip = currentMat.GetInt("_TextureYFlip");
        Shader shader = currentMat.shader;

        // Origin map
        originMap = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        currentMat.shader = Shader.Find("TB/UV2WorldPos");
        activeCamera.Render();
        originMap = RenderTexture2Texture2D(rt);
        SaveManager.SaveTexture2D("Assets/originMap.png", originMap, SaveManager.Extension.PNG, false, false, true, true);

        // Direction map
        directionMap = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        currentMat.shader = Shader.Find("TB/UV2Normal");
        activeCamera.Render();
        directionMap = RenderTexture2Texture2D(rt);
        SaveManager.SaveTexture2D("Assets/directionMap.png", directionMap, SaveManager.Extension.PNG, false, false, true, true);

        // Restore
        currentMat.shader = shader;
        currentMat.SetMatrix("_TextureMVP", currentMatrix);
        currentMat.SetInt("_TextureRotate", currentIntRotate);
        currentMat.SetInt("_TextureYFlip", currentIntFlip);

        activeCamera.targetTexture = null;
        DestroyRT(rt);

        outputTex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);

        // loop pixels
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                //normalize and invert x and y to get uv
                Vector2 uv = new Vector2(1 - (float)x / resolution, 1 - (float)y / resolution);

                // convert uv coordinates into surface world space point and normal
                WorldPoint worldPoint = UvToWorld(resolution, uv, originMap, directionMap);

                // do not process pixel if the converted point isn't mapped
                if (!worldPoint.mapped)
                {
                    continue;
                }

                // create ray
                Ray ray = new Ray(worldPoint.point, -worldPoint.normal);
                Debug.DrawRay(worldPoint.point, -worldPoint.normal, Color.red, 20);

                // cast ray to check intersections
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log(hit.collider.gameObject.name);
                    /*
                    MeshRenderer meshRenderer = hit.collider.GetComponent<MeshRenderer>();
                    Material mat = meshRenderer.sharedMaterial;
                    Texture2D tex = (Texture2D)mat.GetTexture("_MainTex");
                    Color pixel = tex.GetPixelBilinear(uv.x, uv.y);
                    outputTex.SetPixel(x, y, pixel);
                    */
                }
            }
        }
        //outputTex.Apply();
        //SaveManager.SaveTexture2D("Assets/outputTex.png", outputTex, SaveManager.Extension.PNG, false, false, true, true);

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
