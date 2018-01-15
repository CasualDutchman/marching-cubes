using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour {

    CharacterController controller;

    public World world;
    public Transform cube;

    public float mouseSpeed = 5;
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public float speed = 6.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;
    private Vector3 moveDirection = Vector3.zero;

    Vector3 hitter;

    void Start () {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
	}
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene(0);
        }

        //looking around
        yaw += Input.GetAxis("Mouse X") * mouseSpeed;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSpeed, -90, 90);

        transform.GetChild(0).localEulerAngles = new Vector3(pitch, 0, 0);
        transform.localEulerAngles = new Vector3(0, yaw, 0);

        //walking around
        if (controller.isGrounded) {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;

        }
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);

        //looking
        Ray ray = new Ray(transform.GetChild(0).position, transform.GetChild(0).forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 4, LayerMask.GetMask("Terrain"))) {//add layermask
            if (!cube.gameObject.activeSelf) {
                cube.gameObject.SetActive(true);
            }
            hitter = new Vector3(Mathf.RoundToInt(hit.point.x), Mathf.RoundToInt(hit.point.y), Mathf.RoundToInt(hit.point.z));
            if(Input.GetMouseButtonDown(0)) {
                world.SetBlock(hitter, 0);
            }
        }
        else if (cube.gameObject.activeSelf) {
            cube.gameObject.SetActive(false);
        }

        cube.position = hitter;
    }
}
