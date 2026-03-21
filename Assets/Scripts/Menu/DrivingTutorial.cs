using UnityEngine;
using UnityEngine.InputSystem;

public class DrivingTutorial : MonoBehaviour
{
    public VehicleInputReader input;
    public Rigidbody playerRB;
    public Transform player;
    public VehicleContext context;
    public TutorialUI ui;

    public ControlRebindItem[] keyboardItems;
    public ControllerRebindItem[] controllerItems;

    int step = 0;
    float timer;
    float stepTime;
    bool completed;
    bool isTutorialActive = false;

    float startSpeed;

    const float MIN_STEP_TIME = 8f;

    // ---------------- START ----------------

    public void StartTutorial()
    {
        var pause = FindObjectOfType<PauseSystem>();
        pause.Resume();

        isTutorialActive = true; // 🔥 IMPORTANT

        gameObject.SetActive(true);

        step = 0;
        completed = false;
        timer = 0;
        stepTime = 0;

        ResetPlayer();
        ShowStep();
    }

    void ResetPlayer()
    {
        player.position = Vector3.zero;
        player.rotation = Quaternion.identity;

        playerRB.velocity = Vector3.zero;
        playerRB.angularVelocity = Vector3.zero;

        context.currentGear = 0;
    }

    // ---------------- UPDATE ----------------
    bool lastMode;
    void Update()
    {
        if (!enabled) return;

        // 🔥 ONLY RUN WHEN ACTIVE
        if (!isTutorialActive) return;

        // 🔥 STOP if paused or menu
        if (GameStateController.Instance.currentState != GameState.Driving)
        {
            HideUI();
            return;
        }

        if (completed) return;

        bool prevMode = usingController;
        UpdateInputMode();

        if (prevMode != usingController)
            ShowStep();

        timer += Time.deltaTime;
        stepTime += Time.deltaTime;

        switch (step)
        {
            case 0: EngageDrive(); break;
            case 1: Steer(); break;
            case 2: Upshift(); break;
            case 3: Downshift(); break;
            case 4: ReverseOrSlow(); break;
            case 5: Brake(); break;
            case 6: ResetCar(); break;
            case 7: CosmicUIExplain(); break;
        }
    }

    void HideUI()
    {
        if (ui != null)
            ui.Clear();
    }

    bool usingController = false;
    float lastControllerTime;
    float lastKeyboardTime;

    void UpdateInputMode()
    {
        float t = Time.unscaledTime;

        // 🎮 detect controller input
        if (Gamepad.all.Count > 0)
        {
            var gp = Gamepad.all[0];

            if (gp.leftStick.ReadValue().sqrMagnitude > 0.01f ||
                gp.rightStick.ReadValue().sqrMagnitude > 0.01f ||
                gp.leftTrigger.ReadValue() > 0.1f ||
                gp.rightTrigger.ReadValue() > 0.1f ||
                gp.buttonSouth.wasPressedThisFrame ||
                gp.buttonEast.wasPressedThisFrame ||
                gp.buttonNorth.wasPressedThisFrame ||
                gp.buttonWest.wasPressedThisFrame)
            {
                lastControllerTime = t;
            }
        }

        // ⌨️ detect keyboard input
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            lastKeyboardTime = t;
        }

        // 🔥 decide based on MOST RECENT input
        usingController = lastControllerTime > lastKeyboardTime;
    }

    void Next()
    {
        step++;
        timer = 0;
        stepTime = 0;
        ShowStep();
    }

    // ---------------- UI ----------------

    void ShowStep()
    {
        string clutch = GetBinding("Clutch");
        string up = GetBinding("Shift Up");
        string down = GetBinding("Shift Down");

        string throttleF = GetComposite("Throttle", "positive");
        string throttleR = GetComposite("Throttle", "negative");

        string steerR = GetComposite("Steering", "positive");
        string steerL = GetComposite("Steering", "negative");

        string brake = GetBinding("Brake");
        string reset = GetBinding("ResetCar");

        switch (step)
        {
            case 0:
                ui.Show($@"ENGAGE DRIVE

Hold {clutch}  
Shift to 1st → {up}

While holding clutch:
Press {throttleF}

Release clutch smoothly

This puts the car into motion");
                break;

            case 1:
                ui.Show($@"STEERING

{steerL}   /   {steerR}

Use smooth inputs  
Avoid sudden turns

Stability comes from control");
                break;

            case 2:
                ui.Show($@"UPSHIFT

{clutch} + {up}

Shift while accelerating

Keeps speed building smoothly");
                break;

            case 3:
                ui.Show($@"DOWNSHIFT

{clutch} + {down}

Use when slowing down

Maintains control and balance");
                break;

            case 4:
                startSpeed = playerRB.velocity.magnitude;

                ui.Show($@"SLOW DOWN / REVERSE

{throttleR}

Moving → slows down  
Stopped → reverses

To move forward again:
Return to neutral → engage drive
");
                break;

            case 5:
                ui.Show($@"BRAKING

{brake}

Immediate strong stop");
                break;

            case 6:
                ui.Show($@"RESET VEHICLE

{reset}

Use if:
• You get stuck  
• Go off track  
• Face the wrong way  

Quickly recover and continue");
                break;

            case 7:
                ui.Show(@"COSMIC UI

Top-right display shows:

Current gear  
Speed  
Shift prompts  
Wrong direction warning  

Follow it to stay in rhythm");
                break;

            case 8:
                ui.Show(@"YOU'RE READY

Drive smooth  
Stay in control  

Master the flow

Press any key");
                completed = true;
                break;
        }
    }

    // ---------------- STEPS ----------------

    const float MAX_STEP_TIME = 15f; // auto progress safety

    // ---------------- STEP HELPERS ----------------

    bool Timeout() => stepTime > MAX_STEP_TIME;

    // ---------------- STEPS ----------------

    void EngageDrive()
    {
        if (stepTime < MIN_STEP_TIME) return;

        bool gearOK = context.currentGear >= 1;
        bool moving = playerRB.velocity.magnitude > 2f;
        bool throttle = input.Throttle > 0.2f;

        if ((gearOK && moving) || (throttle && moving) || Timeout())
            Next();
    }

    void Steer()
    {
        if (stepTime < MIN_STEP_TIME) return;

        if (Mathf.Abs(input.Steering) > 0.3f || Timeout())
            Next();
    }

    void Upshift()
    {
        if (stepTime < MIN_STEP_TIME) return;

        if (input.ShiftUp || context.currentGear > 1 || Timeout())
        {
            input.ConsumeShifts();
            Next();
        }
    }

    void Downshift()
    {
        if (stepTime < MIN_STEP_TIME) return;

        if (input.ShiftDown || context.currentGear < 2 || Timeout())
        {
            input.ConsumeShifts();
            Next();
        }
    }

    void ReverseOrSlow()
    {
        if (stepTime < MIN_STEP_TIME) return;

        if (input.Throttle < -0.2f || Timeout())
            Next();
    }

    void Brake()
    {
        if (stepTime < MIN_STEP_TIME) return;

        bool slowed = playerRB.velocity.magnitude < startSpeed - 1f;
        bool braking = input.Brake > 0.2f;

        if (slowed || braking || Timeout())
            Next();
    }

    void ResetCar()
    {
        if (stepTime < MIN_STEP_TIME) return;

        if (input.ResetCar || playerRB.velocity.magnitude < 0.5f || Timeout())
        {
            input.ConsumeReset();
            Next();
        }
    }

    void CosmicUIExplain()
    {
        if (stepTime > 6f)
            Next();
    }

    // ---------------- EXIT ----------------

    void LateUpdate()
    {
        if (!completed) return;

        if (Keyboard.current.anyKey.wasPressedThisFrame ||
            (Gamepad.current != null &&
             (Gamepad.current.buttonSouth.wasPressedThisFrame ||
              Gamepad.current.startButton.wasPressedThisFrame)))
        {
            ExitTutorial();
        }
    }
    void Awake()
    {
        isTutorialActive = false;

        if (ui != null)
            ui.Clear();
    }
    void ExitTutorial()
    {
        var pause = FindObjectOfType<PauseSystem>();

        isTutorialActive = false; // 🔥 STOP EVERYTHING

        HideUI();

        if (pause != null)
            pause.ExitToMainMenu();

        gameObject.SetActive(false);
    }

    // ---------------- BINDINGS ----------------

    string GetBinding(string actionName)
    {
        var actions = input.GetActions();

        if (usingController)
        {
            foreach (var item in controllerItems)
            {
                item.EnsureInitialized(actions);   // 🔥 KEY FIX
                item.UpdateDisplay();

                if (item.actionName == actionName &&
                    string.IsNullOrEmpty(item.compositePartName))
                    return item.keyText.text;
            }
        }
        else
        {
            foreach (var item in keyboardItems)
            {
                item.UpdateDisplay();

                if (item.actionName == actionName &&
                    string.IsNullOrEmpty(item.compositePartName))
                    return item.keyText.text;
            }
        }

        return "?";
    }
    string GetComposite(string actionName, string part)
    {
        var actions = input.GetActions();

        if (usingController)
        {
            foreach (var item in controllerItems)
            {
                item.EnsureInitialized(actions);   // 🔥 KEY FIX
                item.UpdateDisplay();

                if (item.actionName == actionName &&
                    item.compositePartName == part)
                    return item.keyText.text;
            }
        }
        else
        {
            foreach (var item in keyboardItems)
            {
                item.UpdateDisplay();

                if (item.actionName == actionName &&
                    item.compositePartName == part)
                    return item.keyText.text;
            }
        }

        return "?";
    }
}