using System.Collections.Generic;
using UnityEngine;

public class GamePlayBootStrap : MonoBehaviour
{
    [SerializeField] private CameraController _cameraController;

    private readonly List<ITickable> _tickables = new();


    private void Awake()
    {
        Boot();
    }

    private void Boot()
    {
#if UNITY_EDITOR
        var cameraInput = new MouseCameraInput();
#else
        var cameraInput = new MobileCameraInput();
#endif
        _tickables.Add(cameraInput);

        _cameraController.Construct(cameraInput);
    }

    private void Update()
    {
        foreach (var x in _tickables)
            x.Tick();
    }
}
