﻿using Microsoft.Xna.Framework;
using System;
using System.IO;
using XenoKit.Engine;
using XenoKit.Engine.Rendering;
using XenoKit.Engine.View;
using Xv2CoreLib.EAN;
using YAXLib;

namespace XenoKit.Editor
{
    public class LocalSettings
    {
        private const string PATH = "XenoKit/LocalSettings.xml";
        private static LocalSettings instance;
        public static LocalSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    Load();
                }
                return instance;
            }
        }

        public SerializedVector SerializedBackgroundColor { get; set; }
        public SerializedVector CustomScreenshotBackgroundColor { get; set; } = new SerializedVector(0,0,0,1f);
        [YAXAttributeFor("ScreenshotFormat")]
        [YAXSerializeAs("value")]
        public ScreenshotFormat ScreenshotFormat { get; set; }

        public SerializedCameraState[] CameraStates { get; set; } = new SerializedCameraState[5];

        private static bool Load()
        {
            try
            {
                YAXSerializer serializer = new YAXSerializer(typeof(LocalSettings), YAXSerializationOptions.DontSerializeNullObjects);
                LocalSettings settings = (LocalSettings)serializer.DeserializeFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PATH));

                if (settings != null)
                {
                    instance = settings;

                    if(instance.CameraStates == null)
                    {
                        instance.CameraStates = new SerializedCameraState[5];
                    }

                    for(int i = 0; i < instance.CameraStates.Length; i++)
                    {
                        instance.CameraStates[i] = new SerializedCameraState();
                    }

                    return true;
                }
                else
                {
                    instance = new LocalSettings();
                    return false;
                }
            }
            catch
            {
                instance = new LocalSettings();
                return false;
            }
        }
    
        public static void Save()
        {
            //Call once when closing the program

            if(instance != null)
            {
                instance.SerializedBackgroundColor = new SerializedVector(SceneManager.ViewportBackgroundColor);

                try
                {
                    YAXSerializer serializer = new YAXSerializer(typeof(LocalSettings));
                    serializer.SerializeToFile(instance, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PATH));
                }
                catch { }
            }
        }
    
        public static Color GetScreenshotColor()
        {
            return Instance.CustomScreenshotBackgroundColor.ToColor();
        }
    }

    public class SerializedCameraState
    {
        public SerializedVector CameraPosition { get; set; } = new SerializedVector(0, 0, -2f);
        public SerializedVector CameraTarget { get; set; } = new SerializedVector(0, 0, 2f);
        [YAXAttributeFor("Roll")]
        [YAXSerializeAs("value")]
        public float CameraRoll { get; set; }
        [YAXAttributeFor("FOV")]
        [YAXSerializeAs("value")]
        public float CameraFOV { get; set; } = EAN_File.DefaultFoV;

        public SerializedCameraState() { }

        public SerializedCameraState(CameraState cameraState)
        {
            CameraPosition = new SerializedVector(cameraState.Position);
            CameraTarget = new SerializedVector(cameraState.TargetPosition);
            CameraRoll = cameraState.Roll;
            CameraFOV = cameraState.FieldOfView;
        }
    }

    public class SerializedVector
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("X")]
        public float X { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Y")]
        public float Y { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Z")]
        public float Z { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("W")]
        public float W { get; set; }

        public SerializedVector() { }

        public SerializedVector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public SerializedVector(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public SerializedVector(Color color)
        {
            X = color.R / 255f;
            Y = color.G / 255f;
            Z = color.B / 255f;
            W = color.A / 255f;
        }

        public SerializedVector(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public void SetValues(float x, float y,float z,float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public Color ToColor()
        {
            return new Color(X, Y, Z, W);
        }
    }

}