# GLITCHED question bank: how to add or edit questions

Every `.json` file in this folder is loaded when the game starts, so **teachers and students can add questions without opening Unity**.
In a built game the folder is `GLITCHED_Data/StreamingAssets/Questions/` next to `GLITCHED.exe`.

Files: `CodeCity.json` (Computer Science), `PhysicsValley.json`, `ChemicalChaos.json`, `BioZone.json`, `TheCore.json` (mixed), and `Custom.json` (create it for your own).

**CBSE Class XI topics** (session 2026-27): `Class11_CS.json`, `Class11_Physics.json`, `Class11_Chemistry.json`, `Class11_Biology.json` hold the MCQs that small enemies ask. Each has a `"topic"` field (e.g. `"cs_flow"`) that matches a lesson in `../Lessons/Class11.json`; a lesson terminal teaches that topic, and every small enemy after it asks only that topic's MCQs. Answers short enough for a platform label (13 characters or fewer, no code) are also used by the quiz pits in the parkour runs. These files are written by `Tools/build_class11.py`: edit that script (or the JSON directly) to add questions or lessons.

Every question has `"needsTeacherReview": true`. A teacher should check each one, then change it to `false`.

## Common fields

```json
{
  "id": "CS_LOOP_012",            // unique
  "arc": "CodeCity",              // CodeCity, PhysicsValley, ChemicalChaos, BioZone, TheCore
  "subject": "ComputerScience",
  "concept": "Loops",             // mastery is tracked per concept
  "difficulty": 2,                // 1 Recall, 2 Analysis, 3 Reasoning, 4 Application, 5 Synthesis
  "type": "Debugging",            // one of the 10 types below
  "situation": "Robot repeats forever",
  "prompt": "This loop should print 1 to 5. Tap the bug.",
  "data": { ... },                // depends on the type
  "fastSeconds": 6,               // answering faster than this = CRITICAL hit
  "timeLimit": 0,                 // 0 = automatic (the enemy's wind-up)
  "explanation": "One line: WHY the answer is right.",
  "hint": "Shown after the first boss defeat.",
  "teachingModule": "CS_LoopSandbox",   // CS_LoopSandbox, PH_ProjectileLab, CH_BondLab, BI_FlowTrace
  "needsTeacherReview": true
}
```

Text can use `<b>bold</b>`, `<sub>2</sub>` (H<sub>2</sub>O) and `<sup>2</sup>`.

## The `data` block for each type

| type | data |
|---|---|
| **DragDrop** | `template` with `{0}`, `{1}` slots, `items` list, `answer` (item index per slot) **or** `answerSets` (list of accepted item indices per slot); `reuse: true` lets an item be used twice |
| **Logic** | `mode: "choice"` + `options` + `answerIndex` (optional `code`) · `mode: "order"` + `items` in the CORRECT order (shuffled in game) · `mode: "truth"` + `headers`, `rows` with `"?"` cells, `answers` |
| **Numerical** | `value`, `tolerance`, `unit`, optional `givens` list and `code` |
| **Debugging** | `lines` (code lines), `answerIndex` (the bug), optional `fix` |
| **Coding** | `mode: "blank"`: `lines` with a `____` line, `options`, `answerIndex` · `mode: "arrange"`: `lines` in the correct order. Optional `output` |
| **Simulation** | `sim` (projectile, cart, ohm, pendulum, rate, enzyme, heart, photo), `sliders` [{id,label,min,max,step,value,unit}], `mode: "goal"` + `goal` {metric,min,max,unit} **or** `mode: "read"` + `options` + `answerIndex`; optional `fixed` values |
| **Matching** | `left` and `right` lists (right[i] matches left[i]; shuffled in game) |
| **Prediction** | `options`, `answerIndex`, `outcome` (shown after), `anim` (two_balls, rocket, pendulum, circuit, fizz, neutralise, noble, osmosis, population, induction, print_output) |
| **ShortAnswer** | `accepted` answers and/or `keywords` (a list of synonym groups; every group must match). Spelling mistakes are forgiven (fuzzy matching) |
| **Construction** | `mode: "count"` + `parts` + `required` {"H":2,"O":1} + `target` · `mode: "sequence"` + `parts` + `sequence` |

If a question is broken, the game logs the problem in the Console (or `Player.log`) and skips it.
