using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteeringCalcs;

public class Fly : MonoBehaviour
{
    public FlyState State;

    // Parameters controlling transitions in/out of the Flee state.
    public float StopFleeingRange;
    public float FrogStillFleeRange;
    public float FrogMovingFleeRange;
    public float FrogAlertSpeed;

    public float BubbleFleeRange;

    // Time taken to respawn after being eaten by the frog.
    public float RespawnTime;

    // Time since eaten by the frog (0 when alive).
    private float _timeDead;

    // Reference to the flocking parameters (attached to the "Flock"
    // game object in the "FlockingTest" and "FullGame" scenes).
    private FlockSettings _settings;

    // References to various objects in the scene that we want to be able to modify.
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Transform _frog;
    private Rigidbody2D _frogRb;

    private Transform _bubbles;

    // Fly FSM states
    public enum FlyState : int
    {
        Flocking = 0,
        Alone = 1,
        Fleeing = 2,
        Dead = 3
    }

    // Fly FSM events
    public enum FlyEvent : int
    {
        JoinedFlock = 0,
        LostFlock = 1,
        ScaredByFrog = 2,
        ScaredByBubble = 3,
        EscapedFrog = 4,
        CaughtByFrog = 5,
        RespawnTimeElapsed = 6
    }

    void Start()
    {
        _settings = transform.parent.GetComponent<FlockSettings>();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        // Have to be a bit careful setting _frog, since the frog doesn't exist in all scenes.
        GameObject frog = GameObject.Find("Frog");
        if (frog != null)
        {
            _frog = frog.transform;
            _frogRb = frog.GetComponent<Rigidbody2D>();
        }

        // Bubbles is an empty game object that exists at all times so no need for null check
        _bubbles = GameObject.Find("Bubbles").transform;

        _timeDead = 0.0f;
    }

    void Respawn()
    {
        State = FlyState.Flocking;

        // Respawn 20 units away from the origin at a random angle.
        // The flocking forces should automatically bring the fly back into the main arena.
        float randomAngle = Random.Range(-Mathf.PI, Mathf.PI);
        Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        transform.position = 20.0f * randomDirection;

        _timeDead = 0.0f;
    }

    // GetNeighbours() returns all flock members that:
    // - Are not dead.
    // - Are not equal to this flock member.
    // - Are within a distance of _settings.FlockRadius from this flock member.
    public List<Transform> GetNeighbours()
    {
        List<Transform> neighbours = new List<Transform>();

        foreach (Transform flockMember in transform.parent)
        {
            if (flockMember.GetComponent<Fly>().State != FlyState.Dead
                && flockMember != transform && (transform.position - flockMember.position).magnitude < _settings.FlockRadius)
            {
                neighbours.Add(flockMember);
            }
        }

        return neighbours;
    }

    void FixedUpdate()
    {
        List<Transform> neighbours = GetNeighbours();

        // Check if it's time to respawn.
        if (State == FlyState.Dead)
        {
            _timeDead += Time.fixedDeltaTime;
            if (_timeDead > RespawnTime)
            {
                HandleEvent(FlyEvent.RespawnTimeElapsed);
            }
        }

        // Throw LostFlock / JoinedFlock events.
        // Note: These are repeatedly spammed, but it's fine because HandleEvent
        // doesn't do anything if the state is already up-to-date.
        if (neighbours.Count == 0)
        {
            HandleEvent(FlyEvent.LostFlock);
        }
        else
        {
            HandleEvent(FlyEvent.JoinedFlock);
        }

        // Check if we've been scared by the frog, or no longer need to be scared.
        if (_frog != null)
        {
            if (_frogRb.velocity.magnitude >= FrogAlertSpeed && ((transform.position - _frog.transform.position).magnitude < FrogMovingFleeRange)
                || _frogRb.velocity.magnitude < FrogAlertSpeed && ((transform.position - _frog.transform.position).magnitude < FrogStillFleeRange))
            {
                HandleEvent(FlyEvent.ScaredByFrog);
            }

            if ((transform.position - _frog.transform.position).magnitude > StopFleeingRange && !BubbleInRange(StopFleeingRange))
            {
                HandleEvent(FlyEvent.EscapedFrog);
            }
        }

        // check if a bubble is in range
        // !!!Doing this every fixed update is not good! Idealy I would use a delegate (I think) but I think that's overkill!!!
        if (BubbleInRange(BubbleFleeRange))
        {
            HandleEvent(FlyEvent.ScaredByBubble);
        }

        Vector2 desiredVel = Vector2.zero;

        // Toggle steering behaviours based on the current state.
        switch (State)
        {
            case FlyState.Flocking:

                Vector2 desiredSep = _settings.SeparationWeight * Steering.GetSeparation(transform.position, neighbours, _settings.MaxSpeed);
                Vector2 desiredCoh = _settings.CohesionWeight * Steering.GetCohesion(transform.position, neighbours, _settings.MaxSpeed);
                Vector2 desiredAli = _settings.AlignmentWeight * Steering.GetAlignment(neighbours, _settings.MaxSpeed);
                Vector2 desiredAnch = _settings.AnchorWeight * Steering.GetAnchor(transform.position, _settings.AnchorDims);

                // Draw the forces for debugging purposes.
                Debug.DrawLine(transform.position, (Vector2)transform.position + desiredSep, Color.red);
                Debug.DrawLine(transform.position, (Vector2)transform.position + desiredCoh, Color.green);
                Debug.DrawLine(transform.position, (Vector2)transform.position + desiredAli, Color.blue);
                Debug.DrawLine(transform.position, (Vector2)transform.position + desiredAnch, Color.yellow);

                desiredVel = (desiredSep + desiredCoh + desiredAli + desiredAnch).normalized * _settings.MaxSpeed;
                break;

            case FlyState.Alone:

                Transform nearestFly = null;

                foreach (Transform flockMember in transform.parent)
                {
                    if (flockMember.GetComponent<Fly>().State != FlyState.Dead && flockMember != transform)
                    {
                        if (nearestFly == null || (transform.position - flockMember.position).magnitude < (transform.position - nearestFly.position).magnitude)
                        {
                            nearestFly = flockMember;
                        }
                    }
                }

                if (nearestFly != null)
                {
                    desiredVel = Steering.BasicSeek(transform.position, nearestFly.position, _settings.MaxSpeed);
                    Debug.DrawLine(transform.position, nearestFly.position, Color.yellow);
                }

                break;

            case FlyState.Fleeing:
                desiredVel = Steering.BasicFlee(transform.position, _frog.position, _settings.MaxSpeed);

                break;
        }

        // Convert the desired velocity to a force, then apply it.
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, _settings.AccelTime, _settings.MaxAccel);
        _rb.AddForce(steering);

        UpdateAppearance();
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag.Equals("Frog"))
        {
            if (State != FlyState.Dead)
            {
                collider.gameObject.GetComponent<Frog>().IncreaseFliesCaught();
            }
            HandleEvent(FlyEvent.CaughtByFrog);
        }
    }

    void SetState(FlyState newState)
    {
        if (newState != State)
        {
            // Respawn if the fly was previously dead.
            if (State == FlyState.Dead)
            {
                Respawn();
            }

            // Can uncomment this for debugging purposes.
            //Debug.Log(name + " switching state to " + newState.ToString());

           State = newState;
        }
    }

    // HandleEvent implements the transition logic of the FSM.
    public void HandleEvent(FlyEvent e)
    {
        if (e == FlyEvent.CaughtByFrog)
        {
            SetState(FlyState.Dead);
            return;
        }

        switch (State)
        {
            case FlyState.Flocking:
                if (e == FlyEvent.LostFlock)
                {
                    SetState(FlyState.Alone);
                }
                else if (e == FlyEvent.ScaredByFrog || e ==FlyEvent.ScaredByBubble)
                {
                    SetState(FlyState.Fleeing);
                }
                break;


            case FlyState.Alone:
                if (e == FlyEvent.JoinedFlock)
                {
                    SetState(FlyState.Flocking);
                }
                else if (e == FlyEvent.ScaredByFrog || e == FlyEvent.ScaredByBubble)
                {
                    SetState(FlyState.Fleeing);
                }
                break;

            case FlyState.Fleeing:
                if (e == FlyEvent.EscapedFrog)
                {
                    SetState(FlyState.Flocking);
                }
                break;

            case FlyState.Dead:
                if (e == FlyEvent.RespawnTimeElapsed)
                {
                    SetState(FlyState.Flocking);
                }
                break;

            default:
                break;
        }
    }

    private void UpdateAppearance()
    {
        _sr.flipX = _rb.velocity.x > 0;

        // Update color to provide a visual indication of the current state
        switch (State)
        {
            case FlyState.Flocking:
                _sr.enabled = true;
                _sr.color = new Color(1.0f, 1.0f, 1.0f);
                break;

            case FlyState.Alone:
                _sr.enabled = true;
                _sr.color = new Color(0.6f, 0.6f, 1.0f);
                break;

            case FlyState.Fleeing:
                _sr.enabled = true;
                _sr.color = new Color(1.0f, 1.0f, 0.0f);
                break;

            case FlyState.Dead:
                _sr.enabled = false;
                break;
        }
    }

    private bool BubbleInRange(float range)
    {
        bool ret = false;
        foreach (Transform bubble in _bubbles)
        {
            if ((transform.position - bubble.position).magnitude < range)
            {
                ret = true;
                break;
            }
        }
        return ret;
    }
}
