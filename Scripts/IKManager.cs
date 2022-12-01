using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKManager : MonoBehaviour
{
    public Transform rightArm, leftArm, shoulderRoot, rightHandle, leftHandle, rightLeg, leftLeg, rightLegTarget, leftLegTarget, rightLegPole, leftLegPole;
    public IK rightLegIK, leftLegIK;
    private Foot right, left;
    private GameManager manager;
    private PlayerMovement movement;
    [SerializeField] private int backLayer, frontLayer; //number applied to the order in layer, back is behind bucket, front is in front
    private LineRenderer rightLine, leftLine;
    public GameObject rightFoot, leftFoot;

    [Header("Walk parameters")]
    //defines how far from idealPos a foot can be before it's allowed to move
    //if this is below gaitLength, changes make no difference, if above then movement stops
    //when at rest defines how far the legs are apart
    [SerializeField] private float leashDist;

    //The amount of time the foot takes to move from its prev to its next, one of the two parameters that determine walk speed
    public float stepTime;

    //how far ahead of the centre of mass the idealPos is set, i.e. how long the steps are
    //the other paramter that determines walk speed
    public float gaitLength;

    //A step is defined by a bezier curve, where one of the points is extruded outwards from the globe by a certain length
    //this length is defined by the highest peak within the range of the step (such that the step will arc over any little hills)
    // + stepHeight, this variable
    [SerializeField] private float stepHeight;


    [SerializeField] private float rotationMultiplier;

    //the position that feet will move towards, determines movement of the player
    private Vector2 idealPos;
    private float peakHeight, minFootHeight; //used to store info that is used to calculate step curve every step
    private Foot movingFoot;
    private Vector2 peakPoint; //the point on the bezier curve (defining a step) that is heighest

    void Start() {
        //set linerenderer variables
        rightLine = rightArm.GetComponent<LineRenderer>();
        leftLine = leftArm.GetComponent<LineRenderer>();

        //initialise references
        manager = GameObject.FindObjectOfType<GameManager>();
        movement = GameObject.FindObjectOfType<PlayerMovement>();

        //construct foot objects used for easier storing of information
        right = new Foot(Globe.GetPosOnCircle(rightLegTarget.position, manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp), ref rightLegTarget);
        left = new Foot(Globe.GetPosOnCircle(leftLegTarget.position, manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp), ref leftLegTarget);
        left.other = right;
        right.other = left;

        SetBodyProportions(); 
    }

    void Update() {
        // Debug.Log(rightLegTarget.position);
        HandSortingOrder(); 

        //Leg IK management
        ValidateGroundedness();

        idealPos = movement.CalculateIdealPos();

        UpdateGlobePositions(right);
        UpdateGlobePositions(left);

        UpdateFootGoals();

        UpdateFootPosition(right);
        UpdateFootPosition(left);

        // Debug.Log("right grounded: " + right.isGrounded);
        // Debug.Log("left grounded: " + left.isGrounded);
    }

    public void LegPoles(float dir) {
        //determine which leg is facing left/right relative to the players up and assign poles accordingly

        if (dir > 0) { //facing right
            rightLegIK.Pole = rightLegPole;
            leftLegIK.Pole = rightLegPole;

            rightFoot.transform.localScale = new Vector2(Math.Abs(rightFoot.transform.localScale.x) * dir, rightFoot.transform.localScale.y);
            leftFoot.transform.localScale = new Vector2(Math.Abs(leftFoot.transform.localScale.x) * dir, leftFoot.transform.localScale.y);
        }
        else if (dir < 0) { //facing left
            rightLegIK.Pole = leftLegPole;
            leftLegIK.Pole = leftLegPole;

            rightFoot.transform.localScale = new Vector2(Math.Abs(rightFoot.transform.localScale.x) * dir, rightFoot.transform.localScale.y);
            leftFoot.transform.localScale = new Vector2(Math.Abs(leftFoot.transform.localScale.x) * dir, leftFoot.transform.localScale.y);
        }
        else { //not moving
            //find the leg on the left, assign left pole to this and right to the other


            int rightDir = FeetDirection();
            int leftDir = -rightDir;

            //assign poles based upon direction
            rightLegIK.Pole = (rightDir > 0) ? rightLegPole : leftLegPole;
            leftLegIK.Pole = (leftDir > 0) ? rightLegPole : leftLegPole;

            //assign feet based upon direction
            if (rightDir > 0) {
                rightFoot.transform.localScale = new Vector2(Math.Abs(rightFoot.transform.localScale.x) * 1, rightFoot.transform.localScale.y);
                leftFoot.transform.localScale = new Vector2(Math.Abs(leftFoot.transform.localScale.x) * -1, leftFoot.transform.localScale.y);
            }
            else {
                rightFoot.transform.localScale = new Vector2(Math.Abs(rightFoot.transform.localScale.x) * -1, rightFoot.transform.localScale.y);
                leftFoot.transform.localScale = new Vector2(Math.Abs(leftFoot.transform.localScale.x) * 1, leftFoot.transform.localScale.y);
            }
        }

        PlaceFeet();
    }

    //places the feet at the end of the legs and orients them so that they are against the ground
    private void PlaceFeet() {
        rightFoot.transform.position = rightLegIK.line.GetPosition(rightLegIK.line.positionCount - 1);
        rightFoot.transform.up = -manager.NormalAtPosition(rightFoot.transform.position);

        leftFoot.transform.position = leftLegIK.line.GetPosition(leftLegIK.line.positionCount - 1);
        leftFoot.transform.up = -manager.NormalAtPosition(leftFoot.transform.position);
    }

    //the 'left' and 'right' foot arent really any different, sometimes we need to determine which foot is actually on the left or right
    private int FeetDirection() { //returns the x sign of the right foot, the left foot will always be the negation of this
        //find x direction of foot in reference to the player
        //project foot onto the player's right axis to determine its x component along that vector
        //i.e. determine if direction from player to foot is facing the same direction as transform.right
        Transform playerT = movement.playerObj.transform;
        return Math.Sign(Vector2.Dot(rightLegTarget.position - playerT.position, playerT.right));
    }

    public void ResetFeet() {
        right.Reset();
        left.Reset();
    }

    //returns the average of the left foot and right foots position, accessed by PlayerMovement used to place the body accordingly
    public Vector2 FeetCentre() {
        return (right.current + left.current) / 2f;
    }

    //determines which feet are currently grounded, a foot can only move if the other foot is grounded
    private void ValidateGroundedness()
    {
        //has this foot reached its goal? if so its now grounded, set its position to be equal to its goal
        if (right.DistToGoal() == 0 && !right.isGrounded) {
            manager.Audio.Play("Step");
            right.setGrounded(true);
        }

        if (left.DistToGoal() == 0 && !left.isGrounded) {
            manager.Audio.Play("Step");
            left.setGrounded(true);
        }
    }

    //checks the distance for BOTH feet and sets the goal of the one furthest from idealpos, resulting in a walk that always alternates
    private void UpdateFootGoals()
    {
        //if both feet are grounded then a foot can move
        //then calculate the distance between the foots current position and its ideal, if its larger than leash set goal to ideal
        if (right.isGrounded && left.isGrounded) {            
            float rightDist = Vector2.Distance(right.current, idealPos);
            float leftDist = Vector2.Distance(left.current, idealPos);
            Foot further = (rightDist > leftDist) ? right : left;

            if (Math.Max(rightDist, leftDist) > leashDist) {
                further.SetGoal(idealPos);
                movingFoot = further;
                DefineBezierCurve();
            }
        }

    }

    public void OnDrawGizmosSelected() {
        //ideal pos
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(idealPos, 0.05f);

        //average
        Gizmos.color = Color.red;  
        Gizmos.DrawSphere((right.current + left.current) / 2f, 0.05f);

        //where the foot is going
        Gizmos.color = Color.blue;
        //if its moving towards goal then show goal
        if (right.next != right.current) {
            Gizmos.DrawSphere(right.next, 0.05f);
        }
        if (left.next != left.current) {
            Gizmos.DrawSphere(left.next, 0.05f);
        }

        //draw peakPoint of bezier curve for step
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(peakPoint, 0.05f);
    }

    //takes in a foot and uses its previous and next fields to determine where current should be
    //uses a bezier curve to make the foot travel in an arc towards the player rather than straight towards the goal
    private void UpdateFootPosition(Foot foot)
    {
        if (foot == movingFoot) {
            foot.deltaTime += Time.deltaTime; //t = foot.deltaTime / stepTime
            float t = foot.deltaTime / stepTime;

            Vector2 a = Vector2.Lerp(foot.previous, peakPoint, t);
            Vector2 b = Vector2.Lerp(peakPoint, foot.next, t);
            Vector2 c = Vector2.Lerp(a, b, t);

            foot.UpdateFootPos(c);
        }
        else {
            foot.UpdateFootPos(foot.next);
        }       
    }

    private void DefineBezierCurve() {
        // float[] results = manager.GetPeakAndMinInRange(movingFoot.previous, movingFoot.next);
        // peakHeight = results[0];
        // minFootHeight = results[1];
        Vector2 midPoint = (movingFoot.previous + movingFoot.next) / 2f;

        Vector2 dirVec = (midPoint - manager.startGlobe.GetPos()).normalized;
        // float height = (peakHeight - minFootHeight) + stepHeight;
        float height = stepHeight;
        peakPoint = midPoint + (dirVec * height);
    }

    private void UpdateGlobePositions(Foot foot) {
        Vector2 prevOnGlobe = Globe.GetPosOnCircle(foot.previous, manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp);
        Vector2 nextOnGlobe = Globe.GetPosOnCircle(foot.next, manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp);
        foot.previous = prevOnGlobe;
        foot.next = nextOnGlobe;
    }

    //based upon difference in foot height determines how many degrees the body should rotate
    public float FindBodyRotation() {
        Foot absRight, absLeft;
        absRight = (FeetDirection() > 0) ? right: left;
        absLeft = (FeetDirection() > 0) ? left: right;
        float rightHeight = manager.PeakHeightByIndex(manager.GetLineIndexByPosition(absRight.current));
        float leftHeight = manager.PeakHeightByIndex(manager.GetLineIndexByPosition(absLeft.current));

        float delta = leftHeight - rightHeight;

        // Debug.Log(rotationDegrees);
        return delta * rotationMultiplier;
    }
    
    //based upon body height calculates lengths of other body parts
    private void SetBodyProportions() {
        //leg length must be 50% of body height
        //upper arm : forearm = 6:5
        //legs must be 1.2 x the length of arms
        //arms should be the length of body height
        //no idea what lower:upper leg should be, just say 50:50 for now
        const float bodyHeight = 1.5f;
        float armLength = (bodyHeight / 2f) * 1.45f;
        float legLength = (bodyHeight / 2f) * 1.65f;
        float upperArm = (armLength / 11f) * 6f;
        float lowerArm = (armLength / 11f) * 5f;
        float upperLeg = legLength / 2f;
        float lowerLeg = legLength / 2f;

        //set arms
        float[] armLengths = new float[2];
        armLengths[0] = upperArm;
        armLengths[1] = lowerArm;
        rightArm.GetComponent<IK>().BonesLength = armLengths;
        leftArm.GetComponent<IK>().BonesLength = armLengths;

        //set legs
        float[] legLengths = new float[2];
        legLengths[0] = lowerLeg;
        legLengths[1] = upperLeg;
        leftLeg.GetComponent<IK>().BonesLength = legLengths;
        rightLeg.GetComponent<IK>().BonesLength = legLengths;

        //update IK variables
        rightArm.GetComponent<IK>().Init();
        leftArm.GetComponent<IK>().Init();
        rightLeg.GetComponent<IK>().Init();
        leftLeg.GetComponent<IK>().Init();
    }

    //ensures that arms appear in sorting order they should 
    private void HandSortingOrder() {
        //if the left handle is left of the shoulder, then the left hand is behind
        //if the left handle is right of the shoulder, then the left hand is in front
        //this ensures that when the bucket is central to the body the arms will remain behind the bucket as if its being held in front
        float leftHandleDir = (leftHandle.position - shoulderRoot.position).x;

        if (leftHandleDir > 0) { //facing right
            leftLine.sortingOrder = frontLayer;
        }
        else { //facing left or dead centre
            leftLine.sortingOrder = backLayer;
        }

        //same for right handle, in reverse
        float rightHandleDir = (rightHandle.position - shoulderRoot.position).x;

        if (rightHandleDir < 0) { //facing left
            rightLine.sortingOrder = frontLayer;
        }
        else { //facing right or dead centre
            rightLine.sortingOrder = backLayer;
        }
    }
}
