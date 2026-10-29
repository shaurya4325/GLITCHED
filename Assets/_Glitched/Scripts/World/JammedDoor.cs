using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A door jammed by visibly broken logic: solve its challenge to pass (an environmental puzzle).
    /// Uses the same challenge engine, but no enemy: the "attack" is the door slamming on you.
    /// </summary>
    public class JammedDoor : Interactable
    {
        public string id;
        public string concept = "";
        public string questionId = "";
        public ChallengeType preferredType = ChallengeType.Logic;
        public string jammedLine = "The door's logic is broken. It says 'OPEN IF KEY AND NOT KEY'.";
        Transform leaf;
        Collider block;
        bool open;

        void Start()
        {
            prompt = "Repair the door logic";
            leaf = transform.Find("Leaf");
            foreach (var c in GetComponentsInChildren<Collider>()) if (!c.isTrigger) block = c;
            if (!string.IsNullOrEmpty(id) && Services.Game.Data.flags.Contains("door_" + id)) Open(false);
        }

        public override bool CanInteract(PlayerController p) => !open && !EncounterManager.Busy;

        public override void Interact(PlayerController p)
        {
            StartCoroutine(Solve(p));
        }

        IEnumerator Solve(PlayerController p)
        {
            p.Lock(this);
            Services.Audio.Music.Puzzle();
            if (!string.IsNullOrEmpty(jammedLine)) Companion.Say(jammedLine, 3f, "bored");
            var arc = Services.Game.CurrentArc;
            QuestionData q = null;   // a random question on the door's concept every time (questionId is no longer fixed)
            if (q == null) q = Services.Selector.Pick(arc != null ? arc.id : "CodeCity", concept, 1, 2, preferredType);
            bool solved = false;
            int attempts = 0;
            while (!solved && q != null)
            {
                attempts++;
                ChallengeOutcome outcome = default;
                bool done = false;
                ChallengeEngine.Run(q, "DOOR LOGIC: JAMMED", 0.5f, o => { outcome = o; done = true; });
                while (!done) yield return null;
                Services.Mastery.Record(q, outcome.correct, outcome.fast);
                if (outcome.correct)
                {
                    solved = true;
                    p.Visual.Expression?.Play(ExpressionSequence.Correct);
                    Services.Game.AddXP(20 * q.difficulty);
                }
                else
                {
                    p.Visual.Expression?.Play(ExpressionSequence.Wrong);
                    Services.Audio.Play("door_slam");
                    if (CameraRig.Instance) CameraRig.Instance.Shake(0.4f);
                    yield return ChallengeEngine.ShowExplanation(q, outcome, "The door slams. Rude. But it left a note:");
                    q = Services.Selector.Pick(q.arc, q.concept, Mathf.Max(1, q.difficulty - 1), q.difficulty, q.type, q.id);
                    if (attempts >= 3 && q != null) Companion.Say("Third time's the charm. That's not science, I just like saying it.");
                }
            }
            Services.Audio.Music.ResumeExplore();
            p.Unlock(this);
            Open(true);
            if (!string.IsNullOrEmpty(id)) Services.Game.Data.SetFlag("door_" + id);
        }

        public void Open(bool fx)
        {
            open = true;
            if (block != null) block.enabled = false;
            if (leaf != null)
            {
                if (fx) Tween.MoveLocal(leaf, leaf.localPosition + Vector3.up * 3.2f, 0.6f, Ease.OutBack);
                else leaf.localPosition += Vector3.up * 3.2f;
            }
            if (fx)
            {
                Services.Audio.Play("door_open");
                VFX.Burst(transform.position + Vector3.up * 1.5f, UIFactory.Theme.green, 30, "spark");
                Companion.Say("Understanding the world is how you move through it. I read that on a cereal box.");
            }
        }
    }
}
