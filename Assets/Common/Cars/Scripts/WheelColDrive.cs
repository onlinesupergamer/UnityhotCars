using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelColDrive : MonoBehaviour
{
    Rigidbody rb;
    public WheelCollider[] wheelCol;
    public WheelCollider[] frontWheels;
    public WheelCollider[] rearWheels;
    public float enginePower = 4500;
    public float m_steerAngle = 45f;
    public float topSpeed = 20;
    public float gravity;
    public AnimationCurve slipCurve;
    //public AnimationCurve steeringCurve;


    bool b_isGrounded;


    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    
    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");


        foreach(WheelCollider wheel in wheelCol)
        {
            if(rb.velocity.magnitude < topSpeed)
            {

            wheel.motorTorque = enginePower * y;

            }

            b_isGrounded = wheel.isGrounded;

            
        }
        foreach(WheelCollider wheel in frontWheels)
        {
            wheel.steerAngle = m_steerAngle * x;
        }

        HandleGravity();

    }


    void HandleGravity()
    {
        if(b_isGrounded)
        {
            rb.AddForce(-rb.transform.up * gravity);
        }
        if(!b_isGrounded)
        {
            rb.AddForce(-Vector3.up * gravity);
        }

        
    }
}
