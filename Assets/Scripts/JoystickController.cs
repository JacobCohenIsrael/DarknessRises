using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class JoystickController : MonoBehaviour
{
    [SerializeField] PlayerMovement playerMovement;

    [SerializeField] PlayerShooting playerShooting;

    [SerializeField] private Joystick joystick;

    [SerializeField] private Joybutton joybotton;

    void Update()
    {

        //Vector3 dir = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        //playerMovement.gameObject.transform.rotation = Quaternion.LookRotation(dir);

        //if (joybotton.pressed)
        //{
        //    playershooting.attemptshoot();
        //}
    }
}
