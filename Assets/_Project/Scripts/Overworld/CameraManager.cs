using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Transform target;
    public float offsetY = 3;

    public float smoothX, smoothY;

    private void LateUpdate()
    {
        if (target == null)
        {
            // The old target was destroyed or unassigned during scene transition! 
            // Automatically find the active persisted player in the scene!
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player") ?? GameObject.Find("Player_Overworld");
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"[CameraManager] Reassigned camera target to {target.name}");
            }
            else
            {
                return; // Wait until player is available
            }
        }

        transform.position = new Vector3
           (Mathf.Lerp(transform.position.x, target.position.x, smoothX * Time.deltaTime), Mathf.Lerp(transform.position.y, target.position.y, smoothY * Time.deltaTime), transform.position.z);
    }
}
