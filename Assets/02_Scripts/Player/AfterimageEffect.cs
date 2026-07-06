using UnityEngine;
using UnityEngine.InputSystem;

public class AfterimageEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform characterModel;

    [Header("Afterimage Settings")]
    [SerializeField] private Shader ghostShader;
    [SerializeField] private Color ghostColor = new Color(0.2f, 0.5f, 1.0f, 0.6f);
    [SerializeField] private float spawnInterval = 0.05f;
    [SerializeField] private float ghostLifetime = 0.4f;
    [SerializeField] private float startAlpha = 0.6f;

    private Material ghostMaterial;
    private float spawnTimer;
    private bool isActive;

    private SkinnedMeshRenderer[] skinnedRenderers;
    private MeshFilter[] meshFilters;
    private bool useSkinned;

    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");

    private void Start()
    {
        if (ghostShader == null)
            ghostShader = Shader.Find("Custom/AfterimageGhost");

        ghostMaterial = new Material(ghostShader);
        ghostMaterial.SetColor(ColorID, ghostColor);
        ghostMaterial.SetFloat(AlphaID, startAlpha);

        if (characterModel == null)
        {
            var movement = GetComponent<MovementPlayer>();
            if (movement != null)
                characterModel = movement.characterModel;
        }

        if (characterModel != null)
        {
            skinnedRenderers = characterModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            meshFilters = characterModel.GetComponentsInChildren<MeshFilter>();
            useSkinned = skinnedRenderers != null && skinnedRenderers.Length > 0;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (isActive) Deactivate();
            else Activate();
        }

        if (!isActive) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnGhost();
            spawnTimer = spawnInterval;
        }
    }

    public void Activate()
    {
        isActive = true;
        spawnTimer = 0f;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    private void SpawnGhost()
    {
        if (useSkinned)
        {
            foreach (var smr in skinnedRenderers)
            {
                if (!smr.gameObject.activeInHierarchy) continue;

                Mesh bakedMesh = new Mesh();
                smr.BakeMesh(bakedMesh);

                CreateGhostObject(bakedMesh, smr.transform, true);
            }
        }
        else if (meshFilters != null)
        {
            foreach (var mf in meshFilters)
            {
                if (!mf.gameObject.activeInHierarchy) continue;
                if (mf.sharedMesh == null) continue;

                CreateGhostObject(mf.sharedMesh, mf.transform, false);
            }
        }
    }

    private void CreateGhostObject(Mesh mesh, Transform source, bool isBaked)
    {
        GameObject ghost = new GameObject("Afterimage");
        ghost.transform.SetPositionAndRotation(source.position, source.rotation);
        ghost.transform.localScale = isBaked ? Vector3.one : source.lossyScale;

        MeshFilter mf = ghost.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = ghost.AddComponent<MeshRenderer>();
        mr.material = new Material(ghostMaterial);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        AfterimageGhost fadeScript = ghost.AddComponent<AfterimageGhost>();
        fadeScript.Init(ghostLifetime, startAlpha);
    }

    private void OnDestroy()
    {
        if (ghostMaterial != null)
            Destroy(ghostMaterial);
    }
}

public class AfterimageGhost : MonoBehaviour
{
    private float lifetime;
    private float timer;
    private float startAlpha;
    private Material material;

    private static readonly int AlphaID = Shader.PropertyToID("_Alpha");

    public void Init(float lifetime, float startAlpha)
    {
        this.lifetime = lifetime;
        this.startAlpha = startAlpha;
        timer = lifetime;
        material = GetComponent<MeshRenderer>().material;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Destroy(material);
            Destroy(gameObject);
            return;
        }

        float alpha = startAlpha * (timer / lifetime);
        material.SetFloat(AlphaID, alpha);
    }
}
