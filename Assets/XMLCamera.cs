using System;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("Camera")]
public class XMLCamera
{
    public Vector2 Resolution;

    public bool IsOrthographic;
    public float OrthographicSize;

    public bool UseProjectionMatrix;
    public Matrix4x4 ProjectionMatrix;

    public bool IsPhysicalCamera;

    public float Near;
    public float Far;
    public float FieldOfView;
    public float Focal;
    public Vector2 SensorSize;
    public Vector2 LensShift;
    public Camera.GateFitMode GateFit;

    public float Pitch;     
    public float Yaw;       
    public float Roll;      
    public Vector3 Position; 
}
