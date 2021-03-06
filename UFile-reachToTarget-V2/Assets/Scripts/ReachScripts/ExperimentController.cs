﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UXF;

/*
 * File: ExperimentController.cs
 * License: York University (c) 2019
 * Author: Peter Caruana
 * Desc:    Experiment Controller is what operates the trials of the loaded block. It is responsible for applying settings
 * of the trial to the Unity game at runtime
 */
public class ExperimentController : MonoBehaviour
{
    public Session session;
    public TextMeshPro Instruction; //Text
    public GameObject handCursor;
    public GameObject homeCursor;
    public TargetContainerController targetContainerController;
    public HomeCursorController homeCursorController;
    public GameObject trackerHolderObject;
    public PlaneController planeController;
    public GameObject instructionAcceptor;
    public InstructionAcceptor instructionAcceptorScript;
    public PositionLocCursorController positionLocCursorController;

    public GameObject LeftControllerAnchor, RightControllerAnchor;
    public GameObject LeftHandRenderer, RightHandRenderer;


    // things to log
    public float stepTime;
    public string objShape;
    public float objSpawnX;
    public float objSpawnZ;
    public float recepticleX;
    public float recepticleY;
    public float recepticleZ;
    public float distractorLoc;
    public float targetX;
    public float targetY;
    public float targetZ;

    public float locX;
    public float locY;
    public float locZ;
    

    //-- Internal Variables
    private float timerStart;
    private float timerEnd;
    private float reachTime;
    [SerializeField]
    private OVRInput.Controller m_controller; //Link to the Oculus controller to read button inputs
    
    public OVRInput.Controller GetController() { return m_controller; }

    public void StartTrial() 
    {
        
        homeCursorController.Disappear(); //Hides homeposition at begining of trial
        session.BeginNextTrial();
        
    }

    //Called 
    public void BeginTrialSteps(Trial trial)
    {
        if(trial.settings.GetString("experiment_mode") == "objectToBox")
        {
            ////pseudo randomized block shapes
            //System.Random rando = new System.Random();
            //int flag = rando.Next(100); // randomizing the shape -- this will change depending on the experiment

            //if (flag % 2 == 0) //even number
            //{
            //    trial.settings.SetValue("object_type", "cube");
            //}
            //else
            //{
            //    trial.settings.SetValue("object_type", "sphere");
            //}

            targetContainerController.IsGrabTrial = true;

            if (trial.settings.GetString("type") == "instruction")
            {
                // jsut wait? for a keypress?
                trackerHolderObject.GetComponent<PositionRotationTracker>().enabled = false;
                instructionAcceptor.SetActive(true);
                instructionAcceptorScript.doneInstruction = false;

            }
            else
            {

                trackerHolderObject.GetComponent<PositionRotationTracker>().enabled = true;

                // do the plane thing at the start of each block 
                if (trial.numberInBlock == 1)
                {
                    planeController.SetTilt(trial);
                }
                else
                {
                    targetContainerController.SpawnTarget(trial);
                }
            }

            GameObject grabableObject = GameObject.FindGameObjectWithTag("ExperimentObject");

        }
        else if(trial.settings.GetString("experiment_mode") == "target")
        {
            targetContainerController.IsGrabTrial = false;

            if (trial.settings.GetString("type") == "localization")
            {
                trackerHolderObject.GetComponent<PositionRotationTracker>().enabled = false;

                UnrenderObject(handCursor);

                if (GameObject.Find("RayPositionCursor") == null)
                {
                    targetContainerController.SpawnTarget(trial);
                }
            }
            else
            {
                if (trial.settings.GetString("type") == "instruction")
                {
                    // jsut wait? for a keypress?
                    trackerHolderObject.GetComponent<PositionRotationTracker>().enabled = false;

                    instructionAcceptor.SetActive(true);
                    instructionAcceptorScript.doneInstruction = false;

                }
                else
                {
                    RenderObject(handCursor);
                    trackerHolderObject.GetComponent<PositionRotationTracker>().enabled = true;

                    // do the plane thing at the start of each block 
                    if (trial.numberInBlock == 1)
                    {
                        planeController.SetTilt(trial);
                    }
                    else
                    {
                        targetContainerController.SpawnTarget(trial);
                    }
                }
            }
        }
    }
    // end session or begin next trial (This should ideally be called via event system)
    // destroys the the current target and starts next trial
    public void EndAndPrepare()
    {

        session.CurrentTrial.result["home_x"] = homeCursor.transform.position.x;
        session.CurrentTrial.result["home_y"] = homeCursor.transform.position.y;
        session.CurrentTrial.result["home_z"] = homeCursor.transform.position.z;

        session.CurrentTrial.result["step_time"] = stepTime;
        session.CurrentTrial.result["obj_shape"] = objShape;
        session.CurrentTrial.result["distractor_loc"] = distractorLoc;
        session.CurrentTrial.result["obj_spawn_x"] = objSpawnX;
        session.CurrentTrial.result["obj_spawn_z"] = objSpawnZ;
        session.CurrentTrial.result["recepticle_x"] = recepticleX;
        session.CurrentTrial.result["recepticle_y"] = recepticleY;
        session.CurrentTrial.result["recepticle_z"] = recepticleZ;

        session.CurrentTrial.result["target_x"] = targetX;
        session.CurrentTrial.result["target_y"] = targetY;
        session.CurrentTrial.result["target_z"] = targetZ;

        session.CurrentTrial.result["loc_x"] = locX;
        session.CurrentTrial.result["loc_y"] = locY;
        session.CurrentTrial.result["loc_z"] = locZ;

        //Debug.Log("ending reach trial...");
        // destroy the target, spawn home?
        targetContainerController.DestroyTargets();
        if (GameObject.Find("RayPositionCursor") != null)
        {
            positionLocCursorController.Deactivate();
        }

        // Reassign the hand controlling the cursor as well as the virtual 3D hand model
        HandCursorController cursorCntrler = handCursor.GetComponent<HandCursorController>();
        SkinnedMeshRenderer rightHandRender = RightHandRenderer.GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer leftHandRender = LeftHandRenderer.GetComponent<SkinnedMeshRenderer>();

        bool rightHanded = false;
        try
        {
            rightHanded = session.NextTrial.settings.GetString("hand") == "r";
        }
        catch (NoSuchTrialException)
        {
            // If we are at the end of a block, this will grab the first trial of the next block
            try
            {
                rightHanded = session.GetBlock(session.currentBlockNum + 1).firstTrial.settings.GetString("hand") == "r";
                Debug.Log("Reached end of block. Starting next block.");
            }
            catch (System.ArgumentOutOfRangeException)
            {
                Debug.Log("Reached end of experiment");
            }
        }
        Debug.Log("Reassigning hand for next trial. Righthanded: " + rightHanded);

        // Assign the Oculus controller
        m_controller = rightHanded ?
            OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;

        // Assign the virtual hand model
        //if (!cursorCntrler.holdingItem && !cursorCntrler.taskCompleted)
        //{
        //    rightHandRender.enabled = rightHanded;
        //    leftHandRender.enabled = !rightHanded;
        //}
        //else { rightHandRender.enabled = leftHandRender.enabled = false; }

        // Assign the cursor
        cursorCntrler.HandForNextTrial = rightHanded ? "r" : "l";
        cursorCntrler.ChangeHand(rightHanded ? RightControllerAnchor : LeftControllerAnchor);

        // Vibrate the controller to let the participant know which hand to use

        if (session.CurrentTrial.settings.GetString("experiment_mode") != "target")
        {
            cursorCntrler.ShortVibrateController(1.0f, 0.5f);
        }
        else
        {
            cursorCntrler.visible = false;
        }

        // Make home cursor appear (at dock for next trial)
        homeCursorController.Appear();

        // end the current trial in UXF
        if (session.CurrentTrial.number == session.LastTrial.number)
        {
            session.End();
        }
        else
        {
            session.CurrentTrial.End();
        }
    }

    //-----------------------------------------------------
    // Modifiers
    private void UpdateInstruction(string instruction)
    {
        Instruction.text = instruction;
    }

    private void HideInstruction()
    {
        Instruction.gameObject.SetActive(false);
    }

    private void ShowInstruction()
    {
        Instruction.gameObject.SetActive(true);
    }

    //Start timer when home Disapears, End when target disapears
    public void StartTimer()
    {
        ClearTime();
        timerStart = Time.fixedTime;
        //Debug.Log("Timer started : " + timerStart);
    }

    public void PauseTimer()
    {
        timerEnd = Time.fixedTime;
        //Debug.Log("Timer end : " + timerEnd);
        //Debug.LogFormat("Reach Time: {0}", timerEnd - timerStart);
    }

    public void ClearTime()
    {
        timerStart = 0;
        timerEnd = 0;
    }

    public void CalculateReachTime()
    {
        reachTime = timerEnd - timerStart;
    }

    public float GetReachTime()
    {
        return reachTime;
    }
    //Returns vector between A and B
    //Somewhat redundant, however makes code function easier to read
    private Vector3 CalculateVector(Vector3 A, Vector3 B)
    {
        return B - A;
    }

    public void UnrenderObject(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        rend.enabled = false;
    }

    public void RenderObject(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        rend.enabled = true;
    }

    ////Unused for now but useful
    //IEnumerator WaitAFrame()
    //{
    //    //returning 0 will make it wait 1 frame
    //    yield return 0;
    //}
}
