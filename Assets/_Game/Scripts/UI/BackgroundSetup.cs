using UnityEngine;

/// <summary>
/// Full-screen radial gradient background using a world-space quad.
/// [ExecuteAlways] — visible in Editor without pressing Play.
/// Assign bgMaterial in Inspector.
/// </summary>
[ExecuteAlways]
public class BackgroundSetup : MonoBehaviour
{
    [Header("Background")]
    public Material bgMaterial;

    MeshRenderer bgRenderer;
    GameObject   bgQuad;

    void OnEnable()  => Rebuild();
    void OnDisable() => DestroyBG();
    void OnDestroy() => DestroyBG();

#if UNITY_EDITOR
    void OnValidate() =>
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
#endif

    void Rebuild()
    {
        DestroyBG();
        if (bgMaterial == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        float height = cam.orthographicSize * 2f;
        float width  = height * cam.aspect;

        bgQuad = new GameObject("BG_Quad") { hideFlags = HideFlags.DontSave };

        // Root object — no parent so scene object offsets don't affect it
        Vector3 camPos = cam.transform.position;
        bgQuad.transform.position   = new Vector3(camPos.x, camPos.y, camPos.z + 10f);
        bgQuad.transform.localScale = new Vector3(width, height, 1f);

        var mf  = bgQuad.AddComponent<MeshFilter>();
        mf.sharedMesh = BuildQuad();

        bgRenderer               = bgQuad.AddComponent<MeshRenderer>();
        bgRenderer.sharedMaterial = bgMaterial;
        bgRenderer.sortingOrder   = -100;
    }

    void DestroyBG()
    {
        if (bgQuad == null) return;
        if (Application.isPlaying) Destroy(bgQuad);
        else                       DestroyImmediate(bgQuad);
        bgQuad = null;
    }

    static Mesh BuildQuad()
    {
        var m = new Mesh { name = "BGQuad" };
        m.vertices  = new[] {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f), new Vector3(-0.5f,  0.5f, 0f),
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.uv = new[] {
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(1,1), new Vector2(0,1),
        };
        m.RecalculateNormals();
        return m;
    }
}
