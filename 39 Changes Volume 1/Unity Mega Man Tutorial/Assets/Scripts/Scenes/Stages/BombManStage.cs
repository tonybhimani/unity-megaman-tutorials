using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombManStage : MonoBehaviour
{
    public Tilemap tmBackground;
    public Tilemap tmForeground;
    public Tilemap tmDoorways;

    public TileBase doorTileH;
    public TileBase doorTileV;

    public TileBase[] bgTile1;
    public TileBase[] bgTile2;

    bool isSwappingTiles;

    bool isDoorwayMoving;

    bool isDoorway1Open;
    bool isDoorway2Open;

    // Start is called before the first frame update
    void Start()
    {
        // 4:3 resolution bonus items
        GameManager.Instance.SetResolutionScale(GameManager.ResolutionScales.Scale4x3);

        isSwappingTiles = false;
        isDoorwayMoving = false;
        isDoorway1Open = false;
        isDoorway2Open = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleDoorway1();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleDoorway2();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwapTilesAnimation();
        }
    }

    public void ToggleDoorway1()
    {
        if (!isDoorwayMoving)
        {
            StartCoroutine(ToggleDoorway1Co());
        }
    }

    private IEnumerator ToggleDoorway1Co()
    {
        isDoorwayMoving = true;

        if (isDoorway1Open)
        {
            for (int i = 0; i < 4; i++)
            {
                tmDoorways.SetTile(new Vector3Int(244, 61 - i, 0), doorTileV);
                tmDoorways.SetTile(new Vector3Int(245, 61 - i, 0), doorTileV);
                yield return new WaitForSeconds(0.15f);
            }
        }
        else
        {
            for (int i = 3; i >= 0; i--)
            {
                tmDoorways.SetTile(new Vector3Int(244, 61 - i, 0), null);
                tmDoorways.SetTile(new Vector3Int(245, 61 - i, 0), null);
                yield return new WaitForSeconds(0.15f);
            }
        }

        isDoorway1Open = !isDoorway1Open;

        isDoorwayMoving = false;
    }

    public void ToggleDoorway2()
    {
        if (!isDoorwayMoving)
        {
            StartCoroutine(ToggleDoorway2Co());
        }
    }

    private IEnumerator ToggleDoorway2Co()
    {
        isDoorwayMoving = true;

        if (isDoorway2Open)
        {
            for (int i = 0; i < 4; i++)
            {
                tmDoorways.SetTile(new Vector3Int(252 + i, 22, 0), doorTileH);
                tmDoorways.SetTile(new Vector3Int(252 + i, 21, 0), doorTileH);
                yield return new WaitForSeconds(0.15f);
            }
        }
        else
        {
            for (int i = 3; i >= 0; i--)
            {
                tmDoorways.SetTile(new Vector3Int(252 + i, 22, 0), null);
                tmDoorways.SetTile(new Vector3Int(252 + i, 21, 0), null);
                yield return new WaitForSeconds(0.15f);
            }
        }

        isDoorway2Open = !isDoorway2Open;

        isDoorwayMoving = false;
    }

    public void SwapTilesAnimation()
    {
        if (!isSwappingTiles)
        {
            StartCoroutine(SwapTilesAnimationCo());
        }
    }

    private IEnumerator SwapTilesAnimationCo()
    {
        isSwappingTiles = true;

        for (int i = 0; i < 5; i++)
        {
            tmBackground.SwapTile(bgTile1[0], bgTile2[0]);
            tmBackground.SwapTile(bgTile1[1], bgTile2[1]);
            yield return new WaitForSeconds(0.15f);

            tmBackground.SwapTile(bgTile2[0], bgTile1[0]);
            tmBackground.SwapTile(bgTile2[1], bgTile1[1]);
            yield return new WaitForSeconds(0.15f);
        }

        isSwappingTiles = false;
    }
}