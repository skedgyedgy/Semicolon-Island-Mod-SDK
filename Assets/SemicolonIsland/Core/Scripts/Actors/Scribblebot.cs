using UnityEngine;

namespace SemicolonIsland.Actors {
    [RequireComponent (typeof (AudioSource))]
    public class Scribblebot : Actor {
        [SerializeField]
        private string nickname = "Scribblebot";
        [SerializeField]
        private AudioClip[] attackSounds;
        [SerializeField]
        private AudioClip[] jumpSounds;
    }
}
