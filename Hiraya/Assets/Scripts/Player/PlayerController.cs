using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hiraya
{
    /// <summary>Direction the character is looking. Down = toward the camera (front sprite), Up = away (back sprite).</summary>
    public enum Facing
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3
    }

    /// <summary>
    /// Four-way movement for the 2D sprite character in the 3D world: up / down / left / right only,
    /// no diagonals and no analog stick. Input comes from the keyboard (WASD or arrow keys) and the
    /// on-screen D-pad. If two directions are held, the one pressed last wins.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4f;

        public Facing Facing { get; private set; } = Facing.Down;
        public bool IsMoving { get; private set; }

        /// <summary>While true the character stands still and ignores the keyboard and the D-pad (used while the quiz is open).</summary>
        public bool InputLocked { get; set; }

        Rigidbody body;
        readonly List<Facing> pressOrder = new List<Facing>(4);
        readonly bool[] wasHeld = new bool[4];
        Vector3 moveDirection;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        void Update()
        {
            for (int i = 0; i < 4; i++)
            {
                var direction = (Facing)i;
                bool held = !InputLocked && (IsKeyboardHeld(direction) || DPadState.IsHeld(direction));
                if (held && !wasHeld[i])
                {
                    pressOrder.Remove(direction);
                    pressOrder.Add(direction);
                }
                else if (!held && wasHeld[i])
                {
                    pressOrder.Remove(direction);
                }
                wasHeld[i] = held;
            }

            IsMoving = pressOrder.Count > 0;
            if (IsMoving)
            {
                Facing = pressOrder[pressOrder.Count - 1];
                moveDirection = ToVector(Facing);
            }
            else
            {
                moveDirection = Vector3.zero;
            }
        }

        void FixedUpdate()
        {
            Vector3 velocity = body.linearVelocity;
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
            body.linearVelocity = velocity;
        }

        static Vector3 ToVector(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up: return Vector3.forward;
                case Facing.Down: return Vector3.back;
                case Facing.Left: return Vector3.left;
                default: return Vector3.right;
            }
        }

        static bool IsKeyboardHeld(Facing direction)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            switch (direction)
            {
                case Facing.Up: return keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
                case Facing.Down: return keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
                case Facing.Left: return keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                default: return keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            }
        }
    }
}
