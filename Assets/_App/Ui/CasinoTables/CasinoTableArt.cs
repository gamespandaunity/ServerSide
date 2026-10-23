using TMPro;
using UnityEngine;

/// <summary>
/// The pictures and fonts a Teen Patti 5 / Poker 6 table screen is drawn with (the existing Teen Patti and Poker art),
/// filled in the table scene by Tools ▸ Tables ▸ Build table scenes. CasinoTableScreen lays everything out in code, so
/// only this list of assets has to be in the scene.
/// </summary>
public class CasinoTableArt : MonoBehaviour
{
    [Header("Table")]
    public Sprite background;
    public Sprite table;
    [Header("Cards: index = suit * 13 + (rank - 2); suits clubs, diamonds, hearts, spades; ranks 2..14 (ace)")]
    public Sprite[] cardFaces = new Sprite[52];
    public Sprite cardBack;
    [Header("Seats")]
    public Sprite seatFrame;
    public Sprite avatar;
    public Sprite timerRing;
    public Sprite nameBar;
    public Sprite dealer;
    [Header("Money")]
    public Sprite chip;
    public Sprite coin;
    [Header("Buttons")]
    public Sprite button;
    public Sprite buttonAlt;
    public Sprite panel;
    [Header("Text")]
    public TMP_FontAsset font;
}
