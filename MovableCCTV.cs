using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Movable CCTV", "Bazz3l", "1.0.1")]
    [Description("Movable cctv cameras using WASD")]
    class MovableCCTV : CovalencePlugin
    {
        #region Fields
        readonly float RotateSpeed = 0.2f;
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
            config = Config.ReadObject<PluginConfig>();
        }

        void OnEntitySpawned(CCTV_RC cctv)
        {
            if (!cctv.IsStatic())
            {
                cctv.hasPTZ = true;
            }
        }

        void OnPlayerInput(BasePlayer player, InputState inputState)
        {
            ComputerStation station = player.GetMounted() as ComputerStation;
            if (station == null || !station.currentlyControllingEnt.IsValid(true) || station.currentlyControllingEnt.Get(true).GetComponent<CCTV_RC>().IsStatic())
            {
                return;
            }

            float y = inputState.IsDown(BUTTON.FORWARD) ? 1f : (inputState.IsDown(BUTTON.BACKWARD) ? -1f : 0f);
            float x = inputState.IsDown(BUTTON.LEFT) ? -1f : (inputState.IsDown(BUTTON.RIGHT) ? 1f : 0f);

            inputState.current.mouseDelta.y = y * config.RotateSpeed;
            inputState.current.mouseDelta.x = x * config.RotateSpeed;
        }
        #endregion
    }
}