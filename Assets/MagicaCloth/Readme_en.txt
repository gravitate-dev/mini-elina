//------------------------------------------------------------------------------
// Magica Cloth
// Copyright (c) Magica Soft, 2020
// https://magicasoft.jp
//------------------------------------------------------------------------------

### About
Magica Cloth is a high-speed cloth simulation operated by Unity Job System + Burst compiler.


### Support Unity versions
Unity2018.4.0(LTS) or higher


### Feature

* Fast cloth simulation with Unity Job System + Burst compiler
* Works on all platforms except WebGL
* Implement BoneCloth driven by Bone (Transform) and MeshCloth driven by mesh
* MeshCloth can also work with skinning mesh
* Easy setup with an intuitive interface
* Time operation such as slow is possible
* With full source code


### Documentation
Since it is an online manual, please refer to the following URL for details.
https://magicasoft.jp/magica-cloth


### Release Notes
[v1.2.0]
Feature: Added blending function with original posture (Blend Weight).
Feature: Added the function to disable simulation by distance (Distance Disable).
Feature: Added a sample scene for distance disable function (DistanceDisableSample).
Improvement: Added scrollbar to cloth monitor.
Improvement: Data can be created even if the mesh has no UV value.
Improvement: Enhanced error handling.
Fix: Fixed slow playback bug. Time.timeScale works correctly.
Fix: Fixed an issue where an error occurred when duplicating a prefab with [Ctrl+D].
Fix: Fixed an issue where trying to create data without vertex painting would result in an error.

[v1.1.0]
Feature: Added support for Unity2018.4 (LTS).
Improvement: Error details are now displayed along with error codes.
Improvement: Vertex paint now records by vertex hash instead of vertex index.
Fix: If two or more MagicaPhysicsManagers are found, delete those found later.

[v1.0.3]
Fix: Fixed the problem that reference to data is lost while editing in Unity2019.3.0.

[v1.0.2]
Fix: Fixed an issue where an error occurred when running in the Mac editor environment.

[v1.0.1]
Fix: Fixed an error when writing a prefab in Unity2019.3.

[v1.0.0]
Note: first release.




