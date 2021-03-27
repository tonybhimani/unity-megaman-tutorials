using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityFunctions : MonoBehaviour
{
    /*
     * Bezier Curve
     * https://en.wikipedia.org/wiki/Bézier_curve
     * 
     * Quadratic Bézier curve function from Ryan Zehm's YouTube video
     * https://youtu.be/Xwj8_z9OrFw
     */
    public static Vector3 CalculateQuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // return = (1 - t)2 P0 + 2(1 - t)tP1 + t2P2
        //             u              u         tt
        //            uu * P0 + 2 * u * t * P1 + tt * P2
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }

    public static Vector2 RotateByAngle(Vector2 vector, float angle)
    {
        Vector2 nv;
        float theta = angle * Mathf.Deg2Rad;
        float cs = Mathf.Cos(theta);
        float sn = Mathf.Sin(theta);
        nv.x = vector.x * cs - vector.y * sn;
        nv.y = vector.x * sn + vector.y * cs;
        return nv;
    }

    // Time functions for animated storytelling
    public static bool InTime(float runTime, float xTime)
    {
        return (runTime >= xTime && runTime < (xTime + Time.deltaTime));
    }

    public static bool InTime(float runTime, float startTime, float endTime)
    {
        return (runTime >= startTime && runTime < endTime);
    }

    public static bool UntilTime(float runTime, float startTime)
    {
        return (runTime < startTime);
    }

    public static bool OverTime(float runTime, float endTime)
    {
        return (runTime >= endTime);
    }

    // Source: YouTube video by Sebastian Lague
    // Kinematic Equations (E03: ball problem)
    // https://www.youtube.com/watch?v=IvT8hjy6q4o
    public static LaunchData CalculateLaunchData(Vector3 source, Vector3 target, float height, float gravity)
    {
        float displacementY = target.y - source.y;
        Vector3 displacementXZ = new Vector3(target.x - source.x, 0, target.z - source.z);
        float time = Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity);
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        Vector3 velocityXZ = displacementXZ / time;

        return new LaunchData(velocityXZ + velocityY * -Mathf.Sign(gravity), time);
    }

    public struct LaunchData
    {
        public readonly Vector3 initialVelocity;
        public readonly float timeToTarget;

        public LaunchData(Vector3 initialVelocity, float timeToTarget)
        {
            this.initialVelocity = initialVelocity;
            this.timeToTarget = timeToTarget;
        }
    }

}
