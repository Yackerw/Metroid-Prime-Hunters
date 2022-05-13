using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMain : MonoBehaviour
{
    Rigidbody player;
    Vector3 speed;
    float xcam;
    float ycam;
    Transform camera;
    public Transform camerapos;
    // Start is called before the first frame update
    void Start()
    {
        player=GetComponent<Rigidbody>();
        camera = Camera.main.transform; //.main gets the camera, .transform gets the transform of the camera
    }

    // Update is called once per frame
    void Update()
    {
        float xaxis;
        float zaxis;
        float mousex;
        float mousey;
        xaxis=Input.GetAxis("Horizontal");
        zaxis=Input.GetAxis("Vertical");
        mousex = Input.GetAxis("Mouse X");
        mousey = Input.GetAxis("Mouse Y");
        if(xaxis > 0 || xaxis < 0) //this makes you move i think 
        {
            speed.x += xaxis*Time.deltaTime*50;
        }
        if(zaxis > 0 || zaxis < 0)
        {
            speed.z += zaxis*Time.deltaTime*50;
        }
        if (xaxis == 0) //decelration stuff  
        {
            if(speed.x > 0 || speed.x < 0) //so you dont move backwards by not doing anything 
            {
                speed.x -= 1*Time.deltaTime*50;
                speed.x = Mathf.Max(speed.x, 0);
            }
            if (speed.x < 0) //so you dont move backwards by not doing anything 
            {
                speed.x += 1*Time.deltaTime*50;
                speed.x = Mathf.Min(speed.x, 0);
            }
        }
        if (zaxis == 0) //deceleration stuff 
        {
            if (speed.z > 0)
            {
                speed.z -= 1*Time.deltaTime*50;
                speed.z = Mathf.Max(speed.x, 0);
            }
            if (speed.z < 0) //so you dont move backwards by not doing anything 
            {
                speed.z += 1*Time.deltaTime*50;
                speed.z = Mathf.Min(speed.x, 0);
            }
        }
        xcam += mousex;
        ycam -= mousey;
        camera.rotation = Quaternion.Euler(ycam, xcam, 0);
        camera.position = camerapos.position;
        speed.x = Mathf.Clamp(speed.x, -15, 15); //speed cap. with clamp magic 
        speed.z = Mathf.Clamp(speed.z, -15, 15);
        player.velocity = Quaternion.Euler(0, xcam, 0)*speed;
    }
}
