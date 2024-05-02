using LIV.SDK.Unity;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OhshapeULIV {
    public class OhshapeULIVMod : MelonMod {

        public static Action OnPlayerReady;

        bool hasAutoVis = false;
        private GameObject livObject;
        private Camera spawnedCamera;
        private static LIV.SDK.Unity.LIV livInstance;

        public override void OnInitializeMelon() {
            base.OnInitializeMelon();

            OnPlayerReady += TrySetupLiv;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1) {
            hasAutoVis = false;
            TrySetupLiv();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (!hasAutoVis) {
                FixVisibility();
            }

            if (Input.GetKeyDown(KeyCode.F3)) {
                TrySetupLiv();
            }

            UpdateFollowSpawnedCamera();
        }

        public void TrySetupLiv() {
            /*
             * Ohshape
             * 2022.3.4f1
             *
             * Camera names:
			 *   Camera
			*/

            Camera[] arrCam = GameObject.FindObjectsOfType<Camera>().ToArray();
            //MelonLogger.Msg(">>> Camera count: " + arrCam.Length);
            foreach (Camera cam in arrCam) {
                if (cam.name.Contains("LIV ")) {
                    continue;
                } else if (cam.name.Contains("Camera")) {
                    SetUpLiv(cam);
                    break;
                } // else MelonLogger.Msg(cam.name);
            }
        }

        private void UpdateFollowSpawnedCamera() {
            var livRender = GetLivRender();
            if (livRender == null || spawnedCamera == null) return;

            // When spawned objects get removed in Boneworks, they might not be destroyed and just be disabled.
            if (!spawnedCamera.gameObject.activeInHierarchy) {
                spawnedCamera = null;
                return;
            }

            var cameraTransform = spawnedCamera.transform;
            livRender.SetPose(cameraTransform.position, cameraTransform.rotation, spawnedCamera.fieldOfView);
        }

        private static Camera GetLivCamera() {
            try {
                return !livInstance ? null : livInstance.HMDCamera;
            } catch (Exception) {
                livInstance = null;
            }
            return null;
        }

        private static SDKRender GetLivRender() {
            try {
                return !livInstance ? null : livInstance.render;
            } catch (Exception) {
                livInstance = null;
            }
            return null;
        }

        private void SetUpLiv(Camera camera) {
            if (!camera) {
                MelonLogger.Msg("No camera provided, aborting LIV setup.");
                return;
            }

            var livCamera = GetLivCamera();
            if (livCamera == camera) {
                MelonLogger.Msg("LIV already set up with this camera, aborting LIV setup.");
                return;
            }

            MelonLogger.Msg($"Setting up LIV with camera: {camera.name}...");
            if (livObject) {
                Object.Destroy(livObject);
            }

            //var cameraParent = camera.transform.parent;
            var cameraParent = GameObject.Find("Player").transform;
            var cameraPrefab = new GameObject("LivCameraPrefab");
            cameraPrefab.SetActive(false);
            var cameraFromPrefab = cameraPrefab.AddComponent<Camera>();
            cameraFromPrefab.allowHDR = false;
            cameraPrefab.transform.SetParent(cameraParent, false);

            livObject = new GameObject("mLIV");
            livObject.SetActive(false);

            livInstance = livObject.AddComponent<LIV.SDK.Unity.LIV>();
            livInstance.HMDCamera = camera;
            livInstance.MRCameraPrefab = cameraFromPrefab;
            livInstance.stage = cameraParent;
            livInstance.fixPostEffectsAlpha = false;
            livInstance.spectatorLayerMask = ~0;
            livInstance.spectatorLayerMask &= ~(1 << (int)GameLayer.IgnoreLIV);
            livInstance.spectatorLayerMask &= ~(1 << (int)GameLayer.Avatar);
            livInstance.spectatorLayerMask &= ~(1 << (int)GameLayer.Sensors); //Hands
            livInstance.spectatorLayerMask &= ~(1 << (int)GameLayer.IgnoreLayer);
            livObject.SetActive(true);

            hasAutoVis = false;
        }

        private void FixVisibility() {
            if (livInstance == null || livInstance.stage == null)
                return;

            //MelonLogger.Msg("# FixVisibility");

            GameObject fade = GameObject.Find("FadeScreen");
            if (fade == null)
                return;

            if (fade.layer == (int)GameLayer.Default) {
                fade.layer = (int)GameLayer.IgnoreLIV;
            }
            hasAutoVis = true;
        }
    }
}
