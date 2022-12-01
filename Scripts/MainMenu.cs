using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private Globe currGlobe, nextGlobe;

    [Header("Globe parameters")]
    public Vector2 globePos;
    public float radius;
    public float amplitude;
    public float frequency;
    public LineRenderer line;

    [Header("Camera parameters")]
    public Transform cam;
    public float rotSpeed;
    private int rotDir = 1;

    [Header("Transforming parameters")]
    public float transformTime;
    private float startTime;

    // Start is called before the first frame update
    void Start()
    {
        currGlobe = new Globe(globePos, radius, amplitude, frequency, Random.Range(0f, 1000f));
        nextGlobe = new Globe(globePos, radius, amplitude, frequency, Random.Range(0f, 1000f));
        line.positionCount = currGlobe.linePoints.Length;
        line.SetPositions(currGlobe.linePoints);

        rotDir = (Random.Range(0, 2) * 2) - 1;

        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        cam.Rotate(0, 0, rotSpeed * Time.deltaTime * rotDir);
        InterpolatePoints();

        if (Input.GetKeyDown(KeyCode.Space)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    private void InterpolatePoints() {
        //calculate t
        float timePassed = Time.time - startTime;
        float t = timePassed / transformTime;

        if (t >= 1) { //set a new globe
            startTime = Time.time;
            currGlobe = nextGlobe;
            nextGlobe = new Globe(globePos, radius, amplitude, frequency, Random.Range(0f, 1000f));
        }
        else { //interpolate between the 2 globes
            for (int i = 0; i < currGlobe.linePoints.Length; i++)
            {
                line.SetPosition(i, Vector2.Lerp(currGlobe.linePoints[i], nextGlobe.linePoints[i], t)); // set this point in the line for the renderer                
            }
        }
    }
}
