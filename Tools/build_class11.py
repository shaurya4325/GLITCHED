# Builds the CBSE Class XI content of GLITCHED (session 2026-27 syllabus, NCERT textbooks):
#   Assets/StreamingAssets/Lessons/Class11.json        one lesson per terminal topic (3 short pages each)
#   Assets/StreamingAssets/Questions/Class11_*.json    the MCQ bank of each topic (small enemies + answer platforms)
# Topics follow the official CBSE curriculum units / NCERT chapters. Every question is original, written for the game,
# and carries "needsTeacherReview": true until a teacher has checked it. Teachers can edit the JSON directly.
import json, os, re

ROOT = os.path.join(os.path.dirname(__file__), "..", "Assets", "StreamingAssets")
SUB = str.maketrans("₀₁₂₃₄₅₆₇₈₉", "0123456789")
SUP = str.maketrans("⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻", "0123456789+-")


def rich(s):
    """Unicode sub/superscripts -> TextMeshPro <sub>/<sup> tags (fonts rarely carry those glyphs)."""
    if not isinstance(s, str):
        return s
    s = re.sub(r"[₀-₉]+", lambda m: "<sub>" + m.group(0).translate(SUB) + "</sub>", s)
    s = re.sub(r"[⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻]+", lambda m: "<sup>" + m.group(0).translate(SUP) + "</sup>", s)
    return s


TOPICS = []


def topic(id, arc, subject, unit, title, key, pixel, slides, questions):
    """slides: [(heading, body, example)], questions: [(prompt, [options], answer, explanation, difficulty, code?)]"""
    TOPICS.append(dict(id=id, arc=arc, subject=subject, unit=unit, title=title, key=key, pixel=pixel, slides=slides, questions=questions))


# =====================================================================================================================
# COMPUTER SCIENCE (083)  -> Code City
# =====================================================================================================================
CS = ("CodeCity", "Computer Science")

topic("cs_bool", *CS, "Unit 1 · Computer Systems and Organisation", "Boolean Logic",
      "AND needs every input 1, OR needs any 1, XOR needs them to differ, and De Morgan swaps AND/OR when you negate.",
      "The gate's rule is AND plus NOT. Keycard AND NOT alarm. Easy. Unless you're a door.",
      [("NOT, AND, OR",
        "• <b>NOT</b> flips a bit: NOT 1 = 0.\n• <b>AND</b> is 1 only if <b>all</b> inputs are 1.\n• <b>OR</b> is 1 if <b>any</b> input is 1.",
        "A = 1, B = 0  →  A AND B = 0,  A OR B = 1,  NOT A = 0"),
       ("NAND, NOR, XOR",
        "• <b>NAND</b> = NOT(AND),  <b>NOR</b> = NOT(OR).\n• <b>XOR</b> is 1 when the inputs are <b>different</b>.\n• NAND and NOR are <b>universal gates</b>: any circuit can be built from just one of them.",
        "A = 1, B = 1  →  NAND 0,  NOR 0,  XOR 0"),
       ("De Morgan's laws",
        "• NOT(A AND B) = (NOT A) OR (NOT B)\n• NOT(A OR B) = (NOT A) AND (NOT B)\n• \"Break the bar, change the sign.\"  A truth table with n inputs has 2ⁿ rows.",
        "(A·B)' = A' + B'        (A + B)' = A'·B'")],
      [("A = 1, B = 0. What is A AND B?", ["0", "1", "10", "Undefined"], 0, "AND gives 1 only when every input is 1.", 1),
       ("Which gate outputs 1 only when its two inputs are DIFFERENT?", ["XOR", "NAND", "OR", "NOR"], 0, "XOR = 'exclusive OR': one or the other, not both.", 1),
       ("NOT (A OR B) is equal to:", ["(NOT A) AND (NOT B)", "(NOT A) OR (NOT B)", "A AND B", "NOT A OR B"], 0, "De Morgan: negate each input and swap OR for AND.", 2),
       ("Output of a NAND gate when both inputs are 1?", ["0", "1", "2", "11"], 0, "NAND = NOT(AND). 1 AND 1 = 1, so NAND gives 0.", 1),
       ("Which gates are called 'universal gates'?", ["NAND and NOR", "AND and OR", "XOR and NOT", "AND and NOT"], 0, "Any logic circuit can be built from NAND gates alone, or from NOR gates alone.", 2),
       ("A truth table for 2 inputs has how many rows?", ["4", "2", "3", "8"], 0, "n inputs give 2ⁿ combinations: 2² = 4.", 1),
       ("For which inputs does A OR B give 0?", ["A = 0, B = 0", "A = 1, B = 0", "A = 0, B = 1", "A = 1, B = 1"], 0, "OR is 0 only when every input is 0.", 1),
       ("By De Morgan's law, (A·B)' equals:", ["A' + B'", "A'·B'", "A + B", "A·B"], 0, "Break the bar, change the sign: AND becomes OR.", 2),
       ("Which expression is ALWAYS 1, whatever A is?", ["A OR (NOT A)", "A AND (NOT A)", "A XOR A", "NOT 1"], 0, "One of A and NOT A is always 1, so their OR is always 1.", 2)])

topic("cs_python", *CS, "Unit 2 · NCERT Ch 5: Getting Started with Python", "Python Basics",
      "/ always gives a float, // floors, % gives the remainder and ** binds tightest; identifiers can't start with a digit.",
      "Python divides with one slash. Two slashes if it's feeling strict.",
      [("Tokens",
        "The smallest units of a program:\n• <b>keywords</b> (if, for, True)  • <b>identifiers</b> (names you choose)\n• <b>literals</b> (5, 'hi', 3.14)  • <b>operators</b> (+, ==)  • <b>punctuators</b> ( : , ( ) )\nIdentifiers: letters, digits and _ only, never starting with a digit, never a keyword.",
        "speed_2 = 10    # valid\n2speed = 10     # SyntaxError"),
       ("Data types",
        "• Numbers: <b>int</b> 7, <b>float</b> 7.0, <b>complex</b> 2+3j   • <b>bool</b>: True / False\n• Sequences: <b>str</b> 'abc', <b>list</b> [1, 2] (mutable), <b>tuple</b> (1, 2) (immutable)\n• Mapping: <b>dict</b> {'a': 1}   • <b>None</b>.   type(x) tells you which.",
        "type(7 / 2)    ->  <class 'float'>\ntype('7')      ->  <class 'str'>"),
       ("Operators",
        "• <b>/</b> divides into a float (7/2 = 3.5), <b>//</b> floors (7//2 = 3), <b>%</b> is the remainder (7%2 = 1), <b>**</b> is power.\n• Relational == != < >,  logical and / or / not,  identity is,  membership in.\n• Precedence: ** first, then * / // %, then + −.",
        "print(2 + 3 * 2 ** 2)    # 14")],
      [("What does print(7 // 2) display?", ["3", "3.5", "4", "1"], 0, "// is floor division: 7 / 2 = 3.5, floored to 3.", 1),
       ("What is the value of 7 % 3?", ["1", "2", "0", "2.33"], 0, "% gives the remainder: 7 = 2 × 3 + 1.", 1),
       ("Which of these is a valid Python identifier?", ["_total2", "2total", "total-2", "for"], 0, "No digit first, no '-', and 'for' is a keyword.", 1),
       ("What type is the result of 5 / 5?", ["float", "int", "bool", "str"], 0, "/ always returns a float: 5 / 5 = 1.0.", 2),
       ("Which data type is IMMUTABLE?", ["tuple", "list", "dict", "set"], 0, "A tuple can't be changed after it's made; lists and dicts can.", 2),
       ("Evaluate: 2 + 3 * 2 ** 2", ["14", "100", "22", "20"], 0, "** first (2² = 4), then 3 × 4 = 12, then 2 + 12 = 14.", 2),
       ("Which operator checks whether a value is present in a list?", ["in", "is", "==", "not"], 0, "Membership: 3 in [1, 2, 3] is True.", 1),
       ("What is x after:  x = '5' + '3'", ["'53'", "'8'", "8", "Error"], 0, "+ on two strings joins them (concatenation).", 2),
       ("What is the output of print(type(True))?", ["<class 'bool'>", "<class 'str'>", "<class 'int'>", "True"], 0, "True and False are Boolean literals.", 1)])

topic("cs_flow", *CS, "Unit 2 · NCERT Ch 6: Flow of Control", "Flow of Control",
      "range(stop) never includes stop; only the first true branch of if/elif runs; break exits a loop, continue skips a round.",
      "The taxi's loop never changed its condition. That's why it's been driving since Tuesday.",
      [("Three kinds of flow",
        "• <b>Sequential</b>: one line after another.\n• <b>Conditional</b>: if / if-else / if-elif-else (only the first true branch runs).\n• <b>Iterative</b>: for and while loops.\nPython marks a block by its <b>indentation</b>.",
        "if marks >= 33:\n    print('Pass')\nelse:\n    print('Try again')"),
       ("Loops and range()",
        "• range(start, stop, step) <b>stops before</b> stop: range(5) → 0 1 2 3 4.\n• range(2, 10, 3) → 2 5 8.\n• <b>while</b> repeats while its condition is True, so something inside must change or it never ends.",
        "for i in range(1, 6):\n    print(i * i)      # 1 4 9 16 25"),
       ("break, continue, nesting",
        "• <b>break</b>: leave the loop at once.\n• <b>continue</b>: skip the rest of this round.\n• In a <b>nested loop</b> the inner loop runs completely for every round of the outer one.",
        "for n in range(1, 10):\n    if n % 4 == 0:\n        break\n    print(n)          # 1 2 3")],
      [("How many numbers does this loop print?", ["6", "7", "5", "9"], 0, "range(3, 9) is 3, 4, 5, 6, 7, 8: six numbers (9 is excluded).", 1, "for i in range(3, 9):\n    print(i)"),
       ("What does list(range(2, 11, 3)) give?", ["[2, 5, 8]", "[2, 5, 8, 11]", "[3, 6, 9]", "[2, 3, 4]"], 0, "Start at 2, add 3 each time, stop before 11.", 2),
       ("What prints?", ["A", "B", "A then B", "C"], 0, "Only the FIRST true branch of an if-elif-else runs.", 1, "x = 15\nif x > 10:\n    print('A')\nelif x > 5:\n    print('B')\nelse:\n    print('C')"),
       ("What prints?", ["32", "16", "20", "64"], 0, "n doubles 1, 2, 4, 8, 16, 32; 32 < 20 is False, so the loop stops and prints 32.", 3, "n = 1\nwhile n < 20:\n    n = n * 2\nprint(n)"),
       ("What prints?", ["1 2 4 5", "1 2", "1 2 3 4 5", "3"], 0, "continue skips only the round where i == 3.", 2, "for i in range(1, 6):\n    if i == 3:\n        continue\n    print(i, end=' ')"),
       ("What prints?", ["1 2", "1 2 3", "1 2 4 5", "3"], 0, "break leaves the loop as soon as i == 3, before printing it.", 2, "for i in range(1, 6):\n    if i == 3:\n        break\n    print(i, end=' ')"),
       ("In Python, the statements inside an if-block are marked by:", ["indentation", "curly braces { }", "the keyword end", "semicolons"], 0, "Python uses indentation (usually 4 spaces) instead of braces.", 1),
       ("What is printed?", ["12", "7", "3", "4"], 0, "The inner loop runs 4 times for each of the 3 outer rounds: 3 × 4 = 12.", 2, "count = 0\nfor i in range(3):\n    for j in range(4):\n        count += 1\nprint(count)"),
       ("Which loop suits a repeat whose count you DON'T know in advance?", ["while", "for with range()", "a nested for", "none"], 0, "while keeps going until a condition becomes False (e.g. until the user types 'quit').", 2)])

topic("cs_numsys", *CS, "Unit 1 · NCERT Ch 2: Encoding Schemes & Number System", "Number Systems",
      "Each binary place doubles (1, 2, 4, 8...); group bits in 3s for octal and 4s for hexadecimal; ASCII 'A' = 65.",
      "Rooftop rule: wrong answers fall off the building. Binary rule: there are only 10 kinds of people.",
      [("Four number systems",
        "• <b>Binary</b> (base 2): 0, 1   • <b>Octal</b> (base 8): 0–7   • <b>Decimal</b> (base 10)\n• <b>Hexadecimal</b> (base 16): 0–9 and A–F (A = 10 … F = 15)\nEach place is worth 'base' times the place to its right.",
        "1011₂ = 8 + 0 + 2 + 1 = 11₁₀"),
       ("Converting",
        "• Decimal → binary: divide by 2 again and again, read the remainders <b>bottom-up</b>.\n• Binary → octal: group 3 bits from the right.  Binary → hex: group 4 bits.",
        "13 → 1101₂       1101 0110₂ = D6₁₆       110 101₂ = 65₈"),
       ("Encoding text",
        "• <b>ASCII</b>: 7 bits, 128 characters ('A' = 65, 'a' = 97).\n• <b>ISCII</b>: 8-bit code for Indian scripts.\n• <b>Unicode</b>: every script and emoji. UTF-8 uses 1–4 bytes per character, UTF-32 always 4.",
        "'A' = 65 = 1000001₂")],
      [("1011₂ in decimal is:", ["11", "13", "9", "1011"], 0, "8 + 0 + 2 + 1 = 11.", 1),
       ("13₁₀ in binary is:", ["1101", "1011", "1110", "1001"], 0, "8 + 4 + 0 + 1 = 13.", 1),
       ("The hexadecimal digit F stands for:", ["15", "16", "14", "6"], 0, "A = 10, B = 11 … F = 15.", 1),
       ("11111111₂ in decimal is:", ["255", "256", "127", "128"], 0, "Eight 1s = 2⁸ − 1 = 255.", 2),
       ("1010 1100₂ in hexadecimal is:", ["AC", "CA", "9C", "A12"], 0, "1010 = A and 1100 = C.", 2),
       ("17₈ in decimal is:", ["15", "17", "7", "9"], 0, "1 × 8 + 7 = 15.", 2),
       ("How many characters can 7-bit ASCII represent?", ["128", "256", "127", "7"], 0, "2⁷ = 128.", 1),
       ("ASCII 'A' is 65. What is 'C'?", ["67", "66", "99", "68"], 0, "Letters are consecutive: A 65, B 66, C 67.", 1),
       ("Which encoding covers Hindi, Tamil AND emoji?", ["Unicode", "ASCII", "ISCII", "Hex"], 0, "Unicode was designed to cover every writing system (and emoji).", 2),
       ("How many bits are in one byte?", ["8", "4", "16", "2"], 0, "1 byte = 8 bits.", 1),
       ("Largest number with 4 bits?", ["15", "16", "8", "4"], 0, "1111₂ = 2⁴ − 1 = 15.", 1),
       ("2F₁₆ in decimal is:", ["47", "32", "215", "45"], 0, "2 × 16 + 15 = 47.", 2)])

topic("cs_lists", *CS, "Unit 2 · NCERT Ch 8–9: Strings and Lists", "Strings & Lists",
      "Indexes start at 0, negative ones count from the end, slices stop before 'stop'; strings can't be changed, lists can.",
      "Everything in The Source is a list. Even its feelings. Especially its feelings.",
      [("Strings: index and slice",
        "s = 'NEXUS':  s[0] = 'N',  s[-1] = 'S'.\n• A slice s[start:stop] stops <b>before</b> stop: s[1:4] = 'EXU'.\n• Strings are <b>immutable</b>: s[0] = 'X' is an error.",
        "s = 'GLITCHED'\nprint(s[2:5], s[-2:], len(s))   # ITC ED 8"),
       ("String methods",
        "upper(), lower(), title(), count('x'), find('x') (−1 if it's not there), replace(a, b), strip() (trims spaces), split() (makes a list of words), join().",
        "'a,b,c'.split(',')    ->  ['a', 'b', 'c']"),
       ("Lists",
        "Lists are <b>mutable</b>: L = [3, 1, 2]\n• append(x) adds ONE item at the end, extend([…]) adds many, insert(i, x)\n• remove(x) deletes the first match, pop() removes and returns the last\n• sort() sorts in place, sorted(L) returns a new list.",
        "L = [3, 1, 2]\nL.append(5); L.sort()\nprint(L)          # [1, 2, 3, 5]")],
      [("s = 'PYTHON'. What is s[1:4]?", ["'YTH'", "'PYT'", "'YTHO'", "'PYTH'"], 0, "Index 1 to 3: Y, T, H (the slice stops before 4).", 1),
       ("s = 'PYTHON'. What is s[-1]?", ["'N'", "'P'", "'O'", "Error"], 0, "Negative indexes count from the end: -1 is the last character.", 1),
       ("L = [1, 2, 3]; L.append([4, 5]). What is len(L) now?", ["4", "5", "3", "6"], 0, "append adds ONE item, here the whole list [4, 5]. extend would add two.", 3),
       ("'banana'.count('a') returns:", ["3", "2", "1", "6"], 0, "b-a-n-a-n-a has three a's.", 1),
       ("Which statement causes an ERROR?", ["s = 'cat'; s[0] = 'b'", "L = [1]; L[0] = 2", "s = 'cat' + 's'", "L = [1] * 3"], 0, "Strings are immutable: you can't assign to s[0].", 2),
       ("L = [5, 2, 9]. What does print(L.pop()) show?", ["9", "5", "2", "[5, 2]"], 0, "pop() with no index removes and returns the LAST item.", 2),
       ("'hello world'.split() gives:", ["['hello', 'world']", "'helloworld'", "['h', 'e', 'l', 'l', 'o']", "('hello', 'world')"], 0, "split() breaks the string at spaces into a list of words.", 2),
       ("'Nexus'.find('z') returns:", ["-1", "0", "None", "Error"], 0, "find() returns -1 when the text isn't there (index() would raise an error).", 2),
       ("What does print([1, 2] * 2) show?", ["[1, 2, 1, 2]", "[2, 4]", "[1, 2, 2]", "Error"], 0, "* repeats a list.", 2)])

# =====================================================================================================================
# PHYSICS  -> Physics Valley
# =====================================================================================================================
PH = ("PhysicsValley", "Physics")

topic("ph_laws", *PH, "Unit III · Chapter 4", "Laws of Motion",
      "Net force changes momentum (F = ma); action and reaction act on DIFFERENT bodies; with no external force, momentum is conserved.",
      "Baron Von Friction hates this chapter. Read it twice. Out loud. Near him.",
      [("First law: inertia",
        "A body stays at rest, or keeps moving in a straight line at constant speed, unless a <b>net external force</b> acts on it.\nInertia is the resistance to that change: more mass, more inertia.",
        "Bus brakes suddenly → passengers lurch forward"),
       ("Second law: F = dp/dt = ma",
        "• Momentum p = mv.  Force = rate of change of momentum; for constant mass <b>F = ma</b>.\n• Impulse = F × Δt = change in momentum.   1 N = 1 kg m/s².",
        "5 kg cart, net force 20 N  →  a = 20 / 5 = 4 m/s²"),
       ("Third law, momentum, friction",
        "• Every action has an equal and opposite reaction, on a <b>different</b> body.\n• No external force → total momentum is <b>conserved</b> (recoil, rockets).\n• Friction: static ≤ μₛN, kinetic f = μₖN, and usually μₖ < μₛ.  A circle needs a centripetal force mv²/r.",
        "Gun recoil:  m₁v₁ = −m₂v₂")],
      [("A net force of 20 N acts on a 4 kg body. Its acceleration is:", ["5 m/s²", "80 m/s²", "0.2 m/s²", "24 m/s²"], 0, "a = F / m = 20 / 4 = 5 m/s².", 1),
       ("The SI unit of momentum is:", ["kg m/s", "N/s", "kg m/s²", "J s"], 0, "p = mv: kilogram × metre per second.", 1),
       ("Action and reaction forces:", ["act on different bodies", "act on the same body", "cancel out on one body", "exist only when bodies move"], 0, "They're equal and opposite but act on DIFFERENT bodies, so they never cancel each other.", 2),
       ("A 0.2 kg ball hits a wall at 10 m/s and bounces straight back at 10 m/s. Size of its change in momentum:", ["4 kg m/s", "2 kg m/s", "0", "1 kg m/s"], 0, "Δp = m(v − u) = 0.2 × (10 − (−10)) = 4 kg m/s. Direction matters!", 3),
       ("Passengers lurch forward when a bus brakes suddenly. This is due to:", ["inertia of motion", "inertia of rest", "Newton's third law", "friction"], 0, "Their bodies keep moving forward (inertia of motion) while the bus slows.", 1),
       ("A 50 g bullet leaves a 5 kg gun at 100 m/s. The gun's recoil speed is:", ["1 m/s", "10 m/s", "0.1 m/s", "100 m/s"], 0, "Momentum is conserved: 0.05 × 100 = 5 × v, so v = 1 m/s.", 3),
       ("Impulse is equal to:", ["change in momentum", "mass × velocity", "force × distance", "mass × acceleration"], 0, "Impulse = F × Δt = Δp.", 2),
       ("To keep a body moving in a circle you need:", ["a centripetal force mv²/r", "a force along the velocity", "no force at all", "a force away from the centre"], 0, "The force must point to the centre: F = mv²/r.", 2),
       ("Usually, the coefficient of kinetic friction is:", ["less than static", "more than static", "equal to static", "always zero"], 0, "It's harder to start something sliding than to keep it sliding: μₖ < μₛ.", 2)])

topic("ph_kinematics", *PH, "Unit II · Chapters 2 and 3", "Kinematics",
      "v = u + at, s = ut + ½at², v² = u² + 2as; for a projectile R = u² sin2θ / g, which is largest at 45°.",
      "In this game gravity is 34 m/s². In Physics Valley it's whatever the Lord says. In the exam, use 10.",
      [("Equations of motion",
        "For constant acceleration:\n• v = u + at      • s = ut + ½at²      • v² = u² + 2as\nSlope of a velocity–time graph = acceleration; <b>area under it = displacement</b>.",
        "From rest, a = 2 m/s² for 5 s  →  v = 10 m/s,  s = 25 m"),
       ("Projectiles: split the velocity",
        "• uₓ = u cos θ,  u_y = u sin θ.\n• Horizontally: no force, so <b>constant speed</b>.\n• Vertically: accelerates at g downwards; at the top v_y = 0.",
        "u = 20 m/s at 30°  →  uₓ ≈ 17.3 m/s,  u_y = 10 m/s"),
       ("Range, height, time",
        "• Time of flight T = 2u sin θ / g      • Max height H = u² sin²θ / 2g\n• Range <b>R = u² sin 2θ / g</b>: greatest at 45°; 30° and 60° give the same range.\n• Mass appears nowhere!",
        "u = 20 m/s, θ = 45°, g = 10  →  R = 400 / 10 = 40 m")],
      [("A car starts from rest with a = 2 m/s². Its speed after 5 s:", ["10 m/s", "2.5 m/s", "7 m/s", "25 m/s"], 0, "v = u + at = 0 + 2 × 5 = 10 m/s.", 1),
       ("Distance that car covers in those 5 s:", ["25 m", "10 m", "50 m", "12.5 m"], 0, "s = ut + ½at² = ½ × 2 × 25 = 25 m.", 2),
       ("For maximum range, launch a projectile at:", ["45°", "30°", "60°", "90°"], 0, "R = u² sin 2θ / g is largest when sin 2θ = 1, i.e. θ = 45°.", 1),
       ("Which pair of angles gives the SAME range (same speed)?", ["30° and 60°", "30° and 45°", "20° and 80°", "45° and 90°"], 0, "Complementary angles: sin 60° = sin 120°.", 2),
       ("The area under a velocity–time graph gives:", ["displacement", "acceleration", "jerk", "force"], 0, "Velocity × time = displacement.", 1),
       ("At the top of its path, a projectile's VERTICAL velocity is:", ["zero", "maximum", "equal to u", "equal to g"], 0, "It stops rising before it starts falling (the horizontal velocity carries on).", 2),
       ("A stone is dropped from rest (g = 10 m/s²). Distance fallen in 3 s:", ["45 m", "30 m", "90 m", "15 m"], 0, "s = ½gt² = ½ × 10 × 9 = 45 m.", 2),
       ("Range for u = 20 m/s at 45° (g = 10 m/s²):", ["40 m", "20 m", "80 m", "10 m"], 0, "R = u² sin 90° / g = 400 / 10 = 40 m.", 3),
       ("With no air resistance, a projectile's horizontal velocity:", ["stays constant", "increases", "decreases", "is zero at the top"], 0, "No horizontal force acts, so uₓ never changes.", 2)])

topic("ph_work", *PH, "Unit IV · Chapter 5", "Work, Energy and Power",
      "W = Fd cos θ, KE = ½mv², PE = mgh; the net work done equals the change in KE; power is work per second.",
      "Baron Von Friction is up ahead. Friction does NEGATIVE work. Be more positive than him.",
      [("Work",
        "• W = F d cos θ (joules).\n• <b>Zero</b> work when the force is perpendicular to the motion.\n• <b>Negative</b> work when the force opposes the motion (friction).",
        "Push 10 N through 3 m along the force  →  W = 30 J"),
       ("Energy",
        "• KE = ½mv²     • PE (gravity) = mgh     • spring PE = ½kx²\n• <b>Work–energy theorem</b>: net work = change in KE.\n• With only conservative forces (gravity, springs), mechanical energy is conserved.",
        "Dropped from h:  v = √(2gh)"),
       ("Power and collisions",
        "• Power = work / time = F × v  (watt; 1 hp = 746 W).\n• <b>Elastic</b> collision: KE conserved.  <b>Inelastic</b>: some KE lost.\n• Momentum is conserved in ALL collisions.",
        "Lift 50 kg by 2 m in 4 s  →  P = 50 × 10 × 2 / 4 = 250 W")],
      [("Work done when a 10 N force moves a box 3 m in the force's direction:", ["30 J", "3.3 J", "13 J", "0 J"], 0, "W = F × d = 10 × 3 = 30 J.", 1),
       ("Kinetic energy of a 2 kg ball moving at 3 m/s:", ["9 J", "6 J", "18 J", "3 J"], 0, "KE = ½ × 2 × 3² = 9 J.", 1),
       ("If a body's speed doubles, its kinetic energy becomes:", ["4 times", "2 times", "half", "unchanged"], 0, "KE ∝ v², so (2)² = 4 times.", 2),
       ("Gravity does no work on a satellite in a circular orbit because:", ["force ⟂ displacement", "there's no gravity in space", "the satellite has no mass", "its speed is zero"], 0, "The pull is towards the centre, the motion is along the circle: cos 90° = 0.", 2),
       ("In an ELASTIC collision, what is conserved?", ["momentum and KE", "only momentum", "only KE", "neither"], 0, "Elastic: both. Inelastic: momentum only.", 2),
       ("A 50 kg student climbs 4 m of stairs in 5 s (g = 10 m/s²). Power:", ["400 W", "200 W", "1000 W", "40 W"], 0, "P = mgh / t = 50 × 10 × 4 / 5 = 400 W.", 2),
       ("Potential energy of a spring stretched by x:", ["½kx²", "kx", "kx²", "½kx"], 0, "U = ½kx².", 1),
       ("A body falls freely from 5 m (g = 10 m/s²). Its speed just before landing:", ["10 m/s", "5 m/s", "50 m/s", "25 m/s"], 0, "v = √(2gh) = √100 = 10 m/s.", 3),
       ("The work done by friction on a sliding box is:", ["negative", "positive", "zero", "infinite"], 0, "Friction acts against the motion: θ = 180°, cos θ = −1.", 2)])

topic("ph_osc", *PH, "Unit X · Chapter 13", "Oscillations",
      "A pendulum's period T = 2π√(L/g), a spring's T = 2π√(m/k); in SHM you're fastest in the middle and stop at the ends.",
      "Those bars ahead are pendulums. Lean WITH the swing to pump it higher. Physics, but make it parkour.",
      [("Periodic motion and SHM",
        "Motion that repeats is periodic. In <b>simple harmonic motion</b> the restoring force is proportional to the displacement and points to the centre: <b>F = −kx</b>.\nx = A cos(ωt + φ),  T = 2π/ω,  frequency ν = 1/T.",
        "ν = 2 Hz  →  T = 0.5 s"),
       ("The simple pendulum",
        "For small swings <b>T = 2π√(L/g)</b>.\nIt depends on the length and on g, <b>not</b> on the bob's mass (and not on a small amplitude).\n4 × the length → 2 × the period.",
        "L ≈ 1 m  →  T ≈ 2 s  (a 'seconds pendulum')"),
       ("Springs and energy",
        "• Mass on a spring: <b>T = 2π√(m/k)</b>.\n• Speed is greatest at the mean position and zero at the extremes; acceleration is greatest at the extremes.\n• Energy swaps between KE and PE; the total stays ½kA².",
        "x = 0: all KE        x = ±A: all PE")],
      [("The period of a simple pendulum depends on:", ["length & g", "bob's mass", "bob's colour", "nothing"], 0, "T = 2π√(L/g): only the length and g appear.", 1),
       ("A pendulum is made 4 times longer. Its period becomes:", ["2×", "4×", "½×", "16×"], 0, "T ∝ √L, and √4 = 2.", 2),
       ("A body vibrates at 5 Hz. Its period is:", ["0.2 s", "5 s", "0.5 s", "2 s"], 0, "T = 1/ν = 1/5 s.", 1),
       ("In SHM, speed is greatest at:", ["the centre", "the ends", "halfway", "nowhere"], 0, "At the mean position all the energy is kinetic.", 1),
       ("In SHM, acceleration is greatest at:", ["the ends", "the centre", "everywhere", "nowhere"], 0, "a = −ω²x is largest where x is largest.", 2),
       ("Period of a mass m on a spring of constant k:", ["2π√(m/k)", "2π√(k/m)", "2π√(L/g)", "mk/2π"], 0, "A heavier mass or a softer spring swings more slowly.", 2),
       ("The restoring force in SHM is:", ["F = −kx", "F = kx²", "F = mg", "F = 0"], 0, "Proportional to displacement, directed back to the centre.", 1),
       ("The mass on a spring is made 4 times heavier. The period becomes:", ["2×", "4×", "½×", "same"], 0, "T ∝ √m.", 2),
       ("The period of a 'seconds pendulum' is:", ["2 s", "1 s", "0.5 s", "60 s"], 0, "It takes 1 s to swing each way, so a full period is 2 s.", 2)])

topic("ph_grav", *PH, "Unit VI · Chapter 7", "Gravitation",
      "F = Gm₁m₂ / r² (inverse square); g ≈ 9.8 m/s² falls with height and depth; escape speed 11.2 km/s = √2 × orbital speed.",
      "The Source keeps deleting gravity. It's a law. Laws can't be deleted. I checked.",
      [("The universal law",
        "<b>F = G m₁m₂ / r²</b>, with G = 6.67 × 10⁻¹¹ N m² kg⁻².\nDouble the distance → the force becomes ¼.\nEarth pulls you, and you pull Earth with the <b>same</b> force.",
        "r → 2r   ⇒   F → F / 4"),
       ("g, height and depth",
        "g = GM/R² ≈ 9.8 m/s² at the surface.\ng <b>decreases</b> with height and with depth; at Earth's centre g = 0.\nWeight (mg) changes from place to place, mass doesn't.",
        "Moon: g ≈ g/6  →  a 60 kg person weighs about 100 N there"),
       ("Orbits and escape",
        "• Orbital speed near Earth v₀ = √(gR) ≈ 7.9 km/s.\n• Escape speed vₑ = √(2gR) ≈ <b>11.2 km/s</b> = √2 × v₀.\n• Geostationary satellites: period 24 h, about 36 000 km up.\n• Kepler: orbits are ellipses, and T² ∝ r³.",
        "vₑ = √2 · v₀")],
      [("If the distance between two masses doubles, the gravitational force becomes:", ["¼", "½", "2×", "4×"], 0, "Inverse square: 1/2² = 1/4.", 1),
       ("Escape velocity from Earth is about:", ["11.2 km/s", "7.9 km/s", "3 × 10⁸ m/s", "9.8 m/s"], 0, "vₑ = √(2gR) ≈ 11.2 km/s.", 1),
       ("The value of g at the centre of the Earth is:", ["zero", "9.8 m/s²", "infinite", "4.9 m/s²"], 0, "g falls with depth and is zero at the centre.", 2),
       ("Near Earth, escape speed vₑ and orbital speed v₀ are related by:", ["vₑ = √2 · v₀", "vₑ = 2 · v₀", "vₑ = v₀ / √2", "vₑ = v₀"], 0, "√(2gR) = √2 × √(gR).", 2),
       ("The period of a geostationary satellite is:", ["24 hours", "12 hours", "1 hour", "365 days"], 0, "It turns with the Earth, so it stays above one place.", 1),
       ("Kepler's third law: T² is proportional to:", ["r³", "r²", "r", "1/r²"], 0, "Farther planets have much longer years.", 2),
       ("The SI unit of G is:", ["N m² kg⁻²", "N kg⁻¹", "m s⁻²", "N m kg⁻¹"], 0, "From F = Gm₁m₂/r²: G = Fr²/(m₁m₂).", 2),
       ("As you go higher above Earth's surface, g:", ["decreases", "increases", "stays the same", "becomes negative"], 0, "You're farther from Earth's centre.", 1),
       ("A body weighs 60 N on Earth. On the Moon (g/6) it weighs:", ["10 N", "60 N", "360 N", "6 N"], 0, "60 / 6 = 10 N. Its mass is unchanged.", 2)])

# =====================================================================================================================
# CHEMISTRY  -> Chemical Chaos
# =====================================================================================================================
CH = ("ChemicalChaos", "Chemistry")

topic("ch_basic", *CH, "Unit 1 · Some Basic Concepts of Chemistry", "Mole Concept",
      "moles = mass ÷ molar mass; 1 mole = 6.022 × 10²³ particles; 1 mole of gas fills 22.4 L at STP.",
      "The door's fused shut. Make water. Two hydrogens, one oxygen, zero drama.",
      [("The mole",
        "1 mole = <b>6.022 × 10²³</b> particles (Avogadro's number).\nMolar mass = mass of 1 mole in grams: H₂O 18, CO₂ 44, NaCl 58.5 g/mol.\n<b>moles = mass ÷ molar mass</b>.",
        "36 g of water = 36 / 18 = 2 mol = 2 × 6.022 × 10²³ molecules"),
       ("Gases and concentration",
        "At STP one mole of any ideal gas fills <b>22.4 L</b> (22.7 L at the newer 1 bar standard).\n<b>Molarity</b> M = moles of solute ÷ litres of solution.",
        "4 g NaOH (40 g/mol) in 500 mL  →  0.1 mol / 0.5 L = 0.2 M"),
       ("Laws and formulae",
        "• Conservation of mass; definite proportions (water is always 1 : 8 H : O by mass).\n• <b>Empirical</b> formula = simplest ratio (CH₂O for glucose); molecular = n × empirical (C₆H₁₂O₆).\n• The <b>limiting reagent</b> runs out first and decides how much product forms.",
        "2H₂ + O₂ → 2H₂O :   4 g H₂ needs 32 g O₂")],
      [("The molar mass of H₂O is:", ["18 g/mol", "16 g/mol", "10 g/mol", "20 g/mol"], 0, "2 × 1 + 16 = 18 g/mol.", 1),
       ("How many moles are in 88 g of CO₂ (44 g/mol)?", ["2", "0.5", "44", "4"], 0, "88 / 44 = 2 mol.", 1),
       ("One mole of any substance contains:", ["6.022 × 10²³ particles", "6.022 × 10²² particles", "22.4 particles", "1 g of matter"], 0, "That's Avogadro's number.", 1),
       ("Taking 22.4 L per mole at STP, 2 moles of a gas fill:", ["44.8 L", "22.4 L", "11.2 L", "2 L"], 0, "2 × 22.4 = 44.8 L.", 2),
       ("The empirical formula of glucose, C₆H₁₂O₆, is:", ["CH₂O", "C₂H₄O₂", "C₆H₁₂O₆", "CHO"], 0, "Divide 6 : 12 : 6 by 6 → 1 : 2 : 1.", 2),
       ("4 g of NaOH (40 g/mol) is made up to 1 L of solution. Its molarity:", ["0.1 M", "4 M", "1 M", "0.4 M"], 0, "4 / 40 = 0.1 mol in 1 L.", 2),
       ("In 2H₂ + O₂ → 2H₂O, how many moles of water come from 2 mol of O₂ (H₂ in excess)?", ["4", "2", "1", "8"], 0, "1 O₂ gives 2 H₂O, so 2 O₂ give 4 H₂O.", 2),
       ("Water always has H and O in the mass ratio 1 : 8. This is the law of:", ["definite proportions", "multiple proportions", "conservation of mass", "gaseous volumes"], 0, "A compound always has the same elements in the same proportion by mass.", 2),
       ("The reactant that is used up first is the:", ["limiting reagent", "excess reagent", "catalyst", "product"], 0, "It limits how much product can form.", 1)])

topic("ch_atom", *CH, "Unit 2 · Structure of Atom", "Structure of Atom",
      "Z = number of protons; a shell holds 2n² electrons; fill orbitals by Aufbau (n + l), at most 2 per orbital (Pauli), singly first (Hund).",
      "These element creatures used to be the periodic table. Know what's inside them and they calm down.",
      [("Inside the atom",
        "Electron (−1, tiny mass), proton (+1), neutron (0) in a tiny dense <b>nucleus</b> (Rutherford's gold foil).\nAtomic number <b>Z = protons</b>; mass number <b>A = protons + neutrons</b>.\nIsotopes: same Z, different A (¹²C, ¹⁴C).  Isobars: same A, different Z.",
        "²³Na: 11 protons, 12 neutrons, 11 electrons"),
       ("Quantum numbers",
        "• <b>n</b> shell (energy)   • <b>l</b> subshell: 0 = s, 1 = p, 2 = d, 3 = f\n• <b>mₗ</b> orbital: −l … +l   • <b>mₛ</b> spin: +½ or −½\nA shell holds <b>2n²</b> electrons; orbitals per subshell: s 1, p 3, d 5, f 7.",
        "n = 3 can hold 2 × 3² = 18 electrons"),
       ("Filling the orbitals",
        "• <b>Aufbau</b>: lowest energy first (n + l rule): 1s 2s 2p 3s 3p <b>4s 3d</b> 4p …\n• <b>Pauli</b>: no two electrons share all four quantum numbers (max 2 per orbital).\n• <b>Hund</b>: fill equal-energy orbitals singly first.",
        "O (Z = 8): 1s² 2s² 2p⁴      Cr: [Ar] 3d⁵ 4s¹  (half-filled is extra stable)")],
      [("An atom has 17 protons and 18 neutrons. Its mass number is:", ["35", "17", "18", "1"], 0, "A = protons + neutrons = 35 (it's chlorine-35).", 1),
       ("Maximum number of electrons in the n = 3 shell:", ["18", "8", "9", "32"], 0, "2n² = 2 × 9 = 18.", 1),
       ("Isotopes of an element have the same:", ["atomic number", "mass number", "number of neutrons", "mass"], 0, "Same protons (Z), different neutrons.", 1),
       ("For l = 1 the subshell is:", ["p", "s", "d", "f"], 0, "l = 0, 1, 2, 3 → s, p, d, f.", 1),
       ("The electronic configuration of oxygen (Z = 8):", ["1s² 2s² 2p⁴", "1s² 2s² 2p⁶", "1s² 2p⁶", "1s² 2s⁴ 2p²"], 0, "2 + 2 + 4 = 8 electrons, filled in order.", 2),
       ("Which rule says no two electrons in an atom can have all four quantum numbers the same?", ["Pauli exclusion principle", "Hund's rule", "Aufbau principle", "Heisenberg's principle"], 0, "So an orbital holds at most 2 electrons, with opposite spins.", 2),
       ("Which of these orbitals fills FIRST?", ["4s", "3d", "4p", "4d"], 0, "n + l: 4s = 4, 3d = 5, 4p = 5, 4d = 6. Lowest first.", 2),
       ("The number of orbitals in a d subshell is:", ["5", "3", "7", "1"], 0, "mₗ = −2, −1, 0, +1, +2.", 1),
       ("The electronic configuration of chromium (Z = 24) is:", ["[Ar] 3d⁵ 4s¹", "[Ar] 3d⁴ 4s²", "[Ar] 3d⁶", "[Ar] 4s² 4p⁴"], 0, "A half-filled d subshell is extra stable, so one 4s electron moves to 3d.", 3)])

topic("ch_bond", *CH, "Unit 4 · Chemical Bonding and Molecular Structure", "Chemical Bonding",
      "Ionic = electron transfer, covalent = sharing; in VSEPR lone pairs squeeze bond angles: CH₄ 109.5°, NH₃ 107°, H₂O 104.5°.",
      "The Lord of the Lab taught the elements to refuse to bond. Show them how it's done.",
      [("Why atoms bond",
        "Atoms bond to reach a stable <b>octet</b> (a duet for hydrogen).\n• <b>Ionic</b>: electron transfer between a metal and a non-metal (Na⁺Cl⁻).\n• <b>Covalent</b>: electron sharing between non-metals: single, double or triple bonds.",
        "N≡N: a triple bond, very strong, very unreactive"),
       ("VSEPR shapes",
        "Electron pairs repel and spread out; <b>lone pairs repel more</b> than bond pairs.\nBeCl₂ linear 180°  •  BF₃ trigonal planar 120°  •  CH₄ tetrahedral 109.5°\nNH₃ pyramidal 107°  •  H₂O bent 104.5°",
        "H₂O: 2 bond pairs + 2 lone pairs  →  bent, 104.5°"),
       ("Hybridisation, σ and π",
        "• sp → linear 180°,  sp² → trigonal planar 120°,  sp³ → tetrahedral 109.5°.\n• σ bonds: head-on overlap; π bonds: sideways overlap. Double bond = 1σ + 1π, triple = 1σ + 2π.\n• <b>Hydrogen bonding</b> (H bonded to F, O or N) gives water its high boiling point.",
        "C₂H₄: each carbon is sp², the C=C is 1σ + 1π")],
      [("The bond in NaCl is:", ["ionic", "covalent", "metallic", "hydrogen"], 0, "Sodium gives an electron to chlorine: Na⁺ and Cl⁻ attract.", 1),
       ("The shape of a methane (CH₄) molecule is:", ["tetrahedral", "linear", "trigonal planar", "bent"], 0, "Four bond pairs, no lone pairs: 109.5°.", 1),
       ("The H–O–H bond angle in water is about:", ["104.5°", "109.5°", "120°", "180°"], 0, "Two lone pairs push the bonds closer than 109.5°.", 2),
       ("The hybridisation of carbon in CH₄ is:", ["sp³", "sp²", "sp", "dsp²"], 0, "Four equivalent bonds → sp³.", 1),
       ("A triple bond is made of:", ["1 σ + 2 π", "3 σ", "2 σ + 1 π", "3 π"], 0, "Only one bond between two atoms can be head-on (σ).", 2),
       ("The shape of BF₃ is:", ["trigonal planar", "pyramidal", "tetrahedral", "linear"], 0, "Three bond pairs, no lone pair on boron: flat, 120°.", 2),
       ("NH₃ has a bond angle of 107°, less than CH₄'s 109.5°, because:", ["a lone pair repels more", "N is bigger than C", "NH₃ is ionic", "of hydrogen bonds"], 0, "Lone pair–bond pair repulsion squeezes the N–H bonds together.", 3),
       ("Water's unusually high boiling point is mainly due to:", ["hydrogen bonding", "ionic bonds", "its small mass", "metallic bonding"], 0, "Each H₂O hydrogen-bonds to its neighbours, which takes energy to break.", 2),
       ("The hybridisation of carbon in CO₂ (a linear molecule) is:", ["sp", "sp²", "sp³", "none"], 0, "Linear, 180° → sp.", 2)])

topic("ch_periodic", *CH, "Unit 3 · Classification of Elements and Periodicity", "Periodic Trends",
      "Across a period → atoms get smaller, harder to ionise and more electronegative; down a group ↓ they get bigger and easier to ionise.",
      "Answer platforms ahead. Pick the trend, not the vibes. Wrong ones dissolve. Like sodium in water.",
      [("The modern periodic law",
        "Properties repeat with <b>atomic number</b> (Moseley), not atomic mass.\n18 groups, 7 periods; s, p, d and f blocks.\nGroup 1 alkali metals • group 17 halogens • group 18 noble gases • d-block transition metals.",
        "Li, Na, K: same group → similar chemistry"),
       ("Across a period →",
        "• Atomic radius <b>decreases</b> (more protons pull on the same shell).\n• Ionisation enthalpy generally <b>increases</b>.\n• Electronegativity <b>increases</b>; metallic character decreases.",
        "Period 3: Na is the biggest atom, Cl the smallest"),
       ("Down a group ↓",
        "• Atomic radius <b>increases</b> (a new shell each period).\n• Ionisation enthalpy <b>decreases</b>; electronegativity decreases.\n• Fluorine is the most electronegative element of all.",
        "Caesium loses its outer electron far more easily than lithium")],
      [("Which atom is LARGER?", ["Na", "Cl", "Mg", "S"], 0, "Across period 3 the radius shrinks, so sodium (first) is biggest.", 1),
       ("The most electronegative element is:", ["F", "O", "Cl", "N"], 0, "Fluorine: top right (ignoring the noble gases).", 1),
       ("Down a group, atomic radius:", ["increases", "decreases", "stays same", "is zero"], 0, "Every period adds a new shell.", 1),
       ("Across a period, ionisation enthalpy generally:", ["increases", "decreases", "stays same", "drops to 0"], 0, "More nuclear charge holds the electrons tighter.", 1),
       ("The noble gases are in:", ["Group 18", "Group 1", "Group 17", "Group 2"], 0, "Full outer shells, in the last column.", 1),
       ("The modern periodic law is based on:", ["atomic number", "atomic mass", "density", "colour"], 0, "Moseley showed atomic number is the real key.", 1),
       ("Which has the LOWEST first ionisation enthalpy?", ["Cs", "Li", "Na", "K"], 0, "Lowest in group 1 = biggest atom = easiest electron to remove.", 2),
       ("The halogens are in:", ["Group 17", "Group 16", "Group 2", "Group 18"], 0, "F, Cl, Br, I: one electron short of an octet.", 1),
       ("The d-block elements are called:", ["transition metals", "noble gases", "halogens", "alkali metals"], 0, "Groups 3 to 12.", 2),
       ("Which is the SMALLEST atom?", ["F", "Li", "N", "B"], 0, "All in period 2: the radius shrinks to the right, so fluorine.", 2)])

topic("ch_redox", *CH, "Unit 7 · Redox Reactions", "Redox Reactions",
      "OIL RIG: oxidation is loss of electrons (oxidation number up), reduction is gain (down); the oxidising agent is itself reduced.",
      "The Source is ALL oxidising agent. It takes electrons. And dignity.",
      [("Oxidation and reduction",
        "<b>Oxidation</b> = loss of electrons (oxidation number goes up).\n<b>Reduction</b> = gain of electrons (oxidation number goes down).\n<b>OIL RIG</b>: Oxidation Is Loss, Reduction Is Gain. They always happen together.",
        "Zn + Cu²⁺ → Zn²⁺ + Cu :   Zn is oxidised, Cu²⁺ is reduced"),
       ("Oxidation numbers",
        "• A free element = 0.   • O is usually −2 (−1 in peroxides).   • H is +1 (−1 in metal hydrides).\n• They add up to 0 in a neutral compound, and to the charge in an ion.",
        "KMnO₄:  +1 + Mn + 4(−2) = 0  →  Mn = +7"),
       ("Agents",
        "• The <b>oxidising agent</b> takes electrons, so it is itself <b>reduced</b>.\n• The <b>reducing agent</b> gives electrons, so it is itself <b>oxidised</b>.\n• <b>Disproportionation</b>: one element is both oxidised and reduced.",
        "2H₂O₂ → 2H₂O + O₂   (O: −1 → −2 and 0)")],
      [("The oxidation number of Mn in KMnO₄ is:", ["+7", "+5", "+2", "−7"], 0, "+1 + Mn + 4(−2) = 0 → Mn = +7.", 2),
       ("Oxidation is:", ["loss of electrons", "gain of electrons", "gain of protons", "loss of neutrons"], 0, "OIL RIG: Oxidation Is Loss.", 1),
       ("In Zn + CuSO₄ → ZnSO₄ + Cu, the species OXIDISED is:", ["Zn", "Cu²⁺", "SO₄²⁻", "Cu"], 0, "Zn goes from 0 to +2: it lost two electrons.", 2),
       ("The oxidation number of S in H₂SO₄ is:", ["+6", "+4", "−2", "+2"], 0, "2(+1) + S + 4(−2) = 0 → S = +6.", 2),
       ("An oxidising agent:", ["gains electrons (is reduced)", "loses electrons", "is itself oxidised", "never changes"], 0, "It takes electrons from the other species.", 2),
       ("The oxidation number of O in H₂O₂ is:", ["−1", "−2", "0", "+1"], 0, "Peroxide: 2(+1) + 2(O) = 0 → O = −1.", 2),
       ("The oxidation number of an element in its free, uncombined state is:", ["0", "+1", "−1", "its group number"], 0, "O₂, Na, Fe on their own: all 0.", 1),
       ("A reaction where the same element is both oxidised and reduced is:", ["disproportionation", "combination", "displacement", "neutralisation"], 0, "e.g. 2H₂O₂ → 2H₂O + O₂.", 2),
       ("The oxidation number of Cr in K₂Cr₂O₇ is:", ["+6", "+7", "+3", "+12"], 0, "2(+1) + 2Cr + 7(−2) = 0 → 2Cr = +12 → Cr = +6.", 3)])

# =====================================================================================================================
# BIOLOGY  -> Bio-Zone
# =====================================================================================================================
BI = ("BioZone", "Biology")

topic("bio_cell", *BI, "Unit III · Chapter 8", "Cell: The Unit of Life",
      "All cells come from cells; mitochondria make ATP, ribosomes make proteins, the Golgi packs and lysosomes digest.",
      "We're microscopic now. Everything around us is a cell. Including that thing waving at us. Don't wave back.",
      [("Cell theory",
        "Schleiden and Schwann: living things are made of cells. Virchow added '<i>Omnis cellula-e cellula</i>': every cell comes from a cell.\n• <b>Prokaryotes</b> (bacteria): no nuclear membrane, no membrane-bound organelles, 70S ribosomes.\n• <b>Eukaryotes</b>: a true nucleus, organelles, 80S ribosomes.",
        "E. coli: prokaryote.  Your skin cell: eukaryote."),
       ("Organelles",
        "• <b>Mitochondria</b>: aerobic respiration, ATP (with their own DNA).  • <b>Chloroplasts</b>: photosynthesis.\n• <b>Ribosomes</b>: protein synthesis (no membrane).  • <b>RER</b> makes proteins, <b>SER</b> makes lipids.\n• <b>Golgi</b>: packs and secretes.  • <b>Lysosomes</b>: digestive enzymes ('suicide bags').",
        "A cell that secretes lots of enzyme has lots of RER and Golgi"),
       ("Membrane, wall, nucleus",
        "• <b>Fluid mosaic model</b> (Singer and Nicolson, 1972): a lipid bilayer with proteins floating in it.\n• Plant cells add a cellulose <b>cell wall</b> and a large vacuole; centrioles are found in animal cells.\n• The nucleus holds chromatin; the <b>nucleolus</b> makes ribosomal RNA.",
        "Passive transport needs no energy; active transport uses ATP")],
      [("Which organelle is the site of aerobic respiration (ATP)?", ["mitochondrion", "ribosome", "Golgi body", "lysosome"], 0, "The 'powerhouse of the cell'.", 1),
       ("'Omnis cellula-e cellula' was stated by:", ["Rudolf Virchow", "Robert Hooke", "M. Schleiden", "T. Schwann"], 0, "Virchow (1855): every cell arises from a pre-existing cell.", 2),
       ("Ribosomes in prokaryotic cells are:", ["70S", "80S", "60S", "90S"], 0, "Prokaryotes 70S; eukaryotic cytoplasm 80S.", 2),
       ("Which organelle is called the 'suicide bag'?", ["lysosome", "vacuole", "nucleus", "mitochondrion"], 0, "Its digestive enzymes can destroy the cell itself.", 1),
       ("The fluid mosaic model of the membrane was given by:", ["Singer and Nicolson", "Watson and Crick", "Schleiden and Schwann", "Robert Brown"], 0, "In 1972.", 2),
       ("Which of these has NO membrane?", ["ribosome", "mitochondrion", "chloroplast", "Golgi body"], 0, "Ribosomes are made of RNA and protein only.", 2),
       ("Plant cell walls are mainly made of:", ["cellulose", "chitin", "protein", "starch"], 0, "Fungal walls use chitin.", 1),
       ("Prokaryotic cells do NOT have:", ["a membrane-bound nucleus", "ribosomes", "a plasma membrane", "DNA"], 0, "Their DNA lies in the cytoplasm (the nucleoid).", 1),
       ("Which part of the nucleus makes ribosomal RNA?", ["nucleolus", "centriole", "chromatin", "nuclear pore"], 0, "The nucleolus is the site of rRNA synthesis.", 2)])

topic("bio_breath", *BI, "Unit V · Chapter 14", "Breathing and Exchange of Gases",
      "Air ends up in the alveoli; breathing in = the diaphragm contracts; O₂ rides haemoglobin, CO₂ travels mostly as bicarbonate.",
      "This is the airway. Everything here moves because of pressure. Like exams.",
      [("The airway",
        "Nostrils → pharynx → <b>larynx</b> (voice box) → trachea → bronchi → bronchioles → <b>alveoli</b>.\nThe epiglottis stops food going down the windpipe.\nAlveoli are thin, moist and wrapped in capillaries: O₂ goes into the blood, CO₂ comes out.",
        "Your lungs hold about 300 million alveoli"),
       ("How breathing works",
        "<b>Inspiration</b>: the diaphragm contracts and flattens and the external intercostal muscles lift the ribs → chest volume up, pressure down → air flows in.\n<b>Expiration</b> at rest is passive: the muscles relax.",
        "Lung pressure below atmospheric  →  air rushes in"),
       ("Volumes and transport",
        "• Tidal volume ≈ <b>500 mL</b>.  Vital capacity = the most you can breathe out after a deepest breath in.\n• About <b>97% of O₂</b> is carried by haemoglobin (oxyhaemoglobin).\n• CO₂: about 70% as <b>bicarbonate</b>, 20–25% as carbaminohaemoglobin, about 7% dissolved.",
        "Gases move by simple diffusion, from high to low partial pressure")],
      [("Gas exchange in the lungs happens in the:", ["alveoli", "trachea", "bronchi", "larynx"], 0, "Thin walls + capillaries = fast diffusion.", 1),
       ("During inspiration, the diaphragm:", ["contracts and flattens", "relaxes and domes up", "doesn't move", "moves upward"], 0, "Flattening makes the chest bigger, so air flows in.", 2),
       ("Normal tidal volume of a healthy adult is about:", ["500 mL", "50 mL", "5000 mL", "1 mL"], 0, "That's one quiet breath.", 2),
       ("Most of the oxygen in blood is carried:", ["by haemoglobin", "dissolved in plasma", "as bicarbonate", "by platelets"], 0, "About 97% as oxyhaemoglobin in RBCs.", 1),
       ("Most CO₂ is carried in the blood as:", ["bicarbonate", "carbaminohaemoglobin", "dissolved CO₂", "oxyhaemoglobin"], 0, "About 70% as HCO₃⁻.", 2),
       ("The voice box is the:", ["larynx", "pharynx", "trachea", "epiglottis"], 0, "It holds the vocal cords.", 1),
       ("Gases cross the alveolar wall by:", ["simple diffusion", "active transport", "osmosis", "pinocytosis"], 0, "No energy needed: high to low partial pressure.", 2),
       ("The flap that stops food entering the windpipe is the:", ["epiglottis", "glottis", "uvula", "diaphragm"], 0, "It covers the glottis when you swallow.", 2),
       ("The most air you can breathe out after the deepest breath in is the:", ["vital capacity", "tidal volume", "residual volume", "dead space"], 0, "VC = ERV + TV + IRV.", 2)])

topic("bio_circ", *BI, "Unit V · Chapter 15", "Body Fluids and Circulation",
      "The SA node is the pacemaker; blood goes round twice (lungs, then body); arteries carry blood away from the heart, veins bring it back.",
      "Professor Plaque blocks arteries for fun. Know the route and he's just a speed bump.",
      [("Blood",
        "Plasma (about 55%) + formed elements: <b>RBCs</b> carry O₂ with haemoglobin, <b>WBCs</b> defend, <b>platelets</b> clot.\nABO groups depend on antigens on the RBCs: <b>O</b> is the universal donor, <b>AB</b> the universal recipient.",
        "Clotting: thrombin turns fibrinogen into fibrin"),
       ("The heart",
        "Four chambers: 2 atria + 2 ventricles. The <b>SA node</b> is the pacemaker; the AV node passes the signal on.\nValves stop backflow: tricuspid (right), bicuspid/mitral (left), semilunar at the exits.\n<b>Double circulation</b>: pulmonary (heart ↔ lungs) + systemic (heart ↔ body).",
        "Body → RA → RV → lungs → LA → LV → body"),
       ("Cardiac cycle and vessels",
        "• One cycle ≈ <b>0.8 s</b> at 72 beats/min; stroke volume ≈ 70 mL → cardiac output ≈ <b>5 L/min</b>.\n• 'Lub' = AV valves closing, 'dub' = semilunar valves closing.\n• ECG: P (atria), QRS (ventricles depolarise), T (ventricles recover).",
        "72 × 70 mL ≈ 5 L of blood pumped every minute")],
      [("The pacemaker of the heart is the:", ["SA node", "AV node", "bundle of His", "Purkinje fibres"], 0, "It starts every heartbeat.", 1),
       ("The universal donor blood group is:", ["O", "AB", "A", "B"], 0, "Group O RBCs carry no A or B antigens.", 1),
       ("The valve between the left atrium and left ventricle is the:", ["bicuspid (mitral)", "tricuspid", "semilunar", "pulmonary"], 0, "Tricuspid is on the right side.", 2),
       ("One cardiac cycle at 72 beats per minute lasts:", ["0.8 s", "0.08 s", "8 s", "1.2 s"], 0, "60 / 72 ≈ 0.8 s.", 2),
       ("Pulmonary veins carry:", ["oxygen-rich blood to the left atrium", "oxygen-poor blood to the lungs", "oxygen-rich blood to the body", "lymph"], 0, "The one vein that carries oxygenated blood.", 2),
       ("In an ECG, the QRS complex shows:", ["ventricles depolarising", "atria depolarising", "ventricles recovering", "the heart at rest"], 0, "It marks the start of ventricular contraction.", 3),
       ("The 'lub' sound is made when these close:", ["the AV valves", "the semilunar valves", "the SA node", "the aorta"], 0, "Tricuspid and bicuspid close at the start of ventricular systole.", 2),
       ("Resting cardiac output of a healthy adult is about:", ["5 L/min", "500 mL/min", "50 L/min", "70 mL/min"], 0, "Stroke volume (70 mL) × heart rate (72).", 2),
       ("Which blood cells help in clotting?", ["platelets", "RBCs", "lymphocytes", "neutrophils"], 0, "Platelets (thrombocytes) release clotting factors.", 1)])

topic("bio_neural", *BI, "Unit V · Chapter 18", "Neural Control",
      "Dendrite → cell body → axon; at rest the inside is negative (about −70 mV); Na⁺ rushing in = depolarisation; the cerebellum keeps balance.",
      "We're sprinting down an axon. Jump node to node, just like the impulse does. Saltatory conduction, with extra screaming.",
      [("The neuron",
        "<b>Dendrites</b> receive, the <b>cell body</b> processes, the <b>axon</b> carries the impulse away to the terminals.\nA <b>myelin sheath</b> speeds conduction: the impulse jumps between the <b>nodes of Ranvier</b>.",
        "Myelinated fibres conduct far faster than non-myelinated ones"),
       ("The nerve impulse",
        "At rest the outside is positive and the inside negative (about <b>−70 mV</b>), kept up by the Na⁺/K⁺ pump (3 Na⁺ out, 2 K⁺ in).\nA stimulus lets <b>Na⁺ rush in</b> → depolarisation (action potential); K⁺ flowing out → repolarisation.",
        "The impulse travels along the axon as a wave of depolarisation"),
       ("Synapse and brain",
        "At a chemical synapse the impulse releases <b>neurotransmitters</b> (e.g. acetylcholine) across the synaptic cleft.\n<b>Cerebrum</b>: thinking, memory, senses • <b>cerebellum</b>: balance and coordination\n<b>Medulla</b>: breathing, heartbeat • <b>hypothalamus</b>: temperature, hunger, hormones.",
        "Every parkour landing you stick? Thank your cerebellum.")],
      [("The part of the brain that keeps balance:", ["cerebellum", "cerebrum", "medulla", "pons"], 0, "It coordinates movement and posture.", 1),
       ("The resting membrane potential is about:", ["−70 mV", "+70 mV", "0 mV", "−7 V"], 0, "Inside negative, outside positive.", 2),
       ("Which ions rush IN during depolarisation?", ["Na⁺", "K⁺", "Cl⁻", "Ca²⁺"], 0, "Na⁺ channels open and sodium floods in.", 2),
       ("The impulse jumping between nodes of Ranvier is:", ["saltatory", "synaptic", "passive", "reflex"], 0, "Saltatory conduction ('saltare' = to jump).", 2),
       ("Breathing and heartbeat are controlled by the:", ["medulla", "cerebrum", "cerebellum", "thalamus"], 0, "The medulla oblongata runs these automatic functions.", 1),
       ("The sodium–potassium pump moves:", ["3 Na⁺ out, 2 K⁺ in", "2 Na⁺ out, 3 K⁺ in", "Na⁺ in only", "K⁺ out only"], 0, "It uses ATP to keep the resting potential.", 3),
       ("A common neurotransmitter:", ["acetylcholine", "haemoglobin", "insulin", "keratin"], 0, "Released at synapses and neuromuscular junctions.", 1),
       ("Which part carries the impulse AWAY from the cell body?", ["axon", "dendrite", "synapse", "myelin"], 0, "Dendrites bring signals in; the axon sends them out.", 1),
       ("Body temperature and hunger are regulated by the:", ["hypothalamus", "cerebellum", "medulla", "spinal cord"], 0, "It also controls the pituitary gland.", 2),
       ("The tiny gap between two neurons is the:", ["synaptic cleft", "node of Ranvier", "axon hillock", "myelin sheath"], 0, "Neurotransmitters diffuse across it.", 1)])

topic("bio_division", *BI, "Unit III · Chapter 10", "Cell Cycle and Cell Division",
      "G₁ → S (DNA copies) → G₂ → M; mitosis keeps the chromosome number (2 identical cells), meiosis halves it (4 cells, with variation).",
      "The Source copies itself like a cell that skipped the checkpoint. Know the cycle, find the bug.",
      [("The cell cycle",
        "<b>Interphase</b> (about 95% of the time): G₁ growth → <b>S: DNA replicates</b> → G₂ preparation.\nThen <b>M phase</b>: karyokinesis (the nucleus divides) + cytokinesis (the cytoplasm divides).\n<b>G₀</b>: a resting stage for cells that stop dividing (many heart and nerve cells).",
        "A human cell cycle takes about 24 h; M phase only about 1 h"),
       ("Mitosis: equational division",
        "Prophase (chromosomes condense) → <b>Metaphase</b> (line up at the equator) → <b>Anaphase</b> (sister chromatids pulled apart) → Telophase (nuclei re-form).\nResult: <b>2 identical cells</b> with the same chromosome number (2n → 2n): growth and repair.",
        "Onion root tip: the classic mitosis slide"),
       ("Meiosis: reductional division",
        "Two divisions → <b>4 cells</b>, chromosome number <b>halved</b> (2n → n): it makes gametes.\n<b>Crossing over</b> in prophase I (pachytene) shuffles genes → variation.",
        "Human: 46 chromosomes → gametes with 23")],
      [("DNA replication happens in the:", ["S phase", "G₁ phase", "G₂ phase", "M phase"], 0, "S = synthesis (of DNA).", 1),
       ("Chromosomes line up at the equator during:", ["metaphase", "prophase", "anaphase", "telophase"], 0, "The metaphase plate.", 1),
       ("Mitosis produces:", ["2 identical cells (2n)", "4 cells (n)", "2 cells (n)", "4 identical cells (2n)"], 0, "Equational division: same number, identical copies.", 1),
       ("Crossing over happens during:", ["pachytene of prophase I", "metaphase II", "anaphase of mitosis", "interphase"], 0, "Homologous chromosomes swap segments.", 2),
       ("A human body cell has 46 chromosomes. A sperm cell has:", ["23", "46", "92", "12"], 0, "Meiosis halves the number.", 1),
       ("Sister chromatids separate during:", ["anaphase", "metaphase", "prophase", "interphase"], 0, "The spindle pulls them to opposite poles.", 2),
       ("Cells that stop dividing enter the:", ["G₀ phase", "S phase", "G₂ phase", "M phase"], 0, "A quiescent stage: alive and working, but not dividing.", 2),
       ("Meiosis is called a reductional division because it:", ["halves the chromosome number", "shrinks the cell", "destroys the DNA", "reduces the number of cells"], 0, "2n → n.", 2),
       ("The division of the cytoplasm is called:", ["cytokinesis", "karyokinesis", "interkinesis", "synapsis"], 0, "Karyokinesis is the division of the nucleus.", 1)])


def main():
    os.makedirs(os.path.join(ROOT, "Lessons"), exist_ok=True)
    lessons, banks = [], {}
    for t in TOPICS:
        lessons.append({
            "id": t["id"], "arc": t["arc"], "subject": t["subject"], "unit": t["unit"], "title": t["title"],
            "concept": t["title"], "keyIdea": rich(t["key"]), "pixel": t["pixel"],
            "slides": [{"heading": h, "body": rich(b), "example": rich(e)} for h, b, e in t["slides"]],
        })
        qs = banks.setdefault(t["subject"], [])
        for i, q in enumerate(t["questions"]):
            prompt, opts, ans, why, diff = q[:5]
            code = q[5] if len(q) > 5 else ""
            data = {"mode": "choice", "options": [rich(o) for o in opts], "answerIndex": ans}
            if code:
                data["code"] = code
            qs.append({
                "id": f"XI_{t['id'].upper()}_{i + 1:02d}", "arc": t["arc"], "subject": t["subject"].replace(" ", ""),
                "concept": t["title"], "topic": t["id"], "difficulty": diff, "type": "Logic", "situation": "",
                "prompt": rich(prompt), "data": data, "fastSeconds": 7 if diff == 1 else 9 if diff == 2 else 12, "timeLimit": 0,
                "explanation": rich(why), "hint": rich(t["key"]), "teachingModule": "", "needsTeacherReview": True,
            })
    with open(os.path.join(ROOT, "Lessons", "Class11.json"), "w", encoding="utf-8") as f:
        json.dump({"version": 1, "syllabus": "CBSE Class XI, session 2026-27", "topics": lessons}, f, ensure_ascii=False, indent=2)
    names = {"Computer Science": "Class11_CS", "Physics": "Class11_Physics", "Chemistry": "Class11_Chemistry", "Biology": "Class11_Biology"}
    for subj, qs in banks.items():
        arc = qs[0]["arc"]
        with open(os.path.join(ROOT, "Questions", names[subj] + ".json"), "w", encoding="utf-8") as f:
            json.dump({"arc": arc, "version": 1, "questions": qs}, f, ensure_ascii=False, indent=2)
        print(names[subj], len(qs), "questions")
    print(len(lessons), "lessons")


if __name__ == "__main__":
    main()
