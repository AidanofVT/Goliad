using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TileMapAccelerator.Scripts
{

    public class TopDownPlayerController : MonoBehaviour
    {

        public float moveSpeed;

        Vector2 moveDir = Vector2.zero;
        Vector2 lastDir = new Vector2(-999,-999);

        Rigidbody2D body;

        public bool animated;

        PlayerAnimator animator;

        PlayerMoveState lastState = new PlayerMoveState(PlayerMoveState.Direction.S, PlayerMoveState.State.Idle);

        // Start is called before the first frame update
        void Start()
        {
            body = GetComponent<Rigidbody2D>();

            if (animated)
            {
                animator = GetComponent<PlayerAnimator>();
                animator.isPlaying = true;
            }
           
        }

        // Update is called once per frame
        void Update()
        {

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                moveDir.x = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                moveDir.x = 1;
            }
            else
            {
                moveDir.x = 0;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                moveDir.y = 1;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                moveDir.y = -1;
            }
            else
            {
                moveDir.y = 0;
            }

            if(lastDir != moveDir)
            {
                if(animated)
                    animator.SetFullState(lastState = GetCurrentState());

                lastDir = moveDir;
            }

        }

        public PlayerMoveState GetCurrentState()
        {
            if(moveDir.x == 1)
            {
                if (moveDir.y == 1) return new PlayerMoveState(PlayerMoveState.Direction.NE, PlayerMoveState.State.Walking);
                else if (moveDir.y == -1) return new PlayerMoveState(PlayerMoveState.Direction.SE, PlayerMoveState.State.Walking);
                else return new PlayerMoveState(PlayerMoveState.Direction.E, PlayerMoveState.State.Walking);
            }
            else if(moveDir.x == -1)
            {
                if (moveDir.y == 1) return new PlayerMoveState(PlayerMoveState.Direction.NW, PlayerMoveState.State.Walking);
                else if (moveDir.y == -1) return new PlayerMoveState(PlayerMoveState.Direction.SW, PlayerMoveState.State.Walking);
                else return new PlayerMoveState(PlayerMoveState.Direction.W, PlayerMoveState.State.Walking);
            }
            else
            {
                if (moveDir.y == 1) return new PlayerMoveState(PlayerMoveState.Direction.N, PlayerMoveState.State.Walking);
                else if (moveDir.y == -1) return new PlayerMoveState(PlayerMoveState.Direction.S, PlayerMoveState.State.Walking);
                else return new PlayerMoveState(lastState.dir, PlayerMoveState.State.Idle);
            }
        }

        void FixedUpdate()
        {
            body.velocity = moveDir * moveSpeed;
        }

    }

}
