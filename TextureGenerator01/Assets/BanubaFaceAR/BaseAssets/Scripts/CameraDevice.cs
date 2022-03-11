﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
namespace BNB
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraDevice : MonoBehaviour
    {
        public struct CameraTextureData
        {
            public bool isVerticallyFlipped;
            public int Angle;
        }
        Color32[] data;
        Color32Pinner dataPinner;

        WebCamTexture mWebCamTexture;
        private WebCamTexture webCamTexture
        {
            get
            {
                return mWebCamTexture;
            }
            set
            {
                mWebCamTexture = value;
            }
        }
        public CameraTextureData cameraTextureData;

        BanubaSDKBridge.bnb_bpc8_image_t cameraImage;
        public Texture2D cameraTexture;

        public event Action<BanubaSDKBridge.bnb_bpc8_image_t> onCameraImage;
        public event Action<Texture2D, CameraTextureData> onCameraTexture;

        void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                Permission.RequestUserPermission(Permission.Camera);
                return;
            }
#endif
            cameraTextureData = new CameraTextureData();
            OpenCameraDevice();
        }

        void Update()
        {
            if (webCamTexture == null) {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                    OpenCameraDevice();
                    return;
                }
#endif
                Debug.Log("ERROR: Camera not opened!");
                return;
            }

#if UNITY_WEB && !UNITY_EDITOR

            UpdateCameraImage();

#else
            if (webCamTexture.didUpdateThisFrame) {
                UpdateCameraImage();
            }
#endif
        }

        void OnDestroy()
        {
            if (webCamTexture != null) {
                webCamTexture.Stop();
                webCamTexture = null;
            }
        }

        private void OpenCameraDevice()
        {
            String deviceName = null;

            Debug.Log("Webcam's available: ");
            foreach (WebCamDevice device in WebCamTexture.devices) {
                Debug.Log("Name: " + device.name);
                Debug.Log("isFrontFacing: " + device.isFrontFacing);
                var resolutions = device.availableResolutions;
                if (resolutions != null) {
                    foreach (Resolution resolution in resolutions) {
                        Debug.Log("\t" + resolution.ToString());
                    }
                }
                if (device.isFrontFacing) {
                    deviceName = device.name;
                    break;
                }
            }

            if (WebCamTexture.devices.Length > 0) {
                if (deviceName != null) {
                    webCamTexture = new WebCamTexture(deviceName);
                } else {
                    // default device
                    webCamTexture = new WebCamTexture();
                }
                webCamTexture.requestedFPS = 30;
                webCamTexture.Play();
            }

            if (webCamTexture == null) {
                Debug.Log("Camera creation error!");
                return;
            }

            cameraImage = new BanubaSDKBridge.bnb_bpc8_image_t();
            cameraImage.format = new BanubaSDKBridge.bnb_image_format_t();

            UpdateTexture();
            UpdateCameraImage();
        }

        private void UpdateCameraImage()
        {
            bool dataChanged = cameraTextureData.Angle != webCamTexture.videoRotationAngle
                               || cameraTextureData.isVerticallyFlipped != webCamTexture.videoVerticallyMirrored;

            cameraTextureData.Angle = webCamTexture.videoRotationAngle;
            cameraTextureData.isVerticallyFlipped = webCamTexture.videoVerticallyMirrored;

            if ((webCamTexture.width * webCamTexture.height) != data.Length || dataChanged) {
                Debug.Log("Camera size changed: " + webCamTexture.width + "x" + webCamTexture.height);
                UpdateTexture();
            }

            webCamTexture.GetPixels32(data); // NOTE: pixels are vertically flipped according to documentation
            cameraTexture.SetPixels32(data);
            cameraTexture.Apply();
            if (data == null) {
                Debug.Log("ERROR: GetPixels32 return not valid data (null)");
                return;
            }

            cameraImage.format.width = (uint) webCamTexture.width;
            cameraImage.format.height = (uint) webCamTexture.height;
            cameraImage.format.orientation = angleToOrientation(cameraTextureData.Angle);
            cameraImage.format.require_mirroring = 1; // selfie mode: true
            cameraImage.format.face_orientation = 0;

            cameraImage.data = dataPinner; // Use the operator to retrieve the IntPtr
            cameraImage.pixel_format = BanubaSDKBridge.bnb_pixel_format_t.BNB_RGBA;

            if (onCameraImage != null) {
                onCameraImage(cameraImage);
            }
        }

        private void UpdateTexture()
        {
            data = new Color32[webCamTexture.width * webCamTexture.height];
            dataPinner = new Color32Pinner(data);
            if (cameraTexture != null) {
                UnityEngine.Object.Destroy(cameraTexture);
            }
            cameraTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            if (onCameraTexture != null) {
                onCameraTexture(cameraTexture, cameraTextureData);
            }
        }

        private BanubaSDKBridge.bnb_image_orientation_t angleToOrientation(int angle)
        {
            switch (angle) {
                // swap for 180 and 0 due to unity Texture2D.GetPixels32 return vertically flipped image
                case 0:
                    return BanubaSDKBridge.bnb_image_orientation_t.BNB_DEG_180;
                case 90:
                    return BanubaSDKBridge.bnb_image_orientation_t.BNB_DEG_90;
                case 180:
                    return BanubaSDKBridge.bnb_image_orientation_t.BNB_DEG_0;
                case 270:
                    return BanubaSDKBridge.bnb_image_orientation_t.BNB_DEG_270;
                default:
                    return BanubaSDKBridge.bnb_image_orientation_t.BNB_DEG_0;
            }
        }
    }

}