using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IK : MonoBehaviour
{
    /// Chain length of bones
    public int ChainLength = 2;

    /// Target the chain should bent to
    public Transform Target, Root, Pole;

    /// Solver iterations per update
    [Header("Solver Parameters")]
    public int Iterations = 10;

    /// Distance when the solver stops
    public float Delta = 0.001f;

    public float[] BonesLength; //Target to Origin
    private float CompleteLength;
    private Vector2[] Bones;
    private Vector2[] Positions;

    public LineRenderer line; //TEMPORARY


    // Start is called before the first frame update
    void Awake()
    {
        Init();
        line.positionCount = Bones.Length;
    }

    // public Vector2 GetPosition(int index) {
    //     return Positions[index];
    // }

    public void Init()
    {
        //initial array
        Bones = new Vector2[ChainLength + 1]; //for holding existing values to be pushed to line renderer
        Positions = new Vector2[ChainLength + 1]; //for intermediary computation

        //init target
        if (Target == null)
        {
            Target = new GameObject(gameObject.name + " Target").transform;
        }

        //init root
        if (Root == null)
        {
            Root = new GameObject(gameObject.name + " Root").transform;
        }

        //init complete length
        CompleteLength = 0;
        for (var i = Bones.Length - 1; i >= 0; i--)
        {
            if (i == Bones.Length - 1)
            {
            }
            else
            {
                //mid bone
                CompleteLength += BonesLength[i];
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        ResolveIK();

        Vector3[] penis = new Vector3[Bones.Length];
        for (int i = 0; i < penis.Length; i++)
        {
            penis[i] = Bones[i];
        }
        line.SetPositions(penis);
    }

    private void ResolveIK()
    {
        if (Target == null)
            return;

        if (BonesLength.Length != ChainLength)
            Init();

        Bones[0] = Root.position;

        //Fabric

        //  root
        //  (bone0) (bonelen 0) (bone1) (bonelen 1) (bone2)...
        //   x--------------------x--------------------x---...

        //get position
        for (int i = 0; i < Bones.Length; i++)
            Positions[i] = Bones[i];


        //computation in positions array
        //1st is possible to reach?
        if (((Vector2)Target.position - Bones[0]).sqrMagnitude >= CompleteLength * CompleteLength)
        {
            //just stretch it
            Vector2 direction = ((Vector2)Target.position - Bones[0]).normalized;
            //set everything after root
            for (int i = 1; i < Positions.Length; i++)
                Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
        }
        else
        {
            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                //https://www.youtube.com/watch?v=UNoX65PRehA
                //back
                for (int i = Positions.Length - 1; i > 0; i--)
                {
                    if (i == Positions.Length - 1)
                        Positions[i] = Target.position; //set it to target
                    else
                        Positions[i] = Positions[i + 1] + (Positions[i] - Positions[i + 1]).normalized * BonesLength[i]; //set in line on distance
                }

                //forward
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + (Positions[i] - Positions[i - 1]).normalized * BonesLength[i - 1];

                //close enough?
                if ((Positions[Positions.Length - 1] - (Vector2)Target.position).sqrMagnitude < Delta * Delta)
                    break;
            }
        }

        //move towards pole
        if (Pole != null)
        {
            for (int i = 1; i < Positions.Length - 1; i++)
            {
                // generate the two possible positions of the point and pick the one closest to the pole
                Vector2 normal = (Positions[i + 1] - Positions[i - 1]).normalized;
                normal = Vector2.Perpendicular(normal);
                // Debug.DrawRay(Positions[i - 1], normal, Color.green);

                Vector2 currDir = (Positions[i] - Positions[i - 1]).normalized;
                // Debug.DrawRay(Positions[i - 1], currDir, Color.blue);

                Vector2 otherDir = Vector2.Reflect(currDir, normal).normalized;
                // Debug.DrawRay(Positions[i - 1], otherDir, Color.red);

                Vector2 otherPos = Positions[i - 1] + otherDir * (Positions[i] - Positions[i - 1]).magnitude;

                Positions[i] = (Vector2.Distance(Positions[i], Pole.position) > Vector2.Distance(otherPos, Pole.position)) ? Positions[i] = otherPos : Positions[i] = Positions[i];
            }
        }

        //set positions
        for (int i = 0; i < Bones.Length; i++)
            Bones[i] = Positions[i];
    }
}
