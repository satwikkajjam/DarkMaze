using UnityEngine;

/// <summary>
/// The Guiding Eye - a floating spectral eye entity that helps the player
/// locate trinkets 1-13. It floats ahead of the player, subtly pointing
/// toward the next nearest uncollected trinket.
/// Does NOT guide to the 14th (final) trinket - that one must be found
/// through the sky color change mechanic.
/// </summary>
public class GuidingEye : MonoBehaviour
{
    [Header("Movement")]
    public float floatHeight = 2.5f;
    public float followDistance = 5f;
    public float moveSpeed = 3f;
    public float bobAmplitude = 0.3f;
    public float bobFrequency = 1.5f;

    [Header("Appearance")]
    public float eyeSize = 0.3f;
    public Color eyeColor = new Color(0.5f, 0.8f, 1f);
    public float glowIntensity = 2f;
    public float pulseSpeed = 2f;

    [Header("Guidance")]
    public float guidanceStrength = 0.6f;
    public float maxGuideDistance = 50f;
    public float nearTrinketDistance = 8f;
    public float hideWhenEnemyNear = 10f;

    private Transform player;
    private TrinketManager trinketManager;
    private Light eyeLight;
    private Renderer eyeRenderer;
    private Material eyeMaterial;
    private GameObject pupil;
    private float currentAlpha = 1f;
    private Vector3 targetPosition;
    private Trinket nearestTrinket;
    private bool isHiding;
    private ParticleSystem particles;

    void Start()
    {
        CreateEyeVisual();
        CreateParticles();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        trinketManager = FindFirstObjectByType<TrinketManager>();
    }

    void CreateEyeVisual()
    {
        // Main eye sphere
        GameObject eyeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeSphere.name = "EyeBall";
        eyeSphere.transform.SetParent(transform);
        eyeSphere.transform.localPosition = Vector3.zero;
        eyeSphere.transform.localScale = Vector3.one * eyeSize;

        Destroy(eyeSphere.GetComponent<SphereCollider>());

        eyeRenderer = eyeSphere.GetComponent<Renderer>();
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        eyeMaterial = new Material(shader);
        eyeMaterial.color = eyeColor;
        eyeMaterial.EnableKeyword("_EMISSION");
        eyeMaterial.SetColor("_EmissionColor", eyeColor * glowIntensity);
        eyeMaterial.SetFloat("_Mode", 3); // Transparent
        eyeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        eyeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        eyeMaterial.SetInt("_ZWrite", 0);
        eyeMaterial.DisableKeyword("_ALPHATEST_ON");
        eyeMaterial.EnableKeyword("_ALPHABLEND_ON");
        eyeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        eyeMaterial.renderQueue = 3000;
        eyeRenderer.material = eyeMaterial;

        // Pupil
        pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(transform);
        pupil.transform.localPosition = Vector3.forward * (eyeSize * 0.4f);
        pupil.transform.localScale = Vector3.one * (eyeSize * 0.4f);
        Destroy(pupil.GetComponent<SphereCollider>());

        Material pupilMat = new Material(shader);
        pupilMat.color = new Color(0.1f, 0.1f, 0.2f);
        pupilMat.EnableKeyword("_EMISSION");
        pupilMat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 1f));
        pupil.GetComponent<Renderer>().material = pupilMat;

        // Eye light
        eyeLight = gameObject.AddComponent<Light>();
        eyeLight.type = LightType.Point;
        eyeLight.color = eyeColor;
        eyeLight.intensity = glowIntensity;
        eyeLight.range = 6f;
    }

    void CreateParticles()
    {
        GameObject particleObj = new GameObject("EyeParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;

        particles = particleObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startSize = 0.05f;
        main.startLifetime = 1f;
        main.startSpeed = 0.3f;
        main.startColor = eyeColor;
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 10f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = eyeSize;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        FindNearestTrinket();
        CheckEnemyProximity();
        UpdatePosition();
        UpdateVisuals();
        LookAtTarget();
    }

    void FindNearestTrinket()
    {
        if (trinketManager == null) return;

        Trinket[] trinkets = FindObjectsByType<Trinket>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        nearestTrinket = null;

        foreach (var trinket in trinkets)
        {
            // Skip final trinket - no guidance for it
            if (trinket.isFinalTrinket) continue;
            if (trinket.IsCollected) continue;

            float dist = Vector3.Distance(player.position, trinket.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearestTrinket = trinket;
            }
        }
    }

    void CheckEnemyProximity()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        isHiding = false;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < hideWhenEnemyNear)
            {
                isHiding = true;
                break;
            }
        }
    }

    void UpdatePosition()
    {
        Vector3 desiredPos;

        if (nearestTrinket != null && !isHiding)
        {
            // Position between player and trinket, biased toward player
            Vector3 dirToTrinket = (nearestTrinket.transform.position - player.position).normalized;
            desiredPos = player.position + dirToTrinket * followDistance;
            desiredPos.y = player.position.y + floatHeight;
        }
        else
        {
            // Float above and ahead of player
            desiredPos = player.position + player.forward * followDistance * 0.5f;
            desiredPos.y = player.position.y + floatHeight;
        }

        // Bob animation
        desiredPos.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;

        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPos, moveSpeed * Time.deltaTime);
    }

    void UpdateVisuals()
    {
        float targetAlpha = isHiding ? 0.1f : 1f;
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 3f);

        // Pulse effect
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.2f;
        float intensity = glowIntensity * pulse * currentAlpha;

        Color col = eyeColor;
        col.a = currentAlpha;
        eyeMaterial.color = col;
        eyeMaterial.SetColor("_EmissionColor", eyeColor * intensity);

        eyeLight.intensity = intensity;

        // Pulse faster when near a trinket
        if (nearestTrinket != null)
        {
            float distToTrinket = Vector3.Distance(player.position, nearestTrinket.transform.position);
            if (distToTrinket < nearTrinketDistance)
            {
                float urgency = 1f - (distToTrinket / nearTrinketDistance);
                eyeLight.intensity = intensity * (1f + urgency * 2f);
                float fastPulse = Mathf.Sin(Time.time * (pulseSpeed + urgency * 8f));
                eyeLight.intensity *= (1f + fastPulse * 0.3f);
            }
        }
    }

    void LookAtTarget()
    {
        if (nearestTrinket != null && !isHiding)
        {
            // Look toward the nearest trinket
            Vector3 lookDir = (nearestTrinket.transform.position - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);
        }
        else if (player != null)
        {
            // Look at player when idle
            Vector3 lookDir = (player.position + Vector3.up * 1.5f - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);
        }
    }
}
