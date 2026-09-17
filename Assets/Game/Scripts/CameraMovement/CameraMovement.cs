using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Vector2 _maxOffset = new(25, 25);
    [SerializeField] private float _minZoom = 10f;
    [SerializeField] private float _maxZoom = 40f;
    [Space]
    [Header("Smoothness settings")]
    [SerializeField] private float _zoomSmoothness = 7.5f;       //  больше -> мягче возвращается к allowed
    [SerializeField] private float _movementInertial = 7.5f;     //  больше -> сильнее инерция

    private Vector2 _position;          // смещение XZ (map offset)
    private float _zoom;                // текущий zoom (например, камера FieldOfView или расстояние)
    private Vector2 _velocity;          // скорость (инерция)
    private float _zoomVelocity;        // скорость изменения зума (инерция)
    private bool _activeInput;          // есть ли активный инпут (зажат палец/мышь)
    private Camera _camera;

    private void Awake()
    {
        // Инициализация удобная по умолчанию
        _zoom = Mathf.Lerp(_minZoom, _maxZoom, 0.5f);
        _position = Vector2.zero;

        _camera = GetComponent<Camera>();
    }

    public void Move(Vector2 delta)
    {
        // Размер экрана в мировых координатах:
        float screenToWorldScale;

        if (_camera.orthographic)
        {
            // Горизонтальный размер видимой области (ширина в unity-единицах)
            float worldScreenHeight = _camera.orthographicSize * 2f;
            float worldScreenWidth = worldScreenHeight * _camera.aspect;
            // соотношение экранных пикселей к миру:
            screenToWorldScale = worldScreenWidth / Screen.width;
        }
        else
        {
            // Для перспективы (пример для карты на высоте)
            float cameraHeight = Mathf.Abs(_camera.transform.position.y);
            float frustumHeight = 2.0f * cameraHeight * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * _camera.aspect;
            screenToWorldScale = frustumWidth / Screen.width;
        }

        // Конвертируем экранный delta (пиксели) в мировой delta
        Vector2 worldDelta = delta * screenToWorldScale;

        // Теперь смещаем именно на worldDelta (НЕ умножаем и НЕ уменьшаем delta по зуму)
        _position += worldDelta;

        _velocity = worldDelta / Time.deltaTime;

        _activeInput = true;
    }

    public void Zoom(float zoom)
    {
        // Зум оставляем как есть
        _zoom += zoom;
    }

    private void LateUpdate()
    {
        // Если нет активного инпута — запускается инерция и автозум
        if (!_activeInput)
        {
            // Движение по инерции
            _velocity = Vector2.Lerp(_velocity, Vector2.zero, _movementInertial * Time.deltaTime);
            _position += _velocity * Time.deltaTime;

            //if (_zoom >= _minZoom && _zoom <= _maxZoom)
            //{
            //    // Зум по инерции
            //    _zoomVelocity = Mathf.Lerp(_zoomVelocity, 0, _zoomSmoothness * Time.deltaTime);
            //    _zoom += _zoomVelocity * Time.deltaTime;
            //}
        }

        //Если зум вышел за пределы — плавно возвращаем
        if (_zoom < _minZoom)
            _zoom = Mathf.Lerp(_zoom, _minZoom, _zoomSmoothness * Time.deltaTime);
        if (_zoom > _maxZoom)
            _zoom = Mathf.Lerp(_zoom, _maxZoom, _zoomSmoothness * Time.deltaTime);

        // В любом случае ApplyBounds
        _position.x = Mathf.Clamp(_position.x, -_maxOffset.x, _maxOffset.x);
        _position.y = Mathf.Clamp(_position.y, -_maxOffset.y, _maxOffset.y);
        _zoom = Mathf.Clamp(_zoom, _minZoom * 0.7f, _maxZoom * 1.4f);     // Допускаем небольшой выход за допустимое, для плавности автозума

        //// Позиционируем камеру (пример для ортографической карты или top-down)
        //transform.position = new Vector3(_position.x, _zoom, _position.y);
        _camera.orthographicSize = _zoom;
        transform.position = new Vector3(_position.x, _position.y, transform.position.z);

        _activeInput = false;
    }
}
