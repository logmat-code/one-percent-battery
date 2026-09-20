using UnityEngine;

namespace OnePercentBattery.Player
{
    /// <summary>
    /// Single source of truth for "how far/long does a jump last" - simulated with the exact same
    /// integration PlayerController.Step uses, so the procedural spawner can reason about fairness
    /// (reachable gaps) without duplicating or drifting from the real jump math.
    /// </summary>
    public readonly struct JumpProfile
    {
        public readonly float MinAirTime;
        public readonly float MaxAirTime;

        public JumpProfile(float minAirTime, float maxAirTime)
        {
            MinAirTime = minAirTime;
            MaxAirTime = maxAirTime;
        }

        public float MinDistanceAt(float speed) => speed * MinAirTime;
        public float MaxDistanceAt(float speed) => speed * MaxAirTime;
    }

    public static class JumpPhysics
    {
        /// <summary>Simulates a tap (no hold) and a full-hold jump at a fixed timestep to find their airtimes.</summary>
        public static JumpProfile Simulate(float minJumpVelocity, float maxJumpVelocity, float maxHoldSeconds, float gravity, float groundY)
        {
            const float dt = 1f / 120f;
            const float maxSeconds = 5f;

            float minAirTime = SimulateOne(minJumpVelocity, maxJumpVelocity, maxHoldSeconds, gravity, groundY, holding: false, dt, maxSeconds);
            float maxAirTime = SimulateOne(minJumpVelocity, maxJumpVelocity, maxHoldSeconds, gravity, groundY, holding: true, dt, maxSeconds);
            return new JumpProfile(minAirTime, maxAirTime);
        }

        private static float SimulateOne(float minJumpVelocity, float maxJumpVelocity, float maxHoldSeconds, float gravity, float groundY, bool holding, float dt, float maxSeconds)
        {
            float velocity = minJumpVelocity;
            float y = groundY;
            float holdTimer = 0f;
            float airTime = 0f;
            float extraForce = (maxJumpVelocity - minJumpVelocity) / Mathf.Max(0.0001f, maxHoldSeconds);

            while (airTime < maxSeconds)
            {
                if (holding && holdTimer < maxHoldSeconds)
                {
                    velocity += extraForce * dt;
                    holdTimer += dt;
                }
                velocity += gravity * dt;
                y += velocity * dt;
                airTime += dt;

                if (y <= groundY)
                {
                    return airTime;
                }
            }
            return airTime;
        }
    }
}
