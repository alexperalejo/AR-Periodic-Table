// Assets/Scripts/AR/PeriodicTableLayout.cs
//
// Standard periodic-table grid layout. Maps an atomic number (1–118) to a
// (column, row) pair on the textbook table:
//
//   Columns 1–18 across the top of the main block.
//   Rows 1–7 for the main block.
//   Rows 8–9 are the separated lanthanide/actinide block underneath
//   (offset down by one extra row of vertical gap).
//
// Plus a helper that walks a spawned pTableGroup, finds each element by
// either its ElementTile.atomicNumber or by matching the GameObject name
// to a known element symbol, and rewrites local positions to put them in
// the canonical grid. Rotations of direct children are zeroed so the cubes
// align cleanly with the parent root.

using System.Collections.Generic;
using UnityEngine;

namespace PeriodicAR.AR
{
    public static class PeriodicTableLayout
    {
        // Element symbol → atomic number, for the name-based fallback when an
        // ElementTile component isn't present.
        private static readonly Dictionary<string, int> SymbolToNumber = new()
        {
            {"h",1},{"he",2},{"li",3},{"be",4},{"b",5},{"c",6},{"n",7},{"o",8},{"f",9},{"ne",10},
            {"na",11},{"mg",12},{"al",13},{"si",14},{"p",15},{"s",16},{"cl",17},{"ar",18},
            {"k",19},{"ca",20},{"sc",21},{"ti",22},{"v",23},{"cr",24},{"mn",25},{"fe",26},{"co",27},{"ni",28},{"cu",29},{"zn",30},
            {"ga",31},{"ge",32},{"as",33},{"se",34},{"br",35},{"kr",36},
            {"rb",37},{"sr",38},{"y",39},{"zr",40},{"nb",41},{"mo",42},{"tc",43},{"ru",44},{"rh",45},{"pd",46},{"ag",47},{"cd",48},
            {"in",49},{"sn",50},{"sb",51},{"te",52},{"i",53},{"xe",54},
            {"cs",55},{"ba",56},
            {"la",57},{"ce",58},{"pr",59},{"nd",60},{"pm",61},{"sm",62},{"eu",63},{"gd",64},{"tb",65},{"dy",66},{"ho",67},{"er",68},{"tm",69},{"yb",70},{"lu",71},
            {"hf",72},{"ta",73},{"w",74},{"re",75},{"os",76},{"ir",77},{"pt",78},{"au",79},{"hg",80},
            {"tl",81},{"pb",82},{"bi",83},{"po",84},{"at",85},{"rn",86},
            {"fr",87},{"ra",88},
            {"ac",89},{"th",90},{"pa",91},{"u",92},{"np",93},{"pu",94},{"am",95},{"cm",96},{"bk",97},{"cf",98},{"es",99},{"fm",100},{"md",101},{"no",102},{"lr",103},
            {"rf",104},{"db",105},{"sg",106},{"bh",107},{"hs",108},{"mt",109},{"ds",110},{"rg",111},{"cn",112},
            {"nh",113},{"fl",114},{"mc",115},{"lv",116},{"ts",117},{"og",118},
        };

        // Resolve a GameObject's atomic number by checking for an ElementTile
        // component first, then falling back to a name match against known
        // element symbols.
        public static int ResolveAtomicNumber(Transform child)
        {
            var tile = child.GetComponentInChildren<ElementTile>(true);
            if (tile != null && tile.atomicNumber > 0) return tile.atomicNumber;

            string n = child.name?.Trim().ToLowerInvariant() ?? "";
            // Strip "(Clone)" if present, then take the part before any whitespace/underscore.
            int paren = n.IndexOf('(');
            if (paren >= 0) n = n.Substring(0, paren).Trim();
            int sep = n.IndexOfAny(new[] { ' ', '_' });
            if (sep >= 0) n = n.Substring(0, sep);
            return SymbolToNumber.TryGetValue(n, out int z) ? z : 0;
        }

        // Standard textbook layout: returns (column 1..18, row 1..9). Returns
        // (0,0) for unknown atomic numbers.
        public static (int col, int row) GetGridPos(int z)
        {
            // Period 1
            if (z == 1)  return (1,  1);  // H
            if (z == 2)  return (18, 1);  // He

            // Period 2
            if (z == 3)  return (1,  2);  // Li
            if (z == 4)  return (2,  2);  // Be
            if (z >= 5  && z <= 10)  return (z + 8,  2);  // B(5)→13 … Ne(10)→18

            // Period 3
            if (z == 11) return (1,  3);  // Na
            if (z == 12) return (2,  3);  // Mg
            if (z >= 13 && z <= 18)  return (z,       3);  // Al(13)→13 … Ar(18)→18

            // Period 4
            if (z == 19) return (1,  4);  // K
            if (z == 20) return (2,  4);  // Ca
            if (z >= 21 && z <= 36)  return (z - 18,  4);  // Sc(21)→3 … Kr(36)→18

            // Period 5
            if (z == 37) return (1,  5);  // Rb
            if (z == 38) return (2,  5);  // Sr
            if (z >= 39 && z <= 54)  return (z - 36,  5);  // Y(39)→3 … Xe(54)→18

            // Period 6
            if (z == 55) return (1,  6);  // Cs
            if (z == 56) return (2,  6);  // Ba
            if (z >= 57 && z <= 71)  return (z - 54,  8);  // Lanthanides La(57)→3,col … Lu(71)→17,col, row 8
            if (z >= 72 && z <= 86)  return (z - 68,  6);  // Hf(72)→4 … Rn(86)→18

            // Period 7
            if (z == 87) return (1,  7);  // Fr
            if (z == 88) return (2,  7);  // Ra
            if (z >= 89 && z <= 103) return (z - 86,  9);  // Actinides Ac(89)→3,col … Lr(103)→17,col, row 9
            if (z >= 104 && z <= 118)return (z - 100, 7);  // Rf(104)→4 … Og(118)→18

            return (0, 0);
        }

        /// <summary>
        /// Re-positions every direct child of <paramref name="root"/> into the canonical
        /// flat periodic-table grid in the XY plane (Z=0 in root local space). Direct child
        /// rotations are zeroed so each cube aligns cleanly. Element prefab internal
        /// hierarchy and scale are preserved.
        /// </summary>
        /// <param name="root">The pTableGroup(Clone) root.</param>
        /// <param name="cellSize">Size of one grid cell in root-local units. With root scale 0.3 and cellSize 0.18, the world cell is 5.4cm.</param>
        /// <param name="lanthGapRows">Vertical gap between the main block and the separated lanthanide/actinide rows.</param>
        /// <returns>Number of children that were placed.</returns>
        public static int Relayout(Transform root, float cellSize = 0.18f, float lanthGapRows = 0.6f)
        {
            int placed = 0, skipped = 0;
            foreach (Transform child in root)
            {
                int z = ResolveAtomicNumber(child);
                if (z == 0) { skipped++; continue; }

                var (col, row) = GetGridPos(z);
                if (col == 0 || row == 0) { skipped++; continue; }

                // Convert (col, row) to local XY position. Origin at top-left of grid.
                // X grows right with column, Y grows DOWN with row (so we negate Y).
                float effectiveRow = row;
                if (row >= 8) effectiveRow = row + lanthGapRows;  // push lanthanides/actinides down a bit

                child.localRotation = Quaternion.identity;
                child.localPosition = new Vector3(col * cellSize, -effectiveRow * cellSize, 0f);
                placed++;
            }

            Debug.Log($"[PeriodicTableLayout] Relaid {placed} elements; {skipped} skipped (no recognizable atomic number).");
            return placed;
        }
    }
}
