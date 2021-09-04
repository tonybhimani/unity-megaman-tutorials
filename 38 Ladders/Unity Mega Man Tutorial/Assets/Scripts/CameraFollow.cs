using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float timeOffset;
    public Vector3 offsetPos;

    public Vector3 boundsMin;
    public Vector3 boundsMax;

    private void LateUpdate()
    {
        if (player != null)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = player.position;

            targetPos.x += offsetPos.x;
            targetPos.y += offsetPos.y;
            targetPos.z = transform.position.z;

            targetPos.x = Mathf.Clamp(targetPos.x, boundsMin.x, boundsMax.x);
            targetPos.y = Mathf.Clamp(targetPos.y, boundsMin.y, boundsMax.y);

            float t = 1f - Mathf.Pow(1f - timeOffset, Time.deltaTime * 30);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
        }
    }
}
