using BepInEx;
using Cinemachine;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Technie.PhysicsCreator;
using UnityEngine;
using UnityEngine.XR;
using Utilla;
using Valve.VR;

namespace GorillaFPV
{
    /// <summary>
    /// This is your mod's main class.
    /// </summary>

    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool IsSteamVR;
        bool inRoom;
        bool gameInitialized;
        Rigidbody droneRB;
        Vector2 rightJoystick;
        Vector2 leftJoystick;
        GameObject droneCamera;

        Quaternion cameraOffset = Quaternion.Euler(-50, 0, 0);

        float droneGravity = 0.0000002f;
        float idkwhattonamethis = 50;

        bool rightPrimary;
        bool prevRightPrimary;

        bool rightSecondary;
        bool prevRightSecondary;

        bool isDroneCamOn;

        bool prevLeftPrimary;
        bool leftPrimary;

        bool prevLeftSecondary;
        bool leftSecondary;

        float dronespeed;

        GameObject shoulderCamera;

        GameObject drone;

        public AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            return bundle;
        }
        void ChangeCamera()
        {
            if (IsSteamVR)
            {
                if (rightPrimary && !prevRightPrimary)
                {
                    if (!isDroneCamOn)
                    {
                        droneCamera.SetActive(true);
                        shoulderCamera.gameObject.SetActive(false);
                        isDroneCamOn = true;
                        Debug.Log($"GorillaFPV: Switched camera to Drone [SteamVR]");
                    }
                    else if (isDroneCamOn) // returns vr view from drone to player
                    {
                        droneCamera.SetActive(false);
                        shoulderCamera.gameObject.SetActive(true);
                        isDroneCamOn = false;
                        Debug.Log($"GorillaFPV: Switched camera to Player [SteamVR]");
                    }
                }
            }
            else
            {
                if (rightSecondary && !prevRightSecondary)
                {
                    if (!isDroneCamOn) // moves the vr view from player to drone
                    {
                        droneCamera.SetActive(true);
                        shoulderCamera.gameObject.SetActive(false);
                        isDroneCamOn = true;
                        Debug.Log($"GorillaFPV: Switched camera to Drone [Oculus]");
                    }
                    else if (isDroneCamOn) // returns vr view from drone to player
                    {
                        droneCamera.SetActive(false);
                        shoulderCamera.gameObject.SetActive(true);
                        isDroneCamOn = false;
                        Debug.Log($"GorillaFPV: Switched camera to Player [Oculus]");
                    }
                }
            }
        }
        void GetInputs()
        {
            if (IsSteamVR) { leftJoystick = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.axis; }
            else { ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftJoystick); }

            prevRightPrimary = rightPrimary;
            rightPrimary = ControllerInputPoller.instance.rightControllerPrimaryButton;

            rightJoystick = ControllerInputPoller.instance.rightControllerPrimary2DAxis;

            prevRightSecondary = rightSecondary;
            rightSecondary = ControllerInputPoller.instance.rightControllerSecondaryButton;

            prevLeftPrimary = leftPrimary;
            leftPrimary = ControllerInputPoller.instance.leftControllerPrimaryButton;

            prevLeftSecondary = leftSecondary;
            leftSecondary = ControllerInputPoller.instance.leftControllerSecondaryButton;
        }
        void ResetDrone()
        {
            if (IsSteamVR) // for some reason controlls are flipped on steamvr sooooo...
            {
                if (rightSecondary && !prevRightSecondary)
                {
                    drone.transform.position = GorillaLocomotion.Player.Instance.rightHandFollower.position;
                    drone.transform.rotation = Quaternion.identity;
                    droneRB.velocity = Vector3.zero;
                    droneRB.angularVelocity = Vector3.zero;
                    Debug.Log("RESET POSITION - DRONE");
                }
                if (leftSecondary && !prevLeftSecondary)
                {
                    drone.transform.rotation = Quaternion.Euler(drone.transform.rotation.x, 0, drone.transform.rotation.y);
                    droneRB.angularVelocity = Vector3.zero;
                    Debug.Log("Flipped Drone");
                }
            }
            else
            {
                if (rightPrimary && !prevRightPrimary)
                {
                    drone.transform.position = GorillaLocomotion.Player.Instance.rightHandFollower.position;
                    drone.transform.rotation = Quaternion.identity;
                    droneRB.velocity = Vector3.zero;
                    droneRB.angularVelocity = Vector3.zero;
                    Debug.Log("RESET POSITION - DRONE");
                }
                if (leftPrimary && !prevLeftPrimary)
                {
                    drone.transform.rotation = Quaternion.Euler(drone.transform.rotation.x, 0, drone.transform.rotation.y);
                    droneRB.angularVelocity = Vector3.zero;
                    Debug.Log("Flipped Drone");
                }
            }
        }
        void Setup()
        {
            var bundle = LoadAssetBundle("GorillaFPV.Resources.droneassets");
            drone = Instantiate(bundle.LoadAsset<GameObject>("DJI FVP"));
            drone.AddComponent<BoxCollider>();
            drone.GetComponent<BoxCollider>().size = drone.transform.localScale;
            drone.transform.localScale = Vector3.one * 0.1f;
            drone.AddComponent<Rigidbody>();
            droneRB = drone.GetComponent<Rigidbody>();
            droneRB.mass = 8;
            droneRB.drag = 1;
            Debug.Log("adding children colliders");
            foreach (Transform child in drone.transform) // no i am not proud of thise code but it is what it is
            {
                if (child.name == "Empty")
                {
                    foreach (Transform grandchildren in child.transform)
                    {
                        grandchildren.gameObject.AddComponent<BoxCollider>();
                        grandchildren.gameObject.layer = 8;
                    }
                }
                child.gameObject.AddComponent<BoxCollider>();
                child.gameObject.layer = 8;
            }
            Debug.Log("finished adding colliders");
            drone.layer = 8;

            droneCamera = new GameObject();
            droneCamera.AddComponent<Camera>();
            droneCamera.transform.SetParent(drone.transform);
            droneCamera.transform.rotation = drone.transform.rotation;
            droneCamera.transform.localRotation = cameraOffset;
            droneCamera.transform.position = drone.transform.position;
            droneCamera.SetActive(false);
        }


        void Start() { Utilla.Events.GameInitialized += OnGameInitialized; }

        void OnEnable()
        {
            if (inRoom)
            {
                drone.SetActive(true);
            }
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            if (gameInitialized)
            {
                if (droneCamera.activeSelf) { droneCamera.SetActive(false); }
                drone.SetActive(false);
            }
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            try
            {
                IsSteamVR = Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue().ToString().ToLower() == "steam";
                shoulderCamera = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
                Setup();
                gameInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("failed to initialize mod");
                Debug.LogError(ex);

            }
        }

        void FixedUpdate()
        {
            if (inRoom)
            {
                GetInputs();
                ChangeCamera();
                ResetDrone();
                leftJoystick.y++;

                if (leftJoystick.x < 0.15 && leftJoystick.x > -0.15) { leftJoystick.x = 0; } // makes a minimum force required
                if (leftJoystick.x > 0.15) { leftJoystick.x -= 0.15f; } // makes up for the minimum force
                else if (leftJoystick.x < -0.15) { leftJoystick.x += 0.15f; }

                try
                {
                    if (rightJoystick.y > 1.95f) { leftJoystick.y *= 2; }
                    if (leftJoystick.x > 1.95f) { leftJoystick.x *= 2; }
                    if (rightJoystick.x > 1.95f) { rightJoystick.x *= 2; }
                    droneRB.transform.Rotate(new Vector3(rightJoystick.y, leftJoystick.x, -rightJoystick.x) * 4);
                    if (leftJoystick.y > 1.95f) { leftJoystick.y *= 2.5f; }
                    droneRB.AddForce(droneRB.transform.up * (leftJoystick.y * 75));
                }
                catch
                {
                    // STOP SPAMMING ME LOGS WHEN DRONE IS A NULL DAMNIT
                }
            }
        }

        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            inRoom = true;
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            if (droneCamera.activeSelf) { droneCamera.SetActive(false); }
            drone.transform.position = Vector3.zero;
            inRoom = false;
        }
    }
}