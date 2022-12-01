using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishSpawner : MonoBehaviour
{
    [SerializeField] private GameObject fishObj, warningObj;
    [SerializeField] private float warningTime, currTime = 0, warningEdgeBuffer;
    private float startingWarningSize;
    public float timeBtwFish, fishSpeed;
    [SerializeField] private int lives;
    [HideInInspector] public GameManager manager;
    public Color[] fishColours;
    private float camSize;
    private IKManager IK;
    public Image[] healthUI;
    public Sprite deadFishSpr;
    private int sign = 1; //keeps track of last direction fish were spawned from

    // Start is called before the first frame update
    void Start()
    {
        manager = GetComponent<GameManager>();
        IK = GetComponent<PlayerMovement>().IKManager;
        startingWarningSize = warningObj.transform.localScale.x;
    }

    // Update is called once per frame
    void Update()
    {
        if (currTime < timeBtwFish) {
            currTime += Time.deltaTime;
        }
        else {
            SpawnFish();
            currTime = 0;
        }
    }

    private void SpawnFish() {
        //determine where to instantiate the fish
        float dps = (1 / IK.stepTime) * IK.gaitLength; //Distance per second
        float distPotential = dps * 1; //dps multiplied by the time player has
        float circumference = 2 * Mathf.PI * manager.currRadius;

        Vector2 playerPos = GetComponent<PlayerMovement>().playerObj.transform.position.normalized;
        float playerAngle;
        if (playerPos.y < 0) {
            if (playerPos.x < 0) {
                playerAngle = 180 - Mathf.Asin(playerPos.y) * Mathf.Rad2Deg;
            }
            else {
                playerAngle =  360 + Mathf.Asin(playerPos.y) * Mathf.Rad2Deg;
            }
        }
        else {
            playerAngle = Mathf.Acos(playerPos.x) * Mathf.Rad2Deg;
        }

        float angleRange = (distPotential / circumference) * 360f;
        angleRange = Random.Range(0.9f * angleRange, angleRange);
        sign = (Random.Range(0f, 1f) > 0.66f) ? -sign : sign; //66% chance its the same dir as last time
        float angle = (playerAngle + (sign * angleRange)) * Mathf.Deg2Rad;
        Vector2 spawnLoc = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        float dist = Mathf.Pow(Mathf.Pow(Camera.main.orthographicSize, 2) * 2, 0.5f); //dist from centre to corner of camera square
        spawnLoc *= dist;

        //give a warning before spawning the fish
        camSize = Camera.main.orthographicSize;
        Vector2 warningLoc = CastVecToSquare(spawnLoc); //constrain the spawn such that its on the edge of the screen
        warningLoc = new Vector2(Mathf.Clamp(warningLoc.x, -camSize + warningEdgeBuffer, camSize - warningEdgeBuffer), Mathf.Clamp(warningLoc.y, -camSize + warningEdgeBuffer, camSize - warningEdgeBuffer));

        float camChange = Camera.main.orthographicSize / manager.camStartSize; //make warnings scale with camera size
        GameObject warning = Instantiate(warningObj, warningLoc, Quaternion.identity); //spawn the warning
        warning.transform.localScale = new Vector2(startingWarningSize * camChange, startingWarningSize * camChange);
        manager.Audio.Play("Alarm");
        StartCoroutine(PlaceFish(warning, spawnLoc)); //wait, then spawn the fish
    }

    IEnumerator PlaceFish(GameObject warning, Vector2 spawnLoc) {
        yield return new WaitForSeconds(warningTime);
        manager.Audio.Stop("Alarm");
        GameObject.Destroy(warning);
        
        //instantiate the fish and set initial values
        Fish fish = Instantiate(fishObj, spawnLoc, Quaternion.identity).GetComponent<Fish>();
        fish.spawner = GetComponent<FishSpawner>();
        fish.moveSpeed = fishSpeed;
        fish.gameObject.transform.up = spawnLoc;
        
        //randomly reverse it
        int fishDir = (Random.Range(0, 2) * 2 - 1);
        fish.gameObject.transform.localScale = new Vector2(fish.gameObject.transform.localScale.x * fishDir, fish.gameObject.transform.localScale.y);
        foreach (Tentacle tent in fish.gameObject.GetComponentsInChildren<Tentacle>())
        {
            tent.gameObject.transform.localScale = new Vector2(tent.gameObject.transform.localScale.x * fishDir, tent.gameObject.transform.localScale.y);
        }


        //set colour of fish
        Color color = fishColours[Random.Range(0, fishColours.Length)];
        fish.head.line.startColor = color;
        fish.head.line.endColor = color;
        fish.head.fin.GetComponent<SpriteRenderer>().color = color;
        fish.head.fin2.GetComponent<SpriteRenderer>().color = color;

        fish.tail.line.startColor = color;
        fish.tail.line.endColor = color;
        fish.tail.tail.GetComponent<SpriteRenderer>().color = color;
    }

    private Vector2 CastVecToSquare(Vector2 point) {
        float m = point.y / point.x;
        Vector2[] intercepts = new Vector2[4];

        //find all the places the line from 0,0 to the point intercepts
        intercepts[0] = new Vector2(camSize, m * camSize);
        intercepts[1] = new Vector2(-camSize, m * -camSize);
        intercepts[2] = new Vector2(camSize / m, camSize);
        intercepts[3] = new Vector2(-camSize / m, -camSize);

        //the intercept closest to the point is where the fish will pass through
        Vector2 newPoint = intercepts[0];
        float minDist = Mathf.Infinity;
        foreach (Vector2 intercept in intercepts)
        {
            if (Vector2.Distance(point, intercept) < minDist) {
                newPoint = intercept;
                minDist = Vector2.Distance(point, intercept);
            }
        }

        return newPoint;
    }

    public void LoseHealth() {
        healthUI[healthUI.Length - lives].sprite = deadFishSpr;
        healthUI[healthUI.Length - lives].GetComponent<Animator>().SetTrigger("shake");
        lives--;
        
        if (lives <= 0) {
            // Debug.Log("LOST DA GAME");
            manager.GameOver();
        }
    }
}
