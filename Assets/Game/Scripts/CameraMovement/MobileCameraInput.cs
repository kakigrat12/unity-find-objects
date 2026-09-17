using System;
using UnityEngine;

public class MobileCameraInput : ICameraInput, ITickable
{
    public Vector2 Delta { get; private set; }
    public float Zoom { get; private set; }

    public event Action DeltaUpdated;
    public event Action ZoomUpdated;


    public void Tick()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            //if (touch.phase == TouchPhase.Moved)
            //{
                Vector2 d = touch.deltaPosition;
                Delta = new Vector2(d.x, d.y);

                DeltaUpdated?.Invoke();
            //}
        }
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Смещение камеры (среднее перемещение двух пальцев)
            Vector2 avgDelta = (touch0.deltaPosition + touch1.deltaPosition) * 0.5f;
            Delta = new Vector2(avgDelta.x, avgDelta.y);

            // Zoom pinch
            float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
            float prevPinchDistance = Vector2.Distance(touch0.position - touch0.deltaPosition, touch1.position - touch1.deltaPosition);
            Zoom = prevPinchDistance - currentPinchDistance;

            DeltaUpdated?.Invoke();
            ZoomUpdated?.Invoke();
        }
    }
}
