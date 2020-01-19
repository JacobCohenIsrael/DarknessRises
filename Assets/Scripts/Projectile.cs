using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed;

    [SerializeField] private int damagePerShot = 20;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();

        //If the EnemyHealth component exist...
        if (enemyHealth != null)
        {
            // ... the enemy should take damage.
            enemyHealth.TakeDamage(damagePerShot, other.transform.position);
        }

        speed = 0;
        Destroy(gameObject);
    }
}
