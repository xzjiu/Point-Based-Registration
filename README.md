# Point-Based Registration

Unity project for aligning a CT/phantom model to physical fiducials measured with an OptiTrack-tracked probe.

## Requirements

- Unity **2022.3.62f3** (see `ProjectSettings/ProjectVersion.txt`).
- OptiTrack/Motive with NatNet rigid-body streaming enabled for live tracking.
- A tracked probe with its rigid-body origin calibrated to the intended probe tip.

The OptiTrack Unity plugin is included under `Assets/optitrack-unity`. Its license and EULA files remain with the plugin; consult those files for applicable third-party terms.

## Open the project

1. Clone this repository.
2. In Unity Hub, add the repository folder and open it with the version above.
3. Allow Unity to import assets and restore packages.
4. Open `Assets/Scenes/SampleScene.unity`.

`Assets`, `Packages`, and `ProjectSettings` are versioned, along with Unity `.meta` files. Unity regenerates the ignored `Library`, `Temp`, `Logs`, and IDE files locally.

## OptiTrack setup

The saved scene configures the streaming client for **Unicast**, with both server and local addresses set to **127.0.0.1**. This expects Motive to run on the same computer. For a separate tracking computer, update the server and local interface addresses in the Unity Inspector and match Motive's streaming configuration.

`ProbeTip` has an `OptitrackRigidBody` component configured for **rigid body ID 1**. Match this ID to the probe's streaming ID in Motive. The registration script records this transform's world position, so verify that it represents the physical tip before collecting points.

These are saved settings; they do not indicate an active tracking connection.

## Registration workflow

The scene assigns six CT fiducials to `ExistingFiducialRegistration`. Physical points must be recorded in the same order.

1. Enter Play mode with tracking active.
2. Touch the first physical fiducial and press **R**, holding the probe still while the script averages **60 Unity frames**.
3. Repeat for each remaining fiducial in order.
4. Press **Enter** to compute and apply a rigid rotation and translation to the CT root.
5. Review the per-point errors and RMS fiducial registration error (FRE) in the Unity Console. The reported millimeter conversion assumes one Unity unit equals one meter.

| Key | Action |
| --- | --- |
| R | Record the next probe point |
| Enter | Register the model to the recorded points |
| C | Clear recorded points and their visual markers |
| T | Reset the CT root transform |
| L | Reload CT fiducials from the scene |

## Main files

- `Assets/Scripts/ExistingFiducialRegistration.cs`: point collection, rigid registration, and FRE reporting.
- `Assets/Scenes/SampleScene.unity`: model, fiducials, probe, and streaming-client configuration.
- `Assets/Reigistration/`: phantom mesh and material (existing folder spelling preserved).
- `Assets/optitrack-unity/`: OptiTrack streaming integration and bundled examples.

This is an experimental registration project. FRE describes the fit at the collected fiducials and does not by itself establish accuracy elsewhere in the model.
