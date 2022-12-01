using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float bodyHeight, bucketDist, bucketFlexibility;
    private float moveDir = 0;
    [HideInInspector] public GameManager manager;
    [SerializeField] public GameObject playerObj, bucketObj;
    [SerializeField] private Transform shoulderRoot;
    public IKManager IKManager;

    // Start is called before the first frame update
    void Start()
    {
        bucketFlexibility *= Mathf.Deg2Rad;
    }

    // Update is called once per frame
    void Update()
    {
        float circumference = 2 * Mathf.PI * manager.currRadius;
        // moveSpeed = circumference / cycleTimeFactor;
        UpdateRotation();
        UpdateBucket();
        IKManager.LegPoles(Input.GetAxisRaw("Horizontal"));
    }

    //calculates the position that legs should try to move towards, called by IKManager
    public Vector2 CalculateIdealPos() {
        //move direction has changed
        if (moveDir != Input.GetAxisRaw("Horizontal")) {
            IKManager.ResetFeet();
        }
        moveDir = Input.GetAxisRaw("Horizontal");

        Vector2 idealPos = IKManager.FeetCentre();

        //place body according to position of feet
        playerObj.transform.position = idealPos + (idealPos - manager.startGlobe.GetPos()).normalized * bodyHeight;

        //if player is to move
        if (moveDir != 0) {
            idealPos += (Vector2)playerObj.transform.right * moveDir * IKManager.gaitLength;
        }
        
        idealPos = Globe.GetPosOnCircle(idealPos, manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp);
        return idealPos;
    }

    private void UpdateBucket() {
        //find direction to mouse
        Vector2 mouseDir = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseDir -= (Vector2)shoulderRoot.position;
        float armExtension = Mathf.Clamp(mouseDir.magnitude, 0, bucketDist);
        mouseDir.Normalize();

        //place bucket at a point along this direction by armExtension distance
        bucketObj.transform.position = (Vector2)shoulderRoot.position + (mouseDir * armExtension);

        //direction from globe to bucket, determines rotation of bucket
        Vector2 dir = (Vector2)bucketObj.transform.position - manager.startGlobe.GetPos().normalized;
        bucketObj.transform.up = dir;

        //clamp the bucket to a certain angle from the player
        Vector2 bucketPos = (bucketObj.transform.position - shoulderRoot.position).normalized; //bucket dir from player
        float bucketAngle = Mathf.Deg2Rad * Vector2.SignedAngle(shoulderRoot.up, bucketPos); //angle from player's up to bucket
        bucketAngle = Mathf.Clamp(bucketAngle, -bucketFlexibility, bucketFlexibility); //clamp according to restraints
        Vector2 bucketLocal = new Vector2(Mathf.Cos(bucketAngle), Mathf.Sin(bucketAngle)); //transfer to vector
        
        //vector direction is currently in LOCAL space, i.e. its in reference to the players up transform
        //apply transformations to transfer from LOCAL to WORLD space
        //the transform.up is (1, 0) in LOCAL space, but something else in WORLD space, multiply to transfer to WORLD space
        Vector2 bucketWorld = bucketLocal.x * shoulderRoot.up + bucketLocal.y * (shoulderRoot.right * -1);
        bucketObj.transform.position = (Vector2)shoulderRoot.position + (bucketWorld * armExtension); //place bucket
        

        //TODO: IMPLEMENT TUNE BUCKET METHOD
        //this will make slight changes to the buckets position and orientation after placing the rough position above
        //this will make the bucket rotate and move a little towards the fish when its nearby, like an auto aim
        //maybe
    }

    // updates position moving left by moveSpeed if true, right if false
    private void UpdateRotation() {
        // set rotation
        Vector2 dirVec = (Vector2)playerObj.transform.position - manager.startGlobe.GetPos();
        playerObj.transform.up = dirVec;

        // playerSprite.transform.Rotate(Vector3.forward * IKManager.FindBodyRotation());
        // Debug.Log(IKManager.FindBodyRotation());
        playerObj.transform.eulerAngles = new Vector3(0,0,playerObj.transform.eulerAngles.z + IKManager.FindBodyRotation());

        // // set position
        // Debug.DrawRay(playerObj.transform.position, playerObj.transform.right * dir);
        // playerObj.transform.position += playerObj.transform.right * (dir * moveSpeed * Time.deltaTime);
        
        // playerObj.transform.position = Globe.GetPosOnCircle(playerObj.transform.position, manager.startGlobe.GetPos(), manager.currRadius, manager.currAmp) + (Vector2)(playerObj.transform.up * bodyHeight);
    }
}
