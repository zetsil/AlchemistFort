using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StoryManager : MonoBehaviour
{
    [Header("UI")]
    public Image storyImage;
    public TextMeshProUGUI storyText;

    [Header("Story")]
    public Sprite[] images;

    [TextArea(3, 6)]
    public string[] texts;

    [Tooltip("Cate texte are fiecare imagine, in ordine")]
    public int[] textsPerImage;

    [Header("Next Scene")]
    public string nextSceneName;

    int currentImageIndex = 0;
    int currentTextIndex = 0;
    int textIndexForCurrentImage = 0;

    void Start()
    {
        storyImage.sprite = images[currentImageIndex];
        storyText.text = texts[currentTextIndex];
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Next();
        }
    }

    void Next()
    {
        currentTextIndex++;
        textIndexForCurrentImage++;

        // Mai exista text pentru imaginea curenta
        if (textIndexForCurrentImage < textsPerImage[currentImageIndex])
        {
            storyText.text = texts[currentTextIndex];
            return;
        }

        // Trecem la urmatoarea imagine
        currentImageIndex++;
        textIndexForCurrentImage = 0;

        if (currentImageIndex < images.Length)
        {
            storyImage.sprite = images[currentImageIndex];
            storyText.text = texts[currentTextIndex];
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
