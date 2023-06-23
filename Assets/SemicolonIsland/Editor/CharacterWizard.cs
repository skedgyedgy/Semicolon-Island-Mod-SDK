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
    public class CharacterWizard : ScriptableWizard {
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
        public float strength = 0.0F;

        Vector3 right = Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 forward = Vector3.forward;

        Vector3 worldRight = Vector3.right;
        Vector3 worldUp = Vector3.up;
        Vector3 worldForward = Vector3.forward;
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

        [MenuItem ("GameObject/Semicolon Island/Character Wizard...", false, 2000)]
        static void CreateWizard () {
            _ = DisplayWizard<CharacterWizard> ("Setup Custom Character");
        }

        void DecomposeVector (out Vector3 normalCompo, out Vector3 tangentCompo, Vector3 outwardDir, Vector3 outwardNormal) {
            outwardNormal = outwardNormal.normalized;
            normalCompo = outwardNormal * Vector3.Dot (outwardDir, outwardNormal);
            tangentCompo = outwardDir - normalCompo;
        }

        void CalculateAxes () {
            if (head != null && pelvis != null)
                up = CalculateDirectionAxis (pelvis.InverseTransformPoint (head.position));
            if (rightElbow != null && pelvis != null) {
                DecomposeVector (out _, out Vector3 removed, pelvis.InverseTransformPoint (rightElbow.position), up);
                right = CalculateDirectionAxis (removed);
            }

            forward = Vector3.Cross (right, up);
            if (flipForward)
                forward = -forward;
        }

        private bool detected = false;
        private Animator lastBody;

        void OnWizardUpdate () {
            if (!detected) {
                if (Selection.activeGameObject.TryGetComponent (out Animator anim)) {
                    body = anim;
                }
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
            
            errorString = CheckConsistency ();
            CalculateAxes ();

            if (errorString.Length != 0) {
                helpString = "Drag all bones from the hierarchy into their slots.\nMake sure your character is in T-Stand.\n";
            } else {
                helpString = "Make sure your character is in T-Stand.\nMake sure the blue axis faces in the same direction the chracter is looking.\nUse flipForward to flip the direction";
            }

            isValid = errorString.Length == 0;
        }

        void PrepareBones () {
            if (pelvis) {
                worldRight = pelvis.TransformDirection (right);
                worldUp = pelvis.TransformDirection (up);
                worldForward = pelvis.TransformDirection (forward);
            }

            bones = new ArrayList ();

            rootBone = new BoneInfo {
                name = "Pelvis",
                anchor = pelvis,
                parent = null,
                density = 2.5F
            };
            bones.Add (rootBone);

            AddMirroredJoint ("Hips", leftHips, rightHips, "Pelvis", worldRight, worldForward, -20, 70, 30, typeof (CapsuleCollider), 0.3F, 1.5F);
            AddMirroredJoint ("Knee", leftKnee, rightKnee, "Hips", worldRight, worldForward, -80, 0, 0, typeof (CapsuleCollider), 0.25F, 1.5F);

            AddJoint ("Middle Spine", middleSpine, "Pelvis", worldRight, worldForward, -20, 20, 10, null, 1, 2.5F);

            AddMirroredJoint ("Arm", leftArm, rightArm, "Middle Spine", worldUp, worldForward, -70, 10, 50, typeof (CapsuleCollider), 0.25F, 1.0F);
            AddMirroredJoint ("Elbow", leftElbow, rightElbow, "Arm", worldForward, worldUp, -90, 0, 0, typeof (CapsuleCollider), 0.20F, 1.0F);

            AddJoint ("Head", head, "Middle Spine", worldRight, worldForward, -40, 25, 25, null, 1, 1.0F);
        }

        void OnWizardCreate () {
            Cleanup ();
            BuildBodyCollider ();
            BuildCapsules ();
            AddBreastColliders ();
            AddHeadCollider ();

            BuildBodies ();
            BuildJoints ();
            CalculateMass ();
        }

        BoneInfo FindBone (string name) {
            foreach (BoneInfo bone in bones) {
                if (bone.name == name)
                    return bone;
            }
            return null;
        }

        void AddMirroredJoint (string name, Transform leftAnchor, Transform rightAnchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swingLimit, Type colliderType, float radiusScale, float density) {
            AddJoint ("Left " + name, leftAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swingLimit, colliderType, radiusScale, density);
            AddJoint ("Right " + name, rightAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swingLimit, colliderType, radiusScale, density);
        }

        void AddJoint (string name, Transform anchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swingLimit, Type colliderType, float radiusScale, float density) {
            BoneInfo bone = new () {
                name = name,
                anchor = anchor,
                axis = worldTwistAxis,
                normalAxis = worldSwingAxis,
                minLimit = minLimit,
                maxLimit = maxLimit,
                swingLimit = swingLimit,
                density = density,
                colliderType = colliderType,
                radiusScale = radiusScale
            };

            if (FindBone (parent) != null)
                bone.parent = FindBone (parent);
            else if (name.StartsWith ("Left"))
                bone.parent = FindBone ("Left " + parent);
            else if (name.StartsWith ("Right"))
                bone.parent = FindBone ("Right " + parent);


            bone.parent.children.Add (bone);
            bones.Add (bone);
        }

        private void BuildBodyCollider () {
            float width = (rightArm.position - leftArm.position).magnitude;
            float height = (body.transform.position - head.position).magnitude;

            CapsuleCollider collider = Undo.AddComponent<CapsuleCollider> (body.gameObject);

            collider.height = height + (width * 0.5f);
            collider.center = new Vector3 (0, collider.height * 0.5f + 0.0125f, 0);
            collider.radius = width;

            Rigidbody rigidbody = Undo.AddComponent<Rigidbody> (body.gameObject);
            totalMass = ((Mathf.Pow (collider.radius, 3) * (4 / 3)) + (Mathf.Pow (collider.radius, 2) * collider.height)) * Mathf.PI * density;
            rigidbody.mass = totalMass;
            rigidbody.freezeRotation = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            body.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            body.applyRootMotion = false;
            body.runtimeAnimatorController = defaultAnimator;

            Transform firstPersonFocus = new GameObject ("1stPersonFocus").transform;
            firstPersonFocus.SetParent (body.transform);
            firstPersonFocus.position = head.position + (0.375f * width * Vector3.up);
            Transform thirdPersonFocus = new GameObject ("3rdPersonFocus").transform;
            thirdPersonFocus.SetParent (body.transform);
            thirdPersonFocus.position = head.position;
        }

        private void BuildCapsules () {
            foreach (BoneInfo bone in bones) {
                if (bone.colliderType != typeof (CapsuleCollider))
                    continue;

                int direction;
                float distance;
                if (bone.children.Count == 1) {
                    BoneInfo childBone = (BoneInfo)bone.children[0];
                    Vector3 endPoint = childBone.anchor.position;
                    CalculateDirection (bone.anchor.InverseTransformPoint (endPoint), out direction, out distance);
                } else {
                    Vector3 endPoint = (bone.anchor.position - bone.parent.anchor.position) + bone.anchor.position;
                    CalculateDirection (bone.anchor.InverseTransformPoint (endPoint), out direction, out distance);

                    if (bone.anchor.GetComponentsInChildren (typeof (Transform)).Length > 1) {
                        Bounds bounds = new();
                        foreach (Transform child in bone.anchor.GetComponentsInChildren (typeof (Transform)).Cast<Transform> ()) {
                            bounds.Encapsulate (bone.anchor.InverseTransformPoint (child.position));
                        }

                        if (distance > 0)
                            distance = bounds.max[direction];
                        else
                            distance = bounds.min[direction];
                    }
                }

                CapsuleCollider collider = Undo.AddComponent<CapsuleCollider>(bone.anchor.gameObject);
                collider.direction = direction;

                Vector3 center = Vector3.zero;
                center[direction] = distance * 0.5F;
                collider.center = center;
                collider.height = Mathf.Abs (distance * (1 + bone.radiusScale));
                collider.radius = Mathf.Abs (distance * bone.radiusScale);

                collider.isTrigger = true;
            }
        }

        void Cleanup () {
            foreach (BoneInfo bone in bones) {
                if (!bone.anchor)
                    continue;

                Component[] joints = bone.anchor.GetComponentsInChildren(typeof(Joint));
                foreach (Joint joint in joints.Cast<Joint> ())
                    Undo.DestroyObjectImmediate (joint);

                Component[] bodies = bone.anchor.GetComponentsInChildren(typeof(Rigidbody));
                foreach (Rigidbody body in bodies.Cast<Rigidbody> ())
                    Undo.DestroyObjectImmediate (body);

                Component[] colliders = bone.anchor.GetComponentsInChildren(typeof(Collider));
                foreach (Collider collider in colliders.Cast<Collider> ())
                    Undo.DestroyObjectImmediate (collider);
            }
        }

        void BuildBodies () {
            foreach (BoneInfo bone in bones) {
                Undo.AddComponent<Rigidbody> (bone.anchor.gameObject);
                bone.anchor.GetComponent<Rigidbody> ().isKinematic = true;
                bone.anchor.GetComponent<Rigidbody> ().mass = bone.density;
            }
        }

        void BuildJoints () {
            foreach (BoneInfo bone in bones) {
                if (bone.parent == null)
                    continue;

                CharacterJoint joint = Undo.AddComponent<CharacterJoint>(bone.anchor.gameObject);
                bone.joint = joint;

                // Setup connection and axis
                joint.axis = CalculateDirectionAxis (bone.anchor.InverseTransformDirection (bone.axis));
                joint.swingAxis = CalculateDirectionAxis (bone.anchor.InverseTransformDirection (bone.normalAxis));
                joint.anchor = Vector3.zero;
                joint.connectedBody = bone.parent.anchor.GetComponent<Rigidbody> ();
                joint.enablePreprocessing = false; // turn off to handle degenerated scenarios, like spawning inside geometry.

                // Setup limits
                SoftJointLimit softJointLimit = new () {
                    contactDistance = 0, // default to zero, which automatically sets contact distance.

                    limit = bone.minLimit
                };
                SoftJointLimit limit = softJointLimit;
                joint.lowTwistLimit = limit;

                limit.limit = bone.maxLimit;
                joint.highTwistLimit = limit;

                limit.limit = bone.swingLimit;
                joint.swing1Limit = limit;

                limit.limit = 0;
                joint.swing2Limit = limit;
            }
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

        static void CalculateDirection (Vector3 point, out int direction, out float distance) {
            // Calculate longest axis
            direction = 0;
            if (Mathf.Abs (point[1]) > Mathf.Abs (point[0]))
                direction = 1;
            if (Mathf.Abs (point[2]) > Mathf.Abs (point[direction]))
                direction = 2;

            distance = point[direction];
        }

        static Vector3 CalculateDirectionAxis (Vector3 point) {
            CalculateDirection (point, out int direction, out float distance);
            Vector3 axis = Vector3.zero;
            if (distance > 0)
                axis[direction] = 1.0F;
            else
                axis[direction] = -1.0F;
            return axis;
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

        Bounds Clip (Bounds bounds, Transform relativeTo, Transform clipTransform, bool below) {
            int axis = LargestComponent(bounds.size);

            if (Vector3.Dot (worldUp, relativeTo.TransformPoint (bounds.max)) > Vector3.Dot (worldUp, relativeTo.TransformPoint (bounds.min)) == below) {
                Vector3 min = bounds.min;
                min[axis] = relativeTo.InverseTransformPoint (clipTransform.position)[axis];
                bounds.min = min;
            } else {
                Vector3 max = bounds.max;
                max[axis] = relativeTo.InverseTransformPoint (clipTransform.position)[axis];
                bounds.max = max;
            }
            return bounds;
        }

        Bounds GetBreastBounds (Transform relativeTo) {
            // Pelvis bounds
            Bounds bounds = new();
            bounds.Encapsulate (relativeTo.InverseTransformPoint (leftHips.position));
            bounds.Encapsulate (relativeTo.InverseTransformPoint (rightHips.position));
            bounds.Encapsulate (relativeTo.InverseTransformPoint (leftArm.position));
            bounds.Encapsulate (relativeTo.InverseTransformPoint (rightArm.position));
            Vector3 size = bounds.size;
            size[SmallestComponent (bounds.size)] = size[LargestComponent (bounds.size)] / 2.0F;
            bounds.size = size;
            return bounds;
        }

        void AddBreastColliders () {
            // Middle spine and pelvis
            if (middleSpine != null && pelvis != null) {
                Bounds bounds;
                BoxCollider box;

                // Middle spine bounds
                bounds = Clip (GetBreastBounds (pelvis), pelvis, middleSpine, false);
                box = Undo.AddComponent<BoxCollider> (pelvis.gameObject);
                box.center = bounds.center;
                box.size = bounds.size;
                box.isTrigger = true;

                bounds = Clip (GetBreastBounds (middleSpine), middleSpine, middleSpine, true);
                box = Undo.AddComponent<BoxCollider> (middleSpine.gameObject);
                box.center = bounds.center;
                box.size = bounds.size;
                box.isTrigger = true;
            }
            // Only pelvis
            else {
                Bounds bounds = new();
                bounds.Encapsulate (pelvis.InverseTransformPoint (leftHips.position));
                bounds.Encapsulate (pelvis.InverseTransformPoint (rightHips.position));
                bounds.Encapsulate (pelvis.InverseTransformPoint (leftArm.position));
                bounds.Encapsulate (pelvis.InverseTransformPoint (rightArm.position));

                Vector3 size = bounds.size;
                size[SmallestComponent (bounds.size)] = size[LargestComponent (bounds.size)] / 2.0F;

                BoxCollider box = Undo.AddComponent<BoxCollider>(pelvis.gameObject);
                box.center = bounds.center;
                box.size = size;
                box.isTrigger = true;
            }
        }

        void AddHeadCollider () {
            if (head.GetComponent<Collider> ())
                Destroy (head.GetComponent<Collider> ());

            float radius = Vector3.Distance(leftArm.position, rightArm.position) * 0.75f;

            SphereCollider sphere = Undo.AddComponent<SphereCollider>(head.gameObject);
            sphere.radius = radius;
            sphere.isTrigger = true;
            Vector3 center = Vector3.zero;

            CalculateDirection (head.InverseTransformPoint (pelvis.position), out int direction, out float distance);
            if (distance > 0)
                center[direction] = -radius;
            else
                center[direction] = radius;
            sphere.center = center;

            Transform headAttachment = new GameObject ("HeadAttachment").transform;
            headAttachment.SetParent (head);
            headAttachment.SetPositionAndRotation (head.position, pelvis.rotation);
            headAttachment.localScale = new Vector3 (1 / headAttachment.lossyScale.x, 1 / headAttachment.lossyScale.y, 1 / headAttachment.lossyScale.z);

            Transform faceAttachment = new GameObject ("FaceAttachment").transform;
            faceAttachment.SetParent (head);
            faceAttachment.SetPositionAndRotation (head.position + pelvis.TransformDirection (0, radius, radius), pelvis.rotation);
            faceAttachment.localScale = new Vector3 (1 / faceAttachment.lossyScale.x, 1 / faceAttachment.lossyScale.y, 1 / faceAttachment.lossyScale.z);
            Transform eyesAttachment = new GameObject ("EyesAttachment").transform;
            eyesAttachment.SetParent (head);
            eyesAttachment.SetPositionAndRotation (faceAttachment.position, pelvis.rotation);
            eyesAttachment.localScale = new Vector3 (1 / eyesAttachment.lossyScale.x, 1 / eyesAttachment.lossyScale.y, 1 / eyesAttachment.lossyScale.z);

            Transform hatAttachment = new GameObject ("HatAttachment").transform;
            hatAttachment.SetParent (head);
            hatAttachment.SetPositionAndRotation (head.position + (Vector3.up * radius * 2), pelvis.rotation);
            hatAttachment.localScale = new Vector3 (1 / hatAttachment.lossyScale.x, 1 / hatAttachment.lossyScale.y, 1 / hatAttachment.lossyScale.z);
            Transform hatAttachment2 = new GameObject ("AltLHatAttachment").transform;
            hatAttachment2.SetParent (head);
            hatAttachment2.SetPositionAndRotation (head.position + Quaternion.Euler (0, 0, -10) * (Vector3.up * radius * 2), Quaternion.Euler (0, 0, -10));
            hatAttachment2.localScale = new Vector3 (1 / hatAttachment2.lossyScale.x, 1 / hatAttachment2.lossyScale.y, 1 / hatAttachment2.lossyScale.z);
            Transform hatAttachment3 = new GameObject ("AltRHatAttachment").transform;
            hatAttachment3.SetParent (head);
            hatAttachment3.SetPositionAndRotation (head.position + Quaternion.Euler (0, 0, 10) * (Vector3.up * radius * 2), Quaternion.Euler (0, 0, 10));
            hatAttachment3.localScale = new Vector3 (1 / hatAttachment3.lossyScale.x, 1 / hatAttachment3.lossyScale.y, 1 / hatAttachment3.lossyScale.z);

            Transform rightHandGrip = new GameObject ("HandGrip").transform;
            rightHandGrip.SetParent (body.GetBoneTransform (HumanBodyBones.RightHand));
            rightHandGrip.SetLocalPositionAndRotation (Vector3.zero, Quaternion.identity);
            rightHandGrip.localScale = new Vector3 (1 / rightHandGrip.lossyScale.x, 1 / rightHandGrip.lossyScale.y, 1 / rightHandGrip.lossyScale.z);
        }
    }
}