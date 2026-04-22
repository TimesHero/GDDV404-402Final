using UnityEngine;

public class EnemyVisionDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridUnit ownerUnit;
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private GridManager gridManager;

    [Header("Vision")]
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 1.25f, 0f);
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private float unitTargetHeightOffset = 1f;
    [SerializeField] private float barrelTargetHeightOffset = 0.6f;

    [Header("Debug View")]
    [SerializeField] private bool drawVisionGizmo = true;
    [SerializeField] private bool drawOnlyWhenSelected = true;
    [SerializeField] private Color visionArcColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    [SerializeField] private Color visionFillColor = new Color(1f, 0.85f, 0.2f, 0.08f);
    [SerializeField] private Color eyePointColor = new Color(1f, 0.4f, 0.2f, 0.9f);
    [SerializeField, Range(8, 96)] private int gizmoArcSegments = 28;

    [Header("Game View Debug")]
    [SerializeField] private bool drawVisionInGame = false;
    [SerializeField] private bool showGameVisionOnlyWhenSelected = false;
    [SerializeField] private float gameVisionHeightOffset = 0.05f;
    [SerializeField] private float gameVisionLineWidth = 0.06f;
    [SerializeField, Range(8, 96)] private int gameVisionSegments = 36;

    private GameObject runtimeVisionRoot;
    private LineRenderer runtimeVisionOutline;
    private LineRenderer runtimeVisionRays;
    private Material runtimeVisionMaterial;
    private const int IgnoreRaycastLayer = 2;
    private const int VisionDebugSortingOrder = 0;
    private const int VisionDebugRenderQueue = 3000;

    public GridUnit OwnerUnit => ownerUnit;

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        if (ownerUnit == null)
            ownerUnit = GetComponent<GridUnit>();

        if (obstacleManager == null)
            obstacleManager = FindFirstObjectByType<ObstacleManager>();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        EnsureRuntimeVisionRenderers();
        RefreshRuntimeVisionVisibility();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        EnsureRuntimeVisionRenderers();
        RefreshRuntimeVisionVisibility();
    }

    private void LateUpdate()
    {
        UpdateRuntimeVision();
    }

    public bool CanSeeUnit(GridUnit targetUnit)
    {
        if (ownerUnit == null || targetUnit == null)
            return false;

        if (targetUnit.IsDead || ownerUnit.IsDead)
            return false;

        if (targetUnit.Team == ownerUnit.Team)
            return false;

        Vector3 targetPosition = GetUnitTargetPoint(targetUnit);

        if (!IsTargetWithinVisionCone(targetPosition, targetUnit.CurrentTile))
            return false;

        if (!HasLineOfSightToUnit(targetUnit, targetPosition))
            return false;

        bool canSee = HasGridObstacleLineOfSight(targetUnit.CurrentTile);
        if (canSee)
            LevelObjectiveRuntimeManager.NotifyPlayerSeenIfNotHidden(targetUnit);

        return canSee;
    }

    public bool CanSeeBarrel(BarrelInteractable barrel)
    {
        if (ownerUnit == null || barrel == null)
            return false;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        Vector3 targetPosition = GetBarrelTargetPoint(barrel);

        if (!IsTargetWithinVisionCone(targetPosition, barrelTile))
            return false;

        if (!HasLineOfSightToTarget(targetPosition, barrel.transform))
            return false;

        bool canSee = HasGridObstacleLineOfSight(barrelTile);
        if (canSee && barrel.HiddenUnit != null)
            LevelObjectiveRuntimeManager.NotifyPlayerSeenIfNotHidden(barrel.HiddenUnit);

        return canSee;
    }

    public bool CanSeeTile(GridTile tile)
    {
        if (ownerUnit == null || tile == null)
            return false;

        Vector3 targetPosition = tile.transform.position + Vector3.up * barrelTargetHeightOffset;

        if (!IsTargetWithinVisionCone(targetPosition, tile))
            return false;

        return HasGridObstacleLineOfSight(tile);
    }

    public static bool CanAnyEnemySeeUnit(GridUnit targetUnit)
    {
        EnemyVisionDetector[] detectors = FindObjectsByType<EnemyVisionDetector>(FindObjectsSortMode.None);

        foreach (EnemyVisionDetector detector in detectors)
        {
            if (detector != null && detector.CanSeeUnit(targetUnit))
                return true;
        }

        return false;
    }

    public static bool CanAnyEnemySeeBarrel(BarrelInteractable barrel)
    {
        EnemyVisionDetector[] detectors = FindObjectsByType<EnemyVisionDetector>(FindObjectsSortMode.None);

        foreach (EnemyVisionDetector detector in detectors)
        {
            if (detector != null && detector.CanSeeBarrel(barrel))
                return true;
        }

        return false;
    }

    public static void RefreshHiddenState(HiddenStateComponent hiddenState)
    {
        if (hiddenState == null)
            return;

        BarrelInteractable barrel = hiddenState.CurrentBarrel;
        if (barrel == null)
            return;

        bool isCurrentlySeen = CanAnyEnemySeeBarrel(barrel);

        if (isCurrentlySeen)
        {
            if (hiddenState.BarrelKnownToEnemies)
                hiddenState.SetHiddenState(false, true);
            else
                hiddenState.SetHiddenState(true, false);
            return;
        }

        if (hiddenState.BarrelKnownToEnemies)
        {
            hiddenState.SetHiddenState(true, true);
            return;
        }

        hiddenState.SetHiddenState(true, false);
    }

    public static void RevealHiddenByVisibleBarrelMovement(HiddenStateComponent hiddenState)
    {
        if (hiddenState == null || hiddenState.CurrentBarrel == null)
            return;

        if (CanAnyEnemySeeBarrel(hiddenState.CurrentBarrel))
        {
            hiddenState.SetHiddenState(false, true);
            return;
        }

        RefreshHiddenState(hiddenState);
    }

    public static void RefreshAllHiddenStates()
    {
        HiddenStateComponent[] hiddenStates = FindObjectsByType<HiddenStateComponent>(FindObjectsSortMode.None);

        foreach (HiddenStateComponent hiddenState in hiddenStates)
        {
            if (hiddenState == null || hiddenState.CurrentBarrel == null)
                continue;

            RefreshHiddenState(hiddenState);
        }
    }

    private bool IsTargetWithinVisionCone(Vector3 targetWorldPosition, GridTile targetTile)
    {
        if (ownerUnit == null)
            return false;

        if (!IsWithinVisionRange(targetTile, targetWorldPosition))
            return false;

        Vector3 origin = GetEyePosition();
        Vector3 directionToTarget = targetWorldPosition - origin;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.0001f)
            return true;

        Vector3 forward = ownerUnit.GetVisualForward();
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = transform.forward;

        float angleToTarget = Vector3.Angle(forward.normalized, directionToTarget.normalized);
        return angleToTarget <= ownerUnit.VisionAngle * 0.5f;
    }

    private bool IsWithinVisionRange(GridTile targetTile, Vector3 targetWorldPosition)
    {
        if (ownerUnit == null)
            return false;

        if (ownerUnit.CurrentTile != null && targetTile != null)
        {
            int tileDistance =
                Mathf.Abs(ownerUnit.CurrentTile.X - targetTile.X) +
                Mathf.Abs(ownerUnit.CurrentTile.Y - targetTile.Y);

            return tileDistance <= ownerUnit.VisionRange;
        }

        float distance = Vector3.Distance(transform.position, targetWorldPosition);
        return distance <= ownerUnit.VisionRange;
    }

    private bool HasLineOfSightToTarget(Vector3 targetWorldPosition, Transform targetRoot)
    {
        Vector3 origin = GetEyePosition();
        Vector3 direction = targetWorldPosition - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction /= distance;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (ShouldIgnoreHit(hit.collider, targetRoot))
                continue;

            Transform hitRoot = hit.collider.transform.root;

            if (targetRoot != null && hitRoot == targetRoot.root)
                return true;

            BarrelInteractable barrel = hit.collider.GetComponentInParent<BarrelInteractable>();
            if (targetRoot != null && barrel != null && barrel.transform.root == targetRoot.root)
                return true;

            return false;
        }

        return true;
    }

    private bool HasLineOfSightToUnit(GridUnit targetUnit, Vector3 targetWorldPosition)
    {
        if (targetUnit == null)
            return false;

        Vector3 origin = GetEyePosition();
        Vector3 direction = targetWorldPosition - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction /= distance;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (ShouldIgnoreUnitHit(hit.collider, targetUnit))
                continue;

            Transform hitRoot = hit.collider.transform.root;

            if (hitRoot == targetUnit.transform.root)
                return true;

            return false;
        }

        return true;
    }

    private Vector3 GetEyePosition()
    {
        return transform.position + eyeOffset;
    }

    private bool HasGridObstacleLineOfSight(GridTile targetTile)
    {
        if (ownerUnit == null || ownerUnit.CurrentTile == null || targetTile == null)
            return true;

        if (obstacleManager == null)
            return true;

        System.Collections.Generic.List<GridTile> lineTiles = GetTilesOnLine(ownerUnit.CurrentTile, targetTile);
        if (lineTiles.Count <= 2)
            return true;

        float combinedVisibilityChance = 1f;
        int partialObstacleHash = 17;

        for (int i = 1; i < lineTiles.Count - 1; i++)
        {
            GridTile tile = lineTiles[i];
            if (tile == null)
                continue;

            PlacedObstacle placedObstacle = obstacleManager.GetPlacedObstacleAtTile(tile.GridPosition);
            if (placedObstacle == null || placedObstacle.ObstacleData == null)
                continue;

            VisionOcclusionType occlusionType = placedObstacle.ObstacleData.GetResolvedVisionOcclusion();

            if (occlusionType == VisionOcclusionType.None)
                continue;

            if (occlusionType == VisionOcclusionType.Full)
                return false;

            combinedVisibilityChance *= Mathf.Clamp01(placedObstacle.ObstacleData.PartialVisionVisibilityChance);
            partialObstacleHash = partialObstacleHash * 31 + placedObstacle.Origin.x;
            partialObstacleHash = partialObstacleHash * 31 + placedObstacle.Origin.y;
        }

        if (combinedVisibilityChance >= 0.999f)
            return true;

        if (combinedVisibilityChance <= 0f)
            return false;

        return PassesDeterministicPartialCoverCheck(targetTile, combinedVisibilityChance, partialObstacleHash);
    }

    private System.Collections.Generic.List<GridTile> GetTilesOnLine(GridTile startTile, GridTile endTile)
    {
        var result = new System.Collections.Generic.List<GridTile>();

        if (startTile == null || endTile == null)
            return result;

        int x0 = startTile.X;
        int y0 = startTile.Y;
        int x1 = endTile.X;
        int y1 = endTile.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            GridTile tile = GetTileAtGridPosition(x0, y0);
            if (tile != null)
                result.Add(tile);

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = err * 2;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return result;
    }

    private GridTile GetTileAtGridPosition(int x, int y)
    {
        if (gridManager == null)
            return null;

        return gridManager.GetTileAt(new Vector2Int(x, y));
    }

    private bool PassesDeterministicPartialCoverCheck(GridTile targetTile, float visibilityChance, int partialObstacleHash)
    {
        int hash = 17;
        hash = hash * 31 + ownerUnit.CurrentTile.X;
        hash = hash * 31 + ownerUnit.CurrentTile.Y;
        hash = hash * 31 + targetTile.X;
        hash = hash * 31 + targetTile.Y;
        hash = hash * 31 + partialObstacleHash;

        float normalized = Mathf.Abs(hash % 1000) / 999f;
        return normalized <= visibilityChance;
    }

    private Vector3 GetUnitTargetPoint(GridUnit targetUnit)
    {
        if (targetUnit == null)
            return Vector3.zero;

        Renderer[] renderers = targetUnit.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.center;
        }

        return targetUnit.transform.position + Vector3.up * unitTargetHeightOffset;
    }

    private Vector3 GetBarrelTargetPoint(BarrelInteractable barrel)
    {
        if (barrel == null)
            return Vector3.zero;

        Renderer[] renderers = barrel.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.center;
        }

        return barrel.transform.position + Vector3.up * barrelTargetHeightOffset;
    }

    private bool ShouldIgnoreHit(Collider hitCollider, Transform targetRoot)
    {
        if (hitCollider == null)
            return true;

        Transform hitRoot = hitCollider.transform.root;

        if (hitRoot == transform.root)
            return true;

        if (targetRoot != null && hitRoot == targetRoot.root)
            return false;

        if (hitCollider.GetComponentInParent<GridTile>() != null)
            return true;

        return false;
    }

    private bool ShouldIgnoreUnitHit(Collider hitCollider, GridUnit targetUnit)
    {
        if (hitCollider == null)
            return true;

        if (ShouldIgnoreHit(hitCollider, targetUnit != null ? targetUnit.transform : null))
            return true;

        if (targetUnit == null || targetUnit.CurrentTile == null)
            return false;

        BarrelInteractable barrel = hitCollider.GetComponentInParent<BarrelInteractable>();
        if (barrel == null)
            return false;

        GridTile barrelTile = barrel.GetBarrelTilePublic();
        if (barrelTile == null)
            return false;

        return barrelTile == targetUnit.CurrentTile;
    }

    private void OnDrawGizmos()
    {
        if (!drawVisionGizmo || drawOnlyWhenSelected)
            return;

        DrawVisionGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawVisionGizmo)
            return;

        DrawVisionGizmo();
    }

    private void DrawVisionGizmo()
    {
        GridUnit debugOwner = ownerUnit != null ? ownerUnit : GetComponent<GridUnit>();
        if (debugOwner == null)
            return;

        Vector3 origin = transform.position + eyeOffset;
        Vector3 forward = debugOwner.GetVisualForward();
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = transform.forward;

        forward.Normalize();

        float radius = Mathf.Max(0.1f, debugOwner.VisionRange);
        float halfAngle = Mathf.Clamp(debugOwner.VisionAngle * 0.5f, 0f, 180f);
        int segments = Mathf.Clamp(gizmoArcSegments, 8, 96);

        Gizmos.color = eyePointColor;
        Gizmos.DrawSphere(origin, 0.08f);

        if (debugOwner.VisionAngle >= 359.5f)
        {
            Gizmos.color = visionArcColor;
            Gizmos.DrawWireSphere(origin, radius);
            return;
        }

        Vector3 leftEdge = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
        Vector3 rightEdge = Quaternion.Euler(0f, halfAngle, 0f) * forward;

        Gizmos.color = visionArcColor;
        Gizmos.DrawLine(origin, origin + leftEdge * radius);
        Gizmos.DrawLine(origin, origin + rightEdge * radius);

        Vector3 previousPoint = origin + leftEdge * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 direction = Quaternion.Euler(0f, currentAngle, 0f) * forward;
            Vector3 currentPoint = origin + direction * radius;

            Gizmos.DrawLine(previousPoint, currentPoint);

            Gizmos.color = visionFillColor;
            Gizmos.DrawLine(origin, currentPoint);
            Gizmos.color = visionArcColor;

            previousPoint = currentPoint;
        }
    }

    private void EnsureRuntimeVisionRenderers()
    {
        if (runtimeVisionRoot == null)
        {
            Transform existing = transform.Find("RuntimeVisionDebug");
            runtimeVisionRoot = existing != null ? existing.gameObject : new GameObject("RuntimeVisionDebug");
            runtimeVisionRoot.transform.SetParent(transform, false);
            runtimeVisionRoot.transform.localPosition = Vector3.zero;
            runtimeVisionRoot.transform.localRotation = Quaternion.identity;
        }

        runtimeVisionRoot.layer = IgnoreRaycastLayer;
        RemoveDebugColliders(runtimeVisionRoot);

        if (runtimeVisionMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                runtimeVisionMaterial = new Material(shader);
                runtimeVisionMaterial.renderQueue = VisionDebugRenderQueue;
            }
        }

        if (runtimeVisionMaterial != null)
            runtimeVisionMaterial.renderQueue = VisionDebugRenderQueue;

        if (runtimeVisionOutline == null)
            runtimeVisionOutline = GetOrCreateLineRenderer("VisionOutline");

        if (runtimeVisionRays == null)
            runtimeVisionRays = GetOrCreateLineRenderer("VisionRays");
    }

    private LineRenderer GetOrCreateLineRenderer(string childName)
    {
        Transform child = runtimeVisionRoot.transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : new GameObject(childName);
        childObject.transform.SetParent(runtimeVisionRoot.transform, false);
        childObject.layer = IgnoreRaycastLayer;
        RemoveDebugColliders(childObject);

        LineRenderer renderer = childObject.GetComponent<LineRenderer>();
        if (renderer == null)
            renderer = childObject.AddComponent<LineRenderer>();

        renderer.useWorldSpace = true;
        renderer.loop = false;
        renderer.alignment = LineAlignment.View;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.numCornerVertices = 2;
        renderer.numCapVertices = 2;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.sortingOrder = VisionDebugSortingOrder;

        if (runtimeVisionMaterial != null)
            renderer.material = runtimeVisionMaterial;

        renderer.startWidth = gameVisionLineWidth;
        renderer.endWidth = gameVisionLineWidth;
        renderer.startColor = visionArcColor;
        renderer.endColor = visionArcColor;

        return renderer;
    }

    private void RemoveDebugColliders(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (Application.isPlaying)
                Destroy(colliders[i]);
            else
                DestroyImmediate(colliders[i]);
        }
    }

    private void UpdateRuntimeVision()
    {
        if (!Application.isPlaying)
            return;

        EnsureRuntimeVisionRenderers();
        RefreshRuntimeVisionVisibility();

        if (runtimeVisionRoot == null || !runtimeVisionRoot.activeSelf)
            return;

        GridUnit debugOwner = ownerUnit != null ? ownerUnit : GetComponent<GridUnit>();
        if (debugOwner == null)
            return;

        Vector3 origin = GetRuntimeVisionOrigin();
        Vector3 forward = debugOwner.GetVisualForward();
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = transform.forward;

        forward.Normalize();

        float radius = Mathf.Max(0.1f, debugOwner.VisionRange);
        float halfAngle = Mathf.Clamp(debugOwner.VisionAngle * 0.5f, 0f, 180f);
        int segments = Mathf.Clamp(gameVisionSegments, 8, 96);

        runtimeVisionOutline.startWidth = gameVisionLineWidth;
        runtimeVisionOutline.endWidth = gameVisionLineWidth;
        runtimeVisionOutline.startColor = visionArcColor;
        runtimeVisionOutline.endColor = visionArcColor;

        runtimeVisionRays.startWidth = gameVisionLineWidth * 0.75f;
        runtimeVisionRays.endWidth = gameVisionLineWidth * 0.75f;
        runtimeVisionRays.startColor = visionArcColor;
        runtimeVisionRays.endColor = visionArcColor;

        if (debugOwner.VisionAngle >= 359.5f)
        {
            runtimeVisionOutline.loop = true;
            runtimeVisionOutline.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * 360f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                runtimeVisionOutline.SetPosition(i, origin + direction * radius);
            }

            runtimeVisionRays.loop = false;
            runtimeVisionRays.positionCount = 0;
            return;
        }

        runtimeVisionOutline.loop = false;
        runtimeVisionOutline.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * forward;
            runtimeVisionOutline.SetPosition(i, origin + direction * radius);
        }

        Vector3 leftEdge = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
        Vector3 rightEdge = Quaternion.Euler(0f, halfAngle, 0f) * forward;

        runtimeVisionRays.loop = false;
        runtimeVisionRays.positionCount = 4;
        runtimeVisionRays.SetPosition(0, origin + leftEdge * radius);
        runtimeVisionRays.SetPosition(1, origin);
        runtimeVisionRays.SetPosition(2, origin);
        runtimeVisionRays.SetPosition(3, origin + rightEdge * radius);
    }

    private Vector3 GetRuntimeVisionOrigin()
    {
        Vector3 origin = transform.position;
        origin.y += gameVisionHeightOffset;
        return origin;
    }

    private void RefreshRuntimeVisionVisibility()
    {
        if (runtimeVisionRoot == null)
            return;

        bool shouldShow = drawVisionInGame && Application.isPlaying;

        if (showGameVisionOnlyWhenSelected)
            shouldShow &= IsSelectedInEditor();

        runtimeVisionRoot.SetActive(shouldShow);
    }

    private bool IsSelectedInEditor()
    {
#if UNITY_EDITOR
        return UnityEditor.Selection.activeGameObject == gameObject;
#else
        return true;
#endif
    }
}
