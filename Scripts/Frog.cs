using UnityEngine;
using SteeringCalcs;
using Globals;

public class Frog : MonoBehaviour
{
    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // The arrival radius is set up to be dynamic, depending on how far away
    // the player right-clicks from the frog. See the logic in Update().
    public float ArrivePct;
    public float MinArriveRadius;
    public float MaxArriveRadius;
    private float _arriveRadius;

    // Turn this off to make it easier to see overshooting when seek is used
    // instead of arrive.
    public bool HideFlagOnceReached;

    // References to various objects in the scene that we want to be able to modify.
    private Transform _flag;
    private SpriteRenderer _flagSr;
    private Animator _animator;
    private Rigidbody2D _rb;

    // Stores the last position that the player right-clicked. Initially null.
    private Vector2? _lastClickPos;

    // Bubble params
    public GameObject Bubble;
    public float BubbleSpawnOffset;
    public float BubbleInitialVelocity;
    private Transform _bubbleParent;

    // To be used by GUI
    private int Health = 3;
    private int FliesCaught = 0;

    public int GetFliesCaught()
    {
        return FliesCaught;
    }

    public int GetHealth()
    {
        return Health;
    }

    public void LoseHealth()
    {
        Health--;
    }

    public void IncreaseFliesCaught()
    {
        FliesCaught++;
    }

    void Start()
    {
        // Initialise the various object references.
        _flag = GameObject.Find("Flag").transform;
        _flagSr = _flag.GetComponent<SpriteRenderer>();
        _flagSr.enabled = false;

        _animator = GetComponent<Animator>();

        _rb = GetComponent<Rigidbody2D>();

        _lastClickPos = null;
        _arriveRadius = MinArriveRadius;

        // set bubble parent
        _bubbleParent = GameObject.Find("Bubbles").transform;
    }

    void Update()
    {
        // Check whether the player right-clicked (mouse button #1).
        if (Input.GetMouseButtonDown(1))
        {
            _lastClickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Set the arrival radius dynamically.
            _arriveRadius = Mathf.Clamp(ArrivePct * ((Vector2)_lastClickPos - (Vector2)transform.position).magnitude, MinArriveRadius, MaxArriveRadius);

            _flag.position = (Vector2)_lastClickPos + new Vector2(0.55f, 0.55f);
            _flagSr.enabled = true;
        }

        // Check if we are shooting a bubble
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Find vector in direction frog is facing
            Vector2 facedDirection =  transform.right;
            facedDirection = Quaternion.AngleAxis(90, transform.forward) * facedDirection;


            // Add offset in facing direction to transform to spawn bubble from
            Vector2 bubbleSpawnPosition = new Vector2(transform.position.x, transform.position.y) + (facedDirection * BubbleSpawnOffset);

            // Spawn bubble
            GameObject newBubble = Instantiate(Bubble, bubbleSpawnPosition, Quaternion.identity, _bubbleParent);

            // Set bubble velocity
            newBubble.GetComponent<Rigidbody2D>().velocity = BubbleInitialVelocity * facedDirection + transform.GetComponent<Rigidbody2D>().velocity;
        }
    }

    void FixedUpdate()
    {
        Vector2 desiredVel = Vector2.zero;

        // If the last-clicked position is non-null, move there. Otherwise do nothing.
        if (_lastClickPos != null)
        {
            if (((Vector2)_lastClickPos - (Vector2)gameObject.transform.position).magnitude > Constants.TARGET_REACHED_TOLERANCE)
            {
                desiredVel = Steering.BasicArrive(gameObject.transform.position, (Vector2)_lastClickPos, _arriveRadius, MaxSpeed);
            }
            else
            {
                _lastClickPos = null;

                if (HideFlagOnceReached)
                {
                    _flagSr.enabled = false;
                }
            }
        }

        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (_rb.velocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            _animator.SetBool("Walking", true);
            transform.up = _rb.velocity;
        }
        else
        {
            _animator.SetBool("Walking", false);
        }
    }
}
