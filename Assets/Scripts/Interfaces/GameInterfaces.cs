using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void Damage(int damage, float damageDirection);
}

public interface IMineable
{
    void Mine(float damageDirection);
}

public enum BreakableKind
{
    Standard,
    Rock
}

public interface IBreakable
{
    BreakableKind Kind { get; }
    void Hit(float damageDirection);
}
