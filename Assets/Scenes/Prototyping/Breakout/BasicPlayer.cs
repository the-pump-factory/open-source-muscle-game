using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using TempGame;

public class BasicPlayer : MonoBehaviour
{
    public Camera playerCamera;
    public BasicMobileCharacter character;

    private float cameraAngle;
    public float cameraTilt = 40;
    public float cameraDistance = 20;
    public float cameraRotationSpeed = 25;

    public float moveSpeed = 4;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (character == null) character = gameObject.AddOrGetComponent<BasicMobileCharacter>();
    }

    private void Update()
    {
        cameraAngle = cameraAngle + (TempInput.IsometricCameraRotationFactor * cameraRotationSpeed * Time.deltaTime);
        Quaternion camRot = Quaternion.Euler(0, cameraAngle, 0);
        character.nextMoveFrame += camRot * new Vector3(TempInput.MoveAxisX, 0, TempInput.MoveAxisY) * Time.deltaTime * moveSpeed; 
    }

    private void LateUpdate()
    {
        Quaternion camRot = Quaternion.Euler(0, cameraAngle, 0) * Quaternion.Euler(cameraTilt, 0, 0);

        playerCamera.transform.position = character.transform.position + camRot * Vector3.back * cameraDistance;
        playerCamera.transform.rotation = camRot; 
    }
}

