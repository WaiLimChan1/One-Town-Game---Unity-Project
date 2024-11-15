using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCamera : MonoBehaviour
{
    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;
    [SerializeField] private float zoomSpeed;
    [SerializeField] private float speed;
    public bool locked;

    public void Awake()
    {
        locked = true;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = new Vector3(position.x, position.y, -10f);
    }

    public void TakeInput()
    {
        //Unlock and move camera
        Vector3 changeVector = new Vector3(0,0);
        if (Input.GetKey(KeyCode.UpArrow) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W))) 
            changeVector += new Vector3(0, 1);
        if (Input.GetKey(KeyCode.DownArrow) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.S)))
            changeVector += new Vector3(0, -1);
        if (Input.GetKey(KeyCode.LeftArrow) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.A)))
            changeVector += new Vector3(-1, 0);
        if (Input.GetKey(KeyCode.RightArrow) || (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.D)))
            changeVector += new Vector3(1, 0);
        if (changeVector.magnitude > 0) locked = false;
        transform.position += changeVector * speed * Time.deltaTime;

        //Zoom In/Zoom Out
        float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scrollWheelInput * zoomSpeed, minZoom, maxZoom);

        //Lock
        if (Input.GetKeyDown(KeyCode.Q)) locked = true;
        if (Input.GetKeyDown(KeyCode.E)) locked = false;

        //Toggle Camera Locked
        if (Input.GetKeyDown(KeyCode.Space)) this.locked = !locked;
    }
}
