using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public class WheelCast : MonoBehaviour
{
    public Transform[] rays;
    public Transform[] frontWheels;
    public Transform[] rearWheels;
    public Transform liftPoint;
    


    
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

        
        ///
        ///
        ///
        ///


        for(int i = 0; i < rays.Length; i++)
        {
            
            

            if(Physics.Raycast(rays[i].transform.position, -rays[i].transform.up, out m_hit[i], rayLength, groundMask))
            {
                

                //Suspension
                Vector3 springDir = rays[i].up;
                Vector3 tireWorldVel = rb.GetPointVelocity(rays[i].position);
                float offset = suspensionRestDistance - m_hit[i].distance;
                float Vel = Vector3.Dot(springDir, tireWorldVel);
                float Force = (offset * springStrength) - (Vel * springDamper);
                _i = i;

                groundHit[i] = m_hit[i];
                rb.AddForceAtPosition(springDir * Force, rays[i].position);


                b_Isgrounded = true;
                b_hasHit[i] = true;

                
            
            }

            else
            {
                b_Isgrounded = false;
                b_hasHit[i] = false;
                
            }

            Debug.DrawRay(rays[i].position, -rays[i].up * rayLength);

            

            

            
        }

        foreach(Transform wheel in rearWheels)
        {
            Vector3 accelDir = wheel.forward;

            if(y != 0.0f && rb.velocity.magnitude <= topSpeed && b_Isgrounded)
            {
                float carSpeed = Vector3.Dot(transform.forward, rb.velocity);
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);
                float torque = driveTorque.Evaluate(normalizedSpeed) * y * 1000;
                normSpeed = normalizedSpeed;

                

                rb.AddForceAtPosition(Vector3.ProjectOnPlane(accelDir, groundHit[_i].normal) * torque, wheel.position);
                
            }

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

        if(y == 0f && b_Isgrounded && rb.velocity.z <= 0.5f)
        {
           

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
        

        HandleBrake();
        HandleBoost();
        HandleWheelAnimations();
        HandleChassisAnimations();
        


    }

    void HandleChassisAnimations()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        float rollAmount = 6f;
        float pitchAmount = 1f;
        float boostPitchAmount = 30f;
        float boostRollAmount = 2.5f;

        

        
        //Multiply DeltaTime; Higher values means faster
        

        
        if(b_isBoosting && b_Isgrounded)
        {
            
            Quaternion newRotation = transform.rotation * Quaternion.Euler(-boostPitchAmount, 0f, -boostRollAmount * (x * normSpeed));

            
            
            
            chassisModel.rotation = Quaternion.Lerp(chassisModel.rotation, newRotation, Time.fixedDeltaTime * 5);

            for(int i = 0; i < rays.Length; i++)
            {
                if(i == 0)
                {
                    Vector3 RotVec = Vector3.zero;

                    RotVec.x = rays[i].localPosition.x;
                    RotVec.y = 1.15f;
                    RotVec.z = rays[i].localPosition.z;

                    rays[i].localPosition =  RotVec;
                }
            }
            
            

            
        }
        else if(!b_isBoosting)
        {

            Quaternion newRotation = transform.rotation * Quaternion.Euler(y * -pitchAmount, 0f, -rollAmount * (x * normSpeed));
            
            

            
            chassisModel.rotation = Quaternion.Lerp(chassisModel.rotation, newRotation, Time.fixedDeltaTime * 10f);
           

        }



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

    void HandleDrift()
    {
        bool b_isBraking = Input.GetButton("Jump");
        

        if(b_isBraking)
        {
           
            
        }
        else
        {
         
            
        }
    }

    void HandleGravity()
    {
        float groundedGravity = 1200;
        float airGravity = 2000;

        if(b_Isgrounded)
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
        ;

        for(int i = 0; i <= rays.Length; i++)
            {
               if(i == 0)
               {

                if(b_hasHit[i])
                {
                    Vector3 newPos = new Vector3(FLWheel.localPosition.x, -m_hit[i].distance + frontOffset, FLWheel.localPosition.z);

                    FLWheel.localPosition = newPos;
                }
                else
                {
                    FLWheel.localPosition = new Vector3(FLWheel.localPosition.x, -rayLength + frontOffset, FLWheel.localPosition.z);
                }
  
               }



               if(i == 1)
               {

                if(b_hasHit[i])
                {
                    Vector3 newPos = new Vector3(FRWheel.localPosition.x, -m_hit[i].distance + frontOffset, FRWheel.localPosition.z);

                    FRWheel.localPosition = newPos;
                }
                else
                {
                    FRWheel.localPosition = new Vector3(FRWheel.localPosition.x, -rayLength + frontOffset, FRWheel.localPosition.z);
                }



               }

               if(i == 2)
               {

                if(b_hasHit[i])
                {
                    Vector3 newPos = new Vector3(RLWheel.localPosition.x, -m_hit[i].distance + rearOffset, RLWheel.localPosition.z);

                    RLWheel.localPosition = newPos;
                }
                else
                {
                    RLWheel.localPosition = new Vector3(RLWheel.localPosition.x, -rayLength + rearOffset, RLWheel.localPosition.z);
                }



               }

               if(i == 3)
               {

                if(b_hasHit[i])
                {
                    Vector3 newPos = new Vector3(RRWheel.localPosition.x, -m_hit[i].distance + rearOffset, RRWheel.localPosition.z);

                    RRWheel.localPosition = newPos;
                }
                else
                {
                    RRWheel.localPosition = new Vector3(RRWheel.localPosition.x, -rayLength + rearOffset, RRWheel.localPosition.z);
                }



               }

                
               
            }

            if(Input.GetKeyDown(KeyCode.G))
            {
                Time.timeScale = 0.0f;
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
            }

             if(Input.GetKeyDown(KeyCode.H))
            {
                Time.timeScale = 1f;
            }
            

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
                

                //rb.AddForce(Vector3.ProjectOnPlane(accel, m_hit[_i].normal), ForceMode.Acceleration);

                b_isBoosting = true;
            }

            
            //This needs to set the current velocity to the max speed
            
   
        }

        else
        {
            b_isBoosting = false;
        }


        print(rb.velocity);


    }

}
