using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using ExitGames.Client.Photon;

public class KeyboardGameManager : MonoBehaviour
{
    public static KeyboardGameManager Instance;
    public MathCustom _keyboardController;
    [SerializeField] public TMP_InputField textBox;

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }

    public void DeleteLetter()
    {
        if(textBox.text.Length != 0) {
            textBox.text = textBox.text.Remove(textBox.text.Length - 1, 1);
        }
    }

    public void AddLetter(string letter)
    {
        textBox.text = textBox.text + letter;
        textBox.MoveTextEnd(false);
        KeyboardGameManager.Instance.textBox.caretPosition = KeyboardGameManager.Instance.textBox.text.Length ;

    }

    public void SubmitWord()
    {
       if(textBox) textBox.DeactivateInputField();
        textBox=null;
       _keyboardController.ispointerdown = false;
        _keyboardController.ClosePanelButton();
    }

    
    public void OnTMPInputSelected(TMP_InputField input)
    {
        Debug.Log("Input selected: " + input.name);
        _keyboardController.ispointerdown = true;
        _keyboardController.OpenPanelButton(input);
        textBox = input;
    }

   
}
