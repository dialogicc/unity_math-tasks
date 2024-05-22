using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Für den Zugriff auf UI-Elemente
using TMPro; // Für den Zugriff auf TextMeshPro-Elemente
using System.Diagnostics; // Für die Process-Klasse
using System.IO; // Für die Verarbeitung der Ausgabe
using System.Threading.Tasks; // Für asynchrone Methoden
using System.Threading; // Für den UnitySynchronizationContext
#if UNITY_EDITOR
using UnityEditor; // Für den Zugriff auf Editor-spezifische Funktionen wie AssetDatabase
#endif

public class Interaction : MonoBehaviour
{
    public Button actionButton; // Referenz zur Schaltfläche in der UI
    public Button autoplayButton; // Referenz zur Autoplay-Schaltfläche in der UI
    public TMP_Text taskText; // Referenz zum TextMeshPro-Textfeld für die Aufgabe
    public TMP_InputField answerInput; // Referenz zum TextMeshPro-Eingabefeld für die Antwort
    public TMP_Text resultText; // Referenz zum TextMeshPro-Textfeld für das Ergebnis
    public TMP_Text taskTimerText; // Referenz zum TextMeshPro-Textfeld für den Timer
    public TMP_Text totalTimerText; // Referenz zum TextMeshPro-Textfeld für den Gesamttimer
    public TMP_Text preparationTimerText; // Referenz zum TextMeshPro-Textfeld für den Vorbereitungstimer

    private SynchronizationContext unitySyncContext; // Um auf den Hauptthread zuzugreifen
    private string correctAnswer = ""; // Variable zur Speicherung der korrekten Antwort
    private Stopwatch taskTimer; // Timer für einzelne Aufgaben
    private Stopwatch totalTimer; // Timer für die gesamte Zeit zum Lösen der Aufgaben
    private Stopwatch preparationTimer; // Timer für die Zeit bis zur Generierung einer Aufgabe
    private bool isAutoplayEnabled = false; // Flag für den Autoplay-Status

    void Awake()
    {
        unitySyncContext = SynchronizationContext.Current; // Initialisiere den UnitySynchronizationContext
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClick); // Füge einen Listener zur Schaltfläche hinzu
        }
        if (autoplayButton != null)
        {
            autoplayButton.onClick.AddListener(ToggleAutoplay); // Füge einen Listener zur Autoplay-Schaltfläche hinzu
        }
        taskTimer = new Stopwatch(); // Initialisiere den Timer für einzelne Aufgaben
        totalTimer = new Stopwatch(); // Initialisiere den Timer für die gesamte Zeit zum Lösen der Aufgaben
        preparationTimer = new Stopwatch(); // Initialisiere den Timer für die Zeit bis zur Generierung einer Aufgabe

        if (answerInput != null)
        {
            answerInput.onEndEdit.AddListener(OnAnswerInputEndEdit); // Füge einen Listener zum Eingabefeld hinzu
        }
    }

    void OnActionButtonClick()
    {
        if (actionButton.GetComponentInChildren<TMP_Text>().text == "Generate")
        {
            ExecuteActionAsync();
        }
        else if (actionButton.GetComponentInChildren<TMP_Text>().text == "Check")
        {
            CheckAnswer();
        }
    }

    void OnAnswerInputEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnActionButtonClick(); // Rufe die Aktion für den Button auf, wenn Enter gedrückt wird
        }
    }

    async void ExecuteActionAsync()
    {
        string output = await Task.Run(() => ExecuteAction());
        #if UNITY_EDITOR
        AssetDatabase.Refresh(); // Refresh the assets in the editor
        #endif
        unitySyncContext.Post(_ => ProcessOutput(output), null);
    }

    string ExecuteAction()
    {
        string output = "";
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "/Users/franz/miniforge3/envs/master/bin/python", // Pfad zur Python der Conda-Umgebung
            Arguments = "\"Assets/Scripts/text2.py\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = startInfo })
        {
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogError("Python Error: " + error);
            }
        }

        return output;
    }

    void ProcessOutput(string output)
    {
        // Trennen der Aufgabe und der Antwort, hier als Beispiel getrennt durch ein '='
        string[] parts = output.Split('=');
        if (parts.Length == 2)
        {
            string task = parts[0].Trim() + " ="; // Füge das "=" hinter der Aufgabe hinzu
            correctAnswer = parts[1].Trim();
    
            taskText.text = task;
            actionButton.GetComponentInChildren<TMP_Text>().text = "Check";
            
            taskTimer.Restart(); // Timer für einzelne Aufgaben neu starten
            totalTimer.Start(); // Gesamttimer für die Lösungszeit fortsetzen
            StartCoroutine(UpdateTaskTimer()); // Timer-Anzeige für einzelne Aufgaben aktualisieren

            preparationTimer.Stop(); // Vorbereitungstimer stoppen
            preparationTimer.Reset(); // Vorbereitungstimer zurücksetzen

            answerInput.Select(); // Fokussiere das Eingabefeld
            answerInput.ActivateInputField(); // Aktiviere das Eingabefeld
        }
        else
        {
            UnityEngine.Debug.LogError("Unexpected output format: " + output);
        }
    }

    void CheckAnswer()
    {
        string userAnswer = answerInput.text.Trim();
        taskTimer.Stop(); // Timer für einzelne Aufgaben stoppen
        totalTimer.Stop(); // Gesamttimer stoppen, wenn die Antwort überprüft wird
        preparationTimer.Start(); // Vorbereitungstimer starten

        if (userAnswer == correctAnswer)
        {
            resultText.text = $"Correct! Time taken: {taskTimer.Elapsed.TotalSeconds:F2} seconds";
        }
        else
        {
            resultText.text = $"Incorrect. The correct answer is: {correctAnswer}. Time taken: {taskTimer.Elapsed.TotalSeconds:F2} seconds";
        }

        // Button-Text zurück auf "Generate" setzen
        actionButton.GetComponentInChildren<TMP_Text>().text = "Generate";

        // Lösche die Eingabe und das Ergebnisfeld
        answerInput.text = "";
        //resultText.text = "";

        // Setze die Aufgabe zurück
        taskText.text = "Press 'Generate'";

        if (isAutoplayEnabled)
        {
            ExecuteActionAsync(); // Automatisch die nächste Aufgabe generieren
        }
        else
        {
            answerInput.Select(); // Fokussiere das Eingabefeld
            answerInput.ActivateInputField(); // Aktiviere das Eingabefeld
        }
    }

    IEnumerator UpdateTaskTimer()
    {
        while (taskTimer.IsRunning)
        {
            taskTimerText.text = $"Task Time: {taskTimer.Elapsed.TotalSeconds:F2} seconds";
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator UpdateTotalTimer()
    {
        while (true) // Continuously update the total timer
        {
            totalTimerText.text = $"Total Solving Time: {totalTimer.Elapsed.TotalSeconds:F2} seconds";
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator UpdatePreparationTimer()
    {
        while (true) // Continuously update the preparation timer
        {
            preparationTimerText.text = $"Preparation Time: {preparationTimer.Elapsed.TotalSeconds:F2} seconds";
            yield return new WaitForSeconds(0.1f);
        }
    }

    void ToggleAutoplay()
    {
        isAutoplayEnabled = !isAutoplayEnabled;
        if (isAutoplayEnabled)
        {
            autoplayButton.GetComponent<Image>().color = Color.green;
        }
        else
        {
            autoplayButton.GetComponent<Image>().color = Color.red;
        }
    }

    void Start()
    {
        StartCoroutine(UpdateTotalTimer()); // Start updating the total timer display
        StartCoroutine(UpdatePreparationTimer()); // Start updating the preparation timer display
    }
}
