// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.UIElements;

#pragma warning disable 649

namespace SemicolonIsland.Editor {
    public class UpdateCharacterMass : ScriptableWizard {
        private const float DEFAULT_DENSITY = 105;

        public Animator body;
        public Transform pelvis;

        public Transform leftHips = null;
        public Transform leftKnee = null;
        public Transform leftFoot = null;

        public Transform rightHips = null;
        public Transform rightKnee = null;
        public Transform rightFoot = null;

        public Transform leftArm = null;
        public Transform leftElbow = null;

        public Transform rightArm = null;
        public Transform rightElbow = null;

        public Transform middleSpine = null;
        public Transform head = null;

        public RuntimeAnimatorController defaultAnimator;


        private float totalMass = 68;
        public float density = DEFAULT_DENSITY;

        Vector3 right = Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 forward = Vector3.forward;

        public bool flipForward = false;

        class BoneInfo {
            public string name;

            public Transform anchor;
            public CharacterJoint joint;
            public BoneInfo parent;

            public float minLimit;
            public float maxLimit;
            public float swingLimit;

            public Vector3 axis;
            public Vector3 normalAxis;

            public float radiusScale;
            public Type colliderType;

            public ArrayList children = new();
            public float density;
            public float summedMass;// The mass of this and all children bodies
        }

        ArrayList bones;
        BoneInfo rootBone;

        string CheckConsistency () {
            PrepareBones ();
            Hashtable map = new ();
            foreach (BoneInfo bone in bones) {
                if (bone.anchor) {
                    if (map[bone.anchor] != null) {
                        BoneInfo oldBone = (BoneInfo)map[bone.anchor];
                        return $"{bone.name} and {oldBone.name} may not be assigned to the same bone.";
                    }
                    map[bone.anchor] = bone;
                }
            }

            foreach (BoneInfo bone in bones) {
                if (bone.anchor == null)
                    return $"{bone.name} has not been assigned yet.\n";
            }

            return "";
        }

        public void OnDrawGizmos () {
            if (pelvis) {
                Gizmos.color = Color.red; Gizmos.DrawRay (pelvis.position, pelvis.TransformDirection (right));
                Gizmos.color = Color.green; Gizmos.DrawRay (pelvis.position, pelvis.TransformDirection (up));
                Gizmos.color = Color.blue; Gizmos.DrawRay (pelvis.position, pelvis.TransformDirection (forward));
            }
        }

        [MenuItem ("GameObject/Semicolon Island/Update Character Mass...", false, 2000)]
        static void CreateWizard () {
            _ = DisplayWizard<UpdateCharacterMass> ("Update Character Mass");
        }

        private bool detected = false;
        private Animator lastBody;

        private void OnWizardUpdate () {
            if (!detected) {
                if (Selection.activeGameObject.TryGetComponent (out Animator anim))
                    body = anim;
                detected = true;
            }
            if (body != lastBody) {
                lastBody = body;

                if (body.isHuman) {
                    pelvis = body.GetBoneTransform (HumanBodyBones.Hips);
                    middleSpine = body.GetBoneTransform (HumanBodyBones.Chest);
                    head = body.GetBoneTransform (HumanBodyBones.Head);
                    leftArm = body.GetBoneTransform (HumanBodyBones.LeftUpperArm);
                    leftElbow = body.GetBoneTransform (HumanBodyBones.LeftLowerArm);
                    rightArm = body.GetBoneTransform (HumanBodyBones.RightUpperArm);
                    rightElbow = body.GetBoneTransform (HumanBodyBones.RightLowerArm);
                    leftHips = body.GetBoneTransform (HumanBodyBones.LeftUpperLeg);
                    leftKnee = body.GetBoneTransform (HumanBodyBones.LeftLowerLeg);
                    leftFoot = body.GetBoneTransform (HumanBodyBones.LeftFoot);
                    rightHips = body.GetBoneTransform (HumanBodyBones.RightUpperLeg);
                    rightKnee = body.GetBoneTransform (HumanBodyBones.RightLowerLeg);
                    rightFoot = body.GetBoneTransform (HumanBodyBones.RightFoot);
                }
            }

            CheckConsistency ();

            helpString = "Drag all bones from the hierarchy into their slots.";
            isValid = true;
        }

        private void PrepareBones () {
            bones = new ArrayList ();

            rootBone = new BoneInfo {
                name = "Pelvis",
                anchor = pelvis,
                parent = null,
                density = 2.5F
            };
            _ = bones.Add (rootBone);
        }

        private void OnWizardCreate () {
            RecalculateBodyMass ();

            BuildBodies ();
            CalculateMass ();
        }

        private void RecalculateBodyMass () {
            CapsuleCollider collider = body.GetComponent<CapsuleCollider> ();
            Rigidbody rigidbody = body.GetComponent<Rigidbody> ();
            totalMass = ((Mathf.Pow (collider.radius, 3) * (4 / 3)) + (Mathf.Pow (collider.radius, 2) * collider.height)) * Mathf.PI * density;
            rigidbody.mass = totalMass;
        }

        void BuildBodies () {
            foreach (BoneInfo bone in bones)
                bone.anchor.GetComponent<Rigidbody> ().mass = bone.density;
        }

        void CalculateMassRecurse (BoneInfo bone) {
            float mass = bone.anchor.GetComponent<Rigidbody>().mass;
            foreach (BoneInfo child in bone.children) {
                CalculateMassRecurse (child);
                mass += child.summedMass;
            }
            bone.summedMass = mass;
        }

        void CalculateMass () {
            // Calculate allChildMass by summing all bodies
            CalculateMassRecurse (rootBone);

            // Rescale the mass so that the whole character weights totalMass
            float massScale = totalMass / rootBone.summedMass;
            foreach (BoneInfo bone in bones)
                bone.anchor.GetComponent<Rigidbody> ().mass *= massScale;

            // Recalculate allChildMass by summing all bodies
            CalculateMassRecurse (rootBone);
        }

        static int SmallestComponent (Vector3 point) {
            int direction = 0;
            if (Mathf.Abs (point[1]) < Mathf.Abs (point[0]))
                direction = 1;
            if (Mathf.Abs (point[2]) < Mathf.Abs (point[direction]))
                direction = 2;
            return direction;
        }

        static int LargestComponent (Vector3 point) {
            int direction = 0;
            if (Mathf.Abs (point[1]) > Mathf.Abs (point[0]))
                direction = 1;
            if (Mathf.Abs (point[2]) > Mathf.Abs (point[direction]))
                direction = 2;
            return direction;
        }

        public static int SecondLargestComponent (Vector3 point) {
            int smallest = SmallestComponent(point);
            int largest = LargestComponent(point);
            if (smallest < largest) {
                (smallest, largest) = (largest, smallest);
            }

            if (smallest == 0 && largest == 1)
                return 2;
            else if (smallest == 0 && largest == 2)
                return 1;
            else
                return 0;
        }
    }
}