using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    int health = 10;

    public void takeDamage(int damage)
    {
        health -= damage;
        print(health);
        if(health <= 0) die();
    }

    void die()
    {
        Destroy(gameObject);
    }

    // private void OnCollisionEnter2D(Collision2D other) {
    //     GetComponent<CapsuleCollider2D>().enabled = false;
    //     GetComponent<CapsuleCollider2D>().enabled = true;
    // }
}
