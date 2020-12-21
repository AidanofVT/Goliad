using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TileMapAccelerator.Scripts.PlayerMoveState;

namespace TileMapAccelerator.Scripts
{

    

    public class PlayerAnimator : MonoBehaviour
    {

        //Must be in resources folder
        public Texture2D spritesheet;

        public string ResourcesPrefix;

        public SpriteRenderer renderer;

        Sprite[] sprites;

        PlayerMoveState currentState = new PlayerMoveState(Direction.S, State.Idle);

        public int framerate;

        public bool isPlaying;

        public string n_idle;
        public string n_walking;

        public string s_idle;
        public string s_walking;

        public string e_idle;
        public string e_walking;

        public string w_idle;
        public string w_walking;

        public string ne_idle;
        public string ne_walking;

        public string nw_idle;
        public string nw_walking;

        public string se_idle;
        public string se_walking;

        public string sw_idle;
        public string sw_walking;

        int lastrate;

        float animspeed;
        float timer;

        Dictionary<PlayerMoveState, Sprite[]> stateBasedSpriteCollection;
        string[] temp;
        int ti;
        int si;

        Sprite[] currentSprites;
        int currentFrame;

        public void SetDirection(Direction dir)
        {
            this.currentState.dir = dir;
        }

        public void SetAnimationState(State state)
        {
            this.currentState.state = state;
        }

        public void SetFullState(PlayerMoveState state)
        {
            this.currentState = state;
        }

        public Sprite[] stringToSpriteArray(string s, Sprite[] sprites)
        {
            Sprite[] newSprites;
            temp = s.Split(',');
            ti = 0;
            newSprites = new Sprite[temp.Length];

            for(int i=0; i < temp.Length; i++)
            {
                si = int.Parse(temp[i]);

                newSprites[ti++] = sprites[si];
            }

            return newSprites;

        }

        // Start is called before the first frame update
        void Start()
        {
            sprites = Resources.LoadAll<Sprite>(ResourcesPrefix + spritesheet.name);

            stateBasedSpriteCollection = new Dictionary<PlayerMoveState, Sprite[]>();

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.N, State.Idle), stringToSpriteArray(n_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.N, State.Walking), stringToSpriteArray(n_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.S, State.Idle), stringToSpriteArray(s_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.S, State.Walking), stringToSpriteArray(s_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.E, State.Idle), stringToSpriteArray(e_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.E, State.Walking), stringToSpriteArray(e_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.W, State.Idle), stringToSpriteArray(w_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.W, State.Walking), stringToSpriteArray(w_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.NE, State.Idle), stringToSpriteArray(ne_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.NE, State.Walking), stringToSpriteArray(ne_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.NW, State.Idle), stringToSpriteArray(nw_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.NW, State.Walking), stringToSpriteArray(nw_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.SE, State.Idle), stringToSpriteArray(se_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.SE, State.Walking), stringToSpriteArray(se_walking, sprites));

            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.SW, State.Idle), stringToSpriteArray(sw_idle, sprites));
            stateBasedSpriteCollection.Add(new PlayerMoveState(Direction.SW, State.Walking), stringToSpriteArray(sw_walking, sprites));

            animspeed = 1.0f / (lastrate = framerate);

        }

        // Update is called once per frame
        void Update()
        {
            //First we update the timer to make animation tick forward
            if((timer += Time.deltaTime) >= animspeed)
            {
                timer = 0;
                currentFrame++;
            }

            //Then we find the right sprite to use based on state and current frame
            currentSprites = stateBasedSpriteCollection[currentState];
            currentFrame = currentFrame % currentSprites.Length;//Using modulo here "wraps" overflowing values back to sprite range
            renderer.sprite = currentSprites[currentFrame];

            //Update new framerate set in editor
            if(lastrate != framerate)
            {
                animspeed = 1.0f / (lastrate = framerate);
            }

        }
    }

    public struct PlayerMoveState
    {
        public enum Direction { N, S, E, W, NE, NW, SE, SW }
        public enum State { Idle, Walking }

        public Direction dir;
        public State state;

        public PlayerMoveState(Direction dir, State state)
        {
            this.dir = dir;
            this.state = state;
        }

        public static bool operator ==(PlayerMoveState a, PlayerMoveState b)
        {
            return a.dir == b.dir && a.state == b.state;
        }

        public static bool operator !=(PlayerMoveState a, PlayerMoveState b)
        {
            return a.dir != b.dir || a.state != b.state;
        }

        public override bool Equals(object obj)
        {
            return this == (PlayerMoveState)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}


