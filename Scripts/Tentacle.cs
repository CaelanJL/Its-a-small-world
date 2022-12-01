using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour
{
    public LineRenderer line;
    [HideInInspector] public Vector3[] segmentPositions;
    private Vector3[] segmentV;

    [Header("Tail parameters")]
    public float smoothTime;
    public float trailSpeed;
    public float tailDistance;
    public int resolution;

    [Header("Wiggle parameters")]
    public float wiggleFreq;
    public float wiggleAmp;
    public Transform wiggleParent;    

    [Header("Peripherals parameters")]
    public float finPosition = 0.25f;
    public Transform tail;
    public Transform fin, fin2;
    public bool hasTail = false, hasFins = false;

    private int finPos;
    private float targetDist;
    [HideInInspector] public bool alive = true;
    
    // Start is called before the first frame update
    void Start()
    {
        line.positionCount = resolution;
        segmentPositions = new Vector3[resolution];

        //starts at 0,0 not good
        for (int i = 0; i < segmentPositions.Length; i++)
        {
            segmentPositions[i] = transform.position;
        }

        segmentV = new Vector3[resolution];
        targetDist = tailDistance / resolution;
        finPos = Mathf.RoundToInt((float)resolution * finPosition);
        // Debug.Log(finPos);
    }

    // Update is called once per frame
    void Update()
    {
        if (alive) {
            //if alive then move according to this method
            ResolveMovement();
        }
        else {
            //if not alive then simply ensure that the fish remains the right length
            ConstrainLength();
        }
        //the above methods change segmentPositions, apply this to the line renderer
        line.SetPositions(segmentPositions);

        //places the finn, tail etc. according to segmentPositions
        PlacePeripherals();
    }

    private void ConstrainLength(){
        for (int i = 1; i < resolution; i++)
        {
            Vector3 dir = (segmentPositions[i] - segmentPositions[i - 1]).normalized;
            Vector3 targetPos = segmentPositions[i - 1] + dir * targetDist;
            segmentPositions[i] = targetPos;
        }
    }

    private void ResolveMovement() {
        wiggleParent.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * wiggleFreq) * wiggleAmp);

        segmentPositions[0] = transform.position;

        for (int i = 1; i < resolution; i++)
        {
            // Vector3 dir = (segmentPositions[i] - segmentPositions[i - 1]).normalized;
            Vector3 dir = transform.right * Mathf.Sign(transform.localScale.x);
            Vector3 targetPos = segmentPositions[i - 1] + dir * targetDist;
            segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPos, ref segmentV[i], smoothTime + (i / trailSpeed));
        }
    }

    private void PlacePeripherals() {
        if (hasTail) {
            tail.position = segmentPositions[resolution - 1];
            tail.right = segmentPositions[resolution - 1] - segmentPositions[resolution - 2];
        }

        if (hasFins) {
            Vector2 normal = Vector2.Perpendicular(segmentPositions[finPos + 1] - segmentPositions[finPos - 1]);

            fin.position = segmentPositions[finPos];
            fin2.position = segmentPositions[finPos];
            fin.up = normal;
            fin2.up = -normal;
        }
    }
}
