# Project Setup Guide

This guide will walk you through the steps needed to set up the project on a Windows machine.

## Prerequisites

- Anaconda
- Visual Studio Code (VS Code)
- Git
- Python

## Steps

1. **Install Anaconda:**
   Download and install Anaconda from [here]([https://www.anaconda.com/products/individual](https://docs.anaconda.com/free/miniconda/)) and run the Anaconda Prompt (miniconda3) console.

2. **Configure Conda to use conda-forge:**
   Open Anaconda Prompt and run:
   ```sh
   conda config --add channels conda-forge
3. **Create Conda Environment:**
   Ensure you are in the project directory and run:
   ```sh
   conda env create -f conda_environment_windows.yaml
   ```

   Due to another project audio-tools are also included in the environment.
   
   If you encounter issues with Parler TTS due to the Git path:
      - Install Git for Windows from [here](https://gitforwindows.org/).
      - After installation, retry installing Parler TTS.  
      
5. **Download and Install Python:**
   Download the latest version of Python from [here](https://www.python.org/downloads/) and install it.

6. **Install Ollama and Gemma:2b:**
   Follow the instructions provided by Ollama to install it. Then, install the Gemma:2b package.

7. **Adjust Python Path in Script:**
   Open the script "Interaction" and modify the FileName to match the path to your Conda environment's Python executable. For example:
   ```csharp
   FileName = @"C:\Users\INF3_1\miniconda3\envs\master2\python.exe", // Path to the Conda environment's Python executable
8. **Run Unity-Project:**
   Open the math_tasks-folder in Unity and press the 'Generate'-Button to get a math task, and the 'Check'-Button or 'Enter' to evaluate the answer.
