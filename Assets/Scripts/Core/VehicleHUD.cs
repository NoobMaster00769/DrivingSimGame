using UnityEngine;
using UnityEngine.UI;

public class VehicleHUD : MonoBehaviour
{
    public VehicleContext vehicle;
    public Text speedText;
    public Text gearText;
    public Text hintText;

    void Update()
    {
        float speedKmh = vehicle.rb.velocity.magnitude * 3.6f;
        speedText.text = $"Speed: {speedKmh:0} km/h";

        gearText.text = $"Gear: {vehicle.currentGear + 1}";

        // ----- SHIFT HINTS -----
        if (vehicle.engineRPM > vehicle.optimalUpshiftRPM &&
            vehicle.currentGear < vehicle.gearRatios.Length - 1)
        {
            hintText.text = "Upshift (E)";
        }
        else if (vehicle.engineRPM < vehicle.optimalDownshiftRPM &&
                 vehicle.currentGear > 0)
        {
            hintText.text = "Downshift (Q)";
        }
        else
        {
            hintText.text = "";
        }
    }
}
