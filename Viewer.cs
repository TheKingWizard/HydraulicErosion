using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Viewer : MonoBehaviour
{
    private float theta = Mathf.PI + Mathf.PI / 2f;
    private float phi = Mathf.PI / 2f;
    private float r = 5.0f;
    private float x = 0;
    private float y = 0;
    private float z = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    Vector3 dragOrigin;
    bool dragging = false;
    float dragSpeed = 0.05f;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
            dragOrigin = Input.mousePosition;
        if (Input.GetMouseButton(0))
        {
            dragging = true;
        }
        else
        {
            dragging = false;
        }
        if (dragging)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            theta -= delta.x * dragSpeed;
            phi += delta.y * dragSpeed;
            dragOrigin = Input.mousePosition;
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            //theta += (Input.GetKey(KeyCode.LeftArrow)) ? -.05f : .05f;
            x += (Input.GetKey(KeyCode.LeftArrow)) ? -.05f : .05f;

        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
        {
            //phi += (Input.GetKey(KeyCode.UpArrow)) ? -.05f : .05f;
            y -= (Input.GetKey(KeyCode.UpArrow)) ? -.05f : .05f;


        }
        if (Input.mouseScrollDelta.y != 0.0f)
        {
            r -= Input.mouseScrollDelta.y * 0.1f;
            //z += Input.mouseScrollDelta.y * 0.1f;
        }
        SetCameraLocation();
    }

    private void SetCameraLocation()
    {
        transform.position = PolarToCartesian(r, theta, phi);
        Vector3 offset = new Vector3(x, y, z);
        transform.position += offset;
        transform.LookAt(offset);
    }

    private Vector3 PolarToCartesian(float r, float theta, float phi)
    {
        float x = r * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = r * Mathf.Cos(phi);
        float z = r * Mathf.Sin(phi) * Mathf.Sin(theta);

        return new Vector3(x, y, z);
    }
}
