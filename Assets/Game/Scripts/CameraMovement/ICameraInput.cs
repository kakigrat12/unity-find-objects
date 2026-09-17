using System;
using UnityEngine;

public interface ICameraInput
{
    Vector2 Delta { get; }
    float Zoom { get; }

    event Action DeltaUpdated;
    event Action ZoomUpdated;
}