using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteeringCalcs;
using Globals;

public class Snake : MonoBehaviour
{
    // Obstacle avoidance parameters (see the assignment spec for an explanation).
    public AvoidanceParams AvoidParams;

    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // Use this as the arrival radius for all states where the steering behaviour == arrive.
    public float ArriveRadius;

    // Parameters controlling transitions in/out of the Aggro state.
    public float AggroRange;
    public float DeAggroRange;

    // The snake's initial position (the target for the PatrolHome and Harmless states).
    private Vector2 _home;

    // The patrol point (the target for the PatrolAway state).
    public Transform PatrolPoint;

    // Reference to the frog (the target for the Aggro state).
    public GameObject Frog;

    // State for FSM
    private SnakeState State;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;

    // Direction IDs used by the snake animator (don't edit these).
    private enum Direction : int
    {
        Up = 0,
        Left = 1,
        Down = 2,
        Right = 3
    }

    // Snake FSM states
    public enum SnakeState : int
    {
        PatrolAway = 0,
        PatrolHome = 1,
        Harmless = 2,
        Aggro = 3
    }

    // Snake FSM events
    public enum SnakeEvent : int
    {
        ReachedTarget = 0,
        FrogOutOfRange = 1,
        FrogInRange = 2,
        HitFrog = 3,
        HitBubble = 4
    }

    private void Awake()
    {
        // Put the objects radius (scaled by the object scale (assuming x and y scale are the same)) in the avoidParams object
        AvoidParams.SetRadius(gameObject.GetComponent<CircleCollider2D>().radius * gameObject.GetComponent<Transform>().localScale.x);
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        _home = transform.position;
    }

    void FixedUpdate()
    {
        // Move towards the target via seek.
        // Note: You will need to edit this so that the steering behaviour
        // depends on the FSM state (see the spec).


        // change the target based on state
        Vector2 targetPos = Vector2.zero;

        switch (State)
        {
            case SnakeState.PatrolAway:
                targetPos = PatrolPoint.position;
                break;

            case SnakeState.PatrolHome:
                targetPos = _home;
                break;

            case SnakeState.Harmless:
                targetPos = _home;
                break;

            case SnakeState.Aggro:
                targetPos = Frog.transform.position;
                break;

        }
        Vector2 desiredVel = Steering.Seek(transform.position, targetPos, MaxSpeed, AvoidParams);

        // Convert the desired velocity to a force, then apply it.
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);

        UpdateAppearance();

        // check if frog is in range
        Vector2 frogPos = Frog.transform.position;
        Vector2 currentPos = transform.position;
        if (Vector2.Distance(frogPos, currentPos) <= AggroRange)
        {
            HandleEvent(SnakeEvent.FrogInRange);
        }
        // check if frog is out of range
        if (Vector2.Distance(frogPos, currentPos) > DeAggroRange)
        {
            HandleEvent(SnakeEvent.FrogOutOfRange);
        }

        // check if snake has reached target
        if (Vector2.Distance(currentPos, targetPos) <= Constants.TARGET_REACHED_TOLERANCE)
        {
            HandleEvent(SnakeEvent.ReachedTarget);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Frog"))
        {
            // reduce health of frog
            if (State == SnakeState.Aggro)
            {
                collision.gameObject.GetComponent<Frog>().LoseHealth();
            }
            HandleEvent(SnakeEvent.HitFrog);
        }
        else if (collision.gameObject.CompareTag("Bubble"))
        {
            HandleEvent(SnakeEvent.HitBubble);
        }
    }

    void SetState(SnakeState newState)
    {
        State = newState;
    }

    public void HandleEvent(SnakeEvent e)
    {
        switch (State)
        {
            case SnakeState.PatrolAway:
                if (e == SnakeEvent.ReachedTarget)
                {
                    SetState(SnakeState.PatrolHome);
                }
                else if (e == SnakeEvent.FrogInRange)
                {
                    SetState(SnakeState.Aggro);
                }
                break;

            case SnakeState.PatrolHome:
                if (e == SnakeEvent.ReachedTarget)
                {
                    SetState(SnakeState.PatrolAway);
                }
                else if (e == SnakeEvent.FrogInRange)
                {
                    SetState(SnakeState.Aggro);
                }
                break;

            case SnakeState.Harmless:
                if (e == SnakeEvent.ReachedTarget)
                {
                    SetState(SnakeState.PatrolAway);
                }
                break;

            case SnakeState.Aggro:
                if (e == SnakeEvent.FrogOutOfRange)
                {
                    SetState(SnakeState.PatrolHome);
                }
                else if (e == SnakeEvent.HitFrog)
                {
                    SetState(SnakeState.Harmless);
                }
                else if (e == SnakeEvent.HitBubble)
                {
                    SetState(SnakeState.Harmless);
                }
                break;

            default:
                break;
        }
    }

    private void UpdateAppearance()
    {
        if (_rb.velocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            // Determine the bearing of the snake in degrees (between -180 and 180)
            float angle = Mathf.Atan2(_rb.velocity.y, _rb.velocity.x) * Mathf.Rad2Deg;

            if (angle > -135.0f && angle <= -45.0f) // Down
            {
                transform.up = new Vector2(0.0f, -1.0f);
                _animator.SetInteger("Direction", (int)Direction.Down);
            }
            else if (angle > -45.0f && angle <= 45.0f) // Right
            {
                transform.up = new Vector2(1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Right);
            }
            else if (angle > 45.0f && angle <= 135.0f) // Up
            {
                transform.up = new Vector2(0.0f, 1.0f);
                _animator.SetInteger("Direction", (int)Direction.Up);
            }
            else // Left
            {
                transform.up = new Vector2(-1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Left);
            }

            switch (State)
            {
                case SnakeState.Aggro:
                    _sr.color = new Color(0.5f, 0.0f, 0.0f);
                    break;

                case SnakeState.Harmless:
                    _sr.color = new Color(0.5f, 0.5f, 0.5f);
                    break;

                default:
                    _sr.color = new Color(1.0f, 1.0f, 1.0f);
                    break;
            }
        }
    }
}
