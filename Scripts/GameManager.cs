using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Vector2 globeCentre = Vector2.zero;
    [SerializeField] private float startRadius, endRadius, startAmp, endAmp, startFreq, endFreq, transformTime, lineThickMulti;
    [SerializeField] private float startTimeBtwFish, endTimeBtwFish, startFishSpeed, endFishSpeed;
    public float camStartSize, camEndSize;
    private float seedOffset, startTime, lineThickness;
    [HideInInspector] public Globe startGlobe, endGlobe;
    [HideInInspector] public float currRadius, currAmp;
    private PolygonCollider2D globeCollider;
    private PlayerMovement playerScr;
    private LineRenderer line;
    public GameObject paperObj;
    public TMP_Text scoreText, endScoreText, highScoreText;
    public Animator scoreAnim;
    public AudioController Audio;
    public GameObject pauseMenu, gameOverMenu;
    private FishSpawner spawner;
    private bool paused = false, gameOver = false;
    public AnimationCurve difficultyCurve;
    // private int TEST = 0, start, end;

    void Awake() {
        Audio = AudioController.instance;
    }

    // Start is called before the first frame update
    void Start()
    {
        spawner = GetComponent<FishSpawner>();

        scoreText.text = "0";

        //generate starting and end globes
        seedOffset = Random.Range(0f, 1000f);
        startGlobe = new Globe(globeCentre, startRadius, startAmp, startFreq, seedOffset);
        endGlobe = new Globe(globeCentre, endRadius, endAmp, endFreq, seedOffset + 1000);


        playerScr = GetComponent<PlayerMovement>();
        playerScr.manager = GetComponent<GameManager>();
        globeCollider = GetComponent<PolygonCollider2D>();
        line = GetComponent<LineRenderer>();
        line.positionCount = startGlobe.linePoints.Length;
        startTime = Time.time;
        currRadius = startRadius; 
        // camStartSize = Camera.main.orthographicSize;
        lineThickness = line.startWidth;
    }

    // Update is called once per frame
    void Update()
    {
        InterpolatePoints();

        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) { 
            Pause();            
        }

        if (gameOver && Input.GetKeyDown(KeyCode.Space)) {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetKeyDown(KeyCode.X) && (paused || gameOver)) {
            Application.Quit();
        }
    }

    private void Pause() {
        if (!paused) {
                pauseMenu.SetActive(true);
                Audio.Stop("Alarm");
                paused = true;
                Time.timeScale = 0f;
            }
            else {
                pauseMenu.SetActive(false);
                paused = false;
                Time.timeScale = 1f;
            }
    }

    void OnApplicationFocus(bool hasFocus) {
        if (!hasFocus && !paused) { //just tabbed out, pause
            pauseMenu.SetActive(true);
            Audio.Stop("Alarm");
            paused = true;
            Time.timeScale = 0f;
        }
        // Debug.Log(hasFocus);
    }

    public void GameOver() {
        gameOver = true;
        Audio.Stop("Alarm");
        Time.timeScale = 0f;
        gameOverMenu.SetActive(true);
        endScoreText.text = "Score: " + scoreText.text;

        int highscore = PlayerPrefs.GetInt("highscore", 0);
        int score = int.Parse(endScoreText.text);
        if (score > highscore) {
            PlayerPrefs.SetInt("highscore", score);
            highscore = score;
        }
        highScoreText.text = "Highscore: " + highscore.ToString();
    }
    
    // draws to the linerenderer and collider based on an interpolation between the starting globe and ending globe over a time period
    private void InterpolatePoints() {
        // calculate t
        float timePassed = Time.time - startTime;
        float t = timePassed / transformTime;
        // float t = 0.25f;
        // Debug.Log(t);

        Vector2[] currPoints = new Vector2[startGlobe.linePoints.Length]; //stores the points of the line
        for (int i = 0; i < startGlobe.linePoints.Length; i++)
        {
            Vector2 currPoint = Vector2.Lerp(startGlobe.linePoints[i], endGlobe.linePoints[i], t); //interpolate
            currPoints[i] = currPoint;
            line.SetPosition(i, currPoint); // set this point in the line for the renderer                
        }

        // add a little buffer for the poly collider so that its on the surface of the line rather than the middle
        //i.e. take thickness into account
        Vector2[] collPoints = new Vector2[currPoints.Length]; //stores the points of the collider
        currPoints.CopyTo(collPoints, 0);  

        for (int i = 0; i < collPoints.Length; i++)
        {
            //calculate normal of point and extrude it such that it is on the edge of the line rather than middle
            Vector2 tangent = currPoints[(i == 0) ? currPoints.Length - 1: i - 1] - currPoints[(i == currPoints.Length - 1) ? 0: i + 1];
            Vector2 normal = Vector2.Perpendicular(tangent.normalized);
            collPoints[i] += normal * line.startWidth * 0.5f;
        }
        globeCollider.points = collPoints; 

        // update other variables that rely on the current state of the globe/ the t value
        //make the line width scale with planet size
        currRadius = Mathf.Lerp(startRadius, endRadius, t);
        float multiplier = currRadius / startRadius;
        line.startWidth = Mathf.Clamp(lineThickness * multiplier * lineThickMulti,lineThickness, Mathf.Infinity);
        line.endWidth = Mathf.Clamp(lineThickness * multiplier * lineThickMulti,lineThickness, Mathf.Infinity);

        //make camera scale with planet size
        float oldCamSize = Camera.main.orthographicSize;
        Camera.main.orthographicSize = Mathf.Lerp(camStartSize, camEndSize, t);
        paperObj.transform.localScale *= (Camera.main.orthographicSize / oldCamSize); //scale background too
        currAmp = Mathf.Lerp(startAmp, endAmp, t); //accessed by other scripts to use GetPosOnCircle method in Globe

        //parameters related to difficulty scale over time
        // Debug.Log("t: "+ t + ", d: " + difficultyCurve.Evaluate(t));
        spawner.timeBtwFish = Mathf.Lerp(startTimeBtwFish, endTimeBtwFish, difficultyCurve.Evaluate(t));
        spawner.fishSpeed = Mathf.Lerp(startFishSpeed, endFishSpeed, difficultyCurve.Evaluate(t));
    }


    //bunch of code that i was using to find the peak over a certain area on the globe
    //this was going to be used for making the step heigher to clear little hills
    //ended up not really making sense
    //so the master function is never called from anywhere

    //used to create the arc that the foot steps over
    //in index 0 of float[] returns the heighest y value of the curve defined by noise that makes the globe
    //in index 1 returns the min height out of start and end
    public float[] GetPeakAndMinInRange(Vector2 start, Vector2 end) {
        float[] results = new float[2];

        int startI = GetLineIndexByPosition(start);
        int endI = GetLineIndexByPosition(end);

        results[1] = Mathf.Min(PeakHeightByIndex(startI), PeakHeightByIndex(endI));

        float maxHeight = -Mathf.Infinity;
        foreach (int index in IndicesInRange(startI, endI))
        {
            maxHeight = (PeakHeightByIndex(index) > maxHeight) ? maxHeight = PeakHeightByIndex(index) : maxHeight;
        }
        results[0] = maxHeight;
        return results;
    }

    //takes a position in the world and returns the index of the point that is closest to it on the globe
    public int GetLineIndexByPosition(Vector2 pos) {
        float minDist = Mathf.Infinity;
        int index = -1;
        for (int i = 0; i < startGlobe.linePoints.Length; i++)
        {
            Vector2 currPoint = line.GetPosition(i);
            if (Vector2.Distance(currPoint, pos) < minDist) {
                minDist = Vector2.Distance(currPoint, pos);
                index = i;
            }     
        }
        return index;
    }

    //takes an index corresponding to a point on the globe/line renderer, and returns the height of the curve defined by noise at that i
    public float PeakHeightByIndex(int i) {
        // calculate t
        float timePassed = Time.time - startTime;
        float t = timePassed / transformTime;
        // t = 0.25f;

       return Mathf.Lerp(startGlobe.noisePoints[i].y, endGlobe.noisePoints[i].y, t);
    }

    //returns all indices within range start : end, has to cycle
    public int[] IndicesInRange(int start, int end) {
        int store = start;
        start = Mathf.Min(start, end);
        end = Mathf.Max(store, end);

        int[] forwardRange = new int[(end - start) + 1];
        for (int i = start, j = 0; i <= end; i++, j++)
        {
            forwardRange[j] = i;
        }

        int length = startGlobe.linePoints.Length - 1;
        int[] backRange = new int[(length - end) + start + 2];
        int counter = 0;
        for (int i = end; i <= length; i++, counter++)
        {
            backRange[counter] = i;
        }

        for (int i = 0; i <= start; i++, counter++)
        {
            backRange[counter] = i;
        }
        
        return (forwardRange.Length < backRange.Length) ? forwardRange : backRange;
    }

    //takes in a position (e.g. of a foot) and returns the normal at that position on the globe
    public Vector2 NormalAtPosition(Vector2 pos) {
        int globeIndex = GetLineIndexByPosition(pos);

        Vector2 last, next;
        if (globeIndex == line.positionCount - 1) {
            next = line.GetPosition(0);
            last = line.GetPosition(globeIndex - 1);
        }
        else if (globeIndex == 0) {
            next = line.GetPosition(globeIndex + 1);
            last = line.GetPosition(line.positionCount - 1);
        }
        else {
            next = line.GetPosition(globeIndex + 1);
            last = line.GetPosition(globeIndex - 1);
        }

        return Vector2.Perpendicular((next - last).normalized);
    }
}