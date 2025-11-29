using UnityEngine;
using UnityEngine.UI;

// Această clasă gestionează bara de combustibil (slider)
public class BonfireFuelUI : MonoBehaviour
{
    [Tooltip("Slider-ul Unity UI care reprezintă nivelul de combustibil.")]
    public Slider fuelSlider;

    // Referință la executorul care gestionează starea focului
    private LightBonfireExecutor executor;

    public void Setup(LightBonfireExecutor bonfireExecutor)
    {
        executor = bonfireExecutor;
        if (fuelSlider != null)
        {
            // La setup, ascundem bara
            fuelSlider.gameObject.SetActive(false);
        }
    }


    public void UpdateFuelVisuals(float currentFuel, float maxFuel)
    {
        if (fuelSlider == null) return;

        // Setăm limitele sliderului
        fuelSlider.maxValue = maxFuel;
        fuelSlider.value = currentFuel;

        // Afișăm bara dacă există combustibil
        fuelSlider.gameObject.SetActive(currentFuel > 0);
    }


    public void HideUI()
    {
        if (fuelSlider != null)
        {
            fuelSlider.gameObject.SetActive(false);
        }
    }
}