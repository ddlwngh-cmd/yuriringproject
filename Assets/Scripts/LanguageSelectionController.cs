using UnityEngine;

public sealed class LanguageSelectionController : MonoBehaviour
{
    public void SelectKorean()
    {
        LocalizationManager.SetKorean();
    }

    public void SelectEnglish()
    {
        LocalizationManager.SetEnglish();
    }
}
