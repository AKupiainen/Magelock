using UnityEngine;
using MageLock.Events;

namespace MageLock.Gameplay.Events
{
    /// <summary>
    /// Event fired when movement input changes (from any input source)
    /// </summary>
    public struct MovementInputEvent : IEventData
    {
        public Vector2 MoveInput { get; }
        public InputSource Source { get; }
        public float Timestamp { get; }
        
        public MovementInputEvent(Vector2 moveInput, InputSource source)
        {
            MoveInput = moveInput;
            Source = source;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Event fired when a joystick interaction starts
    /// </summary>
    public struct JoystickStartEvent : IEventData
    {
        public Vector2 InitialPosition { get; }
        public float Timestamp { get; }
        
        public JoystickStartEvent(Vector2 initialPosition)
        {
            InitialPosition = initialPosition;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Event fired when a joystick interaction ends
    /// </summary>
    public struct JoystickEndEvent : IEventData
    {
        public Vector2 FinalPosition { get; }
        public float Duration { get; }
        public float Timestamp { get; }
        
        public JoystickEndEvent(Vector2 finalPosition, float duration)
        {
            FinalPosition = finalPosition;
            Duration = duration;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Event fired to request input mode change
    /// </summary>
    public struct InputModeChangeEvent : IEventData
    {
        public InputMode NewMode { get; }
        public InputMode PreviousMode { get; }
        
        public InputModeChangeEvent(InputMode newMode, InputMode previousMode)
        {
            NewMode = newMode;
            PreviousMode = previousMode;
        }
    }
    
    /// <summary>
    /// Event fired when local player status changes
    /// </summary>
    public struct LocalPlayerStatusEvent : IEventData
    {
        public GameObject Player { get; }
        public bool IsLocalPlayer { get; }
        
        public LocalPlayerStatusEvent(GameObject player, bool isLocalPlayer)
        {
            Player = player;
            IsLocalPlayer = isLocalPlayer;
        }
    }
    
    /// <summary>
    /// Enum for input sources
    /// </summary>
    public enum InputSource
    {
        Keyboard,
        VirtualJoystick,
        Gamepad,
        Network
    }
    
    /// <summary>
    /// Enum for input modes
    /// </summary>
    public enum InputMode
    {
        Keyboard,
        VirtualJoystick,
        Both,
        Gamepad
    }
}