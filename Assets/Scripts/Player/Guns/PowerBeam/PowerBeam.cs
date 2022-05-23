using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerBeam : Gun
{
	float fireTimer = 0;
	bool charging;
	float timeSinceRelease;

	public override void FireShot(PlayerMain player, int type)
	{
		// spawn bullet at around camera height
		Vector3 bulletSpawnPos = player.camerapos.position;
		Quaternion bulletRot = Quaternion.Euler(player.ycam, player.xcam, 0);
		bulletSpawnPos += Quaternion.Euler(player.ycam, player.xcam, 0) * new Vector3(0.1f, -0.1f, 0);
		// raycast to find the direction to fire the bullet at
		RaycastHit rhit;
		bool hit = Physics.Raycast(player.camerapos.position, bulletRot * Vector3.forward, out rhit, 1000.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
		if (hit)
		{
			bulletRot = Quaternion.FromToRotation(Vector3.forward, (rhit.point - player.camerapos.position).normalized);
		}
		// spawn the bullet itself now
		GameObject newBullet = GameObject.Instantiate((GameObject)Resources.Load("Objects/Bullets/PowerBeam/PowerBeam"));
		newBullet.transform.position = bulletSpawnPos;
		newBullet.transform.rotation = bulletRot;
		// play sfx
		player.PlayGunSound((AudioClip)Resources.Load("SFX/Player/Guns/PowerBeam/shoot"));
		player.armCannon.GetComponent<Animator>().PlayInFixedTime("Shoot", 0, 0);
	}
	public override void BeginFire(PlayerMain player)
	{
		fireTimer = 0;
		// fire a shot initially
		if (timeSinceRelease <= 0)
		{
			FireShot(player, 0);
		} else
		{
			// keep firerate consistent
			fireTimer = 0.2f - timeSinceRelease;
		}
		charging = false;
	}

	public override void HoldFire(PlayerMain player)
	{
		float tmpTimer = fireTimer;
		fireTimer += Time.deltaTime;
		int fireAmnt = (int)(fireTimer / 0.2f);
		int tmpAmnt = (int)(tmpTimer / 0.2f);
		// fire once every 0.3 second, after firing about 4 shots start charging
		if (fireAmnt > tmpAmnt && !charging)
		{
			FireShot(player, 0);
		}
		if (fireAmnt >= 2)
		{
			charging = true;
		}
	}

	public override void ReleaseFire(PlayerMain player)
	{
		if (timeSinceRelease <= 0)
		{
			if (fireTimer <= 0.8f)
			{
				timeSinceRelease = 0.2f - (fireTimer % 0.2f);
			}
			else
			{
				timeSinceRelease = 0.2f;
			}
		}
	}

	public override void SwitchWeapon(PlayerMain player)
	{
		throw new System.NotImplementedException();
	}
	public override void Update(PlayerMain player)
	{
		if (timeSinceRelease > 0)
		{
			timeSinceRelease -= Time.deltaTime;
		}
	}
}
