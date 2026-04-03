using UnityEngine;

public class VisibilityRangeController : MonoBehaviour
{

    private NewActionUIGenerator uiGenerator;
    private bool isVisible = false;

    // ManualInitialize rămâne util pentru a fi apelat când instanțiezi obiectul
    public void ManualInitialize()
    {
        if (uiGenerator == null) uiGenerator = GetComponent<NewActionUIGenerator>();

        // Ne asigurăm că pornește invizibil
        isVisible = true;
        ToggleVisibility(false);
    }

    // public void ToggleVisibility(bool visible)
    // {
    //     if (isVisible == visible) return;
    //     isVisible = visible;

    //     if (uiGenerator == null) uiGenerator = GetComponent<NewActionUIGenerator>();

    //     if (uiGenerator != null && uiGenerator.containerToRotate != null)
    //     {
    //         // Activăm/Dezactivăm containerul UI
    //         uiGenerator.containerToRotate.gameObject.SetActive(visible);

    //         if (visible)
    //         {
    //             ApplyAlphaToChildren(1f);
    //         }
    //     }
    // }

    private void ApplyAlphaToChildren(float alpha)
    {
        if (uiGenerator == null || uiGenerator.containerToRotate == null) return;

        CanvasRenderer[] renderers = uiGenerator.containerToRotate.GetComponentsInChildren<CanvasRenderer>(true);
        foreach (var cr in renderers)
        {
            cr.SetAlpha(alpha);
        }
    }
    
    public void ToggleVisibility(bool visible)
    {
        // Nu mai punem 'if (isVisible == visible) return;' acum, ca să fim siguri că execută
        isVisible = visible;

        if (uiGenerator == null) uiGenerator = GetComponent<NewActionUIGenerator>();

        if (uiGenerator != null)
        {
            // 1. Gestionăm Obiectul care se rotește (Ancoră sau Container)
            if (uiGenerator.containerToRotate != null)
            {
                uiGenerator.containerToRotate.gameObject.SetActive(visible);
            }

            // 2. Gestionăm Containerul cu Butoane (în caz că e diferit de ancoră)
            // Ne asigurăm că și butoanele efective sunt active
            uiGenerator.ToggleUIContainer(visible);
            
            if (visible) 
            {
                ApplyAlphaToChildren(1f);
            }
        }
    }
}