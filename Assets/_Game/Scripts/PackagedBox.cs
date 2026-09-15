using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// A packed box produced by the <see cref="PackingMachine"/>. Carries the money value of its contents. Boxes are
/// pooled: whoever is done with one calls <see cref="ReturnToPool"/> instead of destroying it.
/// </summary>
public class PackagedBox : MonoBehaviour
{
    #region Private Fields
    private Coroutine flightRoutine;
    #endregion

    #region Public Properties
    public float Value { get; set; }

    /// <summary>Time.time at which the box last landed on a stack.</summary>
    public float PlacedTime { get; set; }

    public IObjectPool<PackagedBox> Pool { get; set; }
    #endregion

    #region Public Methods
    /// <summary>Flies in an arc to the slot reserved by <paramref name="ticket"/>, then lands on the stack.</summary>
    public void FlyTo(BoxStack target, int ticket, float duration, float arcHeight)
    {
        CancelFlight();
        flightRoutine = StartCoroutine(FlyRoutine(target, ticket, duration, arcHeight));
    }

    /// <summary>Hands the box back to its pool (or destroys it when it has none), aborting any flight in progress.</summary>
    public void ReturnToPool()
    {
        CancelFlight();

        if (Pool != null)
            Pool.Release(this);
        else
            Destroy(gameObject);
    }
    #endregion

    #region Private Methods
    private void CancelFlight()
    {
        if (flightRoutine == null)
            return;

        StopCoroutine(flightRoutine);
        flightRoutine = null;
    }

    private IEnumerator FlyRoutine(BoxStack target, int ticket, float duration, float arcHeight)
    {
        Vector3 start = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Fix: the target stack can be destroyed mid-flight (truck despawned); recycle instead of leaking.
            if (target == null)
            {
                flightRoutine = null;
                ReturnToPool();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, target.GetReservedWorldPosition(ticket), t);
            position.y += arcHeight * 4f * t * (1f - t);
            transform.SetPositionAndRotation(position, Quaternion.Slerp(startRotation, target.SlotRotation, t));
            yield return null;
        }

        flightRoutine = null;

        if (target != null)
            target.Place(this, ticket);
        else
            ReturnToPool();
    }
    #endregion
}
