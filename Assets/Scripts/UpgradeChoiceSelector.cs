using System;
using System.Collections.Generic;

public sealed class UpgradeChoiceSelector
{
    private readonly Dictionary<PlayerUpgradeData, int> missedRounds = new();
    private readonly HashSet<PlayerUpgradeData> lastChoices = new();
    private readonly Random random;

    public UpgradeChoiceSelector(int? seed = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public List<PlayerUpgradeData> BuildChoices(
        IReadOnlyList<PlayerUpgradeData> weapons,
        IReadOnlyList<PlayerUpgradeData> utilities,
        bool isWeaponLevel,
        float healthRatio,
        int targetCount = 3)
    {
        AdvanceDrought(weapons);
        AdvanceDrought(utilities);

        List<PlayerUpgradeData> choices = new(targetCount);
        if (isWeaponLevel)
        {
            AddWeightedChoices(weapons, choices, targetCount, healthRatio);
        }

        AddWeightedChoices(utilities, choices, targetCount, healthRatio);
        Remember(choices);
        return choices;
    }

    public void Reset()
    {
        missedRounds.Clear();
        lastChoices.Clear();
    }

    public int MissedRounds(PlayerUpgradeData upgrade)
    {
        return upgrade != null && missedRounds.TryGetValue(upgrade, out int rounds)
            ? rounds
            : 0;
    }

    public static double CalculateWeight(
        UpgradeEffectType effectType,
        int missedRoundCount,
        bool appearedLastRound,
        float healthRatio)
    {
        double weight = 1d + Math.Min(Math.Max(missedRoundCount, 0), 6) * .4d;
        if (appearedLastRound)
        {
            weight *= .32d;
        }

        if (effectType == UpgradeEffectType.Heal)
        {
            if (healthRatio <= .35f) weight *= 3.2d;
            else if (healthRatio >= .7f) weight *= .65d;
        }
        else if (effectType == UpgradeEffectType.MaxHealth && healthRatio <= .35f)
        {
            weight *= 1.6d;
        }

        return weight;
    }

    private void AdvanceDrought(IReadOnlyList<PlayerUpgradeData> source)
    {
        for (int i = 0; i < source.Count; i++)
        {
            PlayerUpgradeData upgrade = source[i];
            if (upgrade == null) continue;
            missedRounds.TryGetValue(upgrade, out int rounds);
            missedRounds[upgrade] = Math.Min(rounds + 1, 6);
        }
    }

    private void AddWeightedChoices(
        IReadOnlyList<PlayerUpgradeData> source,
        List<PlayerUpgradeData> destination,
        int targetCount,
        float healthRatio)
    {
        List<PlayerUpgradeData> remaining = new(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            PlayerUpgradeData upgrade = source[i];
            if (upgrade != null && !destination.Contains(upgrade))
            {
                remaining.Add(upgrade);
            }
        }

        while (remaining.Count > 0 && destination.Count < targetCount)
        {
            double totalWeight = 0d;
            for (int i = 0; i < remaining.Count; i++)
            {
                totalWeight += WeightFor(remaining[i], healthRatio);
            }

            double roll = random.NextDouble() * totalWeight;
            int selectedIndex = remaining.Count - 1;
            for (int i = 0; i < remaining.Count; i++)
            {
                roll -= WeightFor(remaining[i], healthRatio);
                if (roll <= 0d)
                {
                    selectedIndex = i;
                    break;
                }
            }

            destination.Add(remaining[selectedIndex]);
            remaining.RemoveAt(selectedIndex);
        }
    }

    private double WeightFor(PlayerUpgradeData upgrade, float healthRatio)
    {
        missedRounds.TryGetValue(upgrade, out int rounds);
        return CalculateWeight(
            upgrade.EffectType,
            rounds,
            lastChoices.Contains(upgrade),
            healthRatio);
    }

    private void Remember(List<PlayerUpgradeData> choices)
    {
        lastChoices.Clear();
        for (int i = 0; i < choices.Count; i++)
        {
            PlayerUpgradeData choice = choices[i];
            lastChoices.Add(choice);
            missedRounds[choice] = 0;
        }
    }
}
