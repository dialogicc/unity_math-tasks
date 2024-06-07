# %%
import requests
import re

def is_valid_math_task(task):
    """Checks if the math task is valid."""
    match = re.match(r'^(\d+)([\+\-\*/])(\d+)=(\d+)$', task)
    if not match:
        return False
    a, operator, b, c = match.groups()
    a, b, c = int(a), int(b), int(c)
    if operator == '+':
        return a + b == c
    elif operator == '-':
        return a - b == c
    elif operator == '*':
        return a * b == c
    else:
        return False

def query_ollama(prompt):
    api_url = 'http://localhost:11434/v1/chat/completions'
    
    data = {
        "model": "gemma:2b",
        "messages": [
            {"role": "system", "content": "You are an AI who creates simple math tasks using subtraction and multiplication and addition. The output must be in the format: 'a + b = c', 'a - b = c', or 'a * b = c' with no additional text or explanations. Please vary the operators equally."},
            {"role": "user", "content": prompt}
        ]
    }

    while True:
        response = requests.post(api_url, json=data)
        if response.status_code == 200:
            response_text = response.json()['choices'][0]['message']['content']
            # Remove everything except numbers, operators, and the equal sign
            cleaned_text = re.sub(r'[^0-9+\-*/=]', '', response_text)
            # Ensure the format is correct and the calculation is accurate
            if is_valid_math_task(cleaned_text):
                return cleaned_text
        else:
            return f"Error: {response.text}"

# Prompt to execute
prompt = "Create a math task with result in one line. Only give out the task and the result with no words, no explanations, just the task and the result."
response = query_ollama(prompt)
print(response)
