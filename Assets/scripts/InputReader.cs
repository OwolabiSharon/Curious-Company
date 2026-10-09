using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputReader : MonoBehaviour, Controls.IPlayerActions
{

    public static InputReader Instance { get; private set; }

    private Controls controls;

    // Continuous inputs
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    // Events
    public event Action JumpPressed;
    public event Action AttackPressed;
    public event Action InteractPressed;
    public event Action PausePressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);

        controls = new Controls();
        controls.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    #region Movement

    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Look = context.ReadValue<Vector2>();
    }

    #endregion

    #region Buttons

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            JumpPressed?.Invoke();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
            AttackPressed?.Invoke();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
            InteractPressed?.Invoke();

    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
            PausePressed?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {

    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (CampaignDirector.Instance != null)
        {
            CampaignDirector.Instance.Restart();
            return;
        }
        int nextScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadSceneAsync(nextScene);
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (CampaignDirector.Instance != null)
        {
            CampaignDirector.Instance.Next();
            return;
        }
        if (SceneManager.GetActiveScene().buildIndex >= 1) return;

        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadSceneAsync(nextScene);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {

    }
    #endregion
}
