using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globe
{
    private Vector2 pos;

    private float amplitude = 1.25f, frequency = 2f, radius, seedOffset;

    private const int resolution = 500;
    public Vector3[] linePoints;
    public Vector3[] noisePoints = new Vector3[resolution + 1]; //stores noise values

    public Globe(Vector2 pos, float radius, float amplitude, float frequency, float seedOffset) {
        this.pos = pos;
        this.radius = radius;
        this.amplitude = amplitude;
        this.frequency = frequency;
        this.seedOffset = seedOffset;

        linePoints = GenerateLine();
    }

    // generates points that will define the globe
    private Vector3[] GenerateLine() {
        float circumference = 2 * Mathf.PI * radius; // testing purposes: allows for drawing noise in line rather than circle

        Vector3[] lineArr = new Vector3[resolution + 1];

        // step through angles and use getPosOnCircle to find the position and add this to the array
        for (int i = 0; i <= resolution; i++)
        {
            float percent = i / (float)resolution;
            float degree = percent * 360;
            lineArr[i] = GetPosOnCircle(degree);

            // sample perlin noise 
            // does so in a circle, using the degrees generated above, this is so that the starting and ending values are the same
            float noiseVal = ((Mathf.PerlinNoise(seedOffset + Mathf.Cos(Mathf.Deg2Rad * degree) * frequency, seedOffset + Mathf.Sin(Mathf.Deg2Rad * degree) * frequency) * 2) - 1) * amplitude;
            noiseVal = Mathf.Clamp(noiseVal, 0, amplitude); // clamp between 0 and 1, removing troughs so that there are only hills

            noisePoints[i] = new Vector2((i / (float)resolution) * circumference, noiseVal); // used to draw noise in line

            Vector2 dirVec = (lineArr[i] - (Vector3)pos).normalized;
            Vector2 offsetVec = dirVec * noiseVal;
            lineArr[i] += (Vector3)offsetVec;
        }

        return lineArr;
    }

    // takes degrees out of 360 and returns the position on the circle at that rotation (not taking noise into account)
    private Vector2 GetPosOnCircle(float degrees) {
        float unitX = Mathf.Cos(Mathf.Deg2Rad * degrees);
        float unitY = Mathf.Sin(Mathf.Deg2Rad * degrees);

        Vector2 dirVec = new Vector2(unitX, unitY);
        return dirVec * radius;
    }

    // takes a position and calculates where this would be on the circle (taking noise into account)
    public static Vector2 GetPosOnCircle(Vector2 currPos, Vector2 globePos, float radius, float amplitude) {
        // get the dir from centre to the vec, then extend this to the radius + amplitude so it must be above the highest peak
        // then cast a ray to centre, where it hits is the position
        LayerMask globeMask = LayerMask.GetMask("globe");
        Vector2 dirVec = currPos - globePos;
        dirVec.Normalize();
        Vector2 rayStart = dirVec * (radius + amplitude);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, dirVec * -1, 1000, globeMask);
        return hit.point;
    }

    public static Vector2 GetNormalOnCircle(Vector2 currPos, Vector2 globePos, float radius, float amplitude) {
        LayerMask globeMask = LayerMask.GetMask("globe");
        Vector2 dirVec = currPos - globePos;
        dirVec.Normalize();
        Vector2 rayStart = dirVec * (radius + amplitude);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, dirVec * -1, 1000, globeMask);
        return hit.normal;
    }

    public Vector2 GetPos() {
        return pos;
    }
}
