using UnityEngine;
using UnityEngine.UI;

public class VehicleHUD : MonoBehaviour
{
    public VehicleContext vehicle;

    [Header("UI")]
    public Text speedText;
    public Text gearText;
    public Text rpmText;
    public Text hintText;

    void Update()
    {
        UpdateSpeed();
        UpdateGear();
        UpdateRPM();
        UpdateHints();
    }

    // ---------------- SPEED ----------------
    void UpdateSpeed()
    {
        float speedKmh = vehicle.rb.velocity.magnitude * 3.6f;
        speedText.text = $"Speed: {speedKmh:0} km/h";
    }

    // ---------------- GEAR ----------------
    void UpdateGear()
    {
        string gearDisplay =
            vehicle.currentGear == -1 ? "R" :
            vehicle.currentGear == 0 ? "N" :
            vehicle.currentGear.ToString();

        gearText.text = $"Gear: {gearDisplay}";
    }

    // ---------------- RPM ----------------
    void UpdateRPM()
    {
        if (vehicle.engineStalled)
        {
            rpmText.text = "RPM: 0 (STALL)";
            return;
        }

        rpmText.text = $"RPM: {vehicle.engineRPM:0}";
    }

    // ---------------- HINTS ----------------
    void UpdateHints()
    {
        // Stall feedback
        if (vehicle.engineStalled)
        {
            hintText.text = "STALL! Press Clutch";
            return;
        }

        // Neutral guidance
        if (vehicle.currentGear == 0)
        {
            hintText.text = "Select Gear (E / Q)";
            return;
        }

        // Reverse
        if (vehicle.currentGear == -1)
        {
            hintText.text = "Reverse";
            return;
        }

        // Wrong gear start
        if (vehicle.rb.velocity.magnitude < 2f &&
            vehicle.currentGear > 1 &&
            vehicle.clutch < 0.2f)
        {
            hintText.text = "Too High Gear!";
            return;
        }

        // Shift hints
        if (vehicle.engineRPM > vehicle.optimalUpshiftRPM &&
            vehicle.currentGear < vehicle.forwardGearRatios.Length)
        {
            hintText.text = "Upshift (E)";
        }
        else if (vehicle.engineRPM < vehicle.optimalDownshiftRPM &&
                 vehicle.currentGear > 1)
        {
            hintText.text = "Downshift (Q)";
        }
        else
        {
            hintText.text = "";
        }
    }
}
