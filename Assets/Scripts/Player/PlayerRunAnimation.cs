using UnityEngine;

/// <summary>
/// Simple run-cycle animation for the stylized player body.
/// Animates limbs and body bob based on movement speed.
/// </summary>
public class PlayerRunAnimation : MonoBehaviour
{
    public float walkCycleSpeed = 7f;
    public float sprintCycleSpeed = 11f;
    public float crouchCycleSpeed = 4.5f;
    public float walkSwingAmount = 28f;
    public float sprintSwingAmount = 45f;
    public float crouchSwingAmount = 14f;
    public float bodyBobAmount = 0.06f;
    public float bodyBobSpeed = 2f;

    private PlayerController playerController;
    private Transform torso;
    private Transform head;
    private Transform hair;
    private Transform hairBack;
    private Transform armLeft;
    private Transform armRight;
    private Transform legLeft;
    private Transform legRight;
    private Transform scarf;
    private Transform sword;

    private Vector3 visualRootStartLocalPosition;
    private Quaternion torsoDefaultRotation;
    private Quaternion headDefaultRotation;
    private Quaternion hairDefaultRotation;
    private Quaternion hairBackDefaultRotation;
    private Quaternion armLeftDefaultRotation;
    private Quaternion armRightDefaultRotation;
    private Quaternion legLeftDefaultRotation;
    private Quaternion legRightDefaultRotation;
    private Quaternion scarfDefaultRotation;
    private Quaternion swordDefaultRotation;

    private float cycleTimer;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        visualRootStartLocalPosition = transform.localPosition;

        torso = transform.Find("Torso");
        head = transform.Find("Head");
        hair = transform.Find("Hair");
        hairBack = transform.Find("HairBack");
        armLeft = transform.Find("ArmLeft");
        armRight = transform.Find("ArmRight");
        legLeft = transform.Find("LegLeft");
        legRight = transform.Find("LegRight");
        scarf = transform.Find("Scarf");
        sword = transform.Find("Sword");

        if (torso != null) torsoDefaultRotation = torso.localRotation;
        if (head != null) headDefaultRotation = head.localRotation;
        if (hair != null) hairDefaultRotation = hair.localRotation;
        if (hairBack != null) hairBackDefaultRotation = hairBack.localRotation;
        if (armLeft != null) armLeftDefaultRotation = armLeft.localRotation;
        if (armRight != null) armRightDefaultRotation = armRight.localRotation;
        if (legLeft != null) legLeftDefaultRotation = legLeft.localRotation;
        if (legRight != null) legRightDefaultRotation = legRight.localRotation;
        if (scarf != null) scarfDefaultRotation = scarf.localRotation;
        if (sword != null) swordDefaultRotation = sword.localRotation;
    }

    void Update()
    {
        if (playerController == null) return;

        Vector3 horizontalVelocity = new Vector3(playerController.Velocity.x, 0f, playerController.Velocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed < 0.05f)
        {
            ResetPose();
            return;
        }

        bool isSprinting = playerController.IsSprinting;
        bool isCrouching = playerController.IsCrouching;

        float cycleSpeed = isCrouching ? crouchCycleSpeed : (isSprinting ? sprintCycleSpeed : walkCycleSpeed);
        float swingAmount = isCrouching ? crouchSwingAmount : (isSprinting ? sprintSwingAmount : walkSwingAmount);
        float normalizedSpeed = Mathf.Clamp01(speed / 6f);

        cycleTimer += Time.deltaTime * cycleSpeed * Mathf.Lerp(0.8f, 1.4f, normalizedSpeed);
        float swing = Mathf.Sin(cycleTimer) * swingAmount;
        float oppositeSwing = Mathf.Sin(cycleTimer + Mathf.PI) * swingAmount;
        float bob = Mathf.Sin(cycleTimer * bodyBobSpeed) * bodyBobAmount * Mathf.Lerp(0.5f, 1f, normalizedSpeed);

        Vector3 pos = transform.localPosition;
        pos.y = visualRootStartLocalPosition.y + bob;
        transform.localPosition = pos;

        if (torso != null)
            torso.localRotation = torsoDefaultRotation * Quaternion.Euler(Mathf.Sin(cycleTimer * 2f) * 4f, 0f, Mathf.Sin(cycleTimer) * 2f);
        if (head != null)
            head.localRotation = headDefaultRotation * Quaternion.Euler(Mathf.Sin(cycleTimer * 2f) * -2f, 0f, 0f);
        if (hair != null)
            hair.localRotation = hairDefaultRotation * Quaternion.Euler(Mathf.Sin(cycleTimer * 2f) * -1f, 0f, 0f);
        if (hairBack != null)
            hairBack.localRotation = hairBackDefaultRotation * Quaternion.Euler(Mathf.Sin(cycleTimer * 2f) * -1f, 0f, 0f);
        if (armLeft != null)
            armLeft.localRotation = armLeftDefaultRotation * Quaternion.Euler(swing, 0f, 12f);
        if (armRight != null)
            armRight.localRotation = armRightDefaultRotation * Quaternion.Euler(oppositeSwing, 0f, -12f);
        if (legLeft != null)
            legLeft.localRotation = legLeftDefaultRotation * Quaternion.Euler(oppositeSwing * 0.85f, 0f, 4f);
        if (legRight != null)
            legRight.localRotation = legRightDefaultRotation * Quaternion.Euler(swing * 0.85f, 0f, -4f);
        if (scarf != null)
            scarf.localRotation = scarfDefaultRotation * Quaternion.Euler(Mathf.Sin(cycleTimer) * 8f, 0f, Mathf.Sin(cycleTimer * 1.3f) * 4f);
        if (sword != null)
            sword.localRotation = swordDefaultRotation * Quaternion.Euler(Mathf.Sin(cycleTimer) * 4f, 0f, Mathf.Sin(cycleTimer) * 2f);
    }

    void ResetPose()
    {
        cycleTimer = 0f;
        transform.localPosition = visualRootStartLocalPosition;

        if (torso != null) torso.localRotation = torsoDefaultRotation;
        if (head != null) head.localRotation = headDefaultRotation;
        if (hair != null) hair.localRotation = hairDefaultRotation;
        if (hairBack != null) hairBack.localRotation = hairBackDefaultRotation;
        if (armLeft != null) armLeft.localRotation = armLeftDefaultRotation;
        if (armRight != null) armRight.localRotation = armRightDefaultRotation;
        if (legLeft != null) legLeft.localRotation = legLeftDefaultRotation;
        if (legRight != null) legRight.localRotation = legRightDefaultRotation;
        if (scarf != null) scarf.localRotation = scarfDefaultRotation;
        if (sword != null) sword.localRotation = swordDefaultRotation;
    }
}
