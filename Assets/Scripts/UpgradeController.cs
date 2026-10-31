using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private PlayerUpgradeApplier applier;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private Image[] weaponIcons;
    [SerializeField] private PlayerUpgradeData[] availableUpgrades;

    private readonly PlayerUpgradeData[] currentChoices =
        new PlayerUpgradeData[3];
    private Action selectionCompleted;
    private bool isStartingWeaponSelection;
    private int choicesOpenedFrame = -1;
    private readonly UpgradeChoiceSelector choiceSelector = new();

    public bool ExternalChoiceOpen { get; set; }
    public bool IsOpen => ExternalChoiceOpen ||
        (upgradePanel != null && upgradePanel.activeSelf);

    private void Awake()
    {
        foreach (var text in upgradePanel.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == "TitleText") text.gameObject.SetActive(false);
        upgradePanel.SetActive(false);

        for (int i = 0; i < buttons.Length; i++)
        {
            int capturedIndex = i;
            buttons[i].onClick.AddListener(() => Select(capturedIndex));
        }
    }

    private void OnEnable()
    {
        progress.LevelUpRequested += ShowChoices;
    }

    private void OnDisable()
    {
        progress.LevelUpRequested -= ShowChoices;
    }

    public bool ShowStartingWeaponChoices(Action onSelected)
    {
        if (IsOpen || applier.EquippedWeaponCount != 0 ||
            !ValidateChoiceUi())
        {
            return false;
        }

        List<PlayerUpgradeData> weapons = GetEligibleWeaponUpgrades();
        weapons.Sort((left, right) =>
            left.Weapon.WeaponType.CompareTo(right.Weapon.WeaponType));

        HashSet<WeaponType> weaponTypes = new HashSet<WeaponType>();
        for (int i = 0; i < weapons.Count; i++)
        {
            weaponTypes.Add(weapons[i].Weapon.WeaponType);
        }

        if (weapons.Count != 3 || weaponTypes.Count != 3)
        {
            Debug.LogError(
                "Starting weapon selection requires three distinct weapon upgrades.",
                this);
            return false;
        }

        selectionCompleted = onSelected;
        isStartingWeaponSelection = true;
        choiceSelector.Reset();
        OpenChoices(weapons, "CHOOSE STARTING WEAPON");
        return true;
    }

    private void ShowChoices()
    {
        if (IsOpen)
        {
            return;
        }

        List<PlayerUpgradeData> weapons = GetEligibleWeaponUpgrades();
        List<PlayerUpgradeData> utilities = new List<PlayerUpgradeData>();

        foreach (PlayerUpgradeData upgrade in availableUpgrades)
        {
            if (upgrade == null ||
                upgrade.EffectType == UpgradeEffectType.EquipWeapon ||
                !applier.CanApply(upgrade))
            {
                continue;
            }

            utilities.Add(upgrade);
        }

        bool isWeaponLevel = progress.Level % 3 == 0;
        List<PlayerUpgradeData> choices = choiceSelector.BuildChoices(
            weapons,
            utilities,
            isWeaponLevel,
            applier.HealthRatio);

        if (choices.Count == 0)
        {
            Debug.LogError("No eligible upgrades are available.", this);
            return;
        }

        selectionCompleted = null;
        isStartingWeaponSelection = false;
        OpenChoices(choices, $"LV {progress.Level}  /  CHOOSE ONE");
    }

    private List<PlayerUpgradeData> GetEligibleWeaponUpgrades()
    {
        List<PlayerUpgradeData> weapons = new List<PlayerUpgradeData>();

        foreach (PlayerUpgradeData upgrade in availableUpgrades)
        {
            if (upgrade != null &&
                upgrade.EffectType == UpgradeEffectType.EquipWeapon &&
                upgrade.Weapon != null &&
                applier.CanApply(upgrade))
            {
                weapons.Add(upgrade);
            }
        }

        return weapons;
    }

    private void OpenChoices(
        List<PlayerUpgradeData> choices,
        string heading)
    {
        if (!ValidateChoiceUi())
        {
            return;
        }

        headingText.text = heading;

        for (int i = 0; i < currentChoices.Length; i++)
        {
            bool hasChoice = i < choices.Count;
            buttons[i].gameObject.SetActive(hasChoice);
            currentChoices[i] = hasChoice ? choices[i] : null;

            if (!hasChoice)
            {
                continue;
            }

            PlayerUpgradeData choice = currentChoices[i];
            bool isWeapon =
                choice.EffectType == UpgradeEffectType.EquipWeapon;
            weaponIcons[i].gameObject.SetActive(isWeapon);

            if (isWeapon && isStartingWeaponSelection)
            {
                WeaponDefinition weapon = choice.Weapon;
                weaponIcons[i].sprite = weapon.Icon;
                weaponIcons[i].color = weapon.DisplayColor;
                labels[i].text =
                    $"<b>{weapon.DisplayName}</b>\n" +
                    $"<size=22><color=#9FB0BC>" +
                    $"DMG {weapon.Damage:0}  |  " +
                    $"RANGE {weapon.Range:0.0}\n" +
                    $"INTERVAL {weapon.FireInterval:0.00}s" +
                    $"</color></size>";
            }
            else
            {
                string countText = string.Empty;
                if (isWeapon)
                {
                    WeaponDefinition weapon = choice.Weapon;
                    weaponIcons[i].sprite = weapon.Icon;
                    weaponIcons[i].color = weapon.DisplayColor;
                    countText =
                        $"  x{applier.GetWeaponCount(choice)}" +
                        $"  {applier.EquippedWeaponCount}/" +
                        $"{applier.WeaponSlotCapacity} slots";
                }

                labels[i].text =
                    $"<b>{choice.Title}</b>{countText}\n" +
                    $"<size=22><color=#9FB0BC>" +
                    $"{choice.Description}</color></size>";
            }
        }

        upgradePanel.transform.SetAsLastSibling();
        upgradePanel.SetActive(true);
        choicesOpenedFrame = Time.frameCount;
        MobileControlsOverlay.SetGameplayActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(choicesContainer);
        Time.timeScale = 0f;
        SelectButton(buttons[0]);
    }

    private bool ValidateChoiceUi()
    {
        bool isValid = upgradePanel != null && headingText != null &&
            choicesContainer != null && buttons != null &&
            labels != null && weaponIcons != null &&
            buttons.Length == 3 && labels.Length == 3 &&
            weaponIcons.Length == 3;

        if (!isValid)
        {
            Debug.LogError(
                "UpgradeController requires a complete three-card UI.",
                this);
        }

        return isValid;
    }

    private void Select(int index)
    {
        if (!IsOpen || index < 0 || index >= currentChoices.Length ||
            currentChoices[index] == null ||
            Time.frameCount == choicesOpenedFrame)
        {
            return;
        }

        applier.Apply(currentChoices[index]);
        upgradePanel.SetActive(false);

        if (isStartingWeaponSelection)
        {
            Action callback = selectionCompleted;
            isStartingWeaponSelection = false;
            selectionCompleted = null;
            EventSystem.current?.SetSelectedGameObject(null);
            callback?.Invoke();
            return;
        }

        Time.timeScale = 1f;
        MobileControlsOverlay.SetGameplayActive(true);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private static void SelectButton(Button button)
    {
        if (EventSystem.current == null || button == null ||
            !button.gameObject.activeInHierarchy)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
