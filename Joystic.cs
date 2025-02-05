using Godot;

public partial class Joystic : Node2D
{
    [Export] public float BlockMoveRadius = 128.0f;
    [Export] public float MoveSpeed = 200.0f;
    [Export] public float RotateSpeed = 3.0f;
    [Export] public float CameraSpeed = 500.0f;
    [Export] public bool BlockEnabled = true;

    private Vector2 _startPosition;
    private bool _isDragging = false;
    private Vector2 _touchPosition;
    private Vector2 _joystickCenter;
    private Camera2D _camera;
    public Vector2 Direction = Vector2.Zero;

    public override void _Ready()
    {
        _startPosition = Position;
        _joystickCenter = Position;
        _camera = GetNode<Camera2D>("res://scenes/GameCamera.tscn");
    }

    // ...existing Input code...

    public override void _PhysicsProcess(double delta)
    {
        if (_isDragging)
        {
            Vector2 diff = _touchPosition - _joystickCenter;
            
            // Snap to 4 directions
            float angle = diff.Angle();
            if (angle < -3f * Mathf.Pi/4 || angle > 3f * Mathf.Pi/4)
                Direction = Vector2.Left;
            else if (angle < -Mathf.Pi/4)
                Direction = Vector2.Up;
            else if (angle < Mathf.Pi/4)
                Direction = Vector2.Right;
            else if (angle < 3f * Mathf.Pi/4)
                Direction = Vector2.Down;
            
            if (BlockEnabled)
            {
                diff = Direction * BlockMoveRadius;
            }
            
            Position = _joystickCenter + diff;

            // Move camera based on joystick direction
            if (_camera != null)
            {
                _camera.Position += Direction * CameraSpeed * (float)delta;
            }
        }
        else
        {
            Direction = Vector2.Zero;
        }
    }
}