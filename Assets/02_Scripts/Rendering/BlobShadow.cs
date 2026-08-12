using UnityEngine;

/// <summary>
/// Drop shadow blob under a character. Raycasts down onto the ground, lays a
/// quad on the hit surface and shrinks/fades it with height, so a jump reads as
/// a jump instead of the character just sliding up the screen.
///
/// The quad lives at the scene root, not as a child, so the character's own
/// rotation and scale cannot squash it.
/// </summary>
[DisallowMultipleComponent]
public class BlobShadow : MonoBehaviour
{
    private static readonly int StrengthId = Shader.PropertyToID("_Strength");

    [Header("Look")]
    [Tooltip("Material using the Custom/Blob Shadow shader. Shared by every character.")]
    [SerializeField] private Material shadowMaterial;
    [Tooltip("Diameter of the blob in world units when standing on the ground.")]
    [SerializeField] private float size = 1.2f;
    [Range(0f, 1f)]
    [SerializeField] private float strength = 0.55f;

    [Header("Ground Probe")]
    [Tooltip("How far down to look for ground.")]
    [SerializeField] private float maxDistance = 6f;
    [Tooltip("Start the ray this far above the pivot so it does not begin inside the floor.")]
    [SerializeField] private float originHeight = 0.5f;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("Lift off the surface, avoids z-fighting on top of the depth offset in the shader.")]
    [SerializeField] private float surfaceOffset = 0.02f;

    [Header("Height Response")]
    [Tooltip("Height at which the shadow has faded out completely.")]
    [SerializeField] private float maxHeight = 3f;
    [Tooltip("Size multiplier over normalised height (0 = grounded, 1 = maxHeight).")]
    [SerializeField] private AnimationCurve sizeOverHeight = AnimationCurve.Linear(0f, 1f, 1f, 0.55f);
    [Tooltip("Alpha multiplier over normalised height.")]
    [SerializeField] private AnimationCurve alphaOverHeight = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private Transform _quad;
    private Renderer _renderer;
    private Collider _collider;
    private MaterialPropertyBlock _block;
    private bool _warnedNoGround;
    private readonly RaycastHit[] _hits = new RaycastHit[8];

    /// <summary>
    /// World height of the character's feet. Taken from the collider rather than
    /// the transform, because a pivot at hip height would read as a permanent
    /// jump and fade the blob out while the character is standing still.
    /// </summary>
    private float FeetY => _collider != null ? _collider.bounds.min.y : transform.position.y;

    private void Awake()
    {
        if (shadowMaterial == null)
        {
            Debug.LogWarning($"{name}: BlobShadow has no Shadow Material assigned, " +
                             "nothing will be drawn. Assign a material using Custom/Blob Shadow.", this);
        }

        _collider = FindBodyCollider();
        _block = new MaterialPropertyBlock();
        CreateQuad();
    }

    private void OnDestroy()
    {
        if (_quad != null) Destroy(_quad.gameObject);
    }

    private void OnEnable()
    {
        if (_quad != null) _quad.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_quad != null) _quad.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_quad == null) return;

        // Probe from the pivot, not from the collider: a bad collider should only
        // skew the height reading, never break the ground probe outright.
        Vector3 origin = transform.position + Vector3.up * originHeight;
        float feetY = Mathf.Min(FeetY, transform.position.y);

        if (!TryFindGround(origin, out RaycastHit hit))
        {
            // Park the blob on the character so a failed probe is obvious in the
            // hierarchy instead of leaving it stranded at the world origin.
            _quad.position = transform.position;
            _renderer.enabled = false;

            if (!_warnedNoGround)
            {
                _warnedNoGround = true;
                Debug.LogWarning($"{name}: BlobShadow found no ground within {maxDistance} units " +
                                 $"below {origin}. Check Max Distance and Ground Mask.", this);
            }
            return;
        }

        _warnedNoGround = false;

        _renderer.enabled = true;

        float height = Mathf.Max(0f, feetY - hit.point.y);
        float t = maxHeight > 0f ? Mathf.Clamp01(height / maxHeight) : 0f;

        _quad.position = hit.point + hit.normal * surfaceOffset;
        // Quad faces +Z, so rotate it flat first, then match the ground normal.
        _quad.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                         * Quaternion.Euler(90f, 0f, 0f);

        float scale = size * sizeOverHeight.Evaluate(t);
        _quad.localScale = new Vector3(scale, scale, 1f);

        _block.SetFloat(StrengthId, strength * alphaOverHeight.Evaluate(t));
        _renderer.SetPropertyBlock(_block);
    }

    /// <summary>
    /// The collider that represents the body. Attack hitboxes are skipped: they
    /// are triggers and usually disabled, so their bounds are meaningless and
    /// would drag the ground probe to the wrong height.
    /// </summary>
    private Collider FindBodyCollider()
    {
        var controller = GetComponent<CharacterController>();
        if (controller != null) return controller;

        var own = GetComponent<Collider>();
        if (own != null && !own.isTrigger) return own;

        var candidates = GetComponentsInChildren<Collider>();
        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (candidate == null || candidate.isTrigger || !candidate.enabled) continue;
            return candidate;
        }

        return null;
    }

    /// <summary>
    /// First hit that is not part of this character. Without the self check the
    /// blob would stick to the character's own collider.
    /// </summary>
    private bool TryFindGround(Vector3 origin, out RaycastHit result)
    {
        result = default;

        int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, maxDistance + originHeight,
                                            groundMask, QueryTriggerInteraction.Ignore);
        if (count == 0) return false;

        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.distance >= best) continue;

            best = hit.distance;
            result = hit;
            found = true;
        }

        return found;
    }

    private void CreateQuad()
    {
        var go = new GameObject($"{name}_BlobShadow");
        go.transform.SetParent(null, true);

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = QuadMesh();

        _renderer = go.AddComponent<MeshRenderer>();
        _renderer.sharedMaterial = shadowMaterial;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        _quad = go.transform;
        // Start out flat and under the character. Without this the quad spends
        // its first frame upright at the world origin, which reads as a bug.
        _quad.SetPositionAndRotation(transform.position, Quaternion.Euler(90f, 0f, 0f));
    }

    private static Mesh _quadMesh;

    private static Mesh QuadMesh()
    {
        if (_quadMesh != null) return _quadMesh;

        var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _quadMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        // Hide before destroying: Destroy is deferred to the end of the frame,
        // so an active primitive would render once at the world origin.
        temp.SetActive(false);
        Destroy(temp);
        return _quadMesh;
    }
}
