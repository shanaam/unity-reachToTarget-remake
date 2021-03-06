﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UXF;

/*
 * File:    HandCursorController.cs
 * Project: ReachToTarget-Remake
 * Authors: Shanaathanan Modchalingam, Peter Caruana
 * York University 2019 (c)
 */

public class HandCursorController : MonoBehaviour
{
    //link to the actual hand position object
    public GameObject realHand; //Cursor associated with controller position
    public GameObject trackerHolderObject; // child of real sphere
    public TargetContainerController targetContainerController; //Controller class for the object which spawns and moves targets
    public Session session; //UXF session, contains information on the current experiment

    public GameObject particleSystem; // Particle system for the hand, used to create a trail of movement

    public float pauseLength; //length of pause required to consider hand (relatively) stationary
    private float pausedTimeStart = 0;
    private bool checkForPauseTimerActive = true;

    //link to experiment controller (make a static instance of this?)
    public ExperimentController experimentController;

    
    public bool isInTarget = false;
    public bool isInHome = false;
    public bool isPaused = false;
    public bool isInHomeArea = false;
    public bool visible = true;
    public bool holdingItem = false;
    public bool canSpawnTargets = false;

    public GameObject cubePrefab; //Cube prefab for debuging purposes

    public bool taskCompleted = true; //this one is also set by instructionAcceptor

    //public bool collisionHeld = false;
    //private float collision_start_time;

    public CursorMovementType movementType; //movement type of the hand cursor, can be set to aligned, rotated, clamped etc.

    // for the timer
    private float timerStart;
    private float timerEnd;
    private float reachTime;

    //link to the occulus touch controller to read button input
    [SerializeField]
    private OVRInput.Controller m_controller;
    private float rotation; //filler variable used to store rotations
    private string experiment_mode; //experiment mode read from session settings
    

    //variables used for checking pause
    List<float> distanceFromLastList = new List<float>();
    Vector3 lastPosition;
    readonly float checkForPauseRate = 0.05f;
    private bool spawnLock = true; //semaphore variable
    //private Vector3 oldPos; //replace this with local position-based transformations

    private Vector3 pastPosition = new Vector3(0,0,0);
    private Vector3 handPastPosition = new Vector3(0, 0, 0);

    private Vector3 handPosOffset = new Vector3(0, 0, 0);
    public string HandForNextTrial;
    //private Vector3 currentPosition;

    // Use this for initialization
    void Start()
    {
        // disable the whole task initially to give time for the experimenter to use the UI
        // gameObject.SetActive(false);

        movementType = new AlignedHandCursor();

        //determine the offset (from the controller) for the hand cursor
        handPosOffset = realHand.transform.localPosition;
    }


    void Update()
    {
        //movementType should be set based on session settings
        
    }

    public void RotateParent()
    {
        
        transform.parent.transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    public void LocalizeParent()
    {
        transform.parent.transform.position = transform.position;
    }

    public void SetMovementType(Trial trial)
    {
        string type = trial.settings.GetString("type");
        rotation = trial.settings.GetFloat("cursor_rotation");
        experiment_mode = trial.settings.GetString("experiment_mode");
        // set the rotation for the trial

        // dual rotations
        if (trial.settings.GetString("experiment_mode") == "objectToBox") {
            if (targetContainerController.receptaclePrefab.name == targetContainerController.boxPrefab.name) {
                rotation = rotation * -1;
                }
            else{
                rotation = rotation * 1;
            }
        }

        session.CurrentTrial.result["dual_rotation"] = rotation; 

        if (type.Equals("clamped"))
        {
            movementType = new ClampedHandCursor();
            //Debug.Log("MovementType set to : Clamped");
        }
        else if (type.Equals("rotated"))
        {
            movementType = new RotatedHandCursor(rotation);
        }
        else
        {
            movementType = new AlignedHandCursor();
            //Debug.Log("MovementType set to : Aligned");
        }
    }

    public void SetCursorVisibility(Trial trial) //run by UXF at start of trial
    {
        Renderer rend = GetComponent<Renderer>();
        rend.enabled = visible = true;
    }

    // Manually override the cursor visibility
    public void SetCursorVisibility(bool visible)
    {
        Renderer rend = GetComponent<Renderer>();
        rend.enabled = this.visible = visible;
    }

    private void VisibleCursor()
    {
        Renderer rend = GetComponent<Renderer>();
        
        if (visible)
        {
            rend.enabled = true;
            particleSystem.SetActive(true);
        }
        else
        {
            rend.enabled = false;
            particleSystem.SetActive(false);
        }
    }

    // ALL tracking should be done in LateUpdate
    // This ensures that the real object has finished moving (in Update) before the tracking object is moved
    void LateUpdate()
    {
        // get the inputs we need for displaying the cursor
        Vector3 realHandPosition = realHand.transform.position;
        Vector3 centreExpPosition = transform.parent.transform.position;
        Vector3 movementVector = realHandPosition - handPastPosition;
        Vector3 rotatedVector = movementVector;


        CursorMovementType default_movement = new AlignedHandCursor();

        //Update position of the cursor based on mvmt type
        if (experiment_mode == "objectToBox")
        {
            if (holdingItem)
            {
                //if (movementType.Type == "clamped")
                //{
                //    // see GrabableObject script.

                //}
                //else
                //{
                    rotatedVector = Quaternion.Euler(0, rotation * -1, 0) * movementVector;
                    transform.position = pastPosition + rotatedVector;
                //}

                //visible = false;
            }
            else
            {
                //visible = true;
                rotatedVector = Vector3.zero;
                transform.localPosition = default_movement.NewCursorPosition(realHandPosition, centreExpPosition);
                
            }

            VisibleCursor();

            handPastPosition = realHandPosition;
            pastPosition = transform.position;
        }
        else
        {
            GameObject targ = GameObject.FindGameObjectWithTag("Target");
            if (targ != null)
            {
                // If we are in the first stage (IsSecondaryHome == true) use aligned
                // Else use trial specific movement type where the secondary home
                // targCtrler will be null if the experiment type is localization
                TargetController targCtrler = targ.GetComponent<TargetController>();
                if (targCtrler != null && targCtrler.IsSecondaryHome)
                {
                    transform.localPosition = default_movement.NewCursorPosition(realHandPosition, centreExpPosition);
                }
                else
                {
                    transform.localPosition = movementType.NewCursorPosition(realHandPosition,
                        centreExpPosition + new Vector3(0.0f, 0.0f, targetContainerController.secondHomeDist)) + 
                        new Vector3(0.0f, 0.0f, targetContainerController.secondHomeDist); 
                    //- (transform.parent.transform.forward * 0.06f));
                }
            }
            else
            {
                transform.localPosition = movementType.NewCursorPosition(realHandPosition, centreExpPosition);
            }

            VisibleCursor();
        }
        
        Quaternion parentrot = realHand.transform.rotation;
        transform.rotation = parentrot;


        //For Debugging spawning new objects
        OVRInput.Update();
        if (canSpawnTargets)
        {
            if (OVRInput.Get(OVRInput.RawButton.A, m_controller) && spawnLock)
            {
                var target = Instantiate(cubePrefab, transform);
                target.transform.position = transform.position;
                target.transform.parent = null;
                target.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                Debug.Log("Physics cube spawned at hand");
                spawnLock = false;
            }
            if (!OVRInput.Get(OVRInput.RawButton.A, m_controller) && !spawnLock)
            {
                spawnLock = true;
            }
        }
        

        if (isInHomeArea && taskCompleted ||
            (!isInHomeArea && !taskCompleted))
        {
            CheckForPause();
        }
    }

    //modifiers
    //updates position of handCursor
    

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Target"))
        {
            isInTarget = true;
            ShortVibrateController();
        }
        else if (other.CompareTag("Home"))
        {
            //visible = true;
            isInHome = true;
            ShortVibrateController();
        }
        else if (other.CompareTag("HomeArea"))
        {
            isInHomeArea = true;

            /*
            if (taskCompleted)
            {
                InvokeRepeating("CheckForPause", 0, checkForPauseRate);
            }
            else
            {
                CancelInvoke("CheckForPause");
            }
            */
        }
    }

    
    private void OnTriggerExit(Collider other)
    {
        // do things depending on which collider is being exited
        if (other.CompareTag("Target"))
        {
            isInTarget = false;

        }
        else if (other.CompareTag("Home"))
        {
            isInHome = false;
        }
        else if (other.CompareTag("HomeArea"))
        {
            isInHomeArea = false;

            /*
            if (!taskCompleted)
            {
                InvokeRepeating("CheckForPause", 0, checkForPauseRate);
            }
            else
            {
                CancelInvoke("CheckForPause");
            }
            */
        }
        
    }


    public void CheckForPause()
    {
        //calculate the distance from last position
        float distance = Vector3.Distance(lastPosition, transform.position);

        float distanceMean = 1000;

        //add the distance to our List
        distanceFromLastList.Add(distance);

        //if List is over a certain length, check some stuff
        if (distanceFromLastList.Count > 8)
        {
            //check and print the average distance
            //float[] distanceArray = distanceFromLastList.ToArray();
            float distanceSum = 0f;

            for (int i = 0; i < distanceFromLastList.Count; i++)
            {
                distanceSum += distanceFromLastList[i];
            }

            distanceMean = distanceSum / distanceFromLastList.Count;

            distanceFromLastList.RemoveAt(0);
        }

        //replace lastPosition withh the current position
        lastPosition = transform.position;

        if (distanceMean < 0.001)
        {
            //Activates timer when the pause begins (Mutex)
            if (checkForPauseTimerActive)
            {
                pausedTimeStart = Time.fixedTime;
            }
            
            //If paused for longer than pauseLength, isPaused is true
            float delta = Time.fixedTime - pausedTimeStart;

            if (delta >= pauseLength)
            {
                isPaused = true;
            }
            //Blocks pausedTimeStart from being reset
            checkForPauseTimerActive = false;
        }
        else
        {
            isPaused = false;
            //PausedTimeStart can now be reset.
            checkForPauseTimerActive = true;
        }   
    }

    // Resets "isPaused" to false and clears the history of the cursor movement
    public void ResetPause()
    {
        isPaused = false;
        distanceFromLastList.Clear();
    }

    // vibrate controller for 0.2 seconds
    public void ShortVibrateController(float amplitude = 0.6f, float duration = 0.2f)
    {
        // make the controller vibrate
        OVRInput.SetControllerVibration(1, amplitude, m_controller);

        // stop the vibration after x seconds
        Invoke("StopVibrating", duration);
    }


    void StopVibrating()
    {
        OVRInput.SetControllerVibration(0, 0, m_controller);
    }

    //-- Moved to Experiment Controller
    //Start timer when home Disapears, End when target disapears
    private void StartTimer()
    {
        ClearTime();
        timerStart = Time.fixedTime;
        //Debug.Log("Timer started : " + timerStart);
    }

    private void PauseTimer()
    {
        timerEnd = Time.fixedTime;
        //Debug.Log("Timer end : " + timerEnd);
        Debug.LogFormat("Reach Time: {0}", timerEnd - timerStart);
    }

    private void ClearTime()
    {
        timerStart = 0;
        timerEnd = 0;
    }

    public void ReAlignCursor()
    {
        transform.position = realHand.transform.position;
    }

    public void ChangeHand(GameObject newTransform)
    {
        Vector3 handPosOffsetToUse;

        // vector -0.00205, 0, 0.0331 is the coordinate of the front of the controller
        realHand.transform.SetParent(newTransform.transform);
        m_controller = experimentController.GetController();

        handPosOffsetToUse = HandForNextTrial == "r" ?
            handPosOffset : Vector3.Scale(handPosOffset, new Vector3(-1, 1, 1));

        realHand.transform.localPosition = handPosOffsetToUse;
    }
}
