#pragma warning disable IDE0052 // Remove unread private members
using System;
using UnityEngine;

namespace SemicolonIsland.Actors {
    [RequireComponent (typeof (Animator))]
    [RequireComponent (typeof (CapsuleCollider))]
    [RequireComponent (typeof (Rigidbody))]
    public class Actor : MonoBehaviour {
        [SerializeField]
        private ActorStats actorStats = ActorStats.Default;
    }

    [Serializable]
    public struct ActorStats {
        public static ActorStats Default => new (8, 14, 8, 0.35f, 0.05f);

        [SerializeField]
        private float _walkSpeed;
        [SerializeField]
        private float _sprintSpeed;
        [SerializeField]
        private float _jumpSpeed;
        [SerializeField]
        private float _groundAccelFactor;
        [SerializeField]
        private float _airAccelFactor;

        public ActorStats (float walkSpeed, float sprintSpeed, float jumpSpeed, float groundAccelerationFactor, float airborneAccelerationFactor) {
            _walkSpeed = Mathf.Max (walkSpeed, 0);
            _sprintSpeed = Mathf.Max (sprintSpeed, _walkSpeed);
            _jumpSpeed = Mathf.Max (jumpSpeed, 0);
            _groundAccelFactor = Mathf.Clamp01 (groundAccelerationFactor);
            _airAccelFactor = Mathf.Clamp01 (airborneAccelerationFactor);
        }
    }
}
#pragma warning restore IDE0052 // Remove unread private members