using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ActionButtonUI : MonoBehaviour
{
    [Header("Referințe UI")]
    [Tooltip("Imaginea principală a butonului (sprite-ul acesteia va fi schimbat).")]
    public Image iconImage;
    
    // NOU: Acum, "completed" este SPRITE-ul de finalizare, nu o componentă Image.
    [Tooltip("Sprite-ul care trebuie afișat când acțiunea este completă.")]
    public Sprite completedSprite; 
    
    [Tooltip("Componenta TextMeshPro care afișează costul sau numele acțiunii.")]
    public TMP_Text costText;
    
    public ActionRecipeSO recipe;

    private AbstractActionLogicSO boundExecutor;
    private Sprite originalIconSprite; // Salvăm pictograma originală pentru resetare
    private string originalName; 
    private BuildingProgressComponent progressComponent;

    public bool isActionComplete = false;
    

    public void SetVisuals(Sprite icon, string actionName)
    {
        // 💾 Salvăm starea originală
        originalIconSprite = icon;
        originalName = actionName;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            // Ne asigurăm că este vizibilă și de culoare albă la început
            iconImage.gameObject.SetActive(true);
            iconImage.color = Color.white;
        }

        if (costText != null)
        {
            costText.text = actionName;
        }
    }
    
    public void SetupExecutor(AbstractActionLogicSO executor, ActionRecipeSO re)
    {
        // fiecare buton primește propria copie de stare .
        boundExecutor = executor;
        this.recipe = re;
        
        // Resetăm vizualul la setup
        ResetVisualsToInitialState();
    }

    private bool CheckCanExecute()
    {
        return boundExecutor.CheckCanExecute(this.recipe);
    }
    
    // Metodă auxiliară pentru a reseta vizualul la starea inițială
    private void ResetVisualsToInitialState()
    {
        if (iconImage != null)
        {
            iconImage.sprite = originalIconSprite;
            iconImage.color = Color.white;
            iconImage.gameObject.SetActive(true);
        }
        if (costText != null)
        {
            costText.text = originalName;
        }
    }

    // --- LOGICA DE ACTUALIZARE ---

    void Update()
    {
        if (boundExecutor == null || iconImage == null) return;
        
        
        if (isActionComplete && boundExecutor.IsProgressAction)
        {
            
            if (completedSprite != null && iconImage.sprite != completedSprite)
            {
                iconImage.sprite = completedSprite;
                iconImage.color = Color.white; 
            }
            
            if (costText != null)
            {
                 costText.text = "GATA"; 
            }
            
            return; 
        }
        
        if (iconImage.sprite != originalIconSprite)
        {
            iconImage.sprite = originalIconSprite;
            if (costText != null)
            {
                 costText.text = originalName; // Revino la numele original
            }
        }
        
        bool canExecute = CheckCanExecute();
        iconImage.color = canExecute ? Color.white : Color.gray;
    }
}