using System;
using System.Collections.Generic;

namespace Glitched
{
    /// <summary>
    /// One CBSE Class XI topic, taught at a lesson terminal (StreamingAssets/Lessons/Class11.json, written by
    /// Tools/build_class11.py). After a terminal teaches a topic, the small enemies that follow ask MCQs on it.
    /// </summary>
    [Serializable]
    public class LessonData
    {
        public string id;          // "cs_flow"
        public string arc;         // "CodeCity"
        public string subject;     // "Computer Science"
        public string unit;        // "Unit 2 · NCERT Ch 6: Flow of Control"
        public string title;       // "Flow of Control"
        public string concept;     // mastery concept (= the questions' concept)
        public string keyIdea;     // the one-line recap
        public string pixel;       // Pixel's comment after the lesson
        public List<LessonSlide> slides = new List<LessonSlide>();
    }

    [Serializable]
    public class LessonSlide
    {
        public string heading;
        public string body;
        public string example;     // a formula, worked example or code
    }
}
