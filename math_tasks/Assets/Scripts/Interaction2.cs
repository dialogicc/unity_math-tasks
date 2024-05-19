using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Für den Zugriff auf UI-Elemente
using System.Diagnostics; // Für die Process-Klasse
using System.IO; // Für die Verarbeitung der Ausgabe
using System.Threading.Tasks; // Für asynchrone Methoden
using System.Threading; // Für den UnitySynchronizationContext
#if UNITY_EDITOR
using UnityEditor; // Für den Zugriff auf Editor-spezifische Funktionen wie AssetDatabase
#endif

public class Interaction : MonoBehaviour
{
    public Button executeButton; // Referenz zur Schaltfläche in der UI
    private SynchronizationContext unitySyncContext; // Um auf den Hauptthread zuzugreifen

    void Awake()
    {
        unitySyncContext = SynchronizationContext.Current; // Initialisiere den UnitySynchronizationContext
        if (executeButton != null)
        {
            executeButton.onClick.AddListener(OnExecuteButtonClick); // Füge einen Listener zur Schaltfläche hinzu
        }
    }

    void OnExecuteButtonClick()
    {
        ExecuteActionAsync();
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

        if (!string.IsNullOrEmpty(output))
        {
            UnityEngine.Debug.Log("Python Output: " + output);
        }
        return output;
    }

    void ProcessOutput(string output)
    {
        UnityEngine.Debug.Log("Processed Output: " + output);
    }
}
