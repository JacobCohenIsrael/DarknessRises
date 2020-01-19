using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] Projectile projectile;

    public int damagePerShot = 20;                  // The damage inflicted by each bullet.
    public float timeBetweenBullets = 0.15f;        // The time between each shot.
    public float range = 100f;                      // The distance the gun can fire.
    public StressReceiver stressReceiver;

    float timer;                                    // A timer to determine when to fire.
    int shootableMask;                              // A layer mask so the raycast only hits things on the shootable layer.
    ParticleSystem gunParticles;                    // Reference to the particle system.
    AudioSource gunAudio;                           // Reference to the audio source.

    [Tooltip("Stress the effect can inflict upon objects Range([0,1])")]
    public float cameraStress = 0.6f;

    void Awake()
    {
        // Create a layer mask for the Shootable layer.
        shootableMask = LayerMask.GetMask("Shootable");

        // Set up the references.
        gunParticles = GetComponent<ParticleSystem>();
        gunAudio = GetComponent<AudioSource>();
    }

    public void AttemptShoot()
    {
        timer += Time.deltaTime;

        // If the Fire1 button is being press and it's time to fire...
        if (Input.GetButton("Fire1") && timer >= timeBetweenBullets)
        {
            // ... shoot the gun.
            Shoot();
            stressReceiver.InduceStress(cameraStress);
        }
    }

    private void Shoot()
    {
        // Reset the timer.
        timer = 0f;
        Projectile bullet = Instantiate<Projectile>(projectile, transform.position, transform.rotation);
        // Play the gun shot audioclip.
        gunAudio.volume = UnityEngine.Random.Range(0.5f, 1.0f);
        gunAudio.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
        gunAudio.Play();

        // Stop the particles from playing if they were, then start the particles.
        gunParticles.Stop();
        gunParticles.Play();

        //// Enable the line renderer and set it's first position to be the end of the gun.
        //gunLine.enabled = true;
        //gunLine.SetPosition(0, transform.position);

        //// Set the shootRay so that it starts at the end of the gun and points forward from the barrel.
        //shootRay.origin = transform.position;
        //shootRay.direction = transform.forward;

        //// Perform the raycast against gameobjects on the shootable layer and if it hits something...
        //if (Physics.Raycast(shootRay, out shootHit, range, shootableMask))
        //{
        //    // Try and find an EnemyHealth script on the gameobject hit.
        //    EnemyHealth enemyHealth = shootHit.collider.GetComponent<EnemyHealth>();

        //    //If the EnemyHealth component exist...
        //    if (enemyHealth != null)
        //    {
        //        // ... the enemy should take damage.
        //        enemyHealth.TakeDamage(damagePerShot, shootHit.point);
        //    }

        //    // Set the second position of the line renderer to the point the raycast hit.
        //    gunLine.SetPosition(1, shootHit.point);
        //}
        //// If the raycast didn't hit anything on the shootable layer...
        //else
        //{
        //    // ... set the second position of the line renderer to the fullest extent of the gun's range.
        //    gunLine.SetPosition(1, shootRay.origin + shootRay.direction * range);
        //}
    }


    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}

