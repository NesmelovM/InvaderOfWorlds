using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _rotationSpeed;
    [SerializeField] private Camera _camera;

    private Vector3 _moveDirection;
    private Rigidbody _rb;
    private Transform _playerSkin;

    private void Start()
    {
        _playerSkin = transform.GetChild(0);
        _rb = gameObject.GetComponent<Rigidbody>();
        _camera = Camera.main;
    }
    private void Update()
    {
        _moveDirection = -Vector3.zero;
        if (Input.GetAxis("Horizontal") != 0)
        {
            _moveDirection += _camera.transform.right * Input.GetAxis("Horizontal");
        }
        if (Input.GetAxis("Vertical") != 0)
        {
            _moveDirection += _camera.transform.forward * Input.GetAxis("Vertical");
        }

    }
    void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _moveSpeed * _moveDirection * Time.deltaTime);
        //_playerSkin.rotation = Quaternion.Slerp(_playerSkin.rotation, Quaternion.LookRotation(_moveDirection), Time.deltaTime * _rotationSpeed);
    }
}
