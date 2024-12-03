# UnityEyes2
A much more adaptable version of the UnityEyes Package by Erroll Wood

Ensure that the 

Config File to contain the following parameters:
1) Head Pose
   - Variation to the Head Pose:
     - What distribution the noise in the pose is sampled from (Gaussian distribution initially)
     - Specific parameterization of the noise in the position, translation and rotation, with different parameters for each of x, y, z, alpha, beta, gamma

2) Eye Pose (restricted by Head Pose)
   - Variation to the Eye Pose:
     - What distribution the noise in the pose is sampled from (Gaussian distribution initially)
     - Specific parameterization of the noise in the position, (copy the approach from UnityEyes/U2Eyes)
       
3) Camera Pose
   - Intrinsics Matrix (or Projection Matrix): must specify at least one camera
   - Extrinsics Matrix (or Projection Matrix): must specify at least one camera
   - Variation to the Camera Pose:
     - What distribution the noise in the pose is sampled from (Gaussian distribution initially)
     - Specific parameterization of the noise in the position, translation and rotation, with different parameters for each of x, y, z, alpha, beta, gamma
       
   - Multiple cameras (must specify at least one, but should be able to handle arbitrary numbers)
4) Visual Effects
   - Reflections
   - Eye Wetness
   - Background
   - Person body characteristics
     - Demographics
     - Skin tone
     - Male/Female
     - Multiple face meshes and textures, with the ability to upload your own and a tutorial on how to do this
  
5) Other parameters to consider
   - How many images do you want?
   - Where to save the files (optional, will make a new folder implicitly)

Outputs of our imaging system:
1) All of the existing data from UnityEyes and U2Eyes (we'll parse this to make sure that it's easily usable downstream)
2) Make sure the gaze vector and axis of the eye are both provided
3) Output statistics on the entire run based on each eye output.
