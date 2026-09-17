using UnityEngine;

public class CameraController : MonoBehaviour
{
    private ICameraInput _cameraInput;
    [SerializeField] private CameraMovement _cameraMovement;
    [Space]
    [SerializeField] private float _zoomSensetive = 0.1f;

    public void Construct(ICameraInput cameraInput)
    {
        _cameraInput = cameraInput;

        _cameraInput.DeltaUpdated += Move;
        _cameraInput.ZoomUpdated += Zoom;
    }

    private void OnDestroy()
    {
        _cameraInput.DeltaUpdated -= Move;
        _cameraInput.ZoomUpdated -= Zoom;
    }

    private void Move()
    {
        _cameraMovement.Move(-_cameraInput.Delta);
    }

    private void Zoom()
    {
        _cameraMovement.Zoom(_cameraInput.Zoom * _zoomSensetive);
    }
}
