using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHuman : MonoBehaviour
{

    public Player player;

    new public ThirdPersonCamera camera;

    protected void Awake()
    {

        if (player == null) player = gameObject.GetComponent<Player>();

        if (player == null) return;

        player.OnUpdate.AddListener(ReadInput);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    protected void ReadInput()
    {

        if (player != null)
        {

            player.inputX = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            player.inputY = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);

            player.inputSlide = Input.GetKey(KeyCode.C);
            player.inputSprint = Input.GetKey(KeyCode.LeftShift);
            player.inputJump = Input.GetKey(KeyCode.Space);

        }

        if (camera != null)
        {

            camera.inputAxisX = Input.GetAxis("Mouse X");
            camera.inputAxisY = Input.GetAxis("Mouse Y");

        }

    }

}
