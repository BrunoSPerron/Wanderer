using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroMovement : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D rb;

    public float moveSpeed;
    public float gravity;
    public float jumpStrengh;


    void FixedUpdate()
    {
        Vector2 currentMovementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (currentMovementInput.magnitude > 0)
        {
            currentMovementInput.y /= 2;
            rb.AddForce(currentMovementInput * moveSpeed);
        }
    }

    private void SendAxesToAnimator(Vector2 axes)
    {
        if (axes.x > 0.08 || axes.x < -0.08 || axes.y > 0.08 || axes.y < -0.08)
        {
            animator.SetFloat("AxisX", axes.x);
            animator.SetFloat("AxisY", axes.y);
        }
    }
}
