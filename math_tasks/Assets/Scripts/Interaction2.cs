using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For accessing UI elements
using TMPro; // For accessing TextMeshPro elements
using System.Diagnostics; // For the Process class
using System.IO; // For processing the output
using System.Threading.Tasks; // For asynchronous methods
using System.Threading; // For the UnitySynchronizationContext
#if UNITY_EDITOR
using UnityEditor; // For accessing editor-specific functions like AssetDatabase
#endif

public class Interaction : MonoBehaviour
{
    public Button actionButton; // Reference to the button in the UI
    public Button autoplayButton; // Reference to the autoplay button in the UI
    public TMP_Text taskText; // Reference to the TextMeshPro text field for the task
    public TMP_InputField answerInput; // Reference to the TextMeshPro input field for the answer
    public TMP_Text resultText; // Reference to the TextMeshPro text field for the result
    public TMP_Text taskTimerText; // Reference to the TextMeshPro text field for the task timer
    public TMP_Text totalTimerText; // Reference to the TextMeshPro text field for the total timer
    public TMP_Text preparationTimerText; // Reference to the TextMeshPro text field for the preparation timer
    public TMP_Text solvedTasksText; // Reference to the TextMeshPro text field for the number of solved tasks

    private SynchronizationContext unitySyncContext; // To access the main thread
    private string correctAnswer = ""; // Variable to store the correct answer
    private Stopwatch taskTimer; // Timer for individual tasks
    private Stopwatch totalTimer; // Timer for the total time to solve tasks
    private Stopwatch preparationTimer; // Timer for the time until a task is generated
    private bool isAutoplayEnabled = false; // Flag for the autoplay status
    private int solvedTasksCount = 0; // Counter for the number of solved tasks

    void Awake()
    {
        unitySyncContext = SynchronizationContext.Current; // Initialize the UnitySynchronizationContext
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClick); // Add a listener to the button
        }
        if (autoplayButton != null)
        {
            autoplayButton.onClick.AddListener(ToggleAutoplay); // Add a listener to the autoplay button
        }
        taskTimer = new Stopwatch(); // Initialize the timer for individual tasks
        totalTimer = new Stopwatch(); // Initialize the timer for the total time to solve tasks
        preparationTimer = new Stopwatch(); // Initialize the timer for the time until a task is generated

        if (answerInput != null)
        {
            answerInput.onEndEdit.AddListener(OnAnswerInputEndEdit); // Add a listener to the input field
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
            OnActionButtonClick(); // Call the action for the button when Enter is pressed
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
            FileName = "/Users/franz/miniforge3/envs/master/bin/python", // Path to the Python interpreter in the Conda environment
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
        // Split the task and the answer, here as an example separated by an '='
        string[] parts = output.Split('=');
        if (parts.Length == 2)
        {
            string task = parts[0].Trim() + " ="; // Add the "=" behind the task
            correctAnswer = parts[1].Trim();
    
            taskText.text = task;
            actionButton.GetComponentInChildren<TMP_Text>().text = "Check";
            
            taskTimer.Restart(); // Restart the timer for individual tasks
            totalTimer.Start(); // Resume the total timer for the solving time
            StartCoroutine(UpdateTaskTimer()); // Update the task timer display

            preparationTimer.Stop(); // Stop the preparation timer
            preparationTimer.Reset(); // Reset the preparation timer

            answerInput.Select(); // Focus the input field
            answerInput.ActivateInputField(); // Activate the input field
        }
        else
        {
            UnityEngine.Debug.LogError("Unexpected output format: " + output);
        }
    }

    void CheckAnswer()
    {
        string userAnswer = answerInput.text.Trim();
        taskTimer.Stop(); // Stop the timer for individual tasks
        totalTimer.Stop(); // Stop the total timer when the answer is checked
        preparationTimer.Start(); // Start the preparation timer

        if (userAnswer == correctAnswer)
        {
            resultText.text = $"Correct! Time taken: {taskTimer.Elapsed.TotalSeconds:F2} seconds";
            solvedTasksCount++; // Increment the counter for solved tasks
        }
        else
        {
            resultText.text = $"Incorrect. The correct answer is: {correctAnswer}. Time taken: {taskTimer.Elapsed.TotalSeconds:F2} seconds";
        }

        // Set the button text back to "Generate"
        actionButton.GetComponentInChildren<TMP_Text>().text = "Generate";

        // Clear the input and result fields
        answerInput.text = "";
        //resultText.text = "";

        // Reset the task
        taskText.text = "Press 'Generate'";

        if (isAutoplayEnabled)
        {
            ExecuteActionAsync(); // Automatically generate the next task
        }
        else
        {
            answerInput.Select(); // Focus the input field
            answerInput.ActivateInputField(); // Activate the input field
        }
    }

    IEnumerator UpdateTaskTimer()
    {
        while (true)
        {
            if (taskTimer.IsRunning)
            {
                taskTimerText.text = $"Task Time: {taskTimer.Elapsed.TotalSeconds:F2} seconds";
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator UpdateTotalTimer()
    {
        while (true) // Continuously update the total timer
        {
            totalTimerText.text = $"Total Solving Time: {totalTimer.Elapsed.TotalSeconds:F2} seconds";
            if (totalTimer.Elapsed.TotalSeconds >= 60)
            {
                ResetAll();
            }
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
        StartCoroutine(UpdateTaskTimer()); // Start updating the task timer display
        StartCoroutine(UpdateTotalTimer()); // Start updating the total timer display
        StartCoroutine(UpdatePreparationTimer()); // Start updating the preparation timer display
    }

    void ResetAll()
    {
        // Stop all timers
        taskTimer.Stop();
        totalTimer.Stop();
        preparationTimer.Stop();

        // Reset all timers
        taskTimer.Reset();
        totalTimer.Reset();
        preparationTimer.Reset();

        // Reset UI elements
        taskText.text = "Press 'Generate'";
        resultText.text = "";
        answerInput.text = "";
        actionButton.GetComponentInChildren<TMP_Text>().text = "Generate";

        // Display the number of solved tasks
        solvedTasksText.text = $"Solved Tasks: {solvedTasksCount}";

        // Reset the count of solved tasks
        solvedTasksCount = 0;
    }
}
