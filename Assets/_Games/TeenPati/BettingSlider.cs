using UnityEngine;
using UnityEngine.UI;

public class BettingSlider : MonoBehaviour
{
    public Slider slider;
    public Text betAmountText;
    private int totalAmount = 12250;

    private void Start()
    {
        slider.minValue = 1;
        slider.maxValue = totalAmount / 500; // Set this to the maximum number of increments based on total amount
        slider.value = 1; // Default starting value
        UpdateBetAmount(Mathf.RoundToInt(slider.value) * 500 + 250);
    }

    public void OnSliderValueChanged()
    {
        int increment = Mathf.RoundToInt(slider.value);
        int currentBet = (increment * 500) + 250;
        UpdateBetAmount(currentBet);
    }

    public void RaiseButtonClicked()
    {
        int increment = Mathf.RoundToInt(slider.value);
        int currentBet = increment * 500;
        // Implement logic for raising the bet.
        // Add the current bet to the total bet amount, and update the UI.
        totalAmount -= currentBet;
        // Perform other actions as needed for your poker game.
    }

    public void AllInButtonClicked()
    {
        int currentBet = totalAmount;
        slider.value = currentBet / 500; // Set the slider value based on the bet
        UpdateBetAmount(currentBet);
    }

    private void UpdateBetAmount(int currentBet)
    {
        betAmountText.text = "Bet: " + currentBet;
    }
}