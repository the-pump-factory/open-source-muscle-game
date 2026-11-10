using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace TempGame
{

    public static class TempInput
    {

        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp == null) comp = gameObject.AddComponent<T>();

            return comp;

        }

        public static void RumbleMainGamepad(float lowFrequency, float highFrequency, float duration, bool fadeOut = false, float rumbleTimeStartFade = -1)
        {
        }
        public static void StopRumblingMainGamepad()
        {
        }

        #region Standard

        private static KeyCode CloseOrQuitKeyCode = KeyCode.Escape;
        public static bool CloseOrQuitKey { get { return Input.GetKey(CloseOrQuitKeyCode); } }
        public static bool CloseOrQuitKeyDown { get { return Input.GetKeyDown(CloseOrQuitKeyCode); } }
        public static bool CloseOrQuitKeyUp { get { return Input.GetKeyUp(CloseOrQuitKeyCode); } }

        public static bool PauseButton
        {
            get
            {
                return CloseOrQuitKey;
            }
        }
        public static bool PauseButtonDown
        {
            get
            {
                return CloseOrQuitKeyDown;
            }
        }
        public static bool PauseButtonUp
        {
            get
            {
                return CloseOrQuitKeyUp;
            }
        }

        private static KeyCode ConfirmKeyCode = KeyCode.Return;
        public static bool ConfirmKey { get { return Input.GetKey(ConfirmKeyCode); } }
        public static bool ConfirmKeyDown { get { return Input.GetKeyDown(ConfirmKeyCode); } }
        public static bool ConfirmKeyUp { get { return Input.GetKeyUp(ConfirmKeyCode); } }

        public static float MainLeftJoystickHorizontal
        {
            get
            {
                //var settings = swole.Settings;
                return Input.GetAxis("Left Joystick Hor");// * settings.gamepadLeftStickSensitivityX * settings.gamepadLeftStickSensitivityBase;
            }
        }
        public static float MainLeftJoystickVertical
        {
            get
            {
                //var settings = swole.Settings;
                return Input.GetAxis("Left Joystick Ver");// * settings.gamepadLeftStickSensitivityY * settings.gamepadLeftStickSensitivityBase;
            }
        }
        public static float MainRightJoystickHorizontal
        {
            get
            {
                //var settings = swole.Settings;
                return Input.GetAxis("Right Joystick Hor");// * settings.gamepadRightStickSensitivityX * settings.gamepadRightStickSensitivityBase;
            }
        }
        public static float MainRightJoystickVertical
        {
            get
            {
                //var settings = swole.Settings;
                return Input.GetAxis("Right Joystick Ver");// * settings.gamepadRightStickSensitivityY * settings.gamepadRightStickSensitivityBase;
            }
        }

        public static bool LeftBumper
        {
            get
            {
                return false;
            }
        }
        public static bool LeftBumperUp
        {
            get
            {
                return false;
            }
        }
        public static bool LeftBumperDown
        {
            get
            {
                return false;
            }
        }

        public static bool RightBumper
        {
            get
            {
                return false;
            }
        }
        public static bool RightBumperUp
        {
            get
            {
                return false;
            }
        }
        public static bool RightBumperDown
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Inventory

        private static KeyCode ItemCombineKeyCode = KeyCode.LeftShift;
        public static bool ItemCombineKey { get { return Input.GetKey(ItemCombineKeyCode); } }
        public static bool ItemCombineKeyDown { get { return Input.GetKeyDown(ItemCombineKeyCode); } }
        public static bool ItemCombineKeyUp { get { return Input.GetKeyUp(ItemCombineKeyCode); } }

        private static KeyCode ItemAlternateKeyCodeA = KeyCode.LeftControl;
        private static KeyCode ItemAlternateKeyCodeB = KeyCode.LeftCommand;
        public static bool ItemAlternateKey { get { return Input.GetKey(ItemAlternateKeyCodeA) || Input.GetKey(ItemAlternateKeyCodeB); } }
        public static bool ItemAlternateKeyDown { get { return Input.GetKeyDown(ItemAlternateKeyCodeA) || Input.GetKeyDown(ItemAlternateKeyCodeB); } }
        public static bool ItemAlternateKeyUp { get { return Input.GetKeyUp(ItemAlternateKeyCodeA) || Input.GetKeyUp(ItemAlternateKeyCodeB); } }

        private static KeyCode ItemTransferKeyCode = KeyCode.T;
        public static bool ItemTransferKey { get { return Input.GetKey(ItemTransferKeyCode); } }
        public static bool ItemTransferKeyDown { get { return Input.GetKeyDown(ItemTransferKeyCode); } }
        public static bool ItemTransferKeyUp { get { return Input.GetKeyUp(ItemTransferKeyCode); } }

        private static KeyCode ItemRotateKeyCode = KeyCode.R;
        public static bool ItemRotateKey { get { return Input.GetKey(ItemRotateKeyCode); } }
        public static bool ItemRotateKeyUp { get { return Input.GetKeyUp(ItemRotateKeyCode); } }
        public static bool ItemRotateKeyDown { get { return Input.GetKeyDown(ItemRotateKeyCode); } }

        private static KeyCode InventoryKeyCode = KeyCode.I;
        public static bool InventoryKey { get { return Input.GetKey(InventoryKeyCode); } }
        public static bool InventoryKeyUp { get { return Input.GetKeyUp(InventoryKeyCode); } }
        public static bool InventoryKeyDown { get { return Input.GetKeyDown(InventoryKeyCode); } }

        private static KeyCode EquipmentKeyCode = KeyCode.E;
        public static bool EquipmentKey { get { return Input.GetKey(EquipmentKeyCode); } }
        public static bool EquipmentKeyUp { get { return Input.GetKeyUp(EquipmentKeyCode); } }
        public static bool EquipmentKeyDown { get { return Input.GetKeyDown(EquipmentKeyCode); } }

        #endregion

        #region Modding

        private static KeyCode Modding_PrimeActionKeyCodeA = KeyCode.LeftControl;
        private static KeyCode Modding_PrimeActionKeyCodeB = KeyCode.RightControl;
        private static KeyCode Modding_PrimeActionKeyCodeC = KeyCode.LeftCommand;
        private static KeyCode Modding_PrimeActionKeyCodeD = KeyCode.RightCommand;
        public static bool Modding_PrimeActionKey { get { return Input.GetKey(Modding_PrimeActionKeyCodeA) || Input.GetKey(Modding_PrimeActionKeyCodeB) || Input.GetKey(Modding_PrimeActionKeyCodeC) || Input.GetKey(Modding_PrimeActionKeyCodeD); } }
        public static bool Modding_PrimeActionKeyUp { get { return Input.GetKeyUp(Modding_PrimeActionKeyCodeA) || Input.GetKeyUp(Modding_PrimeActionKeyCodeB) || Input.GetKeyUp(Modding_PrimeActionKeyCodeC) || Input.GetKeyUp(Modding_PrimeActionKeyCodeD); } }
        public static bool Modding_PrimeActionKeyDown { get { return Input.GetKeyDown(Modding_PrimeActionKeyCodeA) || Input.GetKeyDown(Modding_PrimeActionKeyCodeB) || Input.GetKeyDown(Modding_PrimeActionKeyCodeC) || Input.GetKeyDown(Modding_PrimeActionKeyCodeD); } }

        private static KeyCode Modding_ModifyActionKeyCodeA = KeyCode.LeftShift;
        private static KeyCode Modding_ModifyActionKeyCodeB = KeyCode.RightShift;
        public static bool Modding_ModifyActionKey { get { return Input.GetKey(Modding_ModifyActionKeyCodeA) || Input.GetKey(Modding_ModifyActionKeyCodeB); } }
        public static bool Modding_ModifyActionKeyUp { get { return Input.GetKeyUp(Modding_ModifyActionKeyCodeA) || Input.GetKeyUp(Modding_ModifyActionKeyCodeB); } }
        public static bool Modding_ModifyActionKeyDown { get { return Input.GetKeyDown(Modding_ModifyActionKeyCodeA) || Input.GetKeyDown(Modding_ModifyActionKeyCodeB); } }

        private static KeyCode Modding_AlternateActionKeyCodeA = KeyCode.LeftAlt;
        private static KeyCode Modding_AlternateActionKeyCodeB = KeyCode.RightAlt;
        public static bool Modding_AlternateActionKey { get { return Input.GetKey(Modding_AlternateActionKeyCodeA) || Input.GetKey(Modding_AlternateActionKeyCodeB); } }
        public static bool Modding_AlternateActionKeyUp { get { return Input.GetKeyUp(Modding_AlternateActionKeyCodeA) || Input.GetKeyUp(Modding_AlternateActionKeyCodeB); } }
        public static bool Modding_AlternateActionKeyDown { get { return Input.GetKeyDown(Modding_AlternateActionKeyCodeA) || Input.GetKeyDown(Modding_AlternateActionKeyCodeB); } }

        private static KeyCode Modding_PanKeyCode = KeyCode.Space;
        public static bool Modding_PanKey { get { return Input.GetKey(Modding_PanKeyCode); } }
        public static bool Modding_PanUp { get { return Input.GetKeyUp(Modding_PanKeyCode); } }
        public static bool Modding_PanDown { get { return Input.GetKeyDown(Modding_PanKeyCode); } }

        private static KeyCode Modding_DeleteKeyCode = KeyCode.Delete;
        public static bool Modding_DeleteKey { get { return Input.GetKey(Modding_DeleteKeyCode); } }
        public static bool Modding_DeleteKeyUp { get { return Input.GetKeyUp(Modding_DeleteKeyCode); } }
        public static bool Modding_DeleteKeyDown { get { return Input.GetKeyDown(Modding_DeleteKeyCode); } }

        private static KeyCode Modding_SelectAllKeyCode = KeyCode.A;
        public static bool Modding_SelectAllKey { get { return Input.GetKey(Modding_SelectAllKeyCode); } }
        public static bool Modding_SelectAllKeyUp { get { return Input.GetKeyUp(Modding_SelectAllKeyCode); } }
        public static bool Modding_SelectAllKeyDown { get { return Input.GetKeyDown(Modding_SelectAllKeyCode); } }

        private static KeyCode Modding_FocusKeyCode = KeyCode.F;
        public static bool Modding_FocusKey { get { return Input.GetKey(Modding_FocusKeyCode); } }
        public static bool Modding_FocusKeyUp { get { return Input.GetKeyUp(Modding_FocusKeyCode); } }
        public static bool Modding_FocusKeyDown { get { return Input.GetKeyDown(Modding_FocusKeyCode); } }

        private static KeyCode Modding_UndoKeyCode = KeyCode.Z;
        public static bool Modding_UndoKey { get { return Modding_PrimeActionKey && Input.GetKey(Modding_UndoKeyCode); } }
        public static bool Modding_UndoKeyUp { get { return Input.GetKeyUp(Modding_UndoKeyCode); } }
        public static bool Modding_UndoKeyDown { get { return Modding_PrimeActionKey && Input.GetKeyDown(Modding_UndoKeyCode); } }

        public static bool Modding_RedoKey { get { return Modding_ModifyActionKey && Modding_UndoKey; } }
        public static bool Modding_RedoKeyUp { get { return Modding_ModifyActionKey && Modding_UndoKeyUp; } }
        public static bool Modding_RedoKeyDown { get { return Modding_ModifyActionKey && Modding_UndoKeyDown; } }

        private static KeyCode Modding_SaveKeyCode = KeyCode.S;
        public static bool Modding_SaveKey { get { return Modding_PrimeActionKey && Input.GetKey(Modding_SaveKeyCode); } }
        public static bool Modding_SaveKeyUp { get { return Input.GetKeyUp(Modding_SaveKeyCode); } }
        public static bool Modding_SaveKeyDown { get { return Modding_PrimeActionKey && Input.GetKeyDown(Modding_SaveKeyCode); } }

        private static KeyCode Modding_CopyKeyCode = KeyCode.C;
        public static bool Modding_CopyKey { get { return Input.GetKey(Modding_CopyKeyCode); } }
        public static bool Modding_CopyKeyUp { get { return Input.GetKeyUp(Modding_CopyKeyCode); } }
        public static bool Modding_CopyKeyDown { get { return Input.GetKeyDown(Modding_CopyKeyCode); } }

        private static KeyCode Modding_PasteKeyCode = KeyCode.V;
        public static bool Modding_PasteKey { get { return Input.GetKey(Modding_PasteKeyCode); } }
        public static bool Modding_PasteKeyUp { get { return Input.GetKeyUp(Modding_PasteKeyCode); } }
        public static bool Modding_PasteKeyDown { get { return Input.GetKeyDown(Modding_PasteKeyCode); } }

        #endregion

        #region Gameplay

        private static KeyCode MoveForwardKeyCode = KeyCode.W;
        public static bool MoveForwardKey { get { return Input.GetKey(MoveForwardKeyCode); } }
        public static bool MoveForwardKeyDown { get { return Input.GetKeyDown(MoveForwardKeyCode); } }
        public static bool MoveForwardKeyUp { get { return Input.GetKeyUp(MoveForwardKeyCode); } }

        private static KeyCode MoveBackwardKeyCode = KeyCode.S;
        public static bool MoveBackwardKey { get { return Input.GetKey(MoveBackwardKeyCode); } }
        public static bool MoveBackwardKeyDown { get { return Input.GetKeyDown(MoveBackwardKeyCode); } }
        public static bool MoveBackwardKeyUp { get { return Input.GetKeyUp(MoveBackwardKeyCode); } }

        private static KeyCode MoveLeftKeyCode = KeyCode.A;
        public static bool MoveLeftKey { get { return Input.GetKey(MoveLeftKeyCode); } }
        public static bool MoveLeftKeyDown { get { return Input.GetKeyDown(MoveLeftKeyCode); } }
        public static bool MoveLeftKeyUp { get { return Input.GetKeyUp(MoveLeftKeyCode); } }

        private static KeyCode MoveRightKeyCode = KeyCode.D;
        public static bool MoveRightKey { get { return Input.GetKey(MoveRightKeyCode); } }
        public static bool MoveRightKeyDown { get { return Input.GetKeyDown(MoveRightKeyCode); } }
        public static bool MoveRightKeyUp { get { return Input.GetKeyUp(MoveRightKeyCode); } }

        public static float MoveAxisX
        {
            get
            {
                return Mathf.Clamp(((MoveRightKey ? 1 : 0) - (MoveLeftKey ? 1 : 0)) + MainLeftJoystickHorizontal, -1, 1);
            }
        }
        public static float MoveAxisY
        {
            get
            {
                return Mathf.Clamp(((MoveForwardKey ? 1 : 0) - (MoveBackwardKey ? 1 : 0)) + MainLeftJoystickVertical, -1, 1);
            }
        }

        private static KeyCode JumpKeyCode = KeyCode.Space;
        public static bool JumpKey { get { return Input.GetKey(JumpKeyCode); } }
        public static bool JumpKeyDown { get { return Input.GetKeyDown(JumpKeyCode); } }
        public static bool JumpKeyUp { get { return Input.GetKeyUp(JumpKeyCode); } }

        public static bool JumpButton
        {
            get
            {
                return JumpKey || Input.GetButton("Jump");      
            }
        }
        public static bool JumpButtonDown
        {
            get
            {
                return JumpKeyDown || Input.GetButtonDown("Jump");      
            }
        }
        public static bool JumpButtonUp
        {
            get
            {
                return JumpKeyUp || Input.GetButtonUp("Jump");      
            }
        }

        public static bool FreeLookMainAction { get { return Input.GetMouseButton(0); } }
        public static bool FreeLookMainActionDown { get { return Input.GetMouseButtonDown(0); } }
        public static bool FreeLookMainActionUp { get { return Input.GetMouseButtonUp(0); } }

        public static bool FreeLookSecondaryAction { get { return Input.GetMouseButton(1); } }
        public static bool FreeLookSecondaryActionDown { get { return Input.GetMouseButtonDown(1); } }
        public static bool FreeLookSecondaryActionUp { get { return Input.GetMouseButtonUp(1); } }

        #endregion

        #region MOBA

        public static bool MOBA_AbilityQ
        {
            get
            {
                return Input.GetKey(KeyCode.Q);
            }
        }
        public static bool MOBA_AbilityQDown
        {
            get
            {
                return Input.GetKeyDown(KeyCode.Q);
            }
        }
        public static bool MOBA_AbilityQUp
        {
            get
            {
                return Input.GetKeyUp(KeyCode.Q);
            }
        }

        public static bool MOBA_AbilityW
        {
            get
            {
                return Input.GetKey(KeyCode.W);
            }
        }
        public static bool MOBA_AbilityWDown
        {
            get
            {
                return Input.GetKeyDown(KeyCode.W);
            }
        }
        public static bool MOBA_AbilityWUp
        {
            get
            {
                return Input.GetKeyUp(KeyCode.W);
            }
        }

        public static bool MOBA_AbilityE
        {
            get
            {
                return Input.GetKey(KeyCode.E);
            }
        }
        public static bool MOBA_AbilityEDown
        {
            get
            {
                return Input.GetKeyDown(KeyCode.E);
            }
        }
        public static bool MOBA_AbilityEUp
        {
            get
            {
                return Input.GetKeyUp(KeyCode.E);
            }
        }

        public static bool MOBA_AbilityR
        {
            get
            {
                return Input.GetKey(KeyCode.R);
            }
        }
        public static bool MOBA_AbilityRDown
        {
            get
            {
                return Input.GetKeyDown(KeyCode.R);
            }
        }
        public static bool MOBA_AbilityRUp
        {
            get
            {
                return Input.GetKeyUp(KeyCode.R);
            }
        }

        #endregion

        #region Isometric

        private static KeyCode RotateClockwiseKeyCode = KeyCode.Q;
        public static bool RotateClockwiseKey { get { return Input.GetKey(RotateClockwiseKeyCode); } }
        public static bool RotateClockwiseKeyDown { get { return Input.GetKeyDown(RotateClockwiseKeyCode); } }
        public static bool RotateClockwiseKeyUp { get { return Input.GetKeyUp(RotateClockwiseKeyCode); } }

        private static KeyCode RotateCounterClockwiseKeyCode = KeyCode.E;
        public static bool RotateCounterClockwiseKey { get { return Input.GetKey(RotateCounterClockwiseKeyCode); } }
        public static bool MRotateCounterClockwiseKeyDown { get { return Input.GetKeyDown(RotateCounterClockwiseKeyCode); } }
        public static bool RotateCounterClockwiseKeyUp { get { return Input.GetKeyUp(RotateCounterClockwiseKeyCode); } }

        public static float IsometricCameraRotationFactor
        {
            get
            {
                return Mathf.Clamp(((RotateClockwiseKey ? 1 : 0) - (RotateCounterClockwiseKey ? 1 : 0)) - MainRightJoystickHorizontal, -1, 1);
            }
        }

        #endregion

        public static float Scroll { get { return Input.mouseScrollDelta.y * ScrollSpeed; } }
        public static bool ScrollUp { get { return Scroll > 0; } }
        public static bool ScrollDown { get { return Scroll < 0; } }

        public static bool Panning { get { return Input.GetKey(KeyCode.Space); } }

        public static int CursorPrimaryButtonID = 0;
        public static bool CursorPrimaryButton { get { return Input.GetMouseButton(CursorPrimaryButtonID); } }
        public static bool CursorPrimaryButtonUp { get { return Input.GetMouseButtonUp(CursorPrimaryButtonID); } }
        public static bool CursorPrimaryButtonDown { get { return Input.GetMouseButtonDown(CursorPrimaryButtonID); } }

        public static int CursorSecondaryButtonID = 1;
        public static bool CursorSecondaryButton { get { return Input.GetMouseButton(CursorSecondaryButtonID); } }
        public static bool CursorSecondaryButtonUp { get { return Input.GetMouseButtonUp(CursorSecondaryButtonID); } }
        public static bool CursorSecondaryButtonDown { get { return Input.GetMouseButtonDown(CursorSecondaryButtonID); } }

        public static int CursorAuxiliaryButtonID = 2;
        public static bool CursorAuxiliaryButton { get { return Input.GetMouseButton(CursorAuxiliaryButtonID); } }
        public static bool CursorAuxiliaryButtonUp { get { return Input.GetMouseButtonUp(CursorAuxiliaryButtonID); } }
        public static bool CursorAuxiliaryButtonDown { get { return Input.GetMouseButtonDown(CursorAuxiliaryButtonID); } }

        public static Vector2 CursorScreenPosition { get { return Input.mousePosition; } set { /* Currently unsupported */ } }
        public static Vector3 CursorWorldPositionMainCameraNCP => GetCursorWorldPositionFromCamera(Camera.main);

        public static Vector3 MousePositionWorld(Camera camera = null)
        {
            return MousePositionWorld(CursorScreenPosition, camera);
        }

        public static Vector3 MousePositionWorld(Vector3 mousePos, Camera camera = null)
        {

            if (camera == null)
            {
                camera = Camera.main;
                if (camera == null) return mousePos;
            }

            mousePos.z = camera.nearClipPlane + 0.001f;

            return camera.ScreenToWorldPoint(mousePos);

        }
        public static Vector3 GetCursorWorldPositionFromCamera(Camera camera) => MousePositionWorld(camera);

        public static float CursorAxisX
        {
            get
            {
                return Input.GetAxis("Mouse X");
            }
        }

        public static float CursorAxisY
        {
            get
            {
                return Input.GetAxis("Mouse Y");
            }
        }

        public static CursorLockMode CursorLockState
        {
            get => Cursor.lockState;

            set => Cursor.lockState = value;
        }

        public static bool IsCursorVisible
        {
            get => Cursor.visible;

            set => Cursor.visible = value;
        }

        private static Vector2 lockedCursorPosition;

        private static CursorLockMode prevLockState;

        public static void LockAndHideCursor()
        {
            LockAndHideCursor(CursorScreenPosition);
        }

        public static void LockAndHideCursor(Vector3 cursorPosition)
        {

            lockedCursorPosition = cursorPosition;

            prevLockState = Cursor.lockState;

            Cursor.lockState = CursorLockMode.Locked;

            Cursor.visible = false;

        }

        public static void UnlockAndShowCursor()
        {

            Cursor.lockState = prevLockState;

            CursorScreenPosition = lockedCursorPosition;

            Cursor.visible = true;

        }

        public static float DoubleClickSpeed => 0.44f;

        public static float ScrollSpeed { get { return 0.3f; } }

        public static float PanningSpeed { get { return 0.05f; } }

    }

}
