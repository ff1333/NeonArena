using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class UpgradeChoiceSelectorTests
{
    private readonly List<PlayerUpgradeData> created = new();

    [Test]
    public void RecentChoiceIsSuppressedAndMissedChoiceGainsWeight()
    {
        double baseWeight = UpgradeChoiceSelector.CalculateWeight(
            UpgradeEffectType.Damage, 0, false, 1f);
        double repeatedWeight = UpgradeChoiceSelector.CalculateWeight(
            UpgradeEffectType.Damage, 0, true, 1f);
        double droughtWeight = UpgradeChoiceSelector.CalculateWeight(
            UpgradeEffectType.Damage, 5, false, 1f);

        Assert.That(repeatedWeight, Is.LessThan(baseWeight));
        Assert.That(droughtWeight, Is.GreaterThan(baseWeight));
    }

    [Test]
    public void LowHealthStronglyRaisesHealWeight()
    {
        double healthy = UpgradeChoiceSelector.CalculateWeight(
            UpgradeEffectType.Heal, 0, false, .9f);
        double critical = UpgradeChoiceSelector.CalculateWeight(
            UpgradeEffectType.Heal, 0, false, .2f);

        Assert.That(critical, Is.GreaterThan(healthy * 4f));
    }

    [Test]
    public void ChoicesAreUniqueAndUnseenOptionsBuildDrought()
    {
        PlayerUpgradeData damage = Create(UpgradeEffectType.Damage);
        PlayerUpgradeData fireRate = Create(UpgradeEffectType.FireRate);
        PlayerUpgradeData moveSpeed = Create(UpgradeEffectType.MoveSpeed);
        PlayerUpgradeData maxHealth = Create(UpgradeEffectType.MaxHealth);
        var utilities = new List<PlayerUpgradeData>
        {
            damage, fireRate, moveSpeed, maxHealth
        };
        var selector = new UpgradeChoiceSelector(17);

        List<PlayerUpgradeData> choices = selector.BuildChoices(
            new List<PlayerUpgradeData>(), utilities, false, 1f);

        Assert.That(choices, Has.Count.EqualTo(3));
        Assert.That(new HashSet<PlayerUpgradeData>(choices), Has.Count.EqualTo(3));
        PlayerUpgradeData omitted = utilities.Find(item => !choices.Contains(item));
        Assert.That(selector.MissedRounds(omitted), Is.EqualTo(1));
        foreach (PlayerUpgradeData choice in choices)
        {
            Assert.That(selector.MissedRounds(choice), Is.Zero);
        }
    }

    [TearDown]
    public void TearDown()
    {
        foreach (PlayerUpgradeData upgrade in created)
        {
            Object.DestroyImmediate(upgrade);
        }
        created.Clear();
    }

    private PlayerUpgradeData Create(UpgradeEffectType effectType)
    {
        PlayerUpgradeData upgrade = ScriptableObject.CreateInstance<PlayerUpgradeData>();
        typeof(PlayerUpgradeData)
            .GetField("effectType", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(upgrade, effectType);
        created.Add(upgrade);
        return upgrade;
    }
}
