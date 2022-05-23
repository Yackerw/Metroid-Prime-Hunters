using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerBeamBullet : Bullet
{
    Rigidbody rb;

    public override byte[] GetSpawnVariables()
	{
        return null;
	}

	public override string GetAssetName()
	{
		return "Bullets/PowerBeam/PowerBeam";
	}

	public override void SetSpawnVariables(byte[] input)
	{
		
	}
	private void OnCollisionEnter(Collision collision)
	{
        // kill ourselves, spawn "bullet hit" graphic
        Destroy(gameObject);
        GameObject hit = Instantiate((GameObject)Resources.Load("Objects/Bullets/PowerBeam/PowerBeamHit"));
        hit.transform.rotation = Quaternion.FromToRotation(Vector3.forward, collision.contacts[0].normal);
        hit.transform.position = collision.contacts[0].point + hit.transform.rotation * new Vector3(0, 0, 0.01f);
        Destroy(hit, 4.0f);
		NetClose();
	}
	public override float GetDamage()
	{
        return 16.0f;
	}
	// Start is called before the first frame update
	void Start()
    {
        rb = GetComponent<Rigidbody>();
		NetStart();
    }

    // Update is called once per frame
    void Update()
    {
        rb.velocity = transform.rotation * new Vector3(0, 0, 75.0f);
    }
}
