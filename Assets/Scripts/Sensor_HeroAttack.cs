using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class Sensor_HeroAttack : MonoBehaviour
{
    [SerializeField] HeroController controller;
    private Collider2D attackCollider2D;
    List<Collider2D> overlappedList = new List<Collider2D>();

    private void Awake()
    {
        //controller = GetComponent<HeroController>();
        attackCollider2D = GetComponent<Collider2D>();
    }

    public void DoAttack()
    {
        Physics2D.OverlapCollider(attackCollider2D, overlappedList);
        if (overlappedList.Count > 0)
        {
            //print("do attack listcCount: " + overlappedList.Count.ToString());
            foreach (Collider2D collider in overlappedList)
            {
                IDamageable damageable = collider.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    HeroController target = collider.GetComponent<HeroController>();
                    target.Damage(20f, transform.parent.gameObject);
                    print(transform.parent.gameObject.name);
                    //print("damage");
                }
                //print(collider.gameObject.name + " " + collider.GetType().Name);
            }
            
        }


    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
    }

}
