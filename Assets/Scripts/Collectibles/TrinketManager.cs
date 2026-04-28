using UnityEngine;

/// <summary>
/// Manages the 14 trinkets that must be collected.
/// The final (14th) trinket is hidden with no direct guidance - only
/// the sky color changes as the player approaches it.
/// </summary>
public class TrinketManager : MonoBehaviour
{
    public int totalTrinkets = 14;
    public int requiredTrinketsToWin = 14;
    public int collectedCount;

    private Trinket finalTrinket;
    private bool winTriggered;

    public System.Action<int, int> OnTrinketCountChanged;
    public System.Action OnAllTrinketsCollected;
    public System.Action<Trinket> OnTrinketPickedUp;

    public int CollectedCount => collectedCount;
    public int TotalTrinkets => totalTrinkets;
    public int RequiredTrinketsToWin => requiredTrinketsToWin;
    public bool AllCollected => collectedCount >= requiredTrinketsToWin;
    public Trinket FinalTrinket => finalTrinket;

    public void SetFinalTrinket(Trinket trinket)
    {
        finalTrinket = trinket;
    }

    public void OnTrinketCollected(Trinket trinket)
    {
        collectedCount++;
        OnTrinketPickedUp?.Invoke(trinket);
        OnTrinketCountChanged?.Invoke(collectedCount, totalTrinkets);

        if (!winTriggered && collectedCount >= requiredTrinketsToWin)
        {
            winTriggered = true;
            OnAllTrinketsCollected?.Invoke();
            GameManager.Instance?.OnAllTrinketsFound();
            GameManager.Instance?.OnPlayerEscaped();
        }
    }

    public float GetDistanceToFinalTrinket(Vector3 position)
    {
        if (finalTrinket == null || finalTrinket.IsCollected) return float.MaxValue;
        return Vector3.Distance(position, finalTrinket.transform.position);
    }

    public void ResetTrinkets()
    {
        collectedCount = 0;
        winTriggered = false;
    }
}
