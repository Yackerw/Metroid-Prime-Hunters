using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMain : MonoBehaviour
{
    public enum PlayerType : int { local, network, bot }


    Rigidbody player;
    public Vector3 speed;
    public float xcam;
    public float ycam;
    Transform camera;
    public Transform camerapos;
    public CapsuleCollider myCollider;
    public PlayerType playerType;

    bool onGround;
    bool canJump;

    public Transform gunHolder;
    Vector2 gunRotation = new Vector2();
    float gunBob;
    Vector3 baseGunPos;
    public GameObject armCannon;

    public AudioSource gunSource;
    public AudioSource gunLoopSource;

    const float FLOOR_ANGLE = 55.0f;

    Gun currentWeapon;
    bool holdingFire;
    // Start is called before the first frame update
    void Start()
    {
        player=GetComponent<Rigidbody>();
        camera = Camera.main.transform; //.main gets the camera, .transform gets the transform of the camera
        baseGunPos = gunHolder.position;
        currentWeapon = new PowerBeam();
    }

	private void OnCollisionStay(Collision collision)
    {
        if (Vector3.Dot(collision.contacts[0].normal, Vector3.up) >= Mathf.Cos(FLOOR_ANGLE*Mathf.Deg2Rad) && speed.y <= 0)
		{
            onGround = true;
		}
	}

    public void PlayGunSound(AudioClip audio)
	{
        gunSource.PlayOneShot(audio);
	}

	// Update is called once per frame
	void Update()
    {
        if (playerType == PlayerType.local)
        {
            Vector2 moveInput;
            float mousex;
            float mousey;
            moveInput.x = Input.GetAxis("Horizontal");
            moveInput.y = Input.GetAxis("Vertical");
            // prevent strafing from being too fast
            if (moveInput.magnitude > 1)
            {
                moveInput.Normalize();
            }
            mousex = Input.GetAxis("Mouse X");
            mousey = Input.GetAxis("Mouse Y");
            if (moveInput.x > 0 || moveInput.x < 0) //this makes you move i think 
            {
                speed.x += moveInput.x * Time.deltaTime * 150;
                speed.x = Mathf.Clamp(speed.x, -Mathf.Abs(moveInput.x) * 10, Mathf.Abs(moveInput.x) * 10); //speed cap. with clamp magic 
            }
            if (moveInput.y > 0 || moveInput.y < 0)
            {
                speed.z += moveInput.y * Time.deltaTime * 150;
                speed.z = Mathf.Clamp(speed.z, -Mathf.Abs(moveInput.y) * 10, Mathf.Abs(moveInput.y) * 10);
            }
            if (moveInput.x == 0) //decelration stuff  
            {
                if (speed.x > 0 || speed.x < 0) //so you dont move backwards by not doing anything 
                {
                    speed.x -= 1 * Time.deltaTime * 150;
                    speed.x = Mathf.Max(speed.x, 0);
                }
                if (speed.x < 0) //so you dont move backwards by not doing anything 
                {
                    speed.x += 1 * Time.deltaTime * 150;
                    speed.x = Mathf.Min(speed.x, 0);
                }
            }
            if (moveInput.y == 0) //deceleration stuff 
            {
                if (speed.z > 0)
                {
                    speed.z -= 1 * Time.deltaTime * 150;
                    speed.z = Mathf.Max(speed.z, 0);
                }
                if (speed.z < 0) //so you dont move backwards by not doing anything 
                {
                    speed.z += 1 * Time.deltaTime * 150;
                    speed.z = Mathf.Min(speed.z, 0);
                }
            }
            xcam += mousex;
            ycam -= mousey;
            ycam = Mathf.Clamp(ycam, -80, 80);
            camera.rotation = Quaternion.Euler(ycam, xcam, 0);
            transform.rotation = Quaternion.Euler(0, xcam, 0);
            camera.position = camerapos.position;
            // rotate our gun based on how we rotate our camera
            gunRotation.x += mousex * Mathf.Clamp(0.5f / (Mathf.Sign(mousex) != Mathf.Sign(gunRotation.x) ? 0.5f : (gunRotation.x == 0 ? 0.1f : Mathf.Abs(gunRotation.x))), -60 * Time.deltaTime, 60 * Time.deltaTime);
            gunRotation.y -= mousey * Mathf.Clamp(0.5f / (Mathf.Sign(mousey) != Mathf.Sign(-gunRotation.y) ? 0.5f : ((gunRotation.y == 0 ? 0.1f : Mathf.Abs(gunRotation.y)))), -60 * Time.deltaTime, 60 * Time.deltaTime);
            gunRotation.x = Mathf.Clamp(gunRotation.x, -15.0f, 15.0f);
            gunRotation.y = Mathf.Clamp(gunRotation.y, -15.0f, 15.0f);
            gunHolder.rotation = Quaternion.Euler(gunRotation.y, gunRotation.x, 0);
            // bob up and down
            if (onGround)
            {
                float gunBobber = new Vector3(speed.x, speed.z, 0).magnitude * Time.deltaTime * 0.5f;
                gunBob += gunBobber;
                gunBob %= Mathf.PI * 2;
                gunHolder.position = baseGunPos - new Vector3(0, Mathf.Sin(gunBob) * 0.03f, 0);
            }
            // handle our weapon
            if (Input.GetButtonDown("Fire1"))
            {
                currentWeapon.BeginFire(this);
            }
            if (Input.GetButton("Fire1"))
            {
                currentWeapon.HoldFire(this);
                holdingFire = true;
            }
            if (!Input.GetButton("Fire1") && holdingFire)
            {
                currentWeapon.ReleaseFire(this);
                holdingFire = false;
            }
        }

        // handle gravity
        if (onGround)
		{
            speed.y = 0;
            canJump = true;
		} else
		{
            speed.y = Mathf.Max(speed.y -= 14.0f * Time.deltaTime, -20.0f);
		}

        if (canJump && Input.GetButtonDown("Jump") && playerType == PlayerType.local)
        {
            speed.y = 9.0f;
            canJump = false;
        }
        currentWeapon.Update(this);

        onGround = false;

        player.velocity = Quaternion.Euler(0, xcam, 0)*speed;
    }
}
