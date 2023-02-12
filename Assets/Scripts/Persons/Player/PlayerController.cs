using Services;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private float _moveSpeed;
    [SerializeField] private Camera _camera;


    private Vector3 _moveDirection;

    private void Start()
    {
        _camera = Camera.main;
    }
    private void Update()
    {
        _moveDirection = Vector3.zero;
        if (Input.GetAxis("Horizontal") != 0)
        {
            _moveDirection += _camera.transform.right * Input.GetAxis("Horizontal");
        }
        if (Input.GetAxis("Vertical") != 0)
        {
            _moveDirection += _camera.transform.forward* Input.GetAxis("Vertical");
        }

    }
    void FixedUpdate()
    {
        gameObject.GetComponent<Rigidbody>().MovePosition(GetComponent<Rigidbody>().position +_moveSpeed * _moveDirection * Time.deltaTime);
    }
}
