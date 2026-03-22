#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RealityLog.Common;
using RealityLog.IO;

namespace RealityLog.OVR
{
    /// <summary>
    /// Logs hand tracking skeleton data (bone rotations, pinch state, confidence)
    /// to CSV during recording sessions. Uses OVRPlugin.GetHandState for each hand.
    ///
    /// <para><b>Output:</b> One CSV per hand (hand_tracking_left.csv, hand_tracking_right.csv).
    /// Each row contains timestamp, tracking status, pinch strengths, hand scale,
    /// confidence, and per-bone quaternion rotations (24 bones per hand).</para>
    ///
    /// <para><b>Timing:</b> Polled every FixedUpdate (~50 Hz). Duplicate timestamps
    /// are filtered based on the OVR hand state time.</para>
    ///
    /// <para><b>Platform:</b> Meta Quest 3. Requires hand tracking permission and
    /// controllers to be set down for hand tracking to activate.</para>
    /// </summary>
    public class HandTrackingLogger : MonoBehaviour
    {
        private const int MAX_HAND_BONES = 24;
        // 4 values per bone (quaternion xyzw)
        private const int VALUES_PER_BONE = 4;
        private const int NUM_FINGERS = 5;

        private static readonly string[] BONE_NAMES = new string[]
        {
            "WristRoot",            // 0
            "ForearmStub",          // 1
            "Thumb0",               // 2
            "Thumb1",               // 3
            "Thumb2",               // 4
            "Thumb3",               // 5
            "Index1",               // 6
            "Index2",               // 7
            "Index3",               // 8
            "Middle1",              // 9
            "Middle2",              // 10
            "Middle3",              // 11
            "Ring1",                // 12
            "Ring2",                // 13
            "Ring3",                // 14
            "Pinky0",               // 15
            "Pinky1",               // 16
            "Pinky2",               // 17
            "Pinky3",               // 18
            "ThumbTip",             // 19 (OVRPlugin.BoneId.Hand_MaxSkinnable = 19, then tips)
            "IndexTip",             // 20
            "MiddleTip",            // 21
            "RingTip",              // 22
            "PinkyTip",             // 23
        };

        private static readonly string[] FINGER_NAMES = new string[]
        {
            "Thumb", "Index", "Middle", "Ring", "Pinky"
        };

        [SerializeField] private OVRPlugin.Hand hand = OVRPlugin.Hand.HandLeft;
        [SerializeField] private string fileName = "hand_tracking_left.csv";
        [SerializeField] private string directoryName = "";
        [SerializeField] private bool startLoggingOnStart = false;

        private CsvWriter? writer = null;

        private double baseOvrTimeSec;
        private long baseUnixTimeMs;
        private double latestTimestamp;

        public string DirectoryName
        {
            get => directoryName;
            set => directoryName = value;
        }

        public void StartLogging()
        {
            try
            {
                StopLogging();

                baseOvrTimeSec = OVRPlugin.GetTimeInSeconds();
                baseUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                latestTimestamp = 0;

                Debug.Log($"[{Constants.LOG_TAG}] {fileName} - Reset base times: OVR={baseOvrTimeSec:F3}s, Unix={baseUnixTimeMs}ms");

                if (!OVRPlugin.GetHandTrackingEnabled())
                {
                    Debug.LogWarning($"[{Constants.LOG_TAG}] HandTrackingLogger ({hand}) - Hand tracking not enabled on device, will log when hands become visible");
                }

                var filePath = Path.Combine(Application.persistentDataPath, DirectoryName, fileName);
                writer = new CsvWriter(filePath, BuildHeader());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Constants.LOG_TAG}] HandTrackingLogger ({hand}) - Failed to start: {ex.Message}");
                writer = null;
            }
        }

        public void StopLogging()
        {
            try
            {
                writer?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Constants.LOG_TAG}] HandTrackingLogger ({hand}) - Failed to dispose writer: {ex.Message}");
            }
            writer = null;
        }

        private void Start()
        {
            baseOvrTimeSec = OVRPlugin.GetTimeInSeconds();
            baseUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (startLoggingOnStart)
            {
                StartLogging();
            }
        }

        private void FixedUpdate()
        {
            if (writer == null)
                return;

            var handState = new OVRPlugin.HandState();
            if (!OVRPlugin.GetHandState(OVRPlugin.Step.Render, hand, ref handState))
                return;

            // Check if hand is actually tracked
            if ((handState.Status & OVRPlugin.HandStatus.HandTracked) == 0)
                return;

            var timestamp = handState.SampleTimeStamp;
            if (timestamp <= latestTimestamp)
                return;

            latestTimestamp = timestamp;

            var boneRotations = handState.BoneRotations;
            if (boneRotations == null || boneRotations.Length == 0)
                return;

            int boneCount = Mathf.Min(boneRotations.Length, MAX_HAND_BONES);

            // Row layout:
            // unix_time, ovr_timestamp, status, hand_confidence,
            // root_pose_pos_x/y/z, root_pose_rot_x/y/z/w,
            // hand_scale,
            // pinch_strength_thumb, pinch_strength_index, pinch_strength_middle, pinch_strength_ring, pinch_strength_pinky,
            // finger_confidence_thumb, ..., finger_confidence_pinky,
            // then per-bone quaternions (24 bones * 4 values)
            int metaFields = 4 + 7 + 1 + NUM_FINGERS + NUM_FINGERS; // 22
            var row = new double[metaFields + boneCount * VALUES_PER_BONE];

            int idx = 0;
            row[idx++] = ConvertOvrSecToUnixTimeMs(timestamp);
            row[idx++] = timestamp;
            row[idx++] = (double)handState.Status;
            row[idx++] = (double)handState.HandConfidence;

            // Root pose (wrist position and orientation in tracking space)
            row[idx++] = handState.RootPose.Position.x;
            row[idx++] = handState.RootPose.Position.y;
            row[idx++] = handState.RootPose.Position.z;
            row[idx++] = handState.RootPose.Orientation.x;
            row[idx++] = handState.RootPose.Orientation.y;
            row[idx++] = handState.RootPose.Orientation.z;
            row[idx++] = handState.RootPose.Orientation.w;

            // Hand scale
            row[idx++] = handState.HandScale;

            // Pinch strengths per finger
            for (int f = 0; f < NUM_FINGERS; f++)
            {
                row[idx++] = handState.PinchStrength[f];
            }

            // Finger confidences
            for (int f = 0; f < NUM_FINGERS; f++)
            {
                row[idx++] = (double)handState.FingerConfidences[f];
            }

            // Per-bone quaternion rotations
            for (int b = 0; b < boneCount; b++)
            {
                var rot = boneRotations[b];
                row[idx++] = rot.x;
                row[idx++] = rot.y;
                row[idx++] = rot.z;
                row[idx++] = rot.w;
            }

            writer.EnqueueRow(row);
        }

        private string[] BuildHeader()
        {
            var header = new List<string>
            {
                "unix_time", "ovr_timestamp", "status", "hand_confidence",
                "root_pose_pos_x", "root_pose_pos_y", "root_pose_pos_z",
                "root_pose_rot_x", "root_pose_rot_y", "root_pose_rot_z", "root_pose_rot_w",
                "hand_scale"
            };

            // Pinch strengths
            for (int f = 0; f < NUM_FINGERS; f++)
            {
                header.Add($"pinch_strength_{FINGER_NAMES[f].ToLower()}");
            }

            // Finger confidences
            for (int f = 0; f < NUM_FINGERS; f++)
            {
                header.Add($"finger_confidence_{FINGER_NAMES[f].ToLower()}");
            }

            // Per-bone quaternions
            for (int b = 0; b < MAX_HAND_BONES; b++)
            {
                string boneName = GetBoneName(b);
                header.Add($"{boneName}_rot_x");
                header.Add($"{boneName}_rot_y");
                header.Add($"{boneName}_rot_z");
                header.Add($"{boneName}_rot_w");
            }

            return header.ToArray();
        }

        private static string GetBoneName(int index)
        {
            if (index >= 0 && index < BONE_NAMES.Length)
            {
                return BONE_NAMES[index];
            }
            return $"Bone_{index}";
        }

        private long ConvertOvrSecToUnixTimeMs(double ovrTime)
        {
            var deltaSec = ovrTime - baseOvrTimeSec;
            var deltaMs = (long)(deltaSec * 1000.0);
            return baseUnixTimeMs + deltaMs;
        }

        private void OnDestroy()
        {
            writer?.Dispose();
            writer = null;
        }
    }
}
