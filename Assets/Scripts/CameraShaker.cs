using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public Vector3 ShakeValue { get; private set; }
    private Coroutine ShakeGenerator = null;

    public void GenerateShake(float duration, float magnitude, float violence)
    {
        if (ShakeGenerator != null)
            StopCoroutine(ShakeGenerator);

        ShakeGenerator = StartCoroutine(C_SetShakeValue(duration, magnitude, violence));
    }

    private IEnumerator C_SetShakeValue(float duration, float magnitude, float violence)
    {
        //Used to flip the random
        Vector2 flipMultiplier = Vector2.one;
        /* Use 1 as a base for violence. More violence will
        * cause the upgradeFrequency to be a lower value, thus quicker. */
        float updateFrequency = 1f / violence;
        //When the effect should end.
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            //Generate a random position.
            Vector2 position = new Vector2(
                Random.Range(0f, magnitude),
                Random.Range(0f, magnitude)
                ) * flipMultiplier;

            ShakeValue = position;

            //Wait until the update frequency ends.
            yield return new WaitForSeconds(updateFrequency);
            //Flip multiplier.
            flipMultiplier *= -1f;
        }

        //Reset shake value and nullify coroutine.
        ShakeValue = Vector3.zero;
        ShakeGenerator = null;
    }
}
