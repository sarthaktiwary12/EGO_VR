# EGO_VR

**Egocentric multi-modal data capture on Meta Quest 3**

Record synchronized egocentric video, body pose, hand tracking, controller poses, and IMU data for research and data collection.

---

## Overview

`EGO_VR` is a Unity-based multi-modal data collection tool for Meta Quest 3. It captures synchronized streams of egocentric sensor data:

* **Egocentric video** — compressed passthrough camera stream (`.mp4`, left camera, H.264)
* **Body pose** — full-body skeleton tracking (84 joints via `XR_META_body_tracking_full_body`)
* **Hand tracking** — per-hand skeleton data (24 bone rotations, pinch strengths, root pose, confidence)
* **Controller poses** — HMD and controller 6-DOF tracking (`hmd_poses.csv`, `left_controller_poses.csv`, `right_controller_poses.csv`)
* **IMU data** — linear acceleration, gyroscope, velocity, angular acceleration at 200 Hz
* **Camera intrinsics** — Camera2 API characteristics (fx, fy, cx, cy, distortion, sensor info)
* **Device metadata** — device serial, OS, GPU, Unity version, headset type

All streams are time-synchronized using OVR timestamps aligned to Unix time.

---

## Quick Start

1. **Build & sideload** the APK to your Meta Quest 3.
2. **Enable permissions**: When first launching, grant Camera and Body/Hand Tracking permissions.
3. **Start recording**: Press the **Menu button** on the left controller to start capture.
4. **Stop recording**: Press the Menu button again to stop.
5. **Manage recordings**: Press **Y button** on the left controller to open the Recording Menu (export, delete sessions).
6. **Transfer data**: Connect Quest 3 via USB or export to ZIP from the Recording Menu.

---

## Data Structure

Each recording session creates a timestamped directory:

```
/sdcard/Android/data/com.samusynth.OpenQuestCapture/files/
└── YYYYMMDD_hhmmss/
    ├── center_camera.mp4                    # Egocentric video (H.264)
    ├── center_camera_characteristics.json   # Camera intrinsics
    │
    ├── hmd_poses.csv                        # Head tracking (6-DOF)
    ├── left_controller_poses.csv            # Left controller pose
    ├── right_controller_poses.csv           # Right controller pose
    │
    ├── hand_tracking_left.csv               # Left hand skeleton
    ├── hand_tracking_right.csv              # Right hand skeleton
    ├── body_tracking.csv                    # Full body skeleton (84 joints)
    │
    ├── imu.csv                              # IMU: accel, gyro, velocity (200 Hz)
    ├── video_metadata.json                  # Video timing metadata
    ├── device_info.json                     # Device info
    └── video_start_time.txt                 # Unix ms of first video frame
```

---

## Data Format Details

### Pose CSV

* Files: `hmd_poses.csv`, `left_controller_poses.csv`, `right_controller_poses.csv`
* Format: `unix_time, ovr_timestamp, pos_x, pos_y, pos_z, rot_x, rot_y, rot_z, rot_w`

### Hand Tracking CSV

* Files: `hand_tracking_left.csv`, `hand_tracking_right.csv`
* Per row: timestamp, tracking status, hand confidence, root pose (pos + rot), hand scale, pinch strengths (5 fingers), finger confidences (5 fingers), bone rotations (24 bones x quaternion xyzw)

### Body Tracking CSV

* File: `body_tracking.csv`
* Per row: timestamp, confidence, calibration status, fidelity, then 84 joint positions + orientations

### IMU CSV

* File: `imu.csv`
* Format: `unix_time, ovr_timestamp, linear_acc_x/y/z, gyro_x/y/z, vel_x/y/z, ang_acc_x/y/z`
* Sampling: 200 Hz on dedicated background thread

### Camera Video (MP4)

* File: `center_camera.mp4`
* Codec: H.264 inside MP4 container
* Default: 30 FPS, 4 Mbps bitrate

---

## Environment

* Unity **6000.2.9f1**
* Meta OpenXR SDK
* Device: **Meta Quest 3** only

---

## Acknowledgement

Built on top of **[OpenQuestCapture](https://github.com/sarthaktiwary12/OpenQuestCapture)** and the original **[QuestRealityCapture](https://github.com/t-34400/QuestRealityCapture)** by **[t-34400](https://github.com/t-34400)**.

---

## License

This project is licensed under the **[MIT License](LICENSE)**.
