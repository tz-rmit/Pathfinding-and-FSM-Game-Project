using System.Collections.Generic;
using System.Drawing;
using System.Net;
using UnityEngine;

namespace SteeringCalcs
{
    [System.Serializable]
    public class AvoidanceParams
    {
        public bool Enable;
        public LayerMask ObstacleMask;
        // Note: As mentioned in the spec, you're free to add extra parameters to AvoidanceParams.
        public float angle;
        private float radius;


        public float GetRadius()
        {
            return radius;
        }
        public void SetRadius(float newRadius)
        {
            radius = newRadius;
        }
    }

    public class Steering
    {
        // PLEASE NOTE:
        // You do not need to edit any of the methods in the HelperMethods region.
        // In Visual Studio, you can collapse the HelperMethods region by clicking
        // the "-" to the left.
        #region HelperMethods

        // Helper method for rotating a vector by an angle (in degrees).
        public static Vector2 rotate(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;

            return new Vector2(
                v.x * Mathf.Cos(radians) - v.y * Mathf.Sin(radians),
                v.x * Mathf.Sin(radians) + v.y * Mathf.Cos(radians)
            );
        }

        // Converts a desired velocity into a steering force, as will
        // be explained in class (Week 2).
        public static Vector2 DesiredVelToForce(Vector2 desiredVel, Rigidbody2D rb, float accelTime, float maxAccel)
        {
            Vector2 accel = (desiredVel - rb.velocity) / accelTime;

            if (accel.magnitude > maxAccel)
            {
                accel = accel.normalized * maxAccel;
            }

            // F = ma
            return rb.mass * accel;
        }

        // In addition to separation, cohesion and alignment, the flies also have
        // an "anchor" force applied to them while flocking, to keep them within
        // the game arena. This is already implemented for you.
        public static Vector2 GetAnchor(Vector2 currentPos, Vector2 anchorDims)
        {
            Vector2 desiredVel = Vector2.zero;

            if (Mathf.Abs(currentPos.x) > anchorDims.x)
            {
                desiredVel -= new Vector2(currentPos.x, 0.0f);
            }

            if (Mathf.Abs(currentPos.y) > anchorDims.y)
            {
                desiredVel -= new Vector2(0.0f, currentPos.y);
            }

            return desiredVel;
        }

        // This "parent" seek method toggles between SeekAndAvoid and BasicSeek
        // depending on whether obstacle avoidance is enabled. Do not edit this.
        public static Vector2 Seek(Vector2 currentPos, Vector2 targetPos, float maxSpeed, AvoidanceParams avoidParams)
        {
            if (avoidParams.Enable)
            {
                return SeekAndAvoid(currentPos, targetPos, maxSpeed, avoidParams);
            }
            else
            {
                return BasicSeek(currentPos, targetPos, maxSpeed);
            }
        }

        // Seek is already implemented for you. Do not edit this method.
        public static Vector2 BasicSeek(Vector2 currentPos, Vector2 targetPos, float maxSpeed)
        {
            Vector2 offset = targetPos - currentPos;
            Vector2 desiredVel = offset.normalized * maxSpeed;
            return desiredVel;
        }

        // Do not edit this method. To implement obstacle avoidance, the only method
        // you need to edit is GetAvoidanceTarget.
        public static Vector2 SeekAndAvoid(Vector2 currentPos, Vector2 targetPos, float maxSpeed, AvoidanceParams avoidParams)
        {
            targetPos = GetAvoidanceTarget(currentPos, targetPos, avoidParams);

            return BasicSeek(currentPos, targetPos, maxSpeed);
        }

        // This "parent" arrive method toggles between ArriveAndAvoid and BasicArrive
        // depending on whether obstacle avoidance is enabled. Do not edit this.
        public static Vector2 Arrive(Vector2 currentPos, Vector2 targetPos, float radius, float maxSpeed, AvoidanceParams avoidParams)
        {
            if (avoidParams.Enable)
            {
                return ArriveAndAvoid(currentPos, targetPos, radius, maxSpeed, avoidParams);
            }
            else
            {
                return BasicArrive(currentPos, targetPos, radius, maxSpeed);
            }
        }

        // Do not edit this method. To implement obstacle avoidance, the only method
        // you need to edit is GetAvoidanceTarget.
        public static Vector2 ArriveAndAvoid(Vector2 currentPos, Vector2 targetPos, float radius, float maxSpeed, AvoidanceParams avoidParams)
        {
            targetPos = GetAvoidanceTarget(currentPos, targetPos, avoidParams);

            return BasicArrive(currentPos, targetPos, radius, maxSpeed);
        }

        #endregion

        // Below are all the methods that you *do* need to edit.
        #region MethodsToImplement

        // See the spec for a detailed explanation of how GetAvoidanceTarget is expected to work.
        // You're expected to use Physics2D.CircleCast (https://docs.unity3d.com/ScriptReference/Physics2D.CircleCast.html)
        // You'll also probably want to use the rotate() method declared above.
        public static Vector2 GetAvoidanceTarget(Vector2 currentPos, Vector2 targetPos, AvoidanceParams avoidParams)
        {
            Vector2 newTarget = targetPos;

            if (avoidParams.Enable)
            {
                // TODO: Add logic here for calculating the new target position.
                Vector2 direction = targetPos - currentPos;
                RaycastHit2D hit = Physics2D.CircleCast(currentPos, avoidParams.GetRadius(), direction, direction.magnitude, avoidParams.ObstacleMask);

                Debug.DrawLine(currentPos, targetPos, UnityEngine.Color.cyan);

                for (int i = 1; hit && i <= 360/avoidParams.angle; i++)
                {
                    // adjust the angle based on interation
                    float adjAngle = Mathf.Ceil(i / 2.0f) * avoidParams.angle;
                    // if iteration is even negate angle to check the right side
                    if (i % 2 == 0)
                    {
                        adjAngle *= -1;
                    }

                    // rotate the direction vector (which points from the hunter to the target)
                    Vector2 targetVec = rotate(direction, adjAngle);

                    // set new target 
                    newTarget = currentPos + targetVec;

                    // do circle cast
                    hit = Physics2D.CircleCast(currentPos, avoidParams.GetRadius(), targetVec, targetVec.magnitude, avoidParams.ObstacleMask);
                    UnityEngine.Color colour = UnityEngine.Color.white;
                    if (hit)
                    {
                        colour = UnityEngine.Color.red;
                    }
                    Debug.DrawLine(currentPos, newTarget, colour);

                }

            }

            return newTarget;
        }

        public static Vector2 BasicFlee(Vector2 currentPos, Vector2 predatorPos, float maxSpeed)
        {
            // TODO: Implement proper flee logic.
            // The method should return the character's *desired velocity*, not a steering force.
            return BasicSeek(currentPos, predatorPos, maxSpeed) * -1;
        }

        public static Vector2 BasicArrive(Vector2 currentPos, Vector2 targetPos, float radius, float maxSpeed)
        {
            // TODO: Replace the BasicSeek() call with proper arrive logic.
            // The method should return the character's *desired velocity*, not a steering force.

            //calc desired velocity
            Vector2 desiredVel = BasicSeek(currentPos, targetPos, maxSpeed);

            //calc dist
            float dist = (targetPos - currentPos).magnitude;

            //scale desired vel by d/r
            return desiredVel * (dist / radius);
        }

        public static Vector2 GetSeparation(Vector2 currentPos, List<Transform> neighbours, float maxSpeed)
        {
            // TODO: Replace with proper separation calculation.
            // The method should return the character's *desired velocity*, not a steering force.
            // Note that there are various online guides/tutorials that calculate this in
            // different ways, but you are expected to follow the approach shown in class (Week 2).

            Vector2 sRaw = Vector2.zero;

            foreach (Transform t in neighbours)
            {
                // calc dist
                float dist = Vector2.Distance(currentPos, new Vector2(t.position.x, t.position.y));

                sRaw += (currentPos - new Vector2(t.position.x, t.position.y)) / (dist * dist);
            }

            return sRaw.normalized * maxSpeed;
        }

        public static Vector2 GetCohesion(Vector2 currentPos, List<Transform> neighbours, float maxSpeed)
        {
            // TODO: Replace with proper cohesion calculation.
            // The method should return the character's *desired velocity*, not a steering force.
            // Note that there are various online guides/tutorials that calculate this in
            // different ways, but you are expected to follow the approach shown in class (Week 2).

            // calc sum of neighbour positions
            Vector2 sum = Vector2.zero;

            foreach (Transform t in neighbours)
            {
                sum += new Vector2(t.position.x, t.position.y);
            }

            // calc average position
            Vector2 avgPos = sum / neighbours.Count;

            Vector2 cRaw = avgPos - currentPos;

            return cRaw.normalized * maxSpeed;
        }

        public static Vector2 GetAlignment(List<Transform> neighbours, float maxSpeed)
        {
            // TODO: Replace with proper alignment calculation.
            // The method should return the character's *desired velocity*, not a steering force.
            // Note that there are various online guides/tutorials that calculate this in
            // different ways, but you are expected to follow the approach shown in class (Week 2).

            // calc the sum of neighbours velocities
            Vector2 sum = Vector2.zero;

            foreach (Transform t in neighbours)
            {
                sum += t.GetComponent<Rigidbody2D>().velocity;
            }

            // calc average vel
            Vector2 avgVel = sum / neighbours.Count;

            return avgVel.normalized * maxSpeed;
        }

        #endregion
    }
}
