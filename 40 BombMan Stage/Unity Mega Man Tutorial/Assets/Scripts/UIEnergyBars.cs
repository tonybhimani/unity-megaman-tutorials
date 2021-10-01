using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIEnergyBars : MonoBehaviour
{
    public static UIEnergyBars Instance = null;

    [System.Serializable]
    public struct EnergyBarsStruct
    {
        public Image mask;
        public float size;
    }
    public EnergyBarsStruct[] energyBarsStructs;

    public enum EnergyBars { PlayerHealth, PlayerWeapon, EnemyHealth };

    [SerializeField] Sprite[] energySprites;
    public enum EnergyBarTypes
    {
        PlayerLife,
        MagnetBeam,
        BombMan,
        HyperBomb,
        CutMan,
        RollingCutter,
        ElecMan,
        ThunderBeam,
        FireMan,
        FireStorm,
        GutsMan,
        SuperArm,
        IceMan,
        IceSlasher,
        CopyRobot,
        CWU01P_1,
        CWU01P_2,
        CWU01P_3,
        CWU01P_4,
        CWU01P_5,
        CWU01P_6,
        CWU01P_7,
        YellowDevil,
        WilyMachine
    };

    private void Awake()
    {
        // If there is not already an instance of UIEnergyBars, set it to this
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // get the initial height of the mask for each energy bar
        foreach (EnergyBars energyBar in Enum.GetValues(typeof(EnergyBars)))
        {
            energyBarsStructs[(int)energyBar].size =
                energyBarsStructs[(int)energyBar].mask.rectTransform.rect.height;
        }
    }

    public void SetValue(EnergyBars energyBar, float value)
    {
        // adjust the height of the mask to "hide" lost energy bars
        energyBarsStructs[(int)energyBar].mask.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            energyBarsStructs[(int)energyBar].size * value);
    }

    public void SetImage(EnergyBars energyBar, EnergyBarTypes energyBarType)
    {
        // assign sprite image (type) to the mask's child to switch between different colored bars
        energyBarsStructs[(int)energyBar].mask.
            gameObject.transform.GetChild(0).GetComponent<Image>().sprite =
                energySprites[(int)energyBarType];
    }

    public void SetVisibility(EnergyBars energyBar, bool visible)
    {
        // set energy bar visibility through canvas group's alpha
        energyBarsStructs[(int)energyBar].mask.
            gameObject.transform.parent.GetComponent<CanvasGroup>().alpha = visible ? 1f : 0f;
    }
}
