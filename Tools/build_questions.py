# Builds the GLITCHED starter question bank as JSON files in Assets/StreamingAssets/Questions/.
# Teachers can also edit the JSON files directly; this script is just how the starter bank was written.
# Every question carries "needsTeacherReview": true until a teacher has checked it.
import json, os, re

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "StreamingAssets", "Questions")
SUB = str.maketrans({"₀": "0", "₁": "1", "₂": "2", "₃": "3", "₄": "4", "₅": "5", "₆": "6", "₇": "7", "₈": "8", "₉": "9"})


def rich(s):
    """Turn unicode subscripts into TextMeshPro <sub> tags (fonts rarely carry subscript glyphs)."""
    if not isinstance(s, str):
        return s
    return re.sub(r"[₀-₉]+", lambda m: "<sub>" + m.group(0).translate(SUB) + "</sub>", s)


def deep(o):
    if isinstance(o, str):
        return rich(o)
    if isinstance(o, list):
        return [deep(x) for x in o]
    if isinstance(o, dict):
        return {k: deep(v) for k, v in o.items()}
    return o


BANK = {}


def q(arc, subject, id, concept, difficulty, type, prompt, data, explanation, hint, situation="", fast=8, module="", timeLimit=0):
    if type == "Numerical" and "answer" in data:
        data = dict(data); data["value"] = data.pop("answer")
    BANK.setdefault(arc, []).append({
        "id": id, "arc": arc, "subject": subject, "concept": concept, "difficulty": difficulty,
        "type": type, "situation": situation, "prompt": prompt, "data": data,
        "fastSeconds": fast, "timeLimit": timeLimit, "explanation": explanation, "hint": hint,
        "teachingModule": module, "needsTeacherReview": True,
    })


# ---------------------------------------------------------------- CODE CITY (Computer Science, Python as in CBSE)
def cs(*a, **k):
    k.setdefault("module", "CS_LoopSandbox")
    q("CodeCity", "ComputerScience", *a, **k)


cs("CS_BASIC_001", "Programming Basics", 1, "Logic", "Which of these is a VALID Python variable name?",
   {"mode": "choice", "options": ["2fast", "fast_2", "fast-2", "for"], "answerIndex": 1},
   "A name can't start with a digit, can't contain '-', and can't be a keyword like 'for'.",
   "Rules: letters, digits and _ only, no digit first, no keywords.", "A robot can't remember its own name", fast=6)
cs("CS_BASIC_002", "Programming Basics", 1, "Matching", "Pair each built-in function with what it does.",
   {"left": ["print()", "input()", "len()", "int()"], "right": ["shows output", "reads what the user types", "counts the items", "converts to a whole number"]},
   "print shows, input reads, len counts, int converts.", "Read the function names as English words.", "A broken help terminal", fast=10)
cs("CS_VAR_001", "Variables", 1, "Prediction", "What does the terminal print?",
   {"code": "x = 5\nx = x + 3\nprint(x)", "options": ["5", "3", "8", "x + 3"], "answerIndex": 2, "outcome": "x becomes 5 + 3 = 8", "anim": "print_output"},
   "x = x + 3 takes the OLD value of x (5), adds 3, and stores 8 back in x.", "The right side is worked out first, then stored.", "A vending machine counting coins", fast=6)
cs("CS_VAR_002", "Variables", 2, "Prediction", "The glitch swapped two registers. What prints?",
   {"code": "a, b = 4, 7\na, b = b, a\nprint(a, b)", "options": ["4 7", "7 4", "7 7", "Error"], "answerIndex": 1, "outcome": "Python swaps both values at once: a = 7, b = 4", "anim": "print_output"},
   "a, b = b, a evaluates the right side (7, 4) first, then assigns both together, so it swaps.", "Work out the whole right side before assigning.", "Two lifts swapped floors", fast=7)
cs("CS_LOOP_001", "Loops", 1, "Prediction", "How many times does the robot laugh?",
   {"code": "for i in range(3):\n    print('HA')", "options": ["2", "3", "4", "Forever"], "answerIndex": 1, "outcome": "range(3) gives 0, 1, 2 -> HA HA HA", "anim": "print_output"},
   "range(3) produces 0, 1 and 2: three values, so three HAs.", "range(n) gives n values starting at 0.", "A robot stuck laughing", fast=6)
cs("CS_LOOP_002", "Loops", 2, "Debugging", "This loop should print 1 to 5 and then STOP. Tap the bug.",
   {"lines": ["i = 1", "while i <= 5:", "    print(i)", "    i = i - 1"], "answerIndex": 3, "fix": "    i = i + 1"},
   "i goes DOWN (1, 0, -1 ...), so i <= 5 stays true forever: an infinite loop. It must be i = i + 1.",
   "Which way does i move each time the loop runs?", "Robot repeats forever", fast=7)
cs("CS_LOOP_003", "Loops", 2, "DragDrop", "BUG MONSTER uses OFF-BY-ONE! This loop must print 1 to 5. Drag the correct range into the gap.",
   {"template": "for i in range({0}):\n    print(i)", "items": ["1, 5", "1, 6", "0, 5", "5"], "answer": [1]},
   "range stops BEFORE the end value, so range(1, 6) gives 1, 2, 3, 4, 5.", "range(start, stop) never includes stop.", "A lift that skips the top floor", fast=6)
cs("CS_LOOP_004", "Loops", 3, "Coding", "Rebuild the program so it prints the sum of 1 to 10. Arrange the blocks.",
   {"mode": "arrange", "lines": ["total = 0", "for n in range(1, 11):", "    total = total + n", "print(total)"], "output": "55"},
   "Start the total at 0, add every n inside the loop, and print once AFTER the loop finishes.", "Indented lines belong inside the loop.", "A power meter that lost its count", fast=14)
cs("CS_LOOP_005", "Loops", 3, "Numerical", "How many times does count go up?",
   {"code": "count = 0\nfor i in range(2, 20, 3):\n    count += 1\nprint(count)", "answer": 6, "tolerance": 0, "unit": ""},
   "range(2, 20, 3) gives 2, 5, 8, 11, 14, 17: six values.", "List the values: start at 2, jump by 3, stop before 20.", "Counting glitched drones", fast=12)
cs("CS_LOOP_006", "Loops", 4, "Coding", "Make the robot stop when its battery reaches 0. Fill the gap.",
   {"mode": "blank", "lines": ["battery = 10", "while ____:", "    battery = battery - 1"], "options": ["battery > 0", "battery == 0", "battery < 0", "True"], "answerIndex": 0, "output": "battery: 10 -> 0, loop ends"},
   "The loop should keep going WHILE there is charge left: battery > 0. 'True' would never stop.", "A while loop runs as long as its condition is True.", "A malfunctioning robot", fast=9)
cs("CS_LOOP_007", "Loops", 4, "Prediction", "How many stars does this nested loop print?",
   {"code": "for i in range(3):\n    for j in range(2):\n        print('*', end='')", "options": ["5", "6", "3", "2"], "answerIndex": 1, "outcome": "3 outer x 2 inner = 6 stars: ******", "anim": "print_output"},
   "The inner loop runs fully (2 times) for each of the 3 outer runs: 3 x 2 = 6.", "Multiply the counts of nested loops.", "A star-printer gone wild", fast=8)
cs("CS_LOOP_008", "Loops", 2, "ShortAnswer", "What do we call a loop whose condition NEVER becomes False?",
   {"accepted": ["infinite loop", "endless loop", "infinite"], "keywords": [["infinite", "endless", "never ending", "forever", "infinity"]], "placeholder": "type your answer"},
   "An infinite loop never stops because its condition never becomes False, like Lord Infinite Loop himself.", "Think of the Lord's name.", "Lord Infinite Loop", fast=10)
cs("CS_LOOP_009", "Loops", 5, "Coding", "FINAL TEST: Rebuild the GLITCH-counter. Multiples of 15 print GLITCH, multiples of 3 print GLI, others print the number.",
   {"mode": "arrange", "lines": ["for n in range(1, 16):", "    if n % 15 == 0:", "        print('GLITCH')", "    elif n % 3 == 0:", "        print('GLI')", "    else:", "        print(n)"], "output": "1 2 GLI 4 5 GLI 7 8 GLI 10 11 GLI 13 14 GLITCH"},
   "Check the most specific condition (15) first. If % 3 came first, 15 would print GLI and never GLITCH.", "Order matters in if / elif: the first true branch wins.", "Lord Infinite Loop's core", fast=25)
cs("CS_LOOP_010", "Loops", 5, "Numerical", "What value of x leaks out of the loop?",
   {"code": "x = 1\nfor i in range(5):\n    x = x * 2\nprint(x)", "answer": 32, "tolerance": 0, "unit": ""},
   "x doubles five times: 1 -> 2 -> 4 -> 8 -> 16 -> 32.", "Track x after each turn of the loop.", "A self-copying virus", fast=12)
cs("CS_LOOP_011", "Loops", 4, "Debugging", "This countdown should print 5, 4, 3, 2, 1. Nothing prints! Tap the bug.",
   {"lines": ["for n in range(5, 0):", "    print(n)"], "answerIndex": 0, "fix": "for n in range(5, 0, -1):"},
   "range(5, 0) counts UP from 5 and stops before 0, which is immediately empty. Counting down needs a step of -1.",
   "Which direction does range count by default?", "A launch countdown that never starts", fast=9)
cs("CS_LOOP_012", "Loops", 3, "Logic", "The lift loop runs while floor < 4, starting at floor 1 and going up 1 each time. Which floors does it visit inside the loop?",
   {"mode": "choice", "options": ["1, 2, 3", "1, 2, 3, 4", "2, 3, 4", "0, 1, 2, 3"], "answerIndex": 0},
   "It runs for 1, 2 and 3. When floor becomes 4 the condition 4 < 4 is False and the loop stops.", "Test the condition each time BEFORE the body runs.", "A lift that goes nowhere", fast=10)
cs("CS_COND_001", "Conditions", 1, "Prediction", "The weather tower reads 35. What does it display?",
   {"code": "temp = 35\nif temp > 30:\n    print('HOT')\nelse:\n    print('OK')", "options": ["HOT", "OK", "HOT OK", "Nothing"], "answerIndex": 0, "outcome": "35 > 30 is True, so only HOT prints", "anim": "print_output"},
   "35 > 30 is True, so the if-branch runs and the else-branch is skipped.", "Only one branch of if/else ever runs.", "A broken weather tower", fast=6)
cs("CS_COND_002", "Conditions", 2, "Debugging", "The exam gate won't compile. Tap the broken line.",
   {"lines": ["marks = 75", "if marks >= 33", "    print('Pass')", "else:", "    print('Fail')"], "answerIndex": 1, "fix": "if marks >= 33:"},
   "Every if statement must end with a colon ':'.", "Look at the end of each line that opens a block.", "A jammed door", fast=7)
cs("CS_COND_003", "Conditions", 3, "Logic", "x = 15. Which output appears?",
   {"mode": "choice", "code": "if x > 20: print('A')\nelif x > 10: print('B')\nelif x > 5: print('C')\nelse: print('D')", "options": ["A", "B", "B C", "C"], "answerIndex": 1},
   "15 > 10 is the first TRUE test, so B prints and the rest of the chain is skipped, even though 15 > 5 is also true.", "In an if/elif chain, only the FIRST true branch runs.", "Traffic light logic", fast=9)
cs("CS_COND_004", "Conditions", 4, "Coding", "Fix the calendar terminal: it should say 'Leap year' correctly.",
   {"mode": "blank", "lines": ["year = 2024", "if ____:", "    print('Leap year')"], "options": ["year % 4 == 0 and (year % 100 != 0 or year % 400 == 0)", "year % 4 == 0 or year % 100 == 0", "year / 4 == 0", "year % 400 == 4"], "answerIndex": 0, "output": "Leap year"},
   "Leap years divide by 4, except century years, which must also divide by 400 (2000 yes, 1900 no).", "Remember the century rule.", "A calendar that lost a day", fast=14)
cs("CS_BOOL_001", "Boolean Logic", 2, "Logic", "Repair the AND gate's truth table. Tap the ? cells to set 0 or 1.",
   {"mode": "truth", "headers": ["A", "B", "A and B"], "rows": [["0", "0", "?"], ["0", "1", "?"], ["1", "0", "?"], ["1", "1", "?"]], "answers": ["0", "0", "0", "1"]},
   "AND is 1 only when BOTH inputs are 1.", "Both, or nothing.", "A security gate", fast=12)
cs("CS_BOOL_002", "Boolean Logic", 3, "Logic", "What does the door controller evaluate?",
   {"mode": "choice", "code": "not (True and False) or False", "options": ["True", "False", "Error", "None"], "answerIndex": 0},
   "True and False = False; not False = True; True or False = True.", "Brackets first, then not, then and, then or.", "A door that argues with itself", fast=10)
cs("CS_BOOL_003", "Boolean Logic", 3, "Construction", "Build the lock rule: the vault opens only when KEYCARD is present AND the ALARM is NOT on. Place the blocks in order.",
   {"mode": "sequence", "display": "chain", "parts": ["KEYCARD", "AND", "NOT", "ALARM", "OR"], "sequence": ["KEYCARD", "AND", "NOT", "ALARM"]},
   "KEYCARD AND NOT ALARM: true only if the card is there and the alarm is off.", "NOT goes right before the thing it flips.", "A data vault", fast=14)
cs("CS_BOOL_004", "Boolean Logic", 4, "Logic", "Repair the XOR gate. XOR outputs 1 when the inputs are DIFFERENT.",
   {"mode": "truth", "headers": ["A", "B", "A xor B"], "rows": [["0", "0", "?"], ["0", "1", "?"], ["1", "0", "?"], ["1", "1", "?"]], "answers": ["0", "1", "1", "0"]},
   "XOR is exclusive OR: 1 when exactly one input is 1.", "Same inputs give 0, different inputs give 1.", "A toggle switch", fast=12)
cs("CS_BIN_001", "Binary", 1, "Numerical", "The door code is binary 1011. Type it in decimal.",
   {"answer": 11, "tolerance": 0, "unit": "", "givens": ["Place values: 8 4 2 1"]},
   "1011 = 8 + 0 + 2 + 1 = 11.", "Add the place values that have a 1.", "A binary keypad", fast=8)
cs("CS_BIN_002", "Binary", 2, "Numerical", "Write decimal 25 in binary (type only 0s and 1s).",
   {"answer": 11001, "tolerance": 0, "unit": "", "givens": ["Place values: 16 8 4 2 1"]},
   "25 = 16 + 8 + 1, so the bits are 1 1 0 0 1.", "Take the biggest place value that fits, then repeat.", "A data car's licence plate", fast=12)
cs("CS_BIN_003", "Binary", 2, "Matching", "Pair each unit of data with its size.",
   {"left": ["1 bit", "1 nibble", "1 byte", "1 KB"], "right": ["a single 0 or 1", "4 bits", "8 bits", "1024 bytes"]},
   "bit < nibble (4 bits) < byte (8 bits) < kilobyte (1024 bytes).", "A nibble is half a byte.", "A memory warehouse", fast=10)
cs("CS_BIN_004", "Binary", 3, "DragDrop", "Set the bits so the register holds 13. Drag 0 or 1 into each slot.",
   {"template": "8:{0}  4:{1}  2:{2}  1:{3}", "items": ["0", "1"], "reuse": True, "answer": [1, 1, 0, 1]},
   "13 = 8 + 4 + 1, so 1101.", "Which place values add up to 13?", "A register reboot", fast=12)
cs("CS_ALGO_001", "Algorithms", 2, "Logic", "Put the steps of 'find the largest number in a list' in order.",
   {"mode": "order", "items": ["Set max = the first item", "Move to the next item", "If this item > max, set max = this item", "Repeat until the list ends", "Output max"]},
   "Start with a guess, compare every item once, update when something bigger appears, output at the end.", "You need a starting value before you can compare.", "A broken scoreboard", fast=16)
cs("CS_ALGO_002", "Algorithms", 4, "Numerical", "Binary search halves a sorted list each step. What is the SMALLEST number of halvings that shrinks 1000 items down to 1?",
   {"answer": 10, "tolerance": 0, "unit": "steps", "givens": ["2^9 = 512", "2^10 = 1024"]},
   "Each halving divides by 2. 2^10 = 1024 >= 1000, so 10 halvings are enough, and 9 (512) are not.", "Find the smallest power of 2 that is at least 1000.", "Searching the city archive", fast=14)
cs("CS_ALGO_003", "Algorithms", 3, "Prediction", "The sorting bot runs. What is printed?",
   {"code": "nums = [5, 2, 9, 1]\nnums.sort()\nprint(nums[0])", "options": ["5", "1", "9", "2"], "answerIndex": 1, "outcome": "sorted -> [1, 2, 5, 9], first item is 1", "anim": "print_output"},
   "sort() arranges ascending: [1, 2, 5, 9]. Index 0 is the smallest: 1.", "Index 0 is the first item AFTER sorting.", "A sorting conveyor", fast=8)
cs("CS_ALGO_004", "Algorithms", 2, "Numerical", "Linear search checks names one by one. In a list of 50 names, how many checks in the WORST case?",
   {"answer": 50, "tolerance": 0, "unit": "checks"},
   "Worst case: the name is last (or missing), so all 50 are checked.", "What if the name is at the very end?", "Finding a lost NPC", fast=8)
cs("CS_DATA_001", "Data Types", 1, "Matching", "Pair each value with its Python data type.",
   {"left": ["42", "3.14", "'Hello'", "True"], "right": ["int", "float", "str", "bool"]},
   "Whole number int, decimal float, text in quotes str, True/False bool.", "Quotes always mean text.", "Sorting data packets", fast=8)
cs("CS_DATA_002", "Data Types", 2, "Prediction", "What does the list terminal print?",
   {"code": "nums = [10, 20, 30, 40]\nprint(nums[1] + nums[-1])", "options": ["30", "50", "60", "70"], "answerIndex": 2, "outcome": "nums[1] = 20, nums[-1] = 40 -> 60", "anim": "print_output"},
   "Index 1 is the SECOND item (20) and index -1 is the LAST item (40). 20 + 40 = 60.", "Indexes start at 0; -1 counts from the end.", "A data train", fast=9)
cs("CS_DATA_003", "Data Types", 3, "Debugging", "The birthday bot crashes with a TypeError. Tap the bug.",
   {"lines": ["name = input('Name: ')", "age = input('Age: ')", "next_year = age + 1", "print(name, next_year)"], "answerIndex": 2, "fix": "next_year = int(age) + 1"},
   "input() always returns text (str). Text + number is a TypeError, so convert with int(age) first.", "What type does input() give back?", "A birthday bot", fast=10)
cs("CS_SEC_001", "Cybersecurity", 1, "Logic", "Which password would survive the Glitch's guessing attack longest?",
   {"mode": "choice", "options": ["password123", "Aarav2009", "T!g3r#Moon_88", "qwerty"], "answerIndex": 2},
   "Long passwords mixing upper/lower case, digits and symbols are hardest to guess. Names, dates and keyboard patterns are easy.", "Length + variety = strength.", "A password door", fast=6)
cs("CS_SEC_002", "Cybersecurity", 2, "Matching", "Pair each cyber-term with its meaning.",
   {"left": ["Phishing", "Malware", "Firewall", "Encryption"], "right": ["a fake message that tricks you into giving data", "software designed to cause harm", "filters network traffic using rules", "scrambles data so only key-holders can read it"]},
   "Phishing tricks people, malware harms systems, firewalls filter, encryption scrambles.", "Phishing sounds like fishing: it uses bait.", "The city's security wall", fast=12)
cs("CS_SEC_003", "Cybersecurity", 3, "ShortAnswer", "An email says: 'Your account is locked! Click here and type your password.' What kind of attack is this?",
   {"accepted": ["phishing", "phishing attack"], "keywords": [["phishing", "phish", "fishing"]], "placeholder": "name the attack"},
   "Phishing: a fake message pretending to be trustworthy, to steal passwords or data.", "It uses bait.", "A suspicious hologram", fast=10)

# ---------------------------------------------------------------- PHYSICS VALLEY
def ph(*a, **k):
    k.setdefault("module", "PH_ProjectileLab")
    q("PhysicsValley", "Physics", *a, **k)


ph("PH_MOT_001", "Motion", 1, "Numerical", "A runaway cart covers 120 m in 8 s at constant speed. What is its speed?",
   {"answer": 15, "tolerance": 0.1, "unit": "m/s", "givens": ["speed = distance / time"]},
   "speed = 120 m / 8 s = 15 m/s.", "Divide distance by time.", "A runaway cart", fast=8)
ph("PH_MOT_002", "Motion", 2, "Numerical", "A glider starts from rest and accelerates at 3 m/s² for 4 s. Final velocity?",
   {"answer": 12, "tolerance": 0.1, "unit": "m/s", "givens": ["v = u + at", "u = 0"]},
   "v = 0 + 3 x 4 = 12 m/s.", "Use v = u + at with u = 0.", "A glider in wrong gravity", fast=9)
ph("PH_MOT_003", "Motion", 3, "Numerical", "From rest, a = 2 m/s² for 5 s. How far does it travel?",
   {"answer": 25, "tolerance": 0.1, "unit": "m", "givens": ["s = ut + 1/2 at²", "u = 0"]},
   "s = 0 + 1/2 x 2 x 5² = 25 m.", "Square the time first.", "A falling elevator", fast=11)
ph("PH_MOT_004", "Motion", 2, "Matching", "Pair each quantity with its definition.",
   {"left": ["Distance", "Displacement", "Speed", "Velocity"], "right": ["total path length (scalar)", "straight-line change in position (vector)", "distance per unit time (scalar)", "displacement per unit time (vector)"]},
   "Scalars have only size; vectors also have direction.", "Which ones care about direction?", "A confused compass", fast=12)
ph("PH_MOT_005", "Motion", 4, "Simulation", "Launch the probe so it lands on the platform 40 m away (g = 10 m/s²). Adjust, then press RUN.",
   {"sim": "projectile", "mode": "goal", "sliders": [{"id": "v", "label": "Speed", "min": 5, "max": 30, "step": 0.5, "value": 15, "unit": "m/s"}, {"id": "angle", "label": "Angle", "min": 10, "max": 80, "step": 1, "value": 30, "unit": "°"}],
    "fixed": {"g": 10}, "goal": {"metric": "range", "min": 37, "max": 43, "unit": "m"}},
   "Range R = v² sin(2θ) / g. At 45° and 20 m/s, R = 400 x 1 / 10 = 40 m.", "45° gives the longest range for a given speed.", "Launching across a broken bridge", fast=20)
ph("PH_MOT_006", "Motion", 3, "Prediction", "Ball A is DROPPED. Ball B is thrown HORIZONTALLY from the same height at the same moment. Which lands first? (no air)",
   {"options": ["A", "B", "Both together", "The heavier one"], "answerIndex": 2, "outcome": "Both land together: horizontal speed doesn't change vertical fall.", "anim": "two_balls"},
   "Vertical motion is independent of horizontal motion. Both start with zero vertical speed and fall with the same g.", "Separate up-down from sideways.", "Two glitched balls", fast=9)
ph("PH_NEW_001", "Newton's Laws", 1, "Logic", "The cart's engine dies on a frictionless slope but it keeps rolling. Which law explains this?",
   {"mode": "choice", "options": ["First law (inertia)", "Second law (F = ma)", "Third law (action-reaction)", "Law of gravitation"], "answerIndex": 0},
   "First law: a body keeps moving at constant velocity unless a net force acts on it.", "Which law is about things carrying on as they are?", "A runaway car on a slope", fast=7)
ph("PH_NEW_002", "Newton's Laws", 2, "Numerical", "A 50 kg robot must accelerate at 2 m/s². What net force is needed?",
   {"answer": 100, "tolerance": 0.5, "unit": "N", "givens": ["F = ma"]},
   "F = 50 x 2 = 100 N.", "Multiply mass by acceleration.", "A malfunctioning robot", fast=8)
ph("PH_NEW_003", "Newton's Laws", 2, "Prediction", "A rocket pushes hot gas DOWN out of its nozzle. What happens?",
   {"options": ["The gas pushes the rocket UP", "The rocket stays still", "The rocket moves down", "The rocket spins"], "answerIndex": 0, "outcome": "Action: rocket pushes gas down. Reaction: gas pushes rocket up. LIFT-OFF!", "anim": "rocket"},
   "Third law: forces come in equal and opposite pairs. The gas pushes back on the rocket.", "Every action has an equal and opposite...", "A launching rocket", fast=7)
ph("PH_NEW_004", "Newton's Laws", 3, "Numerical", "A 0.5 kg ball goes from 0 to 20 m/s in 0.1 s when kicked. Average force on the ball?",
   {"answer": 100, "tolerance": 0.5, "unit": "N", "givens": ["a = (v - u) / t", "F = ma"]},
   "a = 20 / 0.1 = 200 m/s². F = 0.5 x 200 = 100 N.", "Find the acceleration first.", "A kicked ball", fast=12)
ph("PH_NEW_005", "Newton's Laws", 4, "Simulation", "The robot must accelerate at exactly 4 m/s² to escape. Set force and mass, then RUN.",
   {"sim": "cart", "mode": "goal", "sliders": [{"id": "F", "label": "Force", "min": 0, "max": 200, "step": 5, "value": 50, "unit": "N"}, {"id": "m", "label": "Mass", "min": 5, "max": 50, "step": 1, "value": 20, "unit": "kg"}],
    "goal": {"metric": "a", "min": 3.8, "max": 4.2, "unit": "m/s²"}},
   "a = F / m. For example 100 N on 25 kg gives 4 m/s².", "Pick a mass, then force = 4 x mass.", "A malfunctioning robot", fast=18)
ph("PH_NEW_006", "Newton's Laws", 3, "ShortAnswer", "What is the property of a body to resist any change in its state of rest or motion?",
   {"accepted": ["inertia"], "keywords": [["inertia", "inertial"]], "placeholder": "one word"},
   "Inertia. More mass means more inertia.", "Newton's first law is also called the law of ___.", "A stubborn boulder", fast=8)
ph("PH_NEW_007", "Newton's Laws", 5, "Numerical", "Forces of 30 N east and 10 N west act on a 4 kg block (no friction). Its acceleration?",
   {"answer": 5, "tolerance": 0.05, "unit": "m/s²", "givens": ["net force first", "F = ma"]},
   "Net force = 30 - 10 = 20 N east. a = 20 / 4 = 5 m/s².", "Opposite forces subtract.", "A tug-of-war with the Lord", fast=12)
ph("PH_NEW_008", "Newton's Laws", 5, "Simulation", "FINAL TEST: The Lord broke gravity! Here g = 5 m/s². Land the probe on the core 60 m away.",
   {"sim": "projectile", "mode": "goal", "sliders": [{"id": "v", "label": "Speed", "min": 5, "max": 30, "step": 0.5, "value": 12, "unit": "m/s"}, {"id": "angle", "label": "Angle", "min": 10, "max": 80, "step": 1, "value": 30, "unit": "°"}],
    "fixed": {"g": 5}, "goal": {"metric": "range", "min": 57, "max": 63, "unit": "m"}},
   "R = v² sin(2θ) / g. With g = 5 and θ = 45°, you need v² = 300, so v ≈ 17.3 m/s.", "Weaker gravity means the same throw goes further.", "The Lord of Broken Laws' core", fast=25)
ph("PH_EN_001", "Work & Energy", 1, "Numerical", "You push a crate with 20 N for 5 m in the direction of the force. Work done?",
   {"answer": 100, "tolerance": 0.5, "unit": "J", "givens": ["W = F x d"]},
   "W = 20 x 5 = 100 J.", "Multiply force by distance.", "Pushing a stuck crate", fast=7)
ph("PH_EN_002", "Work & Energy", 2, "Numerical", "Kinetic energy of a 2 kg ball moving at 3 m/s?",
   {"answer": 9, "tolerance": 0.05, "unit": "J", "givens": ["KE = 1/2 m v²"]},
   "KE = 1/2 x 2 x 3² = 9 J.", "Square the speed first.", "A rolling boulder", fast=9)
ph("PH_EN_003", "Work & Energy", 3, "Numerical", "Potential energy of a 5 kg rock lifted 4 m (g = 10 m/s²)?",
   {"answer": 200, "tolerance": 0.5, "unit": "J", "givens": ["PE = m g h"]},
   "PE = 5 x 10 x 4 = 200 J.", "Multiply all three.", "A floating rock island", fast=9)
ph("PH_EN_004", "Work & Energy", 4, "Prediction", "A pendulum is released from a height (no friction). At the LOWEST point it has...",
   {"options": ["Maximum KE, minimum PE", "Maximum PE, zero KE", "Zero energy", "Maximum KE AND maximum PE"], "answerIndex": 0, "outcome": "All the PE has turned into KE: fastest at the bottom.", "anim": "pendulum"},
   "Energy is conserved: height energy (PE) turns into motion energy (KE), so the bottom is fastest.", "Where is it moving fastest?", "A giant pendulum", fast=9)
ph("PH_EN_005", "Work & Energy", 3, "Matching", "Pair each device with the energy change it makes.",
   {"left": ["Battery", "Solar panel", "Electric motor", "Loudspeaker"], "right": ["chemical -> electrical", "light -> electrical", "electrical -> mechanical", "electrical -> sound"]},
   "Devices convert energy from one form to another; the total is conserved.", "What goes in, what comes out?", "A power station", fast=12)
ph("PH_EN_006", "Work & Energy", 5, "Numerical", "A 1 kg ball drops from 5 m. Speed just before it hits the ground? (g = 10, no air)",
   {"answer": 10, "tolerance": 0.1, "unit": "m/s", "givens": ["mgh = 1/2 m v²"]},
   "mgh = 1/2 mv², so v = √(2gh) = √100 = 10 m/s. The mass cancels!", "Set PE at the top equal to KE at the bottom.", "A falling observatory", fast=14)
ph("PH_GRAV_001", "Gravitation", 1, "Logic", "On the Moon, g is about 1/6 of Earth's. Your MASS there is...",
   {"mode": "choice", "options": ["the same", "1/6 as much", "6 times more", "zero"], "answerIndex": 0},
   "Mass is the amount of matter and doesn't change. WEIGHT (mg) becomes 1/6.", "Mass and weight are different things.", "A trip to the Moon", fast=7)
ph("PH_GRAV_002", "Gravitation", 2, "Simulation", "Play with the pendulum. What happens to the time period when the length becomes 4x?",
   {"sim": "pendulum", "mode": "read", "sliders": [{"id": "L", "label": "Length", "min": 0.5, "max": 4, "step": 0.1, "value": 1, "unit": "m"}, {"id": "g", "label": "Gravity", "min": 1, "max": 20, "step": 0.5, "value": 9.8, "unit": "m/s²"}],
    "options": ["It doubles", "It becomes 4x", "It halves", "No change"], "answerIndex": 0},
   "T = 2π√(L/g). 4x the length gives √4 = 2x the period.", "Look for a square root in the formula.", "A giant pendulum", fast=20)
ph("PH_GRAV_003", "Gravitation", 4, "Numerical", "The distance between two masses DOUBLES. The gravitational force becomes what fraction? (type a decimal)",
   {"answer": 0.25, "tolerance": 0.01, "unit": "x", "givens": ["F ∝ 1 / r²"]},
   "F ∝ 1/r². Double r gives 1/2² = 1/4 = 0.25 of the force.", "Inverse SQUARE law.", "Drifting islands", fast=12)
ph("PH_GRAV_004", "Gravitation", 3, "Numerical", "A stone is dropped from rest and falls for 3 s (g = 10 m/s²). Its speed?",
   {"answer": 30, "tolerance": 0.1, "unit": "m/s", "givens": ["v = u + gt"]},
   "v = 0 + 10 x 3 = 30 m/s.", "Free fall is just v = u + at with a = g.", "Debris falling the right way for once", fast=9)
ph("PH_ELEC_001", "Electricity", 1, "Numerical", "2 A flows through a 6 Ω resistor. Voltage across it?",
   {"answer": 12, "tolerance": 0.05, "unit": "V", "givens": ["V = I R"]},
   "V = 2 x 6 = 12 V.", "Ohm's law.", "A Tesla tower fuse", fast=7)
ph("PH_ELEC_002", "Electricity", 2, "Simulation", "Power the glitched gate: set voltage and resistance so the current is 0.5 A, then RUN.",
   {"sim": "ohm", "mode": "goal", "sliders": [{"id": "V", "label": "Voltage", "min": 1, "max": 24, "step": 0.5, "value": 6, "unit": "V"}, {"id": "R", "label": "Resistance", "min": 2, "max": 48, "step": 1, "value": 6, "unit": "Ω"}],
    "goal": {"metric": "I", "min": 0.48, "max": 0.52, "unit": "A"}},
   "I = V / R. For example 6 V across 12 Ω gives 0.5 A.", "Resistance = 2 x voltage gives 0.5 A.", "A Tesla gate", fast=18)
ph("PH_ELEC_003", "Electricity", 3, "Numerical", "Three 6 Ω resistors are joined in SERIES. Total resistance?",
   {"answer": 18, "tolerance": 0.05, "unit": "Ω", "givens": ["Series: R = R1 + R2 + R3"]},
   "In series, resistances add: 6 + 6 + 6 = 18 Ω.", "Series means one after another.", "A chain of towers", fast=8)
ph("PH_ELEC_004", "Electricity", 4, "Numerical", "Two 6 Ω resistors in PARALLEL. Equivalent resistance?",
   {"answer": 3, "tolerance": 0.05, "unit": "Ω", "givens": ["Parallel: 1/R = 1/R1 + 1/R2"]},
   "1/R = 1/6 + 1/6 = 1/3, so R = 3 Ω. Parallel paths make it LESS.", "More paths, less resistance.", "Twin lightning rods", fast=12)
ph("PH_ELEC_005", "Electricity", 2, "DragDrop", "Wire up the emergency light. Drag each part into its job.",
   {"template": "Energy source: {0}\nControl: {1}\nLoad: {2}", "items": ["Cell", "Switch", "Bulb", "Rubber band"], "answer": [0, 1, 2]},
   "A circuit needs a source (cell), a control (switch) and a load (bulb) in a closed loop. Rubber doesn't conduct.", "Which one is an insulator?", "Connect the circuit parts", fast=10)
ph("PH_ELEC_006", "Electricity", 3, "Prediction", "Two bulbs are in SERIES. One burns out. The other...",
   {"options": ["goes out too", "gets brighter", "stays the same", "flickers forever"], "answerIndex": 0, "outcome": "The loop is broken, so no current flows and both go dark.", "anim": "circuit"},
   "Series has only one path. A broken bulb breaks the whole loop.", "How many paths does the current have?", "Festival lights", fast=8)
ph("PH_MAG_001", "Magnetism", 1, "Logic", "Two magnets meet north pole to north pole. They...",
   {"mode": "choice", "options": ["repel", "attract", "do nothing", "swap poles"], "answerIndex": 0},
   "Like poles repel; unlike poles attract.", "Same pushes away.", "Floating magnet bridge", fast=6)
ph("PH_MAG_002", "Magnetism", 3, "ShortAnswer", "A current-carrying coil wound on a soft iron core acts as a temporary magnet. What is it called?",
   {"accepted": ["electromagnet", "an electromagnet"], "keywords": [["electromagnet", "electro magnet", "electro-magnet"]], "placeholder": "one word"},
   "An electromagnet: magnetic only while current flows.", "Electricity + magnet.", "A scrapyard crane", fast=9)
ph("PH_MAG_003", "Magnetism", 4, "Prediction", "You push a magnet quickly INTO a coil connected to a galvanometer. The needle...",
   {"options": ["deflects: a current is induced", "stays at zero", "points north", "melts"], "answerIndex": 0, "outcome": "Changing magnetic flux induces a current (Faraday). Stop moving and it returns to zero.", "anim": "induction"},
   "Electromagnetic induction: a CHANGING magnetic field through a coil induces an EMF and a current.", "Is the magnetic field changing?", "A Tesla generator", fast=10)
ph("PH_WAVE_001", "Waves", 1, "Numerical", "A wave has frequency 50 Hz and wavelength 2 m. Its speed?",
   {"answer": 100, "tolerance": 0.5, "unit": "m/s", "givens": ["v = f λ"]},
   "v = 50 x 2 = 100 m/s.", "Multiply frequency by wavelength.", "A rippling sky", fast=7)
ph("PH_WAVE_002", "Waves", 2, "Matching", "Pair each wave with its description.",
   {"left": ["Sound", "Light", "Wave on a string", "Ultrasound"], "right": ["mechanical longitudinal wave", "electromagnetic wave, needs no medium", "transverse mechanical wave", "frequency above 20 kHz"]},
   "Sound needs a medium and is longitudinal; light is electromagnetic; ultrasound is above human hearing.", "Which one can cross empty space?", "A broken radio tower", fast=12)
ph("PH_WAVE_003", "Waves", 3, "Numerical", "You shout at a cliff; the echo returns after 2 s. Speed of sound 340 m/s. Distance to the cliff?",
   {"answer": 340, "tolerance": 1, "unit": "m", "givens": ["sound travels there AND back"]},
   "Total path = 340 x 2 = 680 m, and that's there and back, so the cliff is 340 m away.", "Don't forget the return trip.", "An echo canyon", fast=12)

# ---------------------------------------------------------------- CHEMICAL CHAOS
def ch(*a, **k):
    k.setdefault("module", "CH_BondLab")
    q("ChemicalChaos", "Chemistry", *a, **k)


ch("CH_ATOM_001", "Atoms", 1, "Matching", "Pair each particle with its description.",
   {"left": ["Proton", "Neutron", "Electron", "Nucleus"], "right": ["positive charge, inside the nucleus", "no charge, inside the nucleus", "negative charge, around the nucleus", "the tiny dense centre of the atom"]},
   "Protons (+) and neutrons (0) sit in the nucleus; electrons (-) move around it.", "Neutron sounds like neutral.", "An atom creature", fast=10)
ch("CH_ATOM_002", "Atoms", 2, "Numerical", "A sodium atom has atomic number 11 and mass number 23. How many neutrons?",
   {"answer": 12, "tolerance": 0, "unit": "", "givens": ["neutrons = mass number - atomic number"]},
   "23 - 11 = 12 neutrons.", "Mass number counts protons + neutrons.", "A sodium creature", fast=8)
ch("CH_ATOM_003", "Atoms", 2, "Logic", "Electronic configuration of sodium (Z = 11)?",
   {"mode": "choice", "options": ["2, 8, 1", "2, 9", "2, 8, 2", "8, 2, 1"], "answerIndex": 0},
   "Shells fill 2, then 8, then the rest: 2, 8, 1. One valence electron is why sodium is so reactive.", "First shell holds 2, second holds 8.", "A sodium creature", fast=8)
ch("CH_ATOM_004", "Atoms", 3, "ShortAnswer", "Atoms of the same element with DIFFERENT numbers of neutrons are called...?",
   {"accepted": ["isotopes", "isotope"], "keywords": [["isotope"]], "placeholder": "one word"},
   "Isotopes, like carbon-12 and carbon-14: same protons, different neutrons.", "Iso = same (place in the periodic table).", "Twin creatures", fast=9)
ch("CH_ATOM_005", "Atoms", 3, "Numerical", "Maximum number of electrons in the shell n = 3? (use 2n²)",
   {"answer": 18, "tolerance": 0, "unit": "electrons", "givens": ["max = 2n²"]},
   "2 x 3² = 18.", "Square n, then double.", "The atom lift", fast=8)
ch("CH_ELEM_001", "Elements", 1, "Matching", "The element creatures forgot their names. Pair symbol to element.",
   {"left": ["Na", "K", "Fe", "Au"], "right": ["Sodium", "Potassium", "Iron", "Gold"]},
   "These symbols come from Latin: Natrium, Kalium, Ferrum, Aurum.", "The symbols come from Latin names.", "Element creatures", fast=10)
ch("CH_ELEM_002", "Elements", 1, "DragDrop", "Sort the creatures into the right cages.",
   {"template": "Metal: {0}\nMetal: {1}\nNon-metal: {2}\nNon-metal: {3}", "items": ["Copper", "Magnesium", "Sulfur", "Oxygen"], "answerSets": [[0, 1], [0, 1], [2, 3], [2, 3]]},
   "Copper and magnesium are metals (shiny, conduct). Sulfur and oxygen are non-metals.", "Which ones conduct electricity?", "Element cages", fast=12)
ch("CH_ELEM_003", "Elements", 3, "ShortAnswer", "The vertical columns of the periodic table are called...?",
   {"accepted": ["groups", "group"], "keywords": [["group"]], "placeholder": "one word"},
   "Columns are groups (1 to 18); rows are periods (1 to 7).", "Rows are periods. Columns are...", "The periodic wall", fast=8)
ch("CH_PT_001", "Periodic Table", 1, "Logic", "Elements in the same GROUP have the same number of...",
   {"mode": "choice", "options": ["valence electrons", "neutrons", "electron shells", "protons"], "answerIndex": 0},
   "Same group means the same number of outer (valence) electrons, so similar chemistry.", "Outer electrons decide behaviour.", "The periodic wall", fast=7)
ch("CH_PT_002", "Periodic Table", 2, "Prediction", "The Lord drops a small piece of sodium into water. What happens?",
   {"options": ["It fizzes, darts around and may catch fire, making NaOH + H₂", "Nothing", "It dissolves silently like sugar", "The water freezes"], "answerIndex": 0, "outcome": "2Na + 2H₂O -> 2NaOH + H₂. Hydrogen gas fizzes and can ignite!", "anim": "fizz"},
   "Group 1 metals react vigorously with water, making an alkali and hydrogen gas.", "Group 1 metals are very reactive.", "A bubbling vat", fast=9)
ch("CH_PT_003", "Periodic Table", 3, "Logic", "Arrange the Group 1 creatures from SMALLEST to LARGEST atom.",
   {"mode": "order", "items": ["Li", "Na", "K", "Rb"]},
   "Going down a group adds shells, so atomic radius increases: Li < Na < K < Rb.", "Each step down adds a shell.", "A line-up of alkali creatures", fast=12)
ch("CH_PT_004", "Periodic Table", 4, "Logic", "Moving left to right across a period, electronegativity generally...",
   {"mode": "choice", "options": ["increases", "decreases", "stays the same", "drops to zero"], "answerIndex": 0},
   "More protons pull the bonding electrons harder, so electronegativity increases across a period (fluorine is highest).", "Fluorine sits top right.", "A tug-of-war wall", fast=9)
ch("CH_PT_005", "Periodic Table", 2, "Numerical", "Chlorine sits in Period 3, Group 17. What is its atomic number?",
   {"answer": 17, "tolerance": 0, "unit": "", "givens": ["Period 3 starts at Na (11)", "configuration 2, 8, 7"]},
   "Chlorine: 2, 8, 7, so 17 electrons and atomic number 17.", "Count 2 + 8 + 7.", "The halogen cage", fast=10)
ch("CH_MOL_001", "Molecules", 1, "Construction", "Build a WATER molecule to put out the acid fire.",
   {"mode": "count", "display": "molecule", "parts": ["H", "O", "C", "N"], "required": {"H": 2, "O": 1}, "target": "H₂O"},
   "Water is H₂O: two hydrogens bonded to one oxygen.", "The formula tells you the count.", "An acid fire", fast=10)
ch("CH_MOL_002", "Molecules", 2, "Construction", "Build CARBON DIOXIDE.",
   {"mode": "count", "display": "molecule", "parts": ["C", "O", "H", "N"], "required": {"C": 1, "O": 2}, "target": "CO₂"},
   "CO₂: one carbon double-bonded to two oxygens.", "Di means two.", "A smoking chimney", fast=10)
ch("CH_MOL_003", "Molecules", 3, "Construction", "Build METHANE, CH₄.",
   {"mode": "count", "display": "molecule", "parts": ["C", "H", "O", "Cl"], "required": {"C": 1, "H": 4}, "target": "CH₄"},
   "Carbon has 4 valence electrons, so it forms 4 bonds, one with each hydrogen.", "Carbon makes four bonds.", "A gas leak", fast=12)
ch("CH_MOL_004", "Molecules", 2, "Construction", "Build AMMONIA, NH₃.",
   {"mode": "count", "display": "molecule", "parts": ["N", "H", "O", "C"], "required": {"N": 1, "H": 3}, "target": "NH₃"},
   "Nitrogen forms 3 bonds, so NH₃.", "Nitrogen makes three bonds.", "A fertiliser factory", fast=11)
ch("CH_MOL_005", "Molecules", 5, "Construction", "FINAL TEST: Build ETHANOL, C₂H₅OH.",
   {"mode": "count", "display": "molecule", "parts": ["C", "H", "O", "N"], "required": {"C": 2, "H": 6, "O": 1}, "target": "C₂H₅OH"},
   "C₂H₅OH has 2 carbons, 5 + 1 = 6 hydrogens, and 1 oxygen.", "Count the H in both places of the formula.", "The Lord of the Lab's core", fast=20)
ch("CH_MOL_006", "Molecules", 3, "Numerical", "Molar mass of H₂O? (H = 1, O = 16)",
   {"answer": 18, "tolerance": 0.05, "unit": "g/mol", "givens": ["H = 1", "O = 16"]},
   "2 x 1 + 16 = 18 g/mol.", "Add up every atom.", "Weighing a water creature", fast=9)
ch("CH_MOL_007", "Molecules", 4, "Numerical", "How many MOLES are in 88 g of CO₂? (C = 12, O = 16)",
   {"answer": 2, "tolerance": 0.01, "unit": "mol", "givens": ["moles = mass / molar mass"]},
   "Molar mass of CO₂ = 12 + 32 = 44 g/mol. 88 / 44 = 2 mol.", "Find the molar mass first.", "A CO₂ storage tank", fast=12)
ch("CH_BOND_001", "Bonding", 1, "Matching", "Pair each substance with its main type of bond.",
   {"left": ["NaCl", "H₂O", "Cl₂", "Copper metal"], "right": ["ionic", "polar covalent", "non-polar covalent", "metallic"]},
   "Metal + non-metal: ionic. Different non-metals: polar covalent. Same atoms: non-polar covalent. Metal atoms: metallic.", "Who is giving, who is sharing?", "The bonding arena", fast=12)
ch("CH_BOND_002", "Bonding", 2, "Logic", "To form NaCl, the sodium atom...",
   {"mode": "choice", "options": ["gives 1 electron to chlorine", "shares electrons with chlorine", "takes an electron from chlorine", "gives a proton to chlorine"], "answerIndex": 0},
   "Na (2,8,1) loses 1 electron to become Na⁺; Cl (2,8,7) gains it to become Cl⁻. Opposite ions attract: an ionic bond.", "Sodium has 1 spare outer electron.", "Salt creatures refusing to bond", fast=8)
ch("CH_BOND_003", "Bonding", 3, "Numerical", "How many covalent bonds hold the two atoms of an N₂ molecule together?",
   {"answer": 3, "tolerance": 0, "unit": "bonds", "givens": ["N has 5 valence electrons"]},
   "Each N needs 3 more electrons, so they share 3 pairs: a triple bond.", "How many electrons does N need for an octet?", "The unbreakable N₂ lock", fast=10)
ch("CH_BOND_004", "Bonding", 3, "ShortAnswer", "A bond formed by SHARING a pair of electrons is called a ___ bond.",
   {"accepted": ["covalent", "covalent bond"], "keywords": [["covalent", "co-valent"]], "placeholder": "one word"},
   "Covalent: atoms share electron pairs (co = together, valent = bonding).", "Not ionic.", "Two atoms holding hands", fast=8)
ch("CH_BOND_005", "Bonding", 4, "Logic", "What shape does a methane (CH₄) molecule have?",
   {"mode": "choice", "options": ["tetrahedral", "linear", "square planar", "bent"], "answerIndex": 0},
   "VSEPR: four bond pairs spread as far apart as possible, at 109.5°: tetrahedral.", "Four pairs pushing apart in 3D.", "A molecule sculpture", fast=10)
ch("CH_BOND_006", "Bonding", 4, "Prediction", "Why does the Neon creature refuse to bond with ANYONE?",
   {"options": ["Its outer shell is already full (stable octet)", "It's too heavy", "It's a metal", "It has no electrons"], "answerIndex": 0, "outcome": "Neon: 2, 8. A full octet, so it has no reason to gain, lose or share.", "anim": "noble"},
   "Noble gases have complete outer shells, so they are very stable and unreactive.", "Look at neon's configuration: 2, 8.", "A grumpy noble gas", fast=9)
ch("CH_BOND_007", "Bonding", 5, "Logic", "FINAL TEST: Which molecule contains a DOUBLE bond?",
   {"mode": "choice", "options": ["O₂", "H₂", "Cl₂", "CH₄"], "answerIndex": 0},
   "Each O has 6 valence electrons and needs 2 more, so O=O shares two pairs. H₂, Cl₂ and CH₄ have only single bonds.", "Who needs TWO more electrons?", "The Lord's final formula", fast=12)
ch("CH_RXN_001", "Reactions", 1, "Matching", "Pair each reaction pattern with its type.",
   {"left": ["A + B -> AB", "AB -> A + B", "A + BC -> AC + B", "AB + CD -> AD + CB"], "right": ["combination", "decomposition", "displacement", "double displacement"]},
   "Join, break apart, swap one, swap partners.", "Look at what joins and what leaves.", "The reaction factory", fast=12)
ch("CH_RXN_002", "Reactions", 2, "Logic", "An iron nail in blue copper sulphate gets a brown coat and the solution turns green. Reaction type?",
   {"mode": "choice", "options": ["displacement", "decomposition", "combination", "neutralisation"], "answerIndex": 0},
   "Fe + CuSO₄ -> FeSO₄ + Cu. More reactive iron displaces copper.", "Who kicked whom out?", "A rusting catwalk", fast=9)
ch("CH_RXN_003", "Reactions", 3, "Simulation", "Run the reactor. Change temperature and concentration and watch the rate. What speeds it up?",
   {"sim": "rate", "mode": "read", "sliders": [{"id": "T", "label": "Temperature", "min": 10, "max": 80, "step": 1, "value": 25, "unit": "°C"}, {"id": "c", "label": "Concentration", "min": 0.1, "max": 2, "step": 0.1, "value": 0.5, "unit": "M"}],
    "options": ["Raising temperature AND concentration", "Lowering the temperature", "Using less reactant", "Nothing changes it"], "answerIndex": 0},
   "Hotter particles move faster and collide harder; more concentration means more collisions. Both raise the rate.", "Think about how often particles collide.", "A reactor core", fast=20)
ch("CH_RXN_004", "Reactions", 2, "Prediction", "HCl (acid) is poured onto NaOH (base). What forms?",
   {"options": ["Salt + water (neutralisation)", "An explosion of gas", "A metal", "Nothing"], "answerIndex": 0, "outcome": "HCl + NaOH -> NaCl + H₂O. Acid and base cancel out.", "anim": "neutralise"},
   "Acid + base -> salt + water.", "They cancel each other.", "Acid pools", fast=8)
ch("CH_RXN_005", "Reactions", 4, "ShortAnswer", "A reaction that RELEASES heat is called...?",
   {"accepted": ["exothermic", "exothermic reaction"], "keywords": [["exothermic", "exo thermic", "exo-thermic"]], "placeholder": "one word"},
   "Exothermic (exo = out). Endothermic reactions absorb heat.", "Exo means out.", "A burning vat", fast=9)
ch("CH_EQ_001", "Equations", 2, "DragDrop", "Balance the equation. Drag numbers into the gaps.",
   {"template": "{0} H₂ + O₂ -> {1} H₂O", "items": ["1", "2", "3", "4"], "reuse": True, "answer": [1, 1]},
   "2H₂ + O₂ -> 2H₂O: 4 H and 2 O on each side.", "Count O first: 2 on the left means 2 waters.", "The water-making machine", fast=12)
ch("CH_EQ_002", "Equations", 3, "DragDrop", "Balance the ammonia equation.",
   {"template": "N₂ + {0} H₂ -> {1} NH₃", "items": ["1", "2", "3", "4"], "reuse": True, "answer": [2, 1]},
   "N₂ + 3H₂ -> 2NH₃: 2 N and 6 H on each side.", "2 N on the left means 2 NH₃.", "The fertiliser factory", fast=14)
ch("CH_EQ_003", "Equations", 4, "DragDrop", "Balance the rusting equation.",
   {"template": "{0} Fe + {1} O₂ -> {2} Fe₂O₃", "items": ["1", "2", "3", "4"], "reuse": True, "answer": [3, 2, 1]},
   "4Fe + 3O₂ -> 2Fe₂O₃: 4 Fe and 6 O on each side.", "Make the oxygens match first (6).", "A rusting catwalk", fast=18)
ch("CH_EQ_004", "Equations", 3, "DragDrop", "Balance the burning of methane.",
   {"template": "CH₄ + {0} O₂ -> CO₂ + {1} H₂O", "items": ["1", "2", "3", "4"], "reuse": True, "answer": [1, 1]},
   "CH₄ + 2O₂ -> CO₂ + 2H₂O: 1 C, 4 H, 4 O each side.", "Balance H, then count O.", "A gas burner", fast=14)
ch("CH_EQ_005", "Equations", 5, "DragDrop", "FINAL TEST: Balance the burning of propane.",
   {"template": "C₃H₈ + {0} O₂ -> {1} CO₂ + {2} H₂O", "items": ["1", "2", "3", "4", "5"], "reuse": True, "answer": [4, 2, 3]},
   "C₃H₈ + 5O₂ -> 3CO₂ + 4H₂O: 3 C, 8 H, 10 O each side.", "C first, then H, and O last.", "The Lord of the Lab's furnace", fast=22)

# ---------------------------------------------------------------- BIO-ZONE
def bi(*a, **k):
    k.setdefault("module", "BI_FlowTrace")
    q("BioZone", "Biology", *a, **k)


bi("BI_CELL_001", "Cells", 1, "Matching", "Pair each organelle with its job.",
   {"left": ["Nucleus", "Mitochondria", "Ribosome", "Cell membrane"], "right": ["holds DNA, controls the cell", "powerhouse: releases energy (ATP)", "makes proteins", "controls what enters and leaves"]},
   "The nucleus controls, mitochondria power, ribosomes build proteins, the membrane guards.", "Which one is the 'powerhouse'?", "A cell city", fast=10)
bi("BI_CELL_002", "Cells", 2, "Logic", "Which of these is found in PLANT cells but NOT animal cells?",
   {"mode": "choice", "options": ["Cell wall", "Nucleus", "Mitochondria", "Cell membrane"], "answerIndex": 0},
   "Plant cells have a cellulose cell wall (and chloroplasts, a large vacuole); animal cells don't.", "What makes plants stiff?", "A leaf cell", fast=8)
bi("BI_CELL_003", "Cells", 1, "ShortAnswer", "What is the basic structural and functional unit of life?",
   {"accepted": ["cell", "the cell", "cells"], "keywords": [["cell"]], "placeholder": "one word"},
   "The cell. All living things are made of one or more cells.", "You're inside one right now.", "Microscopic world", fast=7)
bi("BI_CELL_004", "Cells", 3, "Logic", "The cell is dividing (mitosis). Put the stages in order.",
   {"mode": "order", "items": ["Prophase", "Metaphase", "Anaphase", "Telophase"]},
   "PMAT: chromosomes condense (P), line up (M), separate (A), two nuclei form (T).", "Remember 'PMAT'.", "A dividing cell", fast=12)
bi("BI_CELL_005", "Cells", 3, "Prediction", "A red blood cell is dropped into pure (distilled) water. What happens?",
   {"options": ["It swells and may burst (water enters by osmosis)", "It shrinks", "Nothing", "It turns blue"], "answerIndex": 0, "outcome": "Water rushes in by osmosis: the cell swells... POP.", "anim": "osmosis"},
   "Pure water is hypotonic, so water moves INTO the cell by osmosis and it can burst (haemolysis).", "Water moves to where there's more solute.", "A blood cell's bad day", fast=10)
bi("BI_CELL_006", "Cells", 4, "Matching", "Pair each organelle with its job.",
   {"left": ["Chloroplast", "Lysosome", "Golgi apparatus", "Vacuole"], "right": ["photosynthesis", "digests waste ('suicide bag')", "packages and ships proteins", "stores water and nutrients"]},
   "Chloroplasts make food, lysosomes digest, Golgi packages, vacuoles store.", "Golgi is the cell's post office.", "The cell's factory floor", fast=12)
bi("BI_BODY_001", "Human Body", 1, "Matching", "Pair each organ with its job.",
   {"left": ["Heart", "Lungs", "Kidneys", "Brain"], "right": ["pumps blood", "exchanges gases", "filter blood, make urine", "control centre"]},
   "Organs work in systems to keep the body alive.", "Start with the one you're surest of.", "The body map", fast=8)
bi("BI_BODY_002", "Human Body", 2, "Logic", "Trace a bite of food. Put the organs in order.",
   {"mode": "order", "items": ["Mouth", "Oesophagus", "Stomach", "Small intestine", "Large intestine"]},
   "Mouth -> oesophagus -> stomach -> small intestine (absorption) -> large intestine (water).", "Top to bottom.", "A food capsule's journey", fast=12)
bi("BI_BODY_003", "Human Body", 3, "ShortAnswer", "What is the structural and functional unit of the kidney?",
   {"accepted": ["nephron", "nephrons"], "keywords": [["nephron"]], "placeholder": "one word"},
   "The nephron filters blood and forms urine; each kidney has about a million.", "It starts with 'neph-', like nephrology.", "The filter station", fast=9)
bi("BI_BODY_004", "Human Body", 4, "Logic", "Which enzyme in saliva starts digesting starch?",
   {"mode": "choice", "options": ["Salivary amylase (ptyalin)", "Pepsin", "Lipase", "Trypsin"], "answerIndex": 0},
   "Salivary amylase breaks starch into maltose in the mouth. Pepsin works on proteins in the stomach.", "Amyl- means starch.", "The mouth gate", fast=10)
bi("BI_BODY_005", "Human Body", 3, "Simulation", "Heat the enzyme bath and watch the activity. At about what temperature is the enzyme MOST active?",
   {"sim": "enzyme", "mode": "read", "sliders": [{"id": "T", "label": "Temperature", "min": 0, "max": 80, "step": 1, "value": 10, "unit": "°C"}],
    "options": ["About 37 °C (body temperature)", "0 °C", "80 °C", "It's the same at every temperature"], "answerIndex": 0},
   "Human enzymes work best near 37 °C. Too hot and they denature (lose shape); too cold and they're slow.", "Your body's own temperature.", "An enzyme reactor", fast=20)
bi("BI_GEN_001", "Genetics", 1, "Logic", "DNA bases always pair up as...",
   {"mode": "choice", "options": ["A–T and G–C", "A–G and T–C", "A–C and G–T", "A–A and G–G"], "answerIndex": 0},
   "Adenine pairs with thymine, guanine with cytosine.", "Apple in the Tree, Car in the Garage.", "A DNA ladder", fast=7)
bi("BI_GEN_002", "Genetics", 2, "DragDrop", "Repair the broken DNA strand. Drag the complementary bases.",
   {"template": "A–{0}   T–{1}   G–{2}   C–{3}", "items": ["A", "T", "G", "C"], "reuse": True, "answer": [1, 0, 3, 2]},
   "A pairs with T, G pairs with C.", "A–T, G–C.", "A broken DNA ladder", fast=10)
bi("BI_GEN_003", "Genetics", 3, "Numerical", "Tt x Tt (T = tall, dominant). What PERCENTAGE of offspring are tall?",
   {"answer": 75, "tolerance": 0.5, "unit": "%", "givens": ["Punnett square: TT, Tt, tT, tt"]},
   "TT, Tt, Tt are tall; tt is short. 3 out of 4 = 75%.", "Draw the 2x2 Punnett square.", "Mendel's pea garden", fast=12)
bi("BI_GEN_004", "Genetics", 4, "Numerical", "Tt x tt. What PERCENTAGE of offspring are dwarf (tt)?",
   {"answer": 50, "tolerance": 0.5, "unit": "%", "givens": ["Tt gives T or t", "tt gives only t"]},
   "Offspring are Tt, Tt, tt, tt: half are dwarf, 50%. This is a test cross.", "One parent can only give t.", "A test cross", fast=12)
bi("BI_GEN_005", "Genetics", 3, "ShortAnswer", "Who is called the 'Father of Genetics'?",
   {"accepted": ["mendel", "gregor mendel", "gregor johann mendel"], "keywords": [["mendel"]], "placeholder": "name"},
   "Gregor Mendel, from his experiments on pea plants.", "He grew peas.", "A portrait in the gene vault", fast=9)
bi("BI_GEN_006", "Genetics", 5, "Numerical", "FINAL TEST: Dihybrid cross RrYy x RrYy. Out of 16 offspring, how many show BOTH recessive traits (rryy)?",
   {"answer": 1, "tolerance": 0, "unit": "of 16", "givens": ["ratio 9 : 3 : 3 : 1"]},
   "The dihybrid ratio is 9:3:3:1, and the double recessive is the 1.", "Remember 9 : 3 : 3 : 1.", "The Lord Within's code", fast=14)
bi("BI_RESP_001", "Respiration", 1, "Logic", "Aerobic respiration releases energy from glucose using...",
   {"mode": "choice", "options": ["oxygen", "nitrogen", "only carbon dioxide", "no gas at all"], "answerIndex": 0},
   "Aerobic means with air (oxygen): glucose + oxygen -> CO₂ + water + energy.", "Aero = air.", "A gasping cell", fast=6)
bi("BI_RESP_002", "Respiration", 2, "DragDrop", "Complete the respiration equation.",
   {"template": "Glucose + {0} -> Carbon dioxide + {1} + Energy", "items": ["Oxygen", "Water", "Nitrogen", "Starch"], "answer": [0, 1]},
   "C₆H₁₂O₆ + 6O₂ -> 6CO₂ + 6H₂O + energy (ATP).", "It's the reverse of photosynthesis.", "The energy furnace", fast=10)
bi("BI_RESP_003", "Respiration", 3, "Logic", "During a sprint, muscles make lactic acid. This is...",
   {"mode": "choice", "options": ["anaerobic respiration", "photosynthesis", "aerobic respiration", "digestion"], "answerIndex": 0},
   "Without enough oxygen, muscles respire anaerobically: glucose -> lactic acid + a little energy. Hence cramps.", "Not enough oxygen.", "A muscle cramp", fast=9)
bi("BI_RESP_004", "Respiration", 3, "Logic", "Trace a breath of air. Put the airway in order.",
   {"mode": "order", "items": ["Nose", "Pharynx", "Larynx", "Trachea", "Bronchi", "Alveoli"]},
   "Nose -> pharynx -> larynx -> trachea -> bronchi -> bronchioles -> alveoli (gas exchange).", "The voice box comes before the windpipe.", "Riding a breath", fast=14)
bi("BI_RESP_005", "Respiration", 4, "ShortAnswer", "In which organelle does aerobic respiration mainly happen?",
   {"accepted": ["mitochondria", "mitochondrion", "the mitochondria"], "keywords": [["mitochond"]], "placeholder": "organelle"},
   "Mitochondria: the 'powerhouse of the cell'.", "The powerhouse.", "The power plant", fast=9)
bi("BI_CIRC_001", "Circulation", 1, "Logic", "Which blood vessels carry blood AWAY from the heart?",
   {"mode": "choice", "options": ["Arteries", "Veins", "Capillaries", "Nerves"], "answerIndex": 0},
   "Arteries = Away. Veins return blood to the heart.", "A for Away.", "A blood highway", fast=6)
bi("BI_CIRC_002", "Circulation", 2, "Logic", "Trace deoxygenated blood coming back from the body.",
   {"mode": "order", "items": ["Vena cava", "Right atrium", "Right ventricle", "Pulmonary artery", "Lungs"]},
   "Body -> vena cava -> right atrium -> right ventricle -> pulmonary artery -> lungs (to pick up O₂).", "The RIGHT side of the heart handles used blood.", "A tired blood cell", fast=14)
bi("BI_CIRC_003", "Circulation", 3, "Matching", "Pair each blood component with its job.",
   {"left": ["Red blood cells", "White blood cells", "Platelets", "Plasma"], "right": ["carry oxygen (haemoglobin)", "fight infection", "help blood clot", "liquid that carries nutrients"]},
   "RBCs carry O₂, WBCs defend, platelets clot, plasma transports.", "Which one is the army?", "The blood river", fast=12)
bi("BI_CIRC_004", "Circulation", 3, "Numerical", "The heart beats 72 times a minute and pumps 70 mL per beat. Cardiac output in mL/min?",
   {"answer": 5040, "tolerance": 1, "unit": "mL/min", "givens": ["cardiac output = heart rate x stroke volume"]},
   "72 x 70 = 5040 mL/min, about 5 litres every minute.", "Multiply the two.", "The heart's control room", fast=12)
bi("BI_CIRC_005", "Circulation", 4, "Simulation", "The organism needs 6 litres of blood per minute. Tune the heart, then RUN.",
   {"sim": "heart", "mode": "goal", "sliders": [{"id": "hr", "label": "Heart rate", "min": 40, "max": 180, "step": 1, "value": 60, "unit": "bpm"}, {"id": "sv", "label": "Stroke volume", "min": 40, "max": 120, "step": 1, "value": 60, "unit": "mL"}],
    "goal": {"metric": "co", "min": 5.8, "max": 6.2, "unit": "L/min"}},
   "Cardiac output = heart rate x stroke volume. For example 80 bpm x 75 mL = 6000 mL = 6 L/min.", "Multiply, then divide by 1000 for litres.", "A failing heart", fast=20)
bi("BI_CIRC_006", "Circulation", 5, "Logic", "FINAL TEST: Trace oxygen from the lungs to a working muscle.",
   {"mode": "order", "items": ["Lungs", "Pulmonary vein", "Left atrium", "Left ventricle", "Aorta", "Muscle capillaries"]},
   "Lungs -> pulmonary vein -> left atrium -> left ventricle -> aorta -> arteries -> capillaries in the muscle.", "Oxygen-rich blood uses the LEFT side.", "The Lord Within's blockade", fast=18)
bi("BI_CIRC_007", "Circulation", 1, "Numerical", "How many chambers does the human heart have?",
   {"answer": 4, "tolerance": 0, "unit": "chambers"},
   "Four: two atria (top) and two ventricles (bottom).", "Two up, two down.", "Inside the heart", fast=6)
bi("BI_ECO_001", "Ecosystems", 1, "Construction", "Rebuild the broken food chain. Place the organisms in order, starting with the producer.",
   {"mode": "sequence", "display": "chain", "parts": ["Snake", "Grass", "Frog", "Eagle", "Grasshopper"], "sequence": ["Grass", "Grasshopper", "Frog", "Snake", "Eagle"]},
   "Energy flows from producer -> herbivore -> carnivores: grass -> grasshopper -> frog -> snake -> eagle.", "Start with the one that makes its own food.", "A broken food chain", fast=14)
bi("BI_ECO_002", "Ecosystems", 2, "Matching", "Pair each role with an example.",
   {"left": ["Producer", "Herbivore", "Carnivore", "Decomposer"], "right": ["Grass", "Deer", "Tiger", "Fungi"]},
   "Producers make food, herbivores eat plants, carnivores eat animals, decomposers recycle the dead.", "Who recycles?", "The ecosystem dome", fast=10)
bi("BI_ECO_003", "Ecosystems", 3, "Numerical", "Producers hold 10,000 J. How much energy reaches the SECONDARY consumer? (10% law)",
   {"answer": 100, "tolerance": 0.5, "unit": "J", "givens": ["only 10% passes to each next level"]},
   "10,000 -> 1,000 (primary) -> 100 J (secondary).", "Two steps of 10%.", "An energy pyramid", fast=12)
bi("BI_ECO_004", "Ecosystems", 4, "Prediction", "Every frog in the pond suddenly vanishes. What happens to the insect population FIRST?",
   {"options": ["It increases", "It decreases", "It stays exactly the same", "The insects turn into frogs"], "answerIndex": 0, "outcome": "No predators: the insects multiply. The whole web wobbles.", "anim": "population"},
   "Removing a predator lets its prey increase, then the effect ripples through the food web.", "Who was eating the insects?", "A vanishing pond", fast=9)
bi("BI_ECO_005", "Ecosystems", 3, "Simulation", "Shine light on the pondweed and count the oxygen bubbles. What happens as light keeps increasing?",
   {"sim": "photo", "mode": "read", "sliders": [{"id": "light", "label": "Light", "min": 0, "max": 100, "step": 1, "value": 10, "unit": "%"}, {"id": "co2", "label": "CO₂", "min": 0.01, "max": 0.1, "step": 0.01, "value": 0.04, "unit": "%"}],
    "options": ["Rises, then levels off (another factor becomes limiting)", "Keeps rising forever", "Falls", "No change"], "answerIndex": 0},
   "Photosynthesis rises with light until another factor (CO₂ or temperature) limits it, so the curve flattens.", "Watch the top of the graph.", "A pondweed lab", fast=20)

# ---------------------------------------------------------------- THE CORE (Final arc: mixed synthesis)
def fi(*a, **k):
    q("TheCore", "Mixed", *a, **k)


fi("FI_MIX_001", "Synthesis", 5, "Matching", "The Source scrambled every region's memory. Re-link each idea.",
   {"left": ["Loop", "Inertia", "Covalent bond", "Artery"], "right": ["repeats instructions", "resists change in motion", "shared electron pair", "carries blood away from the heart"]},
   "One idea from each region: CS, Physics, Chemistry and Biology.", "One per region.", "The Source's scrambled memory", fast=14, module="CS_LoopSandbox")
fi("FI_MIX_002", "Synthesis", 5, "Coding", "Model a dividing cell in code. Each hour, every cell splits in two.",
   {"mode": "blank", "lines": ["cells = 1", "for hour in range(4):", "    cells = ____", "print(cells)"], "options": ["cells * 2", "cells + 2", "cells ** 2", "2"], "answerIndex": 0, "output": "16"},
   "Each cell becomes two, so the number doubles: 1 -> 2 -> 4 -> 8 -> 16.", "Splitting in two means doubling.", "Biology meets code", fast=14, module="CS_LoopSandbox")
fi("FI_MIX_003", "Synthesis", 5, "Numerical", "A robot's speed starts at 2 m/s and the loop adds 2 m/s five times. What is its KE if its mass is 3 kg?",
   {"code": "speed = 2\nfor t in range(5):\n    speed = speed + 2", "answer": 216, "tolerance": 1, "unit": "J", "givens": ["KE = 1/2 m v²"]},
   "speed = 2 + 5 x 2 = 12 m/s. KE = 1/2 x 3 x 144 = 216 J.", "Run the loop first, then the physics.", "Physics meets code", fast=20, module="PH_ProjectileLab")
fi("FI_MIX_004", "Synthesis", 5, "Logic", "Glucose + oxygen -> CO₂ + water + energy. In which organelle, and what TYPE of reaction (energy-wise)?",
   {"mode": "choice", "options": ["Mitochondria, exothermic", "Nucleus, endothermic", "Chloroplast, exothermic", "Ribosome, neutralisation"], "answerIndex": 0},
   "Respiration happens in mitochondria and RELEASES energy, so it is exothermic.", "Powerhouse + energy released.", "Chemistry meets biology", fast=14, module="BI_FlowTrace")
fi("FI_MIX_005", "Synthesis", 5, "Debugging", "The Source's ohm-meter code gives the wrong current. Tap the bug.",
   {"lines": ["V = 12", "R = 4", "I = V * R", "print('Current:', I, 'A')"], "answerIndex": 2, "fix": "I = V / R"},
   "Ohm's law is I = V / R = 12 / 4 = 3 A. Multiplying gives 48, which is nonsense.", "V = IR, so I = ?", "Physics meets code", fast=12, module="PH_ProjectileLab")
fi("FI_MIX_006", "Synthesis", 5, "ShortAnswer", "Plants turn light energy into chemical energy, the reverse of respiration. Name this process.",
   {"accepted": ["photosynthesis"], "keywords": [["photosynth", "photo synthesis"]], "placeholder": "one word"},
   "Photosynthesis: 6CO₂ + 6H₂O + light -> C₆H₁₂O₆ + 6O₂.", "Photo = light, synthesis = making.", "The Nexus garden", fast=10, module="BI_FlowTrace")
fi("FI_MIX_007", "Synthesis", 5, "Construction", "Rebuild the Nexus power chain from source to light.",
   {"mode": "sequence", "display": "chain", "parts": ["Bulb glows", "Sunlight", "Solar panel", "Current flows", "Wire"], "sequence": ["Sunlight", "Solar panel", "Current flows", "Wire", "Bulb glows"]},
   "Light -> panel converts to electrical -> current flows through wires -> bulb converts to light and heat.", "Follow the energy.", "The Core's power grid", fast=16, module="PH_ProjectileLab")
fi("FI_MIX_008", "Synthesis", 5, "Prediction", "The Source flips gravity off for one second in Code City. A data car was moving at 5 m/s. Without any force, it...",
   {"options": ["keeps going at 5 m/s in a straight line", "stops instantly", "speeds up forever", "turns into binary"], "answerIndex": 0, "outcome": "Newton's first law even works on data cars.", "anim": "rocket"},
   "No net force means no change in velocity: constant speed in a straight line.", "Newton's first law.", "Physics meets Code City", fast=10, module="PH_ProjectileLab")

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    ids = set()
    total = 0
    for arc, items in BANK.items():
        for it in items:
            assert it["id"] not in ids, it["id"]
            ids.add(it["id"])
        doc = {"arc": arc, "version": 1, "questions": deep(items)}
        with open(os.path.join(OUT, arc + ".json"), "w", encoding="utf-8") as f:
            json.dump(doc, f, ensure_ascii=False, indent=2)
        total += len(items)
        types = sorted({i["type"] for i in items})
        print(f"{arc}: {len(items)} questions, types: {', '.join(types)}")
    print("total", total)
