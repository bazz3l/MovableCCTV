using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Movable CCTV", "Bazz3l", "1.0.3")]
    [Description("Movable cctv cameras using WASD")]
    class MovableCCTV : CovalencePlugin
    {
        #region Fields
        readonly float RotateSpeed = 0.2f;
        public static MovableCCTV plugin;
        #endregion

        #region Config
        PluginConfig config;

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                RotateSpeed = 0.2f
            };
        }

        class PluginConfig
        {
            public float RotateSpeed;
        }
        #endregion

        #region Oxide
        void OnServerInitialized()
        {
            foreach (BaseEntity entity in BaseNetworkable.serverEntities)
            {
                CCTV_RC cctv = entity.GetComponent<CCTV_RC>();

                if (cctv != null && !cctv.IsStatic())
                {
                    cctv.hasPTZ = true;
                }
            }
        }

        void Init()
        {
            plugin = this;
            config = Config.ReadObject<PluginConfig>();
        }

        void Unload()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<CameraMover>().ToList()) GameObject.Destroy(obj);
        }

        void OnEntitySpawned(CCTV_RC cctv)
        {
            if (!cctv.IsStatic())
            {
                cctv.hasPTZ = true;
            }
        }

        void OnEntityMounted(ComputerStation station, BasePlayer player)
        {
            if (player.GetComponent<CameraMover>() == null)
            {
                player.gameObject.AddComponent<CameraMover>();
            }
        }

        void OnEntityDismounted(ComputerStation station, BasePlayer player)
        {
            var cameraMover = player.GetComponent<CameraMover>();
            if (cameraMover == null) return;
            cameraMover.Destroy();
        }

        public class CameraMover : MonoBehaviour
        {
            public BasePlayer player { get; set; }
            public ComputerStation station { get; set; }

            public void Awake()
            {
                player = GetComponent<BasePlayer>();
                station = player.GetMounted() as ComputerStation;
            }

            private void FixedUpdate()
            {
                if (player == null || player.IsSleeping() || !player.IsConnected)
                {
                    Destroy();
                    return;
                }

                if (station == null || !station.currentlyControllingEnt.IsValid(true) || station.currentlyControllingEnt.Get(true).GetComponent<CCTV_RC>().IsStatic())
                {
                    return;
                }

                float y = player.serverInput.IsDown(BUTTON.FORWARD) ? 1f : (player.serverInput.IsDown(BUTTON.BACKWARD) ? -1f : 0f);
                float x = player.serverInput.IsDown(BUTTON.LEFT) ? -1f : (player.serverInput.IsDown(BUTTON.RIGHT) ? 1f : 0f);

                InputState inputState = new InputState();
                inputState.current.mouseDelta.y = y * plugin.config.RotateSpeed;
                inputState.current.mouseDelta.x = x * plugin.config.RotateSpeed;

                station.currentlyControllingEnt.Get(true).GetComponent<CCTV_RC>().UserInput(inputState, player);
            }

            public void Destroy()
            {
                Destroy(this);
            }
        }
        #endregion
    }
}