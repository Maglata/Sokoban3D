using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundHandler : MonoBehaviour
{
    private GridManager gridManager;
    private Vector3 targetPosition;

    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    private void OnMouseDown()
    {
        if (gridManager != null)
        {
            targetPosition = transform.position;
            gridManager.HandleMouseClick(targetPosition);
        }
    }
}
