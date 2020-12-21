using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerPlayerController : MonoBehaviour
{

    public float maxSpeed;
    public float jumpSpeed;

    public float acceleration;
    public float slowFactor;

    Rigidbody2D body;

    sbyte currentDirection;
    bool isJumping;

    Vector2 cforce = Vector2.zero;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            currentDirection = -1;
        }else if (Input.GetKey(KeyCode.RightArrow))
        {
            currentDirection = 1;
        }
        else
        {
            currentDirection = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
        }

        cforce.x = currentDirection * acceleration;

    }

    private void FixedUpdate()
    {

        if (currentDirection != 0 && Mathf.Abs(body.velocity.x) < maxSpeed )
            body.AddForce(cforce);
        else if(currentDirection == 0)
        {
            //body.velocity = Vector2.zero;
            body.AddForce(new Vector2(-body.velocity.x * slowFactor, 0));
        }
            

        if (isJumping)
        {
            body.velocity = new Vector2(body.velocity.x, 0);
            body.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
            isJumping = false;
        }
    }
}
