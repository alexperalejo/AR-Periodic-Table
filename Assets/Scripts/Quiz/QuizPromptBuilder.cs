using System.Collections.Generic;
using UnityEngine;

namespace PeriodicAR.Quiz
{
    /// <summary>
    /// Builds contextual quiz questions from current app state.
    /// Uses local hardcoded questions; future versions could call the LLM for dynamic generation.
    /// </summary>
    public static class QuizPromptBuilder
    {
        public static QuizQuestion BuildQuestion()
        {
            var sp = Tutor.AppStateProvider.Instance;

            // Context-aware question selection
            if (sp != null && !string.IsNullOrEmpty(sp.heldElementSymbol))
                return ElementQuestion(sp.heldElementSymbol);

            if (sp != null && !string.IsNullOrEmpty(sp.halfLifeIsotope))
                return IsotopeQuestion(sp.halfLifeIsotope);

            if (sp != null && !string.IsNullOrEmpty(sp.activePhaseSubstance))
                return PhaseQuestion(sp.activePhaseSubstance);

            if (sp != null && sp.activeFlameElements?.Count > 0)
                return SpectroscopyQuestion(sp.activeFlameElements[0]);

            if (sp != null && !string.IsNullOrEmpty(sp.activeAbundanceContext))
                return AbundanceQuestion(sp.activeAbundanceContext);

            // Fall back to random general question
            return GeneralQuestion();
        }

        private static QuizQuestion ElementQuestion(string symbol)
        {
            var loader = Object.FindAnyObjectByType<ElementLoader>();
            Data.AtomElementData elem = null;
            if (loader?.Table?.elements != null)
                foreach (var e in loader.Table.elements) if (e.symbol == symbol) { elem = e; break; }
            if (elem == null) return GeneralQuestion();

            var q = new QuizQuestion();
            int roll = Random.Range(0, 3);
            switch (roll)
            {
                case 0:
                    q.question     = $"What is the atomic number of {elem.name} ({elem.symbol})?";
                    var nums       = ShuffledDistractors(elem.number, 1, 118);
                    q.choices      = nums;
                    q.correctIndex = nums.IndexOf(elem.number.ToString());
                    q.explanation  = $"{elem.name} has {elem.number} protons in its nucleus.";
                    break;
                case 1:
                    q.question     = $"What category does {elem.name} ({elem.symbol}) belong to?";
                    q.choices      = new List<string> { elem.category, "Noble gas", "Lanthanide", "Transition metal" };
                    q.choices      = Shuffle(q.choices, elem.category);
                    q.correctIndex = q.choices.IndexOf(elem.category);
                    q.explanation  = $"{elem.name} is classified as: {elem.category}.";
                    break;
                default:
                    q.question     = $"What is the symbol for {elem.name}?";
                    q.choices      = Shuffle(new List<string> { elem.symbol, "Hy", "Me", "Ex" }, elem.symbol);
                    q.correctIndex = q.choices.IndexOf(elem.symbol);
                    q.explanation  = $"The symbol comes from {(elem.name.StartsWith(elem.symbol[0].ToString()) ? "the element's name" : "its Latin name")}.";
                    break;
            }
            return q;
        }

        private static QuizQuestion IsotopeQuestion(string isotopeId)
        {
            var iso = HalfLife.IsotopeCatalog.Find(isotopeId);
            if (iso == null) return GeneralQuestion();
            return new QuizQuestion
            {
                question     = $"What is {iso.id} primarily used for?",
                choices      = Shuffle(new List<string> { iso.use, "Nuclear fuel", "Steel production", "Solar cells" }, iso.use),
                correctIndex = 0,
                explanation  = iso.use
            };
        }

        private static QuizQuestion PhaseQuestion(string substance)
        {
            return new QuizQuestion
            {
                question     = $"At standard pressure, what phase is {substance} in at room temperature (25 °C)?",
                choices      = new List<string> { "Liquid", "Solid", "Gas", "Plasma" },
                correctIndex = substance == "H2O" || substance == "Hg" ? 0 : (substance == "Fe" || substance == "NaCl" ? 1 : 2),
                explanation  = $"{substance}'s boiling and melting points determine its phase at 25 °C, 1 atm."
            };
        }

        private static QuizQuestion SpectroscopyQuestion(string symbol)
        {
            return new QuizQuestion
            {
                question     = $"Why does each element produce a unique spectral fingerprint?",
                choices      = Shuffle(new List<string>
                {
                    "Electrons jump between quantized energy levels, emitting photons of specific wavelengths",
                    "Each atom has a different mass, changing light speed",
                    "Atoms absorb all wavelengths except their complement",
                    "Nuclear spin produces spectral lines"
                }, "Electrons jump between quantized energy levels, emitting photons of specific wavelengths"),
                correctIndex = 0,
                explanation  = "Bohr's model: electrons occupy discrete energy levels. Photon energy = ΔE between levels."
            };
        }

        private static QuizQuestion AbundanceQuestion(string context)
        {
            return new QuizQuestion
            {
                question     = $"Which element is most abundant in {context} by mass?",
                choices      = context == "universe"    ? Shuffle(new List<string> {"Hydrogen","Carbon","Oxygen","Iron"}, "Hydrogen") :
                               context == "earth_crust" ? Shuffle(new List<string> {"Oxygen","Silicon","Iron","Aluminum"}, "Oxygen") :
                               context == "human_body"  ? Shuffle(new List<string> {"Oxygen","Carbon","Hydrogen","Nitrogen"}, "Oxygen") :
                                                          Shuffle(new List<string> {"Hydrogen","Oxygen","Nitrogen","Carbon"}, "Hydrogen"),
                correctIndex = 0,
                explanation  = context == "universe"    ? "Hydrogen makes up ~74% of the universe by mass, forged in the Big Bang." :
                               context == "earth_crust" ? "Oxygen forms ~46% of Earth's crust, mostly in silicate minerals." :
                               context == "human_body"  ? "Oxygen is ~65% of body mass, mostly in water." :
                                                          "The most abundant element in this context."
            };
        }

        private static QuizQuestion GeneralQuestion()
        {
            var questions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    question = "How many protons does a carbon atom have?",
                    choices  = Shuffle(new List<string> {"6","8","12","14"}, "6"),
                    correctIndex = 0,
                    explanation = "Carbon is element 6 — it has 6 protons, defining it as carbon."
                },
                new QuizQuestion
                {
                    question = "Which subatomic particle determines the element's identity?",
                    choices  = Shuffle(new List<string> {"Proton","Neutron","Electron","Photon"}, "Proton"),
                    correctIndex = 0,
                    explanation = "The proton count (atomic number) uniquely identifies each element."
                },
                new QuizQuestion
                {
                    question = "What does 'half-life' mean?",
                    choices  = Shuffle(new List<string>
                    {
                        "Time for half of a radioactive sample to decay",
                        "Time until an atom loses half its electrons",
                        "Half the energy of a nuclear reaction",
                        "The decay rate divided by two"
                    }, "Time for half of a radioactive sample to decay"),
                    correctIndex = 0,
                    explanation = "Half-life is the time for 50% of parent atoms to decay to daughter atoms."
                },
                new QuizQuestion
                {
                    question = "Why does pH 7 represent neutral?",
                    choices  = Shuffle(new List<string>
                    {
                        "[H⁺] = [OH⁻] = 10⁻⁷ mol/L in pure water",
                        "Seven is the middle of the 1–14 scale",
                        "Water contains 7 hydrogen atoms",
                        "Neutral compounds have 7 bonds"
                    }, "[H⁺] = [OH⁻] = 10⁻⁷ mol/L in pure water"),
                    correctIndex = 0,
                    explanation = "At 25 °C, pure water auto-ionizes to give exactly 10⁻⁷ mol/L of both H⁺ and OH⁻."
                },
            };
            return questions[Random.Range(0, questions.Count)];
        }

        private static List<string> Shuffle(List<string> items, string correct)
        {
            var result = new List<string>(items);
            // Fisher-Yates
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            // Ensure correct is at index 0 (QuizController puts answer at correctIndex)
            // The controller already receives choices + correctIndex so we don't need to move it
            return result;
        }

        private static List<string> ShuffledDistractors(int correct, int min, int max)
        {
            var result = new List<string> { correct.ToString() };
            while (result.Count < 4)
            {
                int v = Random.Range(min, max + 1);
                if (!result.Contains(v.ToString())) result.Add(v.ToString());
            }
            return Shuffle(result, correct.ToString());
        }
    }
}
