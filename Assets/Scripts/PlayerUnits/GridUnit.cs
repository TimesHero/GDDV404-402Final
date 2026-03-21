using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridUnit : MonoBehaviour
{
    [Header("Unit Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float groundOffset = 0.02f;
    
    [Header("Movement Points")]
    [SerializeField] private int maxMovementPoints = 5;

    public int MaxMovementPoints => maxMovementPoints;
    
    [Header("Visual Root")]
    [SerializeField] private Transform visualRoot;

    [SerializeField] private Vector3 visualRotationOffsetEuler = Vector3.zero;
    private Quaternion visualRotationOffset;
    
    private GridTile currentTile;
    private bool isMoving;

    public GridTile CurrentTile => currentTile;
    public bool IsMoving => isMoving;
    
    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;
        
        visualRotationOffset = Quaternion.Euler(visualRotationOffsetEuler);
    }

    public void PlaceOnTile(GridTile tile)
    {
        if (tile == null)
            return;

        if (currentTile != null)
            currentTile.SetOccupant(null);

        currentTile = tile;
        currentTile.SetOccupant(gameObject);

        transform.position = GetGroundedWorldPosition(tile);
    }

    public void MoveAlongPath(List<GridTile> path)
    {
        if (path == null || path.Count == 0)
            return;

        if (isMoving)
            return;
        List<GridTile> pathCopy = new List<GridTile>(path);
        StartCoroutine(MoveRoutine(path));
    }

    private IEnumerator MoveRoutine(List<GridTile> path)
    {
        isMoving = true;

        if (currentTile != null)
            currentTile.SetOccupant(null);

        for (int i = 1; i < path.Count; i++)
        {
            GridTile nextTile = path[i];
            Vector3 targetPosition = GetGroundedWorldPosition(nextTile);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                Vector3 moveDirection = targetPosition - transform.position;
                moveDirection.y = 0f;

                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);

                    Quaternion finalRotation = lookRotation * visualRotationOffset;
                    
                    visualRoot.rotation = Quaternion.Slerp(
                        visualRoot.rotation,
                        finalRotation,
                        rotationSpeed * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime);

                yield return null;
            }

            transform.position = targetPosition;
            currentTile = nextTile;
        }

        if (currentTile != null)
            currentTile.SetOccupant(gameObject);

        isMoving = false;
    }
    
    private Vector3 GetGroundedWorldPosition(GridTile tile)
    {
        Vector3 tileTopCenter = tile.transform.position + Vector3.up * GetTileTopY(tile);
        float unitBottomOffset = GetUnitBottomOffset();

        return new Vector3(
            tileTopCenter.x,
            tileTopCenter.y + unitBottomOffset + groundOffset,
            tileTopCenter.z);
    }

    private float GetTileTopY(GridTile tile)
    {
        Renderer tileRenderer = tile.GetComponentInChildren<Renderer>();

        if (tileRenderer != null)
            return tileRenderer.bounds.max.y - tile.transform.position.y;

        Collider tileCollider = tile.GetComponent<Collider>();
        if (tileCollider != null)
            return tileCollider.bounds.max.y - tile.transform.position.y;

        return 0f;
    }

    private float GetUnitBottomOffset()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return 0f;

        Bounds combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        return transform.position.y - combinedBounds.min.y;
    }
}