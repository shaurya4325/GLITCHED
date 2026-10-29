using UnityEngine;
using UnityEngine.InputSystem;

namespace Glitched
{
    /// <summary>
    /// Wraps the PlayerControls input action asset (Resources/PlayerControls.inputactions).
    /// Control schemes: Keyboard&amp;Mouse, Gamepad, Touch (touch uses the on-screen controls, which act like a gamepad).
    /// </summary>
    public class InputHub : MonoBehaviour
    {
        public InputActionAsset Asset { get; private set; }
        InputAction move, jump, interact, dash, shrink, pause, submit, advance, debug, debugRestore;
        readonly InputAction[] answers = new InputAction[4];
        public bool GameplayBlocked { get; set; }

        void Awake()
        {
            Asset = Resources.Load<InputActionAsset>("PlayerControls");
            if (Asset == null) Asset = InputActionAsset.FromJson(DefaultJson);
            else Asset = Instantiate(Asset);
            var g = Asset.FindActionMap("Gameplay", true);
            move = g.FindAction("Move"); jump = g.FindAction("Jump"); interact = g.FindAction("Interact"); dash = g.FindAction("Dash");
            shrink = g.FindAction("Shrink"); pause = g.FindAction("Pause"); submit = g.FindAction("Submit"); advance = g.FindAction("Advance");
            debug = g.FindAction("Debug"); debugRestore = g.FindAction("DebugRestore");
            for (int i = 0; i < 4; i++) answers[i] = g.FindAction("Answer" + (i + 1));
            Asset.Enable();
        }

        public Vector2 Move => GameplayBlocked ? Vector2.zero : move.ReadValue<Vector2>();
        public bool JumpPressed => !GameplayBlocked && jump.WasPressedThisFrame();
        public bool JumpHeld => !GameplayBlocked && jump.IsPressed();
        public bool InteractPressed => !GameplayBlocked && interact.WasPressedThisFrame();
        public bool DashPressed => !GameplayBlocked && dash.WasPressedThisFrame();
        public bool ShrinkPressed => !GameplayBlocked && shrink.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool SubmitPressed => submit.WasPressedThisFrame();
        public bool AdvancePressed => advance.WasPressedThisFrame();
        public bool AdvanceHeld => advance.IsPressed();
        public bool DebugPressed => debug.WasPressedThisFrame() || ThreeFingerTap();
        public bool DebugRestorePressed => debugRestore.WasPressedThisFrame();
        public bool AnswerPressed(int i) => i >= 0 && i < 4 && answers[i].WasPressedThisFrame();
        public bool PauseHeld => pause.IsPressed();

        public bool UsingTouch => Touchscreen.current != null && (Application.isMobilePlatform || Touchscreen.current.wasUpdatedThisFrame);
        public static bool IsMobile => Application.isMobilePlatform;

        static bool ThreeFingerTap()
        {
            var ts = Touchscreen.current;
            if (ts == null) return false;
            int began = 0, active = 0;
            foreach (var t in ts.touches)
            {
                if (t.press.isPressed) active++;
                if (t.press.wasPressedThisFrame) began++;
            }
            return active >= 3 && began > 0;
        }

        /// <summary>The default PlayerControls asset (also written to Resources by the editor builder).</summary>
        public const string DefaultJson = @"{
  ""name"": ""PlayerControls"",
  ""maps"": [
    {
      ""name"": ""Gameplay"",
      ""id"": ""6b8f3a39-7f3b-4a54-9f5b-0d2c1a6c4e01"",
      ""actions"": [
        { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000001"", ""expectedControlType"": ""Vector2"" },
        { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000002"" },
        { ""name"": ""Interact"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000003"" },
        { ""name"": ""Dash"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000004"" },
        { ""name"": ""Shrink"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000005"" },
        { ""name"": ""Pause"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000006"" },
        { ""name"": ""Submit"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000007"" },
        { ""name"": ""Advance"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000008"" },
        { ""name"": ""Answer1"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-000000000009"" },
        { ""name"": ""Answer2"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-00000000000a"" },
        { ""name"": ""Answer3"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-00000000000b"" },
        { ""name"": ""Answer4"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-00000000000c"" },
        { ""name"": ""Debug"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-00000000000d"" },
        { ""name"": ""DebugRestore"", ""type"": ""Button"", ""id"": ""a1c0b1d2-0001-4000-8000-00000000000e"" }
      ],
      ""bindings"": [
        { ""name"": ""WASD"", ""path"": ""2DVector"", ""action"": ""Move"", ""isComposite"": true },
        { ""name"": ""up"", ""path"": ""<Keyboard>/w"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""down"", ""path"": ""<Keyboard>/s"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""left"", ""path"": ""<Keyboard>/a"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""right"", ""path"": ""<Keyboard>/d"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""Arrows"", ""path"": ""2DVector"", ""action"": ""Move"", ""isComposite"": true },
        { ""name"": ""up"", ""path"": ""<Keyboard>/upArrow"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""down"", ""path"": ""<Keyboard>/downArrow"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""left"", ""path"": ""<Keyboard>/leftArrow"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""name"": ""right"", ""path"": ""<Keyboard>/rightArrow"", ""action"": ""Move"", ""groups"": ""Keyboard&Mouse"", ""isPartOfComposite"": true },
        { ""path"": ""<Gamepad>/leftStick"", ""action"": ""Move"", ""groups"": ""Gamepad;Touch"" },
        { ""path"": ""<Gamepad>/dpad"", ""action"": ""Move"", ""groups"": ""Gamepad"" },
        { ""path"": ""<Keyboard>/space"", ""action"": ""Jump"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/w"", ""action"": ""Jump"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/upArrow"", ""action"": ""Jump"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/buttonSouth"", ""action"": ""Jump"", ""groups"": ""Gamepad;Touch"" },
        { ""path"": ""<Keyboard>/e"", ""action"": ""Interact"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/buttonWest"", ""action"": ""Interact"", ""groups"": ""Gamepad;Touch"" },
        { ""path"": ""<Keyboard>/leftShift"", ""action"": ""Dash"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/rightShift"", ""action"": ""Dash"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/buttonEast"", ""action"": ""Dash"", ""groups"": ""Gamepad;Touch"" },
        { ""path"": ""<Gamepad>/rightShoulder"", ""action"": ""Dash"", ""groups"": ""Gamepad"" },
        { ""path"": ""<Keyboard>/q"", ""action"": ""Shrink"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/leftShoulder"", ""action"": ""Shrink"", ""groups"": ""Gamepad;Touch"" },
        { ""path"": ""<Keyboard>/escape"", ""action"": ""Pause"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/start"", ""action"": ""Pause"", ""groups"": ""Gamepad;Touch"" },
        { ""path"": ""<Keyboard>/enter"", ""action"": ""Submit"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/numpadEnter"", ""action"": ""Submit"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/buttonSouth"", ""action"": ""Submit"", ""groups"": ""Gamepad"" },
        { ""path"": ""<Keyboard>/space"", ""action"": ""Advance"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/enter"", ""action"": ""Advance"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/e"", ""action"": ""Advance"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Mouse>/leftButton"", ""action"": ""Advance"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Gamepad>/buttonSouth"", ""action"": ""Advance"", ""groups"": ""Gamepad"" },
        { ""path"": ""<Touchscreen>/primaryTouch/press"", ""action"": ""Advance"", ""groups"": ""Touch"" },
        { ""path"": ""<Keyboard>/1"", ""action"": ""Answer1"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/numpad1"", ""action"": ""Answer1"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/2"", ""action"": ""Answer2"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/numpad2"", ""action"": ""Answer2"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/3"", ""action"": ""Answer3"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/numpad3"", ""action"": ""Answer3"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/4"", ""action"": ""Answer4"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/numpad4"", ""action"": ""Answer4"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/f3"", ""action"": ""Debug"", ""groups"": ""Keyboard&Mouse"" },
        { ""path"": ""<Keyboard>/f5"", ""action"": ""DebugRestore"", ""groups"": ""Keyboard&Mouse"" }
      ]
    }
  ],
  ""controlSchemes"": [
    { ""name"": ""Keyboard&Mouse"", ""bindingGroup"": ""Keyboard&Mouse"", ""devices"": [ { ""devicePath"": ""<Keyboard>"", ""isOptional"": false }, { ""devicePath"": ""<Mouse>"", ""isOptional"": true } ] },
    { ""name"": ""Gamepad"", ""bindingGroup"": ""Gamepad"", ""devices"": [ { ""devicePath"": ""<Gamepad>"", ""isOptional"": false } ] },
    { ""name"": ""Touch"", ""bindingGroup"": ""Touch"", ""devices"": [ { ""devicePath"": ""<Touchscreen>"", ""isOptional"": false } ] }
  ]
}";
    }
}
