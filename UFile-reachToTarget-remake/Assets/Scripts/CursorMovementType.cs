﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * File: CursorMovementType.cs
 * License: York University (c) 2019
 * Author: Peter Caruana
 * Desc: Defines the movement of an object with respect to transformations. Abstract class defines the general interface of a movement type
 * Content:
 *      abstract class CursorMovementType
 *          ||
 *          |--- class ClampedHandCursor
 *          |
 *          --- class AlignedHandCurosor
 */
abstract public class CursorMovementType : MonoBehaviour
{
    /*
     * Returns new vector according to the transformType  based on the real transformation
     */
    abstract public Vector3 NewCursorPosition(Vector3 realPosition, Vector3 centreExpPosition); //Returns new position vector based on movementType

    abstract public string Type { get; }
}

public class AlignedHandCursor : CursorMovementType
{
    // Mapped movement will give a 1-1 mapping of input position to output position.
    //Constructor
    public AlignedHandCursor()
    {

    }


    //Interface Methods
    public override Vector3 NewCursorPosition(Vector3 realPosition, Vector3 centreExpPosition)
    {
        //todo: Implement mapped transformation
        return realPosition - centreExpPosition;
    }

    public override string Type => "aligned";


    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {

    }
}

public class ClampedHandCursor : CursorMovementType
{
    // clamped movement type, gives a mapping of the transformation based on the input clamped to 1 plane of movement.
    //Constructor
    public ClampedHandCursor()
    {
        
    }


    //Interface Methods
    public override Vector3 NewCursorPosition(Vector3 realPosition, Vector3 centreExpPosition)
    {
        //todo: Implement clamped transformation
        GameObject target = GameObject.FindGameObjectWithTag("Target");

        // if a target exists
        if (target != null)
        {
            Vector3 targetPosition = target.transform.position;
            Vector3 localTargetPosition = targetPosition - transform.parent.transform.position;

            //transform.localPosition = realHand.transform.position - transform.parent.transform.position;
            Vector3 realHandPosition = realPosition;
            Vector3 rotatorObjectPosition = transform.parent.transform.position;

            //project onto a vector pointing toward target
            //transform.localPosition = Vector3.Project(realHandPosition - rotatorObjectPosition, localTargetPosition);

            //project onto a vertical plane intersecting target and home
            Vector3 vectorForPlane = new Vector3(targetPosition.x, targetPosition.y - 1, targetPosition.z);
            Vector3 normalVector = Vector3.Cross(targetPosition - rotatorObjectPosition, vectorForPlane - rotatorObjectPosition);

            

            return Vector3.ProjectOnPlane(realHandPosition - rotatorObjectPosition, normalVector);
        }
        else
        {
            return new Vector3(0,0,0);
        }
    }


    public override string Type => "clamped";


    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {

    }

   
}

