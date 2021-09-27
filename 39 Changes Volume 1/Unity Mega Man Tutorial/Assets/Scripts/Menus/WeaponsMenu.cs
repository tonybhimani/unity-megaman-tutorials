using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class WeaponsMenu : MonoBehaviour
{
    Animator animator;

    ColorSwap colorSwap;

    // player data
    int playerLives;
    PlayerController.WeaponTypes playerWeaponType;
    PlayerController.WeaponsStruct[] playerWeaponsData;

    // flag to allow moving in the menu
    bool canSelectWeapon;

    // size of the mask's rect transform
    float energyBarSize;

    // timer for letter flashing
    float letterFlashTimer;

    // color swap indexes
    private enum SwapIndex
    {
        Block1 = 252,
        Block2 = 188,
        Background = 116
    }

    // palette choices
    public enum MenuPalettes
    {
        Custom,
        BombMan,
        CutMan,
        ElecMan,
        FireMan,
        GutsMan,
        IceMan,
        Wily1,
        Wily2,
        Wily3,
        Wily4,
        Wily4_1,
        Wily4_2
    }

    [Header("Menu Palette")]
    [SerializeField] MenuPalettes menuPalette = MenuPalettes.BombMan;
    [SerializeField] int customColorBlockLight;
    [SerializeField] int customColorBlockDark;
    [SerializeField] int customColorBackground;

    [Header("Audio Clips")]
    [SerializeField] AudioClip menuSelectClip;

    [Header("Timers")]
    [SerializeField] float letterFlashDelay = 0.15f;

    [Header("Menu Objects")]
    [SerializeField] GameObject[] Letters;
    [SerializeField] GameObject[] EnergyBars;
    [SerializeField] GameObject Icon;
    [SerializeField] GameObject[] Lives;

    [Header("Alphanumeric Sprites")]
    // 0 to 7 for B, E, G, I, C, F, M, P
    [SerializeField] Sprite[] BlueLetters;
    [SerializeField] Sprite[] WhiteLetters;
    // 0 to 9 matches array index
    [SerializeField] Sprite[] WhiteNumbers;

    [Header("Menu Events")]
    public UnityEvent ShowMenuEvent;
    public UnityEvent ExitMenuEvent;

    void Awake()
    {
        // get attached components
        animator = GetComponent<Animator>();
        colorSwap = GetComponent<ColorSwap>();

        // initialize the letter flashing timer
        letterFlashTimer = letterFlashDelay;

        // they're all the same width so get the size of the first bar's mask
        energyBarSize = EnergyBars[0].transform.GetChild(0).GetComponent<Image>().rectTransform.rect.width;
    }

    // Start is called before the first frame update
    void Start()
    {
        // set the menu palette
        SetMenuPalette(menuPalette);
    }

    // Update is called once per frame
    void Update()
    {
        if (canSelectWeapon)
        {
            // move up on weapons and play sound
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                playerWeaponType = FindNextWeapon("Up");
                UpdateWeaponLetters();
                SoundManager.Instance.Play(menuSelectClip);
            }

            // move down on weapons and play sound
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                playerWeaponType = FindNextWeapon("Down");
                UpdateWeaponLetters();
                SoundManager.Instance.Play(menuSelectClip);
            }

            // flash weapon letter (no timescale, no coroutine)
            letterFlashTimer -= Time.unscaledDeltaTime;
            if (letterFlashTimer <= 0)
            {
                // toggle the alpha on the letter's canvas group
                float alpha = Letters[(int)playerWeaponType].GetComponent<CanvasGroup>().alpha;
                Letters[(int)playerWeaponType].GetComponent<CanvasGroup>().alpha = (alpha == 1f) ? 0f : 1f;
                // reset the flashing timer
                letterFlashTimer = letterFlashDelay;
            }
        }
    }

    public void ShowMenu()
    {
        // invoke method before menu is shown
        ShowMenuEvent.Invoke();
        // enable the menu and play its tile-in animation
        gameObject.SetActive(true);
        ApplyMenuPalette();
        animator.Play("Appear");
    }

    public void ExitMenu()
    {
        // play the tile-out menu animation
        animator.Play("Disappear");
    }

    void ShowMenuStartAnimation()
    {
        // hide all elements on the menu
        ToggleElements(false);
    }

    void ShowMenuEndAnimation()
    {
        // show all menu elements on the menu and initialize
        ToggleElements(true);
        InitializeMenu();
        // allow key input to switch between weapons
        canSelectWeapon = true;
    }

    void ExitMenuStartAnimation()
    {
        // hide all menu elements
        ToggleElements(false);
        // disable key input to switch between weapons
        canSelectWeapon = false;
    }

    void ExitMenuEndAnimation()
    {
        // disable the menu and invoke method on exit
        gameObject.SetActive(false);
        ExitMenuEvent.Invoke();
    }

    void ToggleElements(bool active)
    {
        // toggle the menu elements
        int i;
        // Weapon Letters
        for (i = 0; i < Letters.Length; i++)
        {
            Letters[i].SetActive(active);
        }
        // Energy Bars
        for (i = 0; i < EnergyBars.Length; i++)
        {
            EnergyBars[i].SetActive(active);
        }
        // 1-UP / Extra Life Icon
        Icon.SetActive(active);
        // Lives Count
        Lives[0].SetActive(active);
        Lives[1].SetActive(active);
    }

    public void SetMenuData(int lives, PlayerController.WeaponTypes weaponType, PlayerController.WeaponsStruct[] weaponsData)
    {
        // save player data for menu setup
        this.playerLives = lives;
        this.playerWeaponType = weaponType;
        this.playerWeaponsData = weaponsData;
    }

    public PlayerController.WeaponTypes GetWeaponSelection()
    {
        // return the selected weapon
        return playerWeaponType;
    }

    void InitializeMenu()
    {
        // update lives, weapon letters, and energy bars
        UpdatePlayerLives();
        UpdateWeaponLetters();
        UpdateWeaponEnergyBars();
    }

    PlayerController.WeaponTypes FindNextWeapon(string direction)
    {
        // current weapon and total
        int nextWeapon = (int)playerWeaponType;
        int maxWeapons = playerWeaponsData.Length;

        switch (direction)
        {
            case "Up":
                while (true)
                {
                    // cycle to next weapon index
                    if (--nextWeapon < 0)
                    {
                        nextWeapon = maxWeapons - 1;
                    }
                    // if weapon is enabled then use it
                    if (playerWeaponsData[nextWeapon].enabled)
                    {
                        break;
                    }
                }
                break;
            case "Down":
                while (true)
                {
                    // cycle to next weapon index
                    if (++nextWeapon > maxWeapons - 1)
                    {
                        nextWeapon = 0;
                    }
                    // if weapon is enabled then use it
                    if (playerWeaponsData[nextWeapon].enabled)
                    {
                        break;
                    }
                }
                break;
        }

        return (PlayerController.WeaponTypes)nextWeapon;
    }

    void UpdatePlayerLives()
    {
        // lives minus one because it's the one the player is currently on
        // update the Lives object with white number sprite
        Lives[0].GetComponent<Image>().sprite = WhiteNumbers[(playerLives - 1) / 10];
        Lives[1].GetComponent<Image>().sprite = WhiteNumbers[(playerLives - 1) % 10];
    }

    void UpdateWeaponLetters()
    {
        // Player Weapons Data must align with the Letters Array
        for (int i = 0; i < playerWeaponsData.Length; i++)
        {
            // Weapon Letter
            if (Letters[i] != null)
            {
                // set letter to white if current weapon or blue otherwise
                Letters[i].GetComponent<Image>().sprite =
                    (i == (int)playerWeaponType) ? WhiteLetters[i] : BlueLetters[i];
                // toggle canvas transparency
                Letters[i].GetComponent<CanvasGroup>().alpha = playerWeaponsData[i].enabled ? 1f : 0f;
            }
        }
    }

    void UpdateWeaponEnergyBars()
    {
        // Player Weapons Data must align with the Energy Bars Array
        // data length minus one because Mega Buster doesn't have an energy bar
        for (int i = 0; i < playerWeaponsData.Length - 1; i++)
        {
            // Weapon Energy Bar
            if (EnergyBars[i] != null)
            {
                EnergyBars[i].GetComponent<CanvasGroup>().alpha = playerWeaponsData[i].enabled ? 1f : 0f;
                EnergyBars[i].transform.GetChild(0).
                    GetComponent<Image>().rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    energyBarSize * (playerWeaponsData[i].currentEnergy / (float)playerWeaponsData[i].maxEnergy));
            }
        }
    }

    public void SetMenuPalette(MenuPalettes palette)
    {
        // set the menu palette with predefined colors
        this.menuPalette = palette;
    }

    public void SetMenuPalette(int block1Color, int block2Color, int backgroundColor)
    {
        // set the menu palette with custom colors
        this.menuPalette = MenuPalettes.Custom;
        this.customColorBlockLight = block1Color;
        this.customColorBlockDark = block2Color;
        this.customColorBackground = backgroundColor;
    }

    private void ApplyMenuPalette()
    {
        // apply new selected color scheme with ColorSwap
        switch (menuPalette)
        {
            case MenuPalettes.Custom:
                // custom colors
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(customColorBlockLight));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(customColorBlockDark));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(customColorBackground));
                break;
            case MenuPalettes.BombMan:
                // light green, dark green, brown
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0x80D010));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0x009400));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x7C0800));
                break;
            case MenuPalettes.CutMan:
            case MenuPalettes.Wily3:
                // white, medium gray, dark gray
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xFCFCFC));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0xBCBCBC));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x747474));
                break;
            case MenuPalettes.ElecMan:
            case MenuPalettes.Wily4:
                // white, orange, dark orange
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xFCFCFC));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0xFC9838));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0xC84C0C));
                break;
            case MenuPalettes.FireMan:
                // white, medium gray, dark red
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xFCFCFC));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0xBCBCBC));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0xA40000));
                break;
            case MenuPalettes.GutsMan:
                // pink, dark orange, brown
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xFC7460));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0xC84C0C));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x7C0800));
                break;
            case MenuPalettes.IceMan:
                // white, dark green, teal
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xFCFCFC));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0x004400));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x008088));
                break;
            case MenuPalettes.Wily1:
                // white, dark gray, blue
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xFCFCFC));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0x747474));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x0000A8));
                break;
            case MenuPalettes.Wily2:
                // green, mustard, teal
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0x58F898));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0x887000));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x008088));
                break;
            case MenuPalettes.Wily4_1:
                // red, pink, black
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0xD82800));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0xFC7460));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0x000000));
                break;
            case MenuPalettes.Wily4_2:
                // brown, light yellow, orange
                colorSwap.SwapColor((int)SwapIndex.Block1, ColorSwap.ColorFromInt(0x7C0800));
                colorSwap.SwapColor((int)SwapIndex.Block2, ColorSwap.ColorFromInt(0xFCD8A8));
                colorSwap.SwapColor((int)SwapIndex.Background, ColorSwap.ColorFromInt(0xC84C0C));
                break;
        }

        // apply the color changes
        colorSwap.ApplyColor();
    }
}