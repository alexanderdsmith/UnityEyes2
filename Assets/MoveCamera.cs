// Credit to damien_oconnell from http://forum.unity3d.com/threads/39513-Click-drag-camera-movement
// for using the mouse displacement for calculating the amount of camera movement and panning code

using UnityEngine;
using System.Collections;

public class MoveCamera : MonoBehaviour {
	
	public float sensitivity = 1f;
	
	void Start(){
	}
	
	void Update(){

        float deltaX = Input.GetAxis("Mouse X") * sensitivity;
        float deltaY = Input.GetAxis("Mouse Y") * sensitivity;

        // if (Input.GetMouseButton(0))
        // {
        //     transform.RotateAround(Vector3.zero, transform.localRotation * Vector3.up, deltaX);
        //     transform.RotateAround(Vector3.zero, transform.localRotation * Vector3.right, -deltaY);
        //     transform.LookAt(Vector3.zero, Vector3.up);
        // }

        Camera.main.orthographicSize += Input.mouseScrollDelta.y * -0.2f;
        Camera.main.orthographicSize = Mathf.Clamp((float)Camera.main.orthographicSize, 1.8f, 1.8f);
    }
}