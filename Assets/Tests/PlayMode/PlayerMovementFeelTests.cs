using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PlayerMovementFeelTests
{
    [UnityTest]
    public IEnumerator PlayerUsesInterpolationForSmoothRendering()
    {
        SceneManager.LoadScene("Main");
        yield return null;

        PlayerMovement movement =
            Object.FindFirstObjectByType<PlayerMovement>();
        Rigidbody2D body = movement.GetComponent<Rigidbody2D>();

        Assert.That(
            body.interpolation,
            Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
    }

    [Test]
    public void DeadZoneRemovesNoiseAndPreservesFullInput()
    {
        Assert.That(
            PlayerMovement.ApplyDeadZone(new Vector2(0.04f, 0.03f), 0.08f),
            Is.EqualTo(Vector2.zero));

        Vector2 diagonal = PlayerMovement.ApplyDeadZone(Vector2.one, 0.08f);
        Assert.That(diagonal.magnitude, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(diagonal.x, Is.EqualTo(diagonal.y).Within(0.0001f));
    }
}
