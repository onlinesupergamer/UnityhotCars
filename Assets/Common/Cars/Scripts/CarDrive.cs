using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;


public class WheelCast : MonoBehaviour
{
    /// <summary>
    ///                 
    /// 
    /// 
    /// 
    /// 
    /// 
    ///                 
    ///                 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    /// </summary>
    



    [Header("Main Vars")]

    public Transform[] rays;
    public Transform[] frontWheels;
    public Transform[] rearWheels;
    public Transform FLWheelPivot;
    public Transform FRWheelPivot;
    public Transform FLPosition;
    public Transform FRPosition;
    public Transform LiftPoint;
    public Transform DownPoint;

    public PIDController hoverPID;


    
    public Rigidbody rb;
    public Transform chassisModel;
    public float suspensionRestDistance;
    public float springStrength;
    public float springDamper;
    public float rayLength;
    public float tireGripFactor;
    public AnimationCurve driveTorque;
    public AnimationCurve slipCurve;
    public float topSpeed;
    public LayerMask groundMask;


    [Header("Debug")]

    [SerializeField]
    float Speed;
    float slip;
    [SerializeField]
    bool b_isSlipping;
    [SerializeField]
    bool b_isDrifting;
    [SerializeField]
    float slipAmount;
    [SerializeField]
    float inAirTimer = 0.0f;
    

    public float frontOffset = 0.235f;
    public float rearOffset = 0.235f;
    float normSpeed;


    public Transform FLWheel;
    public Transform FRWheel;
    public Transform RLWheel;
    public Transform RRWheel;

    RaycastHit[] m_hit = new RaycastHit[4];
    RaycastHit[] groundHit = new RaycastHit[4];
    bool[] b_hasHit = new bool[4];
    int _i;

    [SerializeField]
    float[] offset = new float[4];

    



    [SerializeField]
    bool b_Isgrounded;
    
    bool b_isBoosting;
    
    


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        
        
    }

    
    void FixedUpdate()
    {
        float y = Input.GetAxisRaw("Vertical");
        float x = Input.GetAxisRaw("Horizontal");

        HandleGravity();


        RaycastHit hitInfo;

        if(Physics.Raycast(LiftPoint.position, -transform.up, out hitInfo, 1.5f))
        {
            Vector3 hitNormal = hitInfo.normal.normalized;
            float floatPercent = hoverPID.Seek(1f, hitInfo.distance);
            float liftForce = 100;

            Vector3 force = hitNormal * liftForce * floatPercent;
            //force = Vector3.ProjectOnPlane(force, hitNormal);

            if(b_isBoosting)
            {
                //rb.AddForceAtPosition(force, LiftPoint.position, ForceMode.Acceleration);
                
                rb.AddTorque(-transform.right * liftForce * floatPercent, ForceMode.Acceleration);
                
            }

        }

        
        


        for(int i = 0; i < rays.Length; i++)
        {
            
            
            

            if(Physics.Raycast(rays[i].transform.position, -rays[i].transform.up, out m_hit[i], rayLength, groundMask))
            {
                ///
                ///                                 
                ///                                 
                ///



                //Suspension
                
                Vector3 springDir = transform.up;
                Vector3 tireWorldVel = rb.GetPointVelocity(rays[i].position);
                offset[i] = suspensionRestDistance - m_hit[i].distance;
                float Vel = Vector3.Dot(springDir, tireWorldVel);
                float Force = (offset[i] * springStrength) - (Vel * springDamper);
                _i = i;

                
                groundHit[i] = m_hit[i]; //Potentially unneeded


                if(i == 0 || i == 1)
                {
                    if(!b_isBoosting)
                    {
                        rb.AddForceAtPosition(springDir * Force, rays[i].position);
                    }
                }
                
                if(i == 2 || i == 3)
                {
                   if(!b_isBoosting)
                    {
                        
                        rb.AddForceAtPosition(springDir * Force, rays[i].position);
                    }

                    else
                    {
                        
                        rb.AddForceAtPosition(springDir * Force, rays[i].position);
                        
                        
                        

                    }
                }

                
                
                b_hasHit[i] = true;
 
            
            }

            else

            {
                
                b_hasHit[i] = false;
                
            }



            if(b_hasHit[0] || b_hasHit[1] || b_hasHit[2] || b_hasHit[3])
            {
                b_Isgrounded = true;
            }

            else
            {
                b_Isgrounded = false;
            }



            Debug.DrawRay(rays[i].position, -transform.up * rayLength);
            

 
            
        }

        
        Vector3 accelDir = rays[_i].forward;

        if(y != 0.0f && rb.velocity.magnitude <= topSpeed && b_Isgrounded)
        {
            float carSpeed = Vector3.Dot(transform.forward, rb.velocity);
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);
            float torque = driveTorque.Evaluate(normalizedSpeed) * y * 1250f;
            normSpeed = normalizedSpeed;
            Vector3 projectedSpeed = Vector3.ProjectOnPlane(transform.forward, hitInfo.normal);

            Debug.DrawRay(transform.position, projectedSpeed * 2f, Color.green);

 
            rb.AddForce(projectedSpeed * torque);
                
        }



        foreach(Transform wheel in frontWheels)
        {
            float m_carSpeed = Vector3.Dot(transform.forward, rb.velocity);
            float m_normalizedSpeed = Mathf.Clamp01(Mathf.Abs(m_carSpeed) / topSpeed);
            float m_slipAngle = slipCurve.Evaluate(m_normalizedSpeed);
            slip = m_slipAngle;
            

            Quaternion target = Quaternion.Euler(0, 35 * x * slip, 0);
            

        }


        if(!b_Isgrounded)
        {   
            inAirTimer += Time.fixedDeltaTime;
        }

        if(x == 0f && b_Isgrounded)
        {

            
            //Lower Time means slower rotation correction

            //This Method is inefficient as fuck
            //But it works so I won't mess with it yet

            if(!b_isSlipping)
            {
                float interpTime = 0.25f; 
                rb.angularVelocity = new Vector3(rb.angularVelocity.x, Mathf.Lerp(rb.angularVelocity.y, 0, interpTime), rb.angularVelocity.z);
                
            }  

            else
            {
                float interpTime = 0.15f; 
                

                rb.angularVelocity = new Vector3(rb.angularVelocity.x, Mathf.Lerp(rb.angularVelocity.y, 0, interpTime), rb.angularVelocity.z);

                
            } 

        }



        if(b_Isgrounded)
        {

            inAirTimer = 0.0f;
            rb.AddForce(-transform.right * (Vector3.Dot(rb.velocity, transform.right) / Time.fixedDeltaTime / 16), ForceMode.Acceleration);
            rb.AddTorque(rb.transform.up * 400 * x * slip, ForceMode.Force);

    
        }

        Speed = rb.velocity.magnitude;

        if(Mathf.Abs(Vector3.Dot(rb.velocity, rb.transform.right)) >= 6f)
        {
            b_isSlipping = true;
        }
        else
        {
             b_isSlipping = false;
             
        }
        slipAmount = Mathf.Abs(Vector3.Dot(rb.velocity, rb.transform.right));
        

       
        HandleBoost();
        HandleWheelAnimations();
        

    }

    void HandleBrake()
    {
        bool b_isBraking = Input.GetButton("Jump");

        if(b_isBraking && b_Isgrounded)
        {
            //This may need to be inversed
            float newSpeed = Mathf.Lerp(rb.velocity.z, rb.velocity.z / 4f, 1f);
            Vector3 newVel = new Vector3(rb.velocity.x, rb.velocity.y, newSpeed);
            
            rb.velocity = newVel;
        }

    }

    void HandleGravity()
    {
        float groundedGravity = 1200;
        float airGravity = 2000;

        if(b_hasHit[0] || b_hasHit[1] || b_hasHit[2] || b_hasHit[3])
        {
            rb.AddForce(m_hit[_i].normal * -groundedGravity);

        }
        else 
        {
            rb.AddForce(Vector3.up * -airGravity);
        }
    }


    void HandleWheelAnimations()
    {

        Vector3[] tmpbffr = new Vector3[4];


        for(int i = 0; i <= rays.Length; i++)
        {
  

            if(i == 0)
            {
                tmpbffr[i] = rays[i].localPosition;
                    

                Vector3 groundedLoc = Vector3.zero;
                Vector3 inAirLoc = Vector3.zero;
                Vector3 boostingLoc = Vector3.zero;

                Vector3 Testvec = Vector3.zero;
                


                if(b_hasHit[i])
                {
                    
                    tmpbffr[i].z = 0.9619961f;

                    groundedLoc.x = 0f;
                    groundedLoc.y = -m_hit[i].distance + frontOffset;
                    groundedLoc.z = 0f;

                    Testvec.y = -m_hit[i].distance;

                    FLWheelPivot.localPosition = groundedLoc;

                    
                }

                if(!b_hasHit[i])
                {
                    inAirLoc.x = 0f;
                    inAirLoc.y = -rayLength + frontOffset;
                    inAirLoc.z = 0f;

                    FLWheelPivot.localPosition = inAirLoc;

                }


            }

            
            if(i == 1)
            {
                tmpbffr[i] = rays[i].localPosition;
                    

                Vector3 groundedLoc = Vector3.zero;
                Vector3 inAirLoc = Vector3.zero;
                Vector3 boostingLoc = Vector3.zero;

                Vector3 Testvec = Vector3.zero;
                


                if(b_hasHit[i] && !b_isBoosting)
                {
                    
                    tmpbffr[i].z = 0.9619961f;

                    groundedLoc.x = 0f;
                    groundedLoc.y = -m_hit[i].distance + frontOffset;
                    groundedLoc.z = 0f;

                    Testvec.y = -m_hit[i].distance;

                    FRWheelPivot.localPosition = groundedLoc;

                    
                }

                if(!b_hasHit[i])
                {
                    inAirLoc.x = 0f;
                    inAirLoc.y = -rayLength + frontOffset;
                    inAirLoc.z = 0f;

                    FRWheelPivot.localPosition = inAirLoc;

                }


            }

            if(i == 2)
            {
                tmpbffr[i] = rays[i].localPosition;
                    

                Vector3 groundedLoc = Vector3.zero;
                Vector3 inAirLoc = Vector3.zero;
                Vector3 boostingLoc = Vector3.zero;

                Vector3 Testvec = Vector3.zero;
                


                if(b_hasHit[i])
                {
                    
                    tmpbffr[i].z = 0.9619961f;

                    groundedLoc.x = 0f;
                    groundedLoc.y = -m_hit[i].distance + rearOffset;
                    groundedLoc.z = 0f;

                    Testvec.y = -m_hit[i].distance;

                    RLWheel.localPosition = groundedLoc;

                    
                }

                if(!b_hasHit[i])
                {
                    inAirLoc.x = 0f;
                    inAirLoc.y = -rayLength + rearOffset;
                    inAirLoc.z = 0f;

                    RLWheel.localPosition = inAirLoc;

                }

            }

            if(i == 3)
            {
                tmpbffr[i] = rays[i].localPosition;
                    

                Vector3 groundedLoc = Vector3.zero;
                Vector3 inAirLoc = Vector3.zero;
                Vector3 boostingLoc = Vector3.zero;

                Vector3 Testvec = Vector3.zero;
                


                if(b_hasHit[i])
                {
                    
                    tmpbffr[i].z = 0.9619961f;

                    groundedLoc.x = 0f;
                    groundedLoc.y = -m_hit[i].distance + rearOffset;
                    groundedLoc.z = 0f;

                    Testvec.y = -m_hit[i].distance;

                    RRWheel.localPosition = groundedLoc;

                    
                }

                if(!b_hasHit[i])
                {
                    inAirLoc.x = 0f;
                    inAirLoc.y = -rayLength + rearOffset;
                    inAirLoc.z = 0f;

                    RRWheel.localPosition = inAirLoc;

                }

 
            }

         
        }


        FRWheelPivot.localRotation = Quaternion.Euler(0, Input.GetAxisRaw("Horizontal") * 45, 0);
        FLWheelPivot.localRotation = Quaternion.Euler(0, Input.GetAxisRaw("Horizontal") * 45, 0);

        
        
            

    }

    void HandleBoost()
    {
       
        if(Input.GetKey(KeyCode.LeftShift))
        {
            
 
            Vector3 BoostVel = rb.transform.forward * topSpeed;
            Vector3 velChange = BoostVel - rb.velocity; //What the hell is this math
            Vector3 accel = velChange / Time.fixedDeltaTime;
            accel = Vector3.ClampMagnitude(accel, 100f);

           
           


            if(b_Isgrounded)
            {

               
                b_isBoosting = true;
            }

            
            //This needs to set the current velocity to the max speed
            
   
        }

        else
        {
            b_isBoosting = false;

             

        }



    }

}
