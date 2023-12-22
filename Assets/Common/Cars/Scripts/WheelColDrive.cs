using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelColDrive : MonoBehaviour
{
    Rigidbody rb;
    public Transform liftPoint;
    public WheelCollider[] wheelCol;
    public WheelCollider[] frontWheels;
    public WheelCollider[] rearWheels;
    public float enginePower = 4500;
    public float m_steerAngle = 45f;
    public float topSpeed = 20;
    public float gravity;
    public PIDController hoverPID;
    public AnimationCurve slipCurve;
    


    bool b_isGrounded;
    bool b_isBoosting;


    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    
    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        b_isBoosting = Input.GetKey(KeyCode.LeftShift);


        

        RaycastHit hitInfo;

        if(Physics.Raycast(liftPoint.position, -transform.up, out hitInfo, 1f))
        {
            Vector3 hitNormal = hitInfo.normal.normalized;
            float floatPercent = hoverPID.Seek(1f, hitInfo.distance);
            float liftForce = 100;

            Vector3 force = hitNormal * liftForce * floatPercent;
            //force = Vector3.ProjectOnPlane(force, hitNormal);

            
            if(b_isBoosting)
            {

            //rb.AddForceAtPosition(force, liftPoint.position, ForceMode.Acceleration);
                
            rb.AddTorque(-transform.right * liftForce * floatPercent, ForceMode.Acceleration);
            }
                
            

        }

        //HandleGravity();

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
