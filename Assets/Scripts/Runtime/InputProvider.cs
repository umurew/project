using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class InputProvider : ServiceBase, IInputProvider
{
    public InputActions.PlayerActions PlayerActions => _inputActions.Player;

    public InputActions.UIActions UIActions => _inputActions.UI;

    private InputActions _inputActions;
    private List<CinemachineCamera> _cinemachineCameras;

    public override void Construct()
    {
        _inputActions = new();
        _cinemachineCameras = new List<CinemachineCamera>(FindObjectsByType<CinemachineCamera>());

        base.Construct();

        DisablePlayerControls();
        Debug.Log($"{GetType().Name} constructed.");
    }
    
    public void SetCursorState(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    public void EnablePlayerControls()
    {
        SetCursorState(true);
        ToggleCameraInput(true);

        PlayerActions.Enable();
        UIActions.Enable();
    }

    public void DisablePlayerControls()
    {
        SetCursorState(false);
        ToggleCameraInput(false);

        PlayerActions.Disable();
        UIActions.Disable();
    }

    public void SetCameraSensitivity(float sensitivityValue)
    {
        foreach (CinemachineCamera cinemachineCamera in _cinemachineCameras)
        {
            if (cinemachineCamera == null)
                continue;

            if (!cinemachineCamera.TryGetComponent(out CinemachineInputAxisController cinemachineInputAxisController))
                continue;

            foreach (var controller in cinemachineInputAxisController.Controllers)
            {
                if (controller.Name == "Look X (Pan)")
                    controller.Input.Gain = sensitivityValue;
                else if (controller.Name == "Look Y (Tilt)")
                    controller.Input.Gain = -sensitivityValue;
            }
        }
    }

    public void Dispose()
    {
        if (_inputActions == null)
            return;

        PlayerActions.Disable();
        UIActions.Disable();

        _inputActions.Dispose();
        _inputActions = null;

        Debug.Log($"{GetType().Name} disposed.");
    }

    private void ToggleCameraInput(bool isEnabled)
    {
        foreach (CinemachineCamera cinemachineCamera in _cinemachineCameras)
        {
            if (cinemachineCamera == null)
                continue;

            if (cinemachineCamera.TryGetComponent(out CinemachineInputAxisController cinemachineInputAxisController))
                cinemachineInputAxisController.enabled = isEnabled;
        }
    }

    private void OnDestroy() => Dispose();
}
