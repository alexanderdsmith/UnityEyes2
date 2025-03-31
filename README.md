# UnityEyes2
Greater camera control for the UnityEyes Package by Erroll Wood, enabling machine learning on specific hardware specifications for eye tracking

Updated by Alexander Smith and Brijesh Muthumanickam

(c) 2025, University of Illinois at Urbana-Champaign

# How to use UnityEyes 2

## Hotkeys
    s: save image
    c: randomize camera and eye parameters
    r: randomize scene parameters
    l: randomize lighting parameters
    h: hide button overlays
    p: toggle preview mode
    →: increment camera index
    ←: decrement camera index

## Settings
**Eye definition:** the UnityEyes 2 simulated eye faces forward along the z-axis, with the y-axis facing down, and the x-axis facing to the right when looking from the perspective of the eye, defining a right-handed coordinate system.

<img width="425" alt="image" src="https://github.com/user-attachments/assets/c129b7d3-302f-41b5-b7f0-249fccbef07d" />

**Camera definition:** Users may define one or multiple cameras viewing the eye. Cameras are defined as pinhole, perspective cameras, which require intrinsics to define the properties of the camera field of view, focal length, and output image dimensions. Our cameras follow the right-hand rule, with the z-axis facing forward, the y-axis down, and the x-axis to the right, when looking at the perspective of the camera. Pixels in generated images have their origin at the top left corner, with *u* corresponding to the x-axis, and *v* corresponding to the y-axis.

For more detail on resolving pixel-based intrinsics, please see this OpenCV documentation: https://docs.opencv.org/4.x/dc/dbb/tutorial_py_calibration.html.

Six parameters define the camera extrinsics: three comprising translation in meters, and three comprising the Euler angles with the ZYZ convention  for rotation, in degrees. Extrinsics can also be calibrated automatically, but may also be determined directly from inspecting a hardware design in CAD software. A helpful tool for confirming extrinsic calibration is this transform tool made by Daniel Dugas: https://dugas.ch/transform_viewer/index.html.

**Camera array motion center:** We enable the user to define a camera array motion center: a 6 degrees-of-freedom transform relative to the eye as a point from which noise and extrinsic calibration of the camera(s) is defined. All cameras' extrinsics will be defined as child transforms of this motion center. If no motion center is provided by the user, it will default to the extrinsics of the first camera. A user may specify noise parameters which will be applied to the motion center, moving all cameras as a fixed array.

<img width="687" alt="image" src="https://github.com/user-attachments/assets/5c89d43f-163c-4b00-a266-451eba98d9f3" />

**Start generation:** Once a user selects "Start Generation", the program will run for the specified number of samples, and files will be saved to the specified output.

**Upload JSON:** Our settings user-interface enables a user to export a JSON file that configures automatic-generation runs of UnityEyes 2. A user may choose to upload their own, and template JSON files for are available in examples/.

# Configuration
The scene can be configured by uploading a JSON file (camera_config.json) containing the following parameters:

## outputPath: path to the folder where data will be saved (must already exist)
## num_samples: number of eye images to capture over the noise parameters for all cameras
## headless_mode: show (0) or hide (1) the User Interface after selecting "Start Generation"
## motion_center: use the center of the first camera (0) or use a relative offset from the eye (1) as the center of motion noise about the eye
## camera_array_center (optional): if motion_center is 1, then this must be defined. x, y, z, rx, ry, and rz correspond to meters and Euler angle degrees about the coordiante axes, where z is defined as forward, y is down, and x is to the right relative to the eye's point of view
## camera_array_center_noise: uniform noise for each of the 6 camera array center parameters (defined as ± provided value)

## cameras: list of cameras, each with the following parameters:
**name:** name of the camera that will be used to define the output file name for each view from that camera in the output dataset
**noise_distribution:** "uniform", or "gaussian", corresponding to the noise of each camera's intrinsics, respectively
**is_orthoraphic:** perspective (false) or orthographic (true)
**intrinsics:** parameters to define the camera's imaging, with focal lengths fx and fy, camera center positions cx and cy, and width (w) and height (h), all in pixels. These can be determined from intrinsics calibration or camera specifications.
**intrinsics_noise:** noise parameters for each of the 6 intrinsic parameters
**extrinsics:** world position of the camera with respect to the camera_array_center f motion_center is 1. Otherwise, the world position fo the camera in eye coordiantes

## lights: list of point light sources (more light sources in development) in the scene, each with the following parameters
**name:** light source name to be included in the output json file detailing the ground truth position of each light source
**type:** "point" is the only option available currently
**array_mounted:** if 0, light source is positioned relative to the eye. If 1, light source is positioned relative to the motion_center or the first camera if motion_center is not set. If 1, noise will be applied to the position of the light source, similarly to the cameras.
**position:** 3D position in x, y, and z relative to camera_array_center (if array_mounted = 1) or relative to the eye (if array_mounted = 0).
**position_noise:** noise added to the light source position (x, y, z) in addition to any noise applied by the camera_array_center if array_mounted is 1.
**properties:** includes range, intensity, color (r,g,b), shadows, and shadow_bias, all defined in the Unity docs for light sources.

## eye_parameters:
**pupil_size_range:** min and max values for the clamped uniform distribution range of the pupil size (in meters)
**iris_size_range:** min and max values for the clamped uniform distribution range of the iris size (in meters)
**default_yaw:** 0 corresponds to the optical axis aligned with the z axis of the eye. Changing this value will set the center of eye noise to an offset optical direction, rotating about the x axis of the eye(degrees). Note: appropriate physiological yaw range is approximately between -30 and 30 degrees.
**default_pitch:** 0 corresponds to the optical axis aligned with the z axis of the eye. Changing this value will set the center of eye noise to an offset optical direction, rotating about the y axis of the eye (degrees). Note: appropriate physiological pitch range is approximately between -45 and 45 degrees.
**yaw_noise:** noise, in degrees, about the default_yaw of the eye
**pitch_noise:** noise, in degrees, about the default_pitch of the eye

See camera_config.json for an example of the camera configuration.

UnityEyes 2 Outputs:
1) All of the existing data from UnityEyes
2) Optical Axis in 3D
3) Camera relative positioning
