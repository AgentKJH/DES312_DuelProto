using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Sensor_HeroAttack : MonoBehaviour
{
    private HeroController controller;
    private Collider2D collider2D;
    List<Collider2D> overlappedList = new List<Collider2D>();

    private void Awake()
    {
        controller = GetComponent<HeroController>();
        collider2D = GetComponent<Collider2D>();
    }

    public void DoAttack()
    {
        Physics2D.OverlapCollider(collider2D, overlappedList);
        if (overlappedList.Count > 0)
        {
            foreach (Collider2D collider in overlappedList)
            {
                IDamageable damageable = collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.Damage(controller.damage);
                }
            }
            
        }


    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
    }

}
