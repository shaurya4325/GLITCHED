using System;
using UnityEngine;

namespace Glitched
{
    /// <summary>The contract every challenge type implements (Section 8). Works with mouse and touch.</summary>
    public interface IChallenge
    {
        void Setup(QuestionData q, RectTransform area, ChallengeContext ctx);
        /// <summary>Evaluate the player's current answer now.</summary>
        void OnSubmit();
        /// <summary>True when the player has given enough input to submit.</summary>
        bool CanSubmit { get; }
        /// <summary>True for types that submit immediately on a click (multiple choice, tap-the-bug).</summary>
        bool AutoSubmits { get; }
        /// <summary>Number keys 1-4 pick answers quickly.</summary>
        void OnAnswerKey(int index);
        /// <summary>After the result, show what was right.</summary>
        void Reveal(bool correct);
        /// <summary>Extra seconds of "playback" after answering (Prediction plays out what happens).</summary>
        float PlaybackSeconds { get; }
        /// <summary>Label of the submit button (SUBMIT, RUN, BOND...).</summary>
        string SubmitLabel { get; }
        event Action<bool, float> Result;
    }

    /// <summary>Shared info for a running challenge.</summary>
    public class ChallengeContext
    {
        public float startTime;
        public float timeLimit;
        public bool showHint;
        public bool relaxed;
        public Action<string> setHelp;      // let a widget change the help line ("Drag the parts into the slots")
        public float Elapsed => Time.unscaledTime - startTime;
    }
}
