using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralAnimator : MonoBehaviour
{
    [SerializeField]
    private Transform positionToCheck;

    [SerializeField]
    private float _raycastRange = 10f;

    [SerializeField]
    private LayerMask _groundLayerMask;

    void Update()
    {
        Vector3 groundPosition = _RaycastToGround(positionToCheck.position, Vector3.up);
        
        if (positionToCheck.position.y < groundPosition.y)
        {
            positionToCheck.position = groundPosition;
            Debug.Log("Position adjusted to ground level: " + groundPosition);
        }
    }

    private Vector3 _RaycastToGround(Vector3 pos, Vector3 up)
    {
        Vector3 point = pos;

        Ray ray = new Ray(pos + _raycastRange * up, -up);
        if (Physics.Raycast(ray, out RaycastHit hit, 2f * _raycastRange, _groundLayerMask))
            point = hit.point;
        return point;
    }
}