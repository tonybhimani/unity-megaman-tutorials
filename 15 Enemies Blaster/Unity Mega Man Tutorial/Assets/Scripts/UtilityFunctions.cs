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
}
