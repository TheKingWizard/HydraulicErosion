using UnityEngine;

public class Viewer : MonoBehaviour
{
    private float theta = Mathf.PI + Mathf.PI / 2f;
    private float phi = Mathf.PI / 2f;
    private float r = 5.0f;
    private float x = 0;
    private float y = 0;
    private float z = 0;

    Vector3 dragOrigin;
    bool dragging = false;
    private readonly float dragSpeed = 0.05f;
    private readonly float moveSpeed = 0.01f;
    private readonly float scrollSpeed = 0.1f;

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
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
            x += (Input.GetKey(KeyCode.LeftArrow)) ? -moveSpeed : moveSpeed;

        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
        {
            y -= (Input.GetKey(KeyCode.UpArrow)) ? -moveSpeed : moveSpeed;


        }
        if (Input.mouseScrollDelta.y != 0.0f)
        {
            r -= Input.mouseScrollDelta.y * scrollSpeed;
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
