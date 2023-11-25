IMyMotorStator Rotor, Hinge;
IMyCameraBlock Camera;
IMyRemoteControl RemoteControl;
ControlMode Control;
IMyGyro Gyroscope;

Vector3D EntityPosition, DronePosition;

double RotorRequiredAngle, HingeRequiredAngle;

const float MOUSE_SENSIVITY = 0.5f;

enum ControlMode
{
    Drone,
    Camera
}

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    Rotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Drone Rotor");
    Hinge = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Drone Hinge");
    Camera = (IMyCameraBlock)GridTerminalSystem.GetBlockWithName("Drone Camera");
    Camera.EnableRaycast = true;
    RemoteControl = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName("Drone Remote Control");
    Gyroscope = (IMyGyro)GridTerminalSystem.GetBlockWithName("Drone Gyroscope");

    EntityPosition = new Vector3D(0, 0, 0);
    DronePosition = new Vector3D();

    Control = ControlMode.Drone;

    ResetAngles();
}

void Main(string argument)
{
    if (argument == "Detect") DetectEntity();
    else if (argument == "SwitchControlMode") { if (Control == ControlMode.Drone) Control = ControlMode.Camera; else Control = ControlMode.Drone; }
    else if (argument == "ClearEntity") { ResetAngles(); EntityPosition = new Vector3D(0, 0, 0); }

    if (Control == ControlMode.Camera)
    {
        Vector2 Mouse = RemoteControl.RotationIndicator;
        Rotor.TargetVelocityRPM = -Mouse.Y * MOUSE_SENSIVITY;
        Hinge.TargetVelocityRPM = -Mouse.X * MOUSE_SENSIVITY;
        Gyroscope.Enabled = false;
    }
    else if (Control == ControlMode.Drone) Gyroscope.Enabled = true;

    if (EntityDetected())
    {
        DronePosition = Rotor.GetPosition();
        Vector3D DirectionToTarget = Vector3D.Normalize(EntityPosition - DronePosition);

        RotorRequiredAngle = GetAngleBetweenVectors(Rotor.WorldMatrix.Forward, Rotor.WorldMatrix.Right, DirectionToTarget);

        HingeRequiredAngle = 90 + GetAngleBetweenVectors(Hinge.WorldMatrix.Forward, Hinge.WorldMatrix.Right, DirectionToTarget);
        Me.CustomData = HingeRequiredAngle.ToString();

        RotateRotor();
        RotateHinge();
    }
}

void DetectEntity()
{
    MyDetectedEntityInfo EntityInfo = Camera.Raycast(3000, 0, 0);
    EntityPosition = EntityInfo.HitPosition.Value;
}

void RotateRotor()
{
    double RotorCurrentAngle = Rotor.Angle * (180 / Math.PI);
    if (RotorCurrentAngle > RotorRequiredAngle)
    {
        Rotor.UpperLimitDeg = 180;
        Rotor.LowerLimitDeg = (float)RotorRequiredAngle;
        Rotor.TargetVelocityRPM = -20f;
    }
    else if (RotorCurrentAngle < RotorRequiredAngle)
    {
        Rotor.UpperLimitDeg = (float)RotorRequiredAngle;
        Rotor.LowerLimitDeg = -180;
        Rotor.TargetVelocityRPM = 20f;
    }
}

void RotateHinge()
{
    double HingeCurrentAngle = Hinge.Angle * (180 / Math.PI) + 90;
    if (HingeCurrentAngle > HingeRequiredAngle)
    {
        Hinge.UpperLimitDeg = 90;
        Hinge.LowerLimitDeg = (float)HingeRequiredAngle;
        Hinge.TargetVelocityRPM = -20f;
    }
    else if (HingeCurrentAngle < HingeRequiredAngle)
    {
        Hinge.UpperLimitDeg = (float)RotorRequiredAngle;
        Hinge.LowerLimitDeg = 0;
        Hinge.TargetVelocityRPM = 20f;
    }
}

bool EntityDetected()
{
    if (EntityPosition.X == 0 && EntityPosition.Y == 0 && EntityPosition.Z == 0) return false;
    else return true;
}

private double GetAngleBetweenVectors(Vector3D VectorForward, Vector3D VectorRight, Vector3D EntityVector)
{
    double Cos = EntityVector.Dot(VectorForward);
    double Sin = EntityVector.Dot(VectorRight);
    return Math.Atan2(Sin, Cos) / Math.PI * 180;
}

private void ResetAngles()
{
    Rotor.UpperLimitDeg = 180f;
    Rotor.LowerLimitDeg = -180f;
    Hinge.UpperLimitDeg = 90f;
    Hinge.LowerLimitDeg = 0f;
}