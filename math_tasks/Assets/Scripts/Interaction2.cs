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
    public TMP_Text taskText; // Referenz zum TextMeshPro-Textfeld für die Aufgabe
    public TMP_InputField answerInput; // Referenz zum TextMeshPro-Eingabefeld für die Antwort
    public TMP_Text resultText; // Referenz zum TextMeshPro-Textfeld für das Ergebnis

    private SynchronizationContext unitySyncContext; // Um auf den Hauptthread zuzugreifen
    private string correctAnswer = ""; // Variable zur Speicherung der korrekten Antwort

    void Awake()
    {
        unitySyncContext = SynchronizationContext.Current; // Initialisiere den UnitySynchronizationContext
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClick); // Füge einen Listener zur Schaltfläche hinzu
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
            FileName = "/Users/franz/miniforge3/envs/master/bin/python", //Pfad zur Python der Conda-Umgebung
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
            string task = parts[0].Trim();
            correctAnswer = parts[1].Trim();

            taskText.text = task;
            actionButton.GetComponentInChildren<TMP_Text>().text = "Check";
        }
        else
        {
            UnityEngine.Debug.LogError("Unexpected output format: " + output);
        }
    }

    void CheckAnswer()
    {
        string userAnswer = answerInput.text.Trim();
        if (userAnswer == correctAnswer)
        {
            resultText.text = "Correct!";
        }
        else
        {
            resultText.text = "Incorrect. The correct answer is: " + correctAnswer;
        }
    }
}
