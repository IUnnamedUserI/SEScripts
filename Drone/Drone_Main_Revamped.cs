// Config const
const float MouseSensitivity = 0.5f;
const float RotationVelocityRpm = 20f;
const float MaxCameraRaycastDistance = 3000f;

// Blocks
IMyMotorStator _rotor, _hinge;
IMyCameraBlock _camera;
IMyRemoteControl _remoteControl;
IMyGyro _gyroscope;

// System status
Vector3D _targetEntityPosition;
ControlMode _currentControlMode = ControlMode.Drone;

enum ControlMode
{
    Drone,
    Camera
}

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    InitializeBlocks();
    ResetMechanicalLimits();
}

void Main(string argument)
{
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

void InitializeBlocks()
{
    _rotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Drone Rotor");
    _hinge = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Drone Hinge");
    _camera = (IMyCameraBlock)GridTerminalSystem.GetBlockWithName("Drone Camera");
    _remoteControl = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName("Drone Remote Control");
    _gyroscope = (IMyGyro)GridTerminalSystem.GetBlockWithName("Drone Gyro");

    if (_camera != null) _camera.EnableRaycast = true;
}

void ProcessCommand(string command)
{
    switch (command)
    {
        case "Detect":
            DetectTargetEntity();
            break;
        
        case "SwitchControlMode":
            ToggleControlMode();
            break;
        
        case "ClearEntity":
            ClearTarget();
            break;
    }
}

void UpdateSystemState()
{
    switch (_currentControlMode)
    {
        case ControlMode.Camera:
            HandleCameraControl();
            break;
        
        case ControlMode.Drone:
            HandleDroneControl();
            break;
    }
}

void DetectTargetEntity()
{
    var entityInfo = _camera.Raycast(MaxCameraRaycastDistance);
    if (entityInfo.HitPosition.HasValue)
    {
        _targetEntityPosition = entityInfo.HitPosition.Value;
    }
}

void ToggleControlMode()
{
    _currentControlMode = _currentControlMode == ControlMode.Drone
        ? ControlMode.Camera
        : ControlMode.Drone;
}

void ClearTarget()
{
    _targetEntityPosition = Vector3D.Zero;
    ResetMechanicalLimits();
}

void HandleCameraControl()
{
    var mouseInput = _remoteControl.RotationIndicator;
    _rotor.TargetVelocityRPM = -mouseInput.Y * MouseSensitivity;
    _hinge.TargetVelocityRPM = -mouseInput.X * MouseSensitivity;
}

void HandleDroneControl()
{
    if (!IsTargetDetected()) return;

    var DirectionToTarget = CalculateDirectionToTarget();
    RotateTowardsTarget(directionToTarget);
}

bool IsTargetDetected()
{
    return _targetEntityPosition != Vector3D.Zero;
}

Vector3D CalculateDirectionToTarget()
{
    var dronePosition = _rotor.GetPosition();
    return Vector3D.Normalize(_targetEntityPosition - dronePosition);
}

void RotateTowardsTarget(Vector3D direction)
{
    var rotorAngle = CalculateRotorAngle(direction);
    var hingeAntle = CalculateHingeAngle(direction);

    AdjustRotorRotation(rotorAngle);
    AdjustHingeRotation(hingeAngle);
}

double CalculateRotorAngle(Vector3D direction)
{
    return GetAngleBetweenVectors(
        _rotor.WorldMatrix.Forward,
        _rotor.WorldMatrix.Right,
        direction);
}

double CalculateHingeAngle(Vector3D direction)
{
    return 90 + GetAngleBetweenVectors(
        _hinge.WorldMatrix.Forward,
        _hinge.WorldMatrix.Right,
        direction);
}

void AdjustRotorRotation()
{
    // ЗДЕСЬ ОСТАНОВИЛСЯ
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

private void ResetMechanicalLimits()
{
    Rotor.UpperLimitDeg = 180f;
    Rotor.LowerLimitDeg = -180f;
    Hinge.UpperLimitDeg = 90f;
    Hinge.LowerLimitDeg = 0f;
}