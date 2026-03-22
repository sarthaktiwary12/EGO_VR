# AGENTS.md — EGO_VR

## Repo Snapshot

- Project type: Unity Quest app (`ProjectSettings/ProjectVersion.txt` -> `6000.2.9f1`).
- Main scene: `Assets/RealityLog/Scenes/RealityLogScene.unity`.
- Runtime focus: synchronized egocentric video + body pose + hand tracking + IMU capture under `Assets/RealityLog/Scripts/Runtime`.
- Android bridge: `Assets/Plugins/Android` + `rebuild_kotlin_library.ps1` + `questcameralib.aar`.
- Target device: Meta Quest 3 only.

## Purpose

EGO_VR captures multi-modal egocentric data on Meta Quest 3 for research:
- Egocentric video (passthrough camera, H.264 MP4)
- Full-body skeleton tracking (84 joints)
- Hand tracking (24 bones per hand, pinch/confidence data)
- HMD and controller 6-DOF poses
- IMU data (200 Hz accelerometer, gyroscope, velocity)

Controllers are used to start/stop recording and manage sessions. Hand tracking captures hand skeleton data when hands are visible.

## Skills

A skill is a set of local instructions stored in a `SKILL.md` file. Use the skills below for this repo.

### Available skills

- capture-pipeline: Maintain synchronized camera/depth/pose/hand-tracking runtime behavior, timestamp alignment, and recording lifecycle logic. Use for changes in `RecordingManager`, `CaptureTimer`, `DepthMapExporter`, `ImageReaderSurfaceProvider`, `PoseLogger`, `HandTrackingLogger`, `BodyTrackingLogger`, or camera session flow. (file: .codex/skills/capture-pipeline/SKILL.md)
- recording-menu-export: Maintain recording menu UI, list rendering, export/delete flows, and world-space menu wiring. Use for changes in `Assets/RealityLog/Scripts/Runtime/UI` or `Runtime/FileOperations/RecordingOperations.cs`. (file: .codex/skills/recording-menu-export/SKILL.md)
- quest-android-bridge: Maintain Unity Android interop, manifest/gradle integration, and QuestCameraLib AAR flow. Use for changes in `Assets/RealityLog/Scripts/Runtime/Camera`, `Assets/Plugins/Android`, or Kotlin/AAR bridge work. (file: .codex/skills/quest-android-bridge/SKILL.md)

### How to use skills

- Trigger rules:
  - If a request clearly matches one of the three areas above, load and follow that skill.
  - If a request spans areas, load all relevant skills and execute in risk order:
    - `capture-pipeline` -> `recording-menu-export` -> `quest-android-bridge`.
- Progressive loading:
  - Read only the selected `SKILL.md` first.
  - Load additional files only when needed for the current task.
- Coordination:
  - State which skill(s) are being used and why in one short line.
  - Keep cross-skill changes explicit (call out boundaries between runtime, UI/export, and Android bridge).
- Fallback:
  - If a skill file is missing or unreadable, state the issue and continue with best-effort repository inspection.

## Repo Guardrails

- Do not hand-edit Unity `.meta` GUID values.
- Do not rename persisted recording directories or output filenames without explicit migration requirements.
- Do not update submodule commit pointers unless explicitly requested.
- Keep Android permission-sensitive behavior explicit when touching export/camera code paths.

## Validation Gates (strict)

- Always run targeted `rg` checks for touched subsystems before finishing.
- For C# runtime changes:
  - Run static grep checks from the relevant skill checklist.
  - Run Unity batch compile if Unity CLI is available; otherwise report it as skipped.
- For scene/prefab wiring changes:
  - Confirm expected components/events in `Assets/RealityLog/Scenes/RealityLogScene.unity` with `rg`.
- For Android bridge/AAR changes:
  - Verify manifest entries and plugin artifact presence.
  - Rebuild/replace AAR when interface-level changes are introduced.
