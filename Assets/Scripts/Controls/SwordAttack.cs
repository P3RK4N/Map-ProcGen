using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    Collider2D swordCollider;
    Transform tf;

    private void Start() 
    {
        swordCollider = GetComponent<Collider2D>();  
        tf = GetComponent<Transform>();  
    }

    public void attackLeft()
    {
        tf.localScale = new Vector3(-1, 1, 1);
        swordCollider.enabled = true;
    }

    public void attackRight()
    {
        tf.localScale = new Vector3(1, 1, 1);
        swordCollider.enabled = true;
    }

    public void stopAttack()
    {
        swordCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(other.gameObject.tag == "Enemy")
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            enemy.takeDamage(2);
        }
    }
}
