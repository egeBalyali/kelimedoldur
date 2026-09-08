using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the pool of letters available to the player (lower third of screen).
/// Automatically divides letters across up to 3 lines based on total count,
/// using custom line prefabs configured to space out elements horizontally 
/// and center vertically. Features full object pooling for tiles and line containers.
/// </summary>
public class LetterPoolController : MonoBehaviour
{
    [SerializeField] private LetterView letterPrefab;
    [SerializeField] private Transform poolContainer;      // Vertical Layout Group (Middle Center)
    [SerializeField] private GameObject poolLinePrefab;   // Prefab with Horizontal Layout Group

    // Active elements currently shown in the UI
    private readonly List<LetterView> activeLetters = new List<LetterView>();
    private readonly List<GameObject> activeLines = new List<GameObject>();

    // Inactive object pools
    private readonly Stack<LetterView> letterPool = new Stack<LetterView>();
    private readonly Stack<GameObject> linePool = new Stack<GameObject>();

    /// <summary>Raised when a pool letter is pressed. The FlowManager decides what to do with it.</summary>
    public event Action<LetterView> LetterPressed;

    /// <summary>Clears active views to pool and sets up tiles grouped across dynamic rows.</summary>
    public void Setup(char[] letters)
    {
        Clear();

        if (letters == null || letters.Length == 0)
            return;

        // 1. Determine target line count based on total letter count
        int lineCount = GetLineCount(letters.Length);

        // 2. Compute letter allocation per line
        List<int> distribution = DistributeLetters(letters.Length, lineCount);

        // 3. Retrieve lines from pool and populate letter tiles
        int letterIndex = 0;
        for (int i = 0; i < distribution.Count; i++)
        {
            GameObject lineObj = GetLineContainer();
            Transform lineTransform = lineObj.transform;
            int countInThisLine = distribution[i];

            for (int j = 0; j < countInThisLine; j++)
            {
                char letter = letters[letterIndex++];
                LetterView view = GetLetterView(letter, lineTransform);
                view.Clicked += OnLetterViewClicked;
                activeLetters.Add(view);
            }
        }
    }

    /// <summary>Recycles all active letters and lines back to their respective pools.</summary>
    public void Clear()
    {
        // Recycle letters
        foreach (LetterView view in activeLetters)
        {
            if (view == null) continue;

            view.Clicked -= OnLetterViewClicked;
            view.gameObject.SetActive(false);
            view.transform.SetParent(transform, false); // Move to disabled controller root
            letterPool.Push(view);
        }

        // Recycle line containers
        foreach (GameObject line in activeLines)
        {
            if (line == null) continue;

            line.SetActive(false);
            line.transform.SetParent(transform, false);
            linePool.Push(line);
        }

        activeLetters.Clear();
        activeLines.Clear();
    }

    /// <summary>Marks a letter as used (sent into a gap) - hides it and disables input.</summary>
    public void SetLetterUsed(LetterView letterView, bool used)
    {
        if (letterView == null)
            return;

        letterView.SetInteractable(!used);
        letterView.SetVisible(!used);
    }

    #region Object Pooling Factory Methods

    private LetterView GetLetterView(char letter, Transform parent)
    {
        LetterView view;

        if (letterPool.Count > 0)
        {
            view = letterPool.Pop();
            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);

            // Re-initialize state for recycled letter
            view.SetLetter(letter);
            view.SetInteractable(true);
            view.SetVisible(true);
        }
        else
        {
            view = LetterView.Create(letterPrefab, parent, letter);
        }

        return view;
    }

    private GameObject GetLineContainer()
    {
        GameObject lineObj;

        if (linePool.Count > 0)
        {
            lineObj = linePool.Pop();
            lineObj.transform.SetParent(poolContainer, false);
            lineObj.gameObject.SetActive(true);
        }
        else
        {
            lineObj = Instantiate(poolLinePrefab, poolContainer);
        }

        activeLines.Add(lineObj);
        return lineObj;
    }

    #endregion

    #region Layout Calculations

    private int GetLineCount(int totalLetters)
    {
        if (totalLetters < 5) return 1;
        if (totalLetters <= 10) return 2;
        return 3;
    }

    private List<int> DistributeLetters(int total, int lines)
    {
        List<int> result = new List<int>();
        int baseCount = total / lines;
        int remainder = total % lines;

        for (int i = 0; i < lines; i++)
        {
            int count = baseCount + (i < remainder ? 1 : 0);
            result.Add(count);
        }

        return result;
    }

    #endregion

    private void OnLetterViewClicked(LetterView view)
    {
        LetterPressed?.Invoke(view);
    }
}