using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    private float PopTime = 2.0f;
    private float ElapsedTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        ElapsedTime += Time.fixedDeltaTime;

        if (ElapsedTime > PopTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer.Equals(6))
        {
            Destroy(gameObject);
        }
        // snake collision check here to clean up the snake script
        else if (collision.gameObject.CompareTag("Snake"))
        {
            Destroy(gameObject);
        }
    }
}
