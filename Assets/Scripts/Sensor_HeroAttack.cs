using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Sensor_HeroAttack : MonoBehaviour
{
    private HeroController controller;

    private void Awake()
    {
        controller = GetComponent<HeroController>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable damageable = collision.otherCollider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.Damage(controller.damage);
        }
    }

}
