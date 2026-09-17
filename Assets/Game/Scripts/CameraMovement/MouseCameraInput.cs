using System;
using UnityEngine;

public class MouseCameraInput : ICameraInput, ITickable
{
    private Vector3 lastMousePosition;
    private bool isDragging = false;

    public Vector2 Delta { get; private set; }
    public float Zoom { get; private set; }

    public event Action DeltaUpdated;
    public event Action ZoomUpdated;


    public void Tick()
    {
        bool currentIsDrugging = isDragging;

        // Перемещение при зажатой ЛКМ
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            currentIsDrugging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            currentIsDrugging = false;
        }

        if (isDragging || currentIsDrugging)
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 diff = mousePosition - lastMousePosition;
            // Преобразуем смещение мыши на экран в координаты плоскости XZ
            Delta = new Vector2(diff.x, diff.y);
            lastMousePosition = mousePosition;

            DeltaUpdated?.Invoke();
        }

        isDragging = currentIsDrugging;

        // Zoom через колесико мыши
        if (Input.mouseScrollDelta.y != 0 || Input.mouseScrollDelta.y != Zoom)
        {
            Zoom = Input.mouseScrollDelta.y;
            ZoomUpdated?.Invoke();
        }
    }
}
