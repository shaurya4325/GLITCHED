using Unity.Cinemachine;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Cinemachine extension: rotates the camera a few degrees toward the movement direction (yaw) and pitches
    /// slightly when jumping/falling. Because it runs before the Position Composer, the camera orbits the player,
    /// so building sides visibly turn: the main source of the "3D perspective is changing" feeling.
    /// </summary>
    [AddComponentMenu("Glitched/Camera/Yaw Sway")]
    public class YawSway : CinemachineExtension
    {
        [Tooltip("Max yaw in degrees (overridden by the graphics tier and the Camera Sway setting).")]
        public float maxYaw = 3f;
        public float maxPitch = 2f;
        public float damping = 0.45f;
        public float maxSpeedForFullSway = 7f;

        [HideInInspector] public float velocityX;
        [HideInInspector] public float velocityY;
        [HideInInspector] public float yawOffset;      // from camera zones
        [HideInInspector] public float pitchOffset;
        [HideInInspector] public bool frozen;

        float yaw, yawVel, pitch, pitchVel;

        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam, ref CameraState curState, float deltaTime)
        {
            float swayDeg = Services.Graphics != null ? Services.Graphics.SwayDegrees : maxYaw;
            float targetYaw = frozen ? 0 : Mathf.Clamp(velocityX / maxSpeedForFullSway, -1f, 1f) * swayDeg;
            float targetPitch = frozen ? 0 : Mathf.Clamp(-velocityY / 14f, -1f, 1f) * maxPitch * Mathf.Clamp01(swayDeg / 3f + 0.3f);
            float dt = deltaTime < 0 ? 0 : deltaTime;
            if (deltaTime < 0) { yaw = targetYaw; pitch = targetPitch; }
            else
            {
                yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVel, damping, Mathf.Infinity, dt);
                pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVel, damping * 1.4f, Mathf.Infinity, dt);
            }
            curState.RawOrientation = Quaternion.Euler(pitch + pitchOffset, yaw + yawOffset, 0f);
        }
    }
}
