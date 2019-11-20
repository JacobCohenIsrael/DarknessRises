using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    Transform player;               // Reference to the player's position.
    PlayerHealth playerHealth;      // Reference to the player's health.
    EnemyHealth enemyHealth;        // Reference to this enemy's health.
    NavMeshAgent nav;               // Reference to the nav mesh agent.
    private Rigidbody rb;

    void Awake()
    {
        // Set up the references.
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerHealth = player.GetComponent<PlayerHealth>();
        enemyHealth = GetComponent<EnemyHealth>();
        nav = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        nav.SetDestination(player.position);
    }

    void Update()
    {
        rb.velocity = Vector3.MoveTowards(rb.velocity, Vector3.zero, 0.25f);
        if (enemyHealth.currentHealth <= Mathf.Epsilon || playerHealth.currentHealth <= Mathf.Epsilon)
        {
            nav.enabled = false;

        }
    }
}
