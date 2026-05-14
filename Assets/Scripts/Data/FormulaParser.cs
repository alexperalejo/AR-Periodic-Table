using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class FormulaParser : MonoBehaviour
{
    // Parses a chemical formula string into a dictionary of element symbols and counts
    // Examples:
    // "H2O"    -> { H: 2, O: 1 }
    // "NaCl"   -> { Na: 1, Cl: 1 }
    // "H2SO4"  -> { H: 2, S: 1, O: 4 }
    // "Ca(OH)2" -> { Ca: 1, O: 2, H: 2 }

    public static Dictionary<string, int> Parse(string formula)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();

        if (string.IsNullOrEmpty(formula))
            return result;

        ParseGroup(formula, 1, result);
        return result;
    }

    private static int ParseGroup(string formula, int multiplier, Dictionary<string, int> result)
    {
        int i = 0;
        while (i < formula.Length)
        {
            // Handle opening parenthesis like Ca(OH)2
            if (formula[i] == '(')
            {
                int depth = 1;
                int j = i + 1;

                // Find matching closing parenthesis
                while (j < formula.Length && depth > 0)
                {
                    if (formula[j] == '(') depth++;
                    if (formula[j] == ')') depth--;
                    j++;
                }

                // j is now one past the closing ')'
                string inner = formula.Substring(i + 1, j - i - 2);

                // Read number after ')'
                int numStart = j;
                while (j < formula.Length && char.IsDigit(formula[j]))
                    j++;

                int groupCount = 1;
                if (j > numStart)
                    groupCount = int.Parse(formula.Substring(numStart, j - numStart));

                ParseGroup(inner, multiplier * groupCount, result);
                i = j;
            }
            // Handle element symbol (uppercase letter followed by optional lowercase)
            else if (char.IsUpper(formula[i]))
            {
                int j = i + 1;

                // Read lowercase letters (e.g. 'a' in Na, 'l' in Cl)
                while (j < formula.Length && char.IsLower(formula[j]))
                    j++;

                string element = formula.Substring(i, j - i);

                // Read number after element
                int numStart = j;
                while (j < formula.Length && char.IsDigit(formula[j]))
                    j++;

                int count = 1;
                if (j > numStart)
                    count = int.Parse(formula.Substring(numStart, j - numStart));

                if (result.ContainsKey(element))
                    result[element] += count * multiplier;
                else
                    result[element] = count * multiplier;

                i = j;
            }
            else
            {
                // Skip unknown characters
                i++;
            }
        }

        return i;
    }

    // Helper to get a formatted display string with TMP subscripts
    // Example: H2O -> "H<sub>2</sub>O"
    public static string FormatWithSubscripts(string formula)
    {
        string result = "";
        for (int i = 0; i < formula.Length; i++)
        {
            if (char.IsDigit(formula[i]))
                result += "<sub>" + formula[i] + "</sub>";
            else
                result += formula[i];
        }
        return result;
    }

    //Test Method
    [ContextMenu("Test Parser")]
    void TestParser()
    {
        string[] testFormulas = { "H2O", "NaCl", "H2SO4", "Ca(OH)2", "C6H12O6" };

        foreach (string formula in testFormulas)
        {
            Dictionary<string, int> result = Parse(formula);
            string output = formula + " -> ";
            foreach (var pair in result)
                output += pair.Key + ":" + pair.Value + " ";
            Debug.Log(output);

            Debug.Log("Formatted: " + FormatWithSubscripts(formula));
        }
    }
}

