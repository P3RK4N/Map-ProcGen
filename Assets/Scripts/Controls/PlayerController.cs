using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float collisionOffset = 0.05f;
    public ContactFilter2D moveFilter;
    public SwordAttack swAttack;

    Rigidbody2D rigidBody;
    Animator animator;
    SpriteRenderer spriteRenderer;

    Vector2 moveInput;
    List<RaycastHit2D> castCollisions = new List<RaycastHit2D>();
    bool canMove = true;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();  
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate() 
    {
        if(canMove)
        {   
            if(moveInput != Vector2.zero)
            {
                //Movement
                bool success = tryMove(moveInput);
                //Axis closer to vector direction has priority
                if(moveInput.x > moveInput.y)
                {
                    if(!success && moveInput.x != 0f) success = tryMove(new Vector2(moveInput.x, 0));
                    if(!success && moveInput.y != 0f) success = tryMove(new Vector2(0, moveInput.y));
                }
                else
                {
                    if(!success && moveInput.y != 0f) success = tryMove(new Vector2(0, moveInput.y));
                    if(!success && moveInput.x != 0f) success = tryMove(new Vector2(moveInput.x, 0));
                }
                animator.SetBool("isMoving", success);

                //Orientation
                if(moveInput.x > 0f) 
                {
                    spriteRenderer.flipX = false;
                }
                else
                {
                    spriteRenderer.flipX = true;
                }
            }
            else
            {
            animator.SetBool("isMoving", false);
            }
        }

    }

    bool tryMove(Vector2 direction)
    {
        int count = rigidBody.Cast(direction, moveFilter, castCollisions, moveSpeed * Time.fixedDeltaTime + collisionOffset);
        if(count == 0)
        {
            rigidBody.MovePosition(rigidBody.position + direction * moveSpeed * Time.fixedDeltaTime);
            return true;
        }
        else
        {
            return false;
        }
    }

    void swordAttack()
    {
        lockMovement();
        if(spriteRenderer.flipX == true)
        {
            swAttack.attackLeft();
        }
        else
        {
            swAttack.attackRight();
        }
    }

    void endAttack()
    {
        unlockMovement();
        swAttack.stopAttack();
    }

    void OnMove(InputValue movementValue)
    {
        moveInput = movementValue.Get<Vector2>();
    }

    void OnFire()
    {
        if(canMove) animator.SetTrigger("isAttacking");
    }

    void lockMovement()
    {
        canMove = false;
    }

    void unlockMovement()
    {
        canMove = true;
    }
}
