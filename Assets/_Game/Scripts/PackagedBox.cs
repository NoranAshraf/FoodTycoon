using System.Collections;
using UnityEngine;

/// <summary>A packed box produced by the <see cref="PackingMachine"/>. Carries the money value of its contents.</summary>
public class PackagedBox : MonoBehaviour
{
    #region Public Properties
    public float Value { get; set; }

    /// <summary>Time.time at which the box last landed on a stack.</summary>
    public float PlacedTime { get; set; }
    #endregion

    #region Public Methods
    /// <summary>Flies in an arc to the slot reserved by <paramref name="ticket"/>, then lands on the stack.</summary>
    public void FlyTo(BoxStack target, int ticket, float duration, float arcHeight)
    {
        StartCoroutine(FlyRoutine(target, ticket, duration, arcHeight));
    }
    #endregion

    #region Private Methods
    private IEnumerator FlyRoutine(BoxStack target, int ticket, float duration, float arcHeight)
    {
        Vector3 start = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, target.GetReservedWorldPosition(ticket), t);
            position.y += arcHeight * 4f * t * (1f - t);
            transform.SetPositionAndRotation(position, Quaternion.Slerp(startRotation, target.SlotRotation, t));
            yield return null;
        }

        target.Place(this, ticket);
    }
    #endregion
}
