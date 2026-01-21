using UnityEngine;

public class DrivingStress : MonoBehaviour
{
    public VehicleContext vehicle;

    [Range(0f, 1f)]
    public float stress;

    float throttlePrev;
    float steeringPrev;

    public float throttleSensitivity = 1.5f;
    public float steeringSensitivity = 2f;
    public float rpmSensitivity = 0.0005f;

    public float stressBuildSpeed = 0.5f;
    public float stressReleaseSpeed = 1.2f;

    void Update()
    {
        float throttleDelta = Mathf.Abs(vehicle.input.Throttle - throttlePrev);
        float steeringDelta = Mathf.Abs(vehicle.input.Steering - steeringPrev);

        float rpmStress = vehicle.engineRPM * rpmSensitivity;

        float inputStress =
            throttleDelta * throttleSensitivity +
            steeringDelta * steeringSensitivity +
            rpmStress;

        if (inputStress > 0.1f)
        {
            stress += inputStress * stressBuildSpeed * Time.deltaTime;
        }
        else
        {
            stress -= stressReleaseSpeed * Time.deltaTime;
        }

        stress = Mathf.Clamp01(stress);

        throttlePrev = vehicle.input.Throttle;
        steeringPrev = vehicle.input.Steering;
    }
}
