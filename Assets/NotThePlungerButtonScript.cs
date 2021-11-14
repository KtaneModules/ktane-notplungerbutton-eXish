using KModkit;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class NotThePlungerButtonScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;
    public KMSelectable button;
    public Animator buttonAnimation;
    public GameObject materialCubes;
    public TextMesh buttonText;
    public TextMesh cbText;

    private readonly string[] randomTexts = { "PUSH\nME?", "PUSH\nHE!", "PUSH\nSHE!", "PUSH\nNE!", "PUSH\nIT!", "PUSH\nWE!", "BUSH\nME!" };
    private readonly string[] hexCodes = { "1A1817", "D71E1E", "42E520", "0F1DB3", "39E2E1", "F208F5", "CED707", "FFFFFF" };
    private readonly string[] cbColors = { "K", "R", "G", "B", "C", "M", "Y", "W" };
    private string correctNumber;
    private bool[][] colorComponents = new bool[][] { new bool[] { false, false, false }, new bool[] { true, false, false }, new bool[] { false, true, false }, new bool[] { false, false, true }, new bool[] { false, true, true }, new bool[] { true, false, true }, new bool[] { true, true, false }, new bool[] { true, true, true } };
    private bool pressed;
    private bool cbEnabled;
    private int[] chosenColors;
    private readonly int[] offsets = { -3, -2, -1, 0, 1, 2, 3 };
    private int btnTextIndex;
    private int strikeCount;
    private int cycleIndex;

    public Material[] colors;
    public Renderer surface;
    public Material blackMat;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        button.OnInteract += delegate () { PressButton(); return false; };
    }

    void Start()
    {
        materialCubes.SetActive(false);
        btnTextIndex = Random.Range(0, randomTexts.Length);
        buttonText.text = randomTexts[btnTextIndex];
        Debug.LogFormat("[Not The Plunger Button #{0}] Text on the button: \"{1}\"", moduleId, randomTexts[btnTextIndex].Replace("\n", " "));
        cbEnabled = Colorblind.ColorblindModeActive;
    }

    void Update()
    {
        if (Bomb.GetStrikes() != strikeCount && !moduleSolved)
        {
            strikeCount = Bomb.GetStrikes();
            if (pressed)
            {
                pressed = false;
                if (cbEnabled)
                    cbText.text = string.Empty;
                Debug.LogFormat("[Not The Plunger Button #{0}] Module reset due to strike on another module.", moduleId);
                button.AddInteractionPunch();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, button.transform);
                buttonAnimation.SetBool("release", true);
                buttonAnimation.SetBool("press", false);
            }
        }
    }

    void GetAnswer()
    {
        chosenColors = new int[]{ -1, -1, -1 };
        for (int i = 0; i < 3; i++)
        {
            int choice = Random.Range(0, colors.Length);
            while (chosenColors.Contains(choice))
                choice = Random.Range(0, colors.Length);
            chosenColors[i] = choice;
        }
        string colorList = colors[chosenColors[0]].name + ", " + colors[chosenColors[1]].name + ", and " + colors[chosenColors[2]].name;
        Debug.LogFormat("[Not The Plunger Button #{0}] Colors flashing: {1}", moduleId, colorList);
        bool[] anchor = new bool[] { colorComponents[chosenColors[0]][0], colorComponents[chosenColors[0]][1], colorComponents[chosenColors[0]][2] };
        for (int j = 1; j < 3; j++)
        {
            for (int i = 0; i < 3; i++)
            {
                if (colorComponents[chosenColors[j]][i])
                    anchor[i] = !anchor[i];
            }
        }
        int hexIndex = -1;
        for (int i = 0; i < colorComponents.Length; i++)
        {
            if (colorComponents[i][0] == anchor[0] && colorComponents[i][1] == anchor[1] && colorComponents[i][2] == anchor[2])
            {
                hexIndex = i;
                break;
            }
        }
        Debug.LogFormat("[Not The Plunger Button #{0}] Acquired color from step one: {1}", moduleId, colors[hexIndex].name);
        int counter = 0;
        for (int i = 0; i < 6; i++)
        {
            if (Bomb.GetSerialNumber().Contains(hexCodes[hexIndex][i]))
                counter++;
        }
        Debug.LogFormat("[Not The Plunger Button #{0}] Aquired number from step two: {1}", moduleId, counter);
        counter += offsets[btnTextIndex];
        if (counter < 0)
            counter += 10;
        correctNumber = counter.ToString();
        Debug.LogFormat("[Not The Plunger Button #{0}] Desired time must have: {1}", moduleId, correctNumber);
    }

    void PressButton()
    {
        if (moduleSolved)
            return;
        button.AddInteractionPunch();
        if (!pressed)
        {
            pressed = true;
            GetAnswer();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, button.transform);
            buttonAnimation.SetBool("release", false);
            buttonAnimation.SetBool("press", true);
            StartCoroutine(Disco());
        }
        else
        {
            pressed = false;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, button.transform);
            buttonAnimation.SetBool("release", true);
            buttonAnimation.SetBool("press", false);
            if (cbEnabled)
                cbText.text = string.Empty;
            CheckInput();
        }
    }

    IEnumerator Disco()
    {
        while (pressed)
        {
            surface.material = colors[chosenColors[cycleIndex]];
            if (cbEnabled)
            {
                if (chosenColors[cycleIndex] == 7 || chosenColors[cycleIndex] == 4 || chosenColors[cycleIndex] == 6 || chosenColors[cycleIndex] == 2)
                    cbText.color = Color.black;
                else
                    cbText.color = Color.white;
                cbText.text = cbColors[chosenColors[cycleIndex]];
            }
            else
                cbText.text = string.Empty;
            cycleIndex++;
            if (cycleIndex > 2)
                cycleIndex = 0;
            yield return new WaitForSeconds(0.125f);
        }
        surface.material = blackMat;
    }

    void CheckInput()
    {
        Debug.LogFormat("[Not The Plunger Button #{0}] Button pushed off at {1}.", moduleId, Bomb.GetFormattedTime());
        if (Bomb.GetFormattedTime().Contains(correctNumber))
        {
            moduleSolved = true;
            Debug.LogFormat("[Not The Plunger Button #{0}] That was correct. Module solved.", moduleId);
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            Debug.LogFormat("[Not The Plunger Button #{0}] That was incorrect. Strike! Module reset.", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} push (#) [Pushes the button on/off (optionally when any digit in the bomb's timer is '#')] | !{0} colorblind [Toggles colorblind mode]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            cbEnabled = !cbEnabled;
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*push\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                int temp = -1;
                if (!int.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror!f The specified number '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp < 0 || temp > 9)
                {
                    yield return "sendtochaterror The specified number '" + parameters[1] + "' is out of range 0-9!";
                    yield break;
                }
                while (!Bomb.GetFormattedTime().Contains(parameters[1])) yield return "trycancel Halted waiting to push the button due to a cancel request!";
                button.OnInteract();
            }
            else if (parameters.Length == 1)
            {
                button.OnInteract();
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!pressed)
        {
            button.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        while (!Bomb.GetFormattedTime().Contains(correctNumber)) yield return true;
        button.OnInteract();
    }
}