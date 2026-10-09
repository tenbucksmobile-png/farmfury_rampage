using FarmFuryRampage.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryRampage.Run
{
    /// <summary>
    /// One-finger relative drag (touch or mouse): the herd target moves by how far the finger moves, not to where it is.
    /// A/D or the arrow keys steer in the editor.
    /// </summary>
    public sealed class RunInputReader
    {
        readonly float trackWidth;
        readonly float dragSensitivity;

        bool dragging;
        float dragStartPointerX;
        float dragStartTarget;

        public float TargetX { get; private set; }

        public RunInputReader(float trackWidth, float dragSensitivity)
        {
            this.trackWidth = trackWidth;
            this.dragSensitivity = dragSensitivity;
        }

        public void Reset()
        {
            dragging = false;
            TargetX = 0f;
        }

        /// <summary>True on the frame a tap/click or Space/Enter happens (used for retry / next level).</summary>
        public static bool ConfirmPressed()
        {
            Pointer pointer = Pointer.current;
            Keyboard keyboard = Keyboard.current;
            return (pointer != null && pointer.press.wasPressedThisFrame)
                || (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame));
        }

        public void Update(RunSim sim)
        {
            float limit = sim.SteerLimit;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
                if (left != right)
                {
                    TargetX = left ? -limit : limit;
                    dragging = false;
                    return;
                }
                if (keyboard.aKey.wasReleasedThisFrame || keyboard.dKey.wasReleasedThisFrame
                    || keyboard.leftArrowKey.wasReleasedThisFrame || keyboard.rightArrowKey.wasReleasedThisFrame)
                    TargetX = sim.HerdX;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
            {
                dragging = false;
                return;
            }

            float pointerX = pointer.position.ReadValue().x;
            if (!dragging)
            {
                dragging = true;
                dragStartPointerX = pointerX;
                dragStartTarget = Mathf.Clamp(TargetX, -limit, limit);
            }

            float screenFraction = (pointerX - dragStartPointerX) / Mathf.Max(1, Screen.width);
            TargetX = Mathf.Clamp(dragStartTarget + screenFraction * trackWidth * dragSensitivity, -limit, limit);
        }
    }
}
