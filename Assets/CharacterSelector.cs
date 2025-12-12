using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    public static string SelectedCharacter { get; private set; }

    // These methods can be assigned directly in the Inspector for each button's OnClick event

    public void SelectFredrick()
    {
        SelectCharacter("Fredrick");
    }

    public void SelectEzikiel()
    {
        SelectCharacter("Ezikiel");
    }

    public void SelectErishikgal()
    {
        SelectCharacter("Erishikgal");
    }

    public void SelectMiranda()
    {
        SelectCharacter("Miranda");
    }

    private void SelectCharacter(string characterName)
    {
        Debug.Log("Selected character: " + characterName);
        SelectedCharacter = characterName;

        // Load the school interior scene
        SceneManager.LoadScene("SchoolInterior");
    }
}
