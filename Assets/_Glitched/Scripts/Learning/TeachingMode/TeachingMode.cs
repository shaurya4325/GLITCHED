using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Interactive teaching mode (Section 7). Never a textbook page with a NEXT button: the player operates a lab.
    ///   Computer Science: rearrange/edit loop code and RUN it (CS_LoopSandbox)
    ///   Physics: sliders for velocity, angle, gravity, mass, force (PH_ProjectileLab)
    ///   Chemistry: combine atoms into molecules (CH_BondLab)
    ///   Biology: trace blood flow / the airway by tapping along it (BI_FlowTrace)
    /// </summary>
    public static class TeachingMode
    {
        public static bool Active { get; private set; }

        public static bool HasModule(QuestionData q) => q != null && !string.IsNullOrEmpty(ModuleFor(q));

        public static string ModuleFor(QuestionData q)
        {
            if (q == null) return null;
            if (!string.IsNullOrEmpty(q.teachingModule)) return q.teachingModule;
            switch (q.subject)
            {
                case "ComputerScience": return "CS_LoopSandbox";
                case "Physics": return "PH_ProjectileLab";
                case "Chemistry": return "CH_BondLab";
                case "Biology": return "BI_FlowTrace";
                default: return null;
            }
        }

        /// <summary>"Show me" after a wrong answer: the lab opens briefly with Pixel narrating.</summary>
        public static IEnumerator QuickDemo(QuestionData q)
        {
            yield return Run(q, null, false);
        }

        /// <summary>Boss defeat 2: the fight pauses, the villain teaches (sighing, annoyed, funny).</summary>
        public static IEnumerator RunForBoss(QuestionData q, BossDataSO boss)
        {
            yield return Run(q, boss, true);
        }

        static IEnumerator Run(QuestionData q, BossDataSO boss, bool requireSolve)
        {
            Active = true;
            string module = ModuleFor(q) ?? "CS_LoopSandbox";
            var root = UIFactory.Stretch(UIFactory.Rect("TeachingMode", Services.UI.PanelLayer));
            TeachingLab lab;
            switch (module)
            {
                case "PH_ProjectileLab": lab = root.gameObject.AddComponent<PhysicsLab>(); break;
                case "CH_BondLab": lab = root.gameObject.AddComponent<ChemBondLab>(); break;
                case "BI_FlowTrace": lab = root.gameObject.AddComponent<BioFlowTrace>(); break;
                default: lab = root.gameObject.AddComponent<CSLoopLab>(); break;
            }
            lab.Init(q, boss, requireSolve);
            Services.Audio.Music.Puzzle();
            while (!lab.Closed) yield return null;
            Object.Destroy(root.gameObject);
            Active = false;
        }
    }
}
