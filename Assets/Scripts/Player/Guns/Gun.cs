using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun
{
    public abstract void BeginFire(PlayerMain player);
    public abstract void HoldFire(PlayerMain player);
    public abstract void ReleaseFire(PlayerMain player);
    public abstract void SwitchWeapon(PlayerMain player);
    public abstract void FireShot(PlayerMain player, int type);
    public abstract void Update(PlayerMain player);
}
