using UnityEngine;

/// <summary>
/// Creates atmospheric fog particle effect inside the maze.
/// Follows the player to create a sense of endless dark corridors.
/// </summary>
public class MazeFogEffect : MonoBehaviour
{
    private ParticleSystem fogParticles;
    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        CreateFogParticles();
    }

    void CreateFogParticles()
    {
        fogParticles = gameObject.AddComponent<ParticleSystem>();

        var main = fogParticles.main;
        main.startSize = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(0.1f, 0.1f, 0.12f, 0.05f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f;

        var emission = fogParticles.emission;
        emission.rateOverTime = 15f;

        var shape = fogParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 3f, 30f);

        var sizeOverLifetime = fogParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = fogParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.1f, 0.1f, 0.12f), 0f),
                new GradientColorKey(new Color(0.08f, 0.08f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.05f, 0.3f),
                new GradientAlphaKey(0.03f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Make particle material
        var particleRenderer = GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            particleRenderer.material.color = new Color(0.1f, 0.1f, 0.12f, 0.05f);
        }
    }

    void Update()
    {
        if (player != null)
        {
            transform.position = player.position;
        }
    }
}
