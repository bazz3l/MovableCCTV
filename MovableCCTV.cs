using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Movable CCTV", "Bazz3l", "1.0.8")]
    [Description("Allows player to control placed cameras.")]
    class MovableCCTV : RustPlugin
    {
        #region Fields
        const string _moveSound = "assets/prefabs/deployable/playerioents/detectors/hbhfsensor/effects/detect_up.prefab";
        const string _permUse = "movablecctv.use";
        const string _panelName = "cctv_panel";

        public static MovableCCTV Instance;

        CuiElementContainer _uiElements;
        CuiTextComponent _textLabel;
        #endregion

        #region Config
        PluginConfig _config;

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                RotateSound = true,
                RotateSpeed = 0.2f
            };
        }

        class PluginConfig
        {
            public float RotateSound;
            public float RotateSpeed;
        }
        #endregion

        #region Local
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                {"mounted", "Control cameras using W A S D."}
            }, this);
        }
        #endregion

        #region Oxide
        void OnServerInitialized()
        {
            permission.RegisterPermission(_permUse, this);

            MakeMovebles();
            MakeUI();
        }

        void Init()
        {
            Instance = this;

            _config = Config.ReadObject<PluginConfig>();
        }

        void Unload()
        {
            foreach (CameraMover obj in UnityEngine.Object.FindObjectsOfType<CameraMover>())
            {
                GameObject.Destroy(obj);
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, _panelName);
            }
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
            if (!permission.UserHasPermission(player.UserIDString, _permUse))
            {
                return;
            }

            if (player.GetComponent<CameraMover>() == null)
            {
                player.gameObject.AddComponent<CameraMover>();
            }

            CuiHelper.AddUi(player, PlayerUI(player));
        }

        void OnEntityDismounted(ComputerStation station, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, _permUse))
            {
                return;
            }

            CameraMover cameraMover = player.GetComponent<CameraMover>();
            if (cameraMover == null)
            {
                return;
            }

            cameraMover.Destroy();

            CuiHelper.DestroyUi(player, _panelName);
        }
        #endregion

        #region Core
        void MakeMovebles()
        {
            foreach(BaseEntity entity in BaseNetworkable.serverEntities)
            {
                CCTV_RC cctv = entity.GetComponent<CCTV_RC>();
                if (cctv != null && !cctv.IsStatic())
                {
                    cctv.hasPTZ = true;
                }
            }
        }

        class CameraMover : MonoBehaviour
        {
            ComputerStation _station;
            BasePlayer _player;

            void Awake()
            {
                _player = GetComponent<BasePlayer>();
                if (_player == null)
                {
                    Destroy();
                    return;
                }

                _station = _player.GetMounted() as ComputerStation;
            }

            void FixedUpdate()
            {
                if (!(_player.serverInput.IsDown(BUTTON.FORWARD) 
                || _player.serverInput.IsDown(BUTTON.BACKWARD) 
                || _player.serverInput.IsDown(BUTTON.LEFT) 
                || _player.serverInput.IsDown(BUTTON.RIGHT)))
                {
                    return;
                }

                if (_station == null || !_station.currentlyControllingEnt.IsValid(true))
                {
                    return;
                }

                CCTV_RC cctv = _station.currentlyControllingEnt.Get(true).GetComponent<CCTV_RC>();
                if (cctv == null || cctv.IsStatic())
                {
                    return;
                }

                float y = _player.serverInput.IsDown(BUTTON.FORWARD) ? 1f : (_player.serverInput.IsDown(BUTTON.BACKWARD) ? -1f : 0f);
                float x = _player.serverInput.IsDown(BUTTON.LEFT) ? -1f : (_player.serverInput.IsDown(BUTTON.RIGHT) ? 1f : 0f);

                InputState inputState = new InputState();
                inputState.current.mouseDelta.y = y * Instance._config.RotateSpeed;
                inputState.current.mouseDelta.x = x * Instance._config.RotateSpeed;

                cctv.UserInput(inputState, _player);

                if (Instance._config.RotateSound)
                {
                    EffectNetwork.Send(new Effect(_moveSound, cctv.transform.position, Vector3.zero));
                }
            }

            public void Destroy() => Destroy(this);
        }
        #endregion

        #region UI
        void MakeUI()
        {
            _uiElements = new CuiElementContainer();

            string panel = _uiElements.Add(new CuiPanel {
                CursorEnabled = true,
                Image = {
                    Color = "0 0 0 0"
                },
                RectTransform = {
                    AnchorMin = "0.293 0.903",
                    AnchorMax = "0.684 0.951"
                }
            }, "Overlay", _panelName);

            CuiLabel label = new CuiLabel
            {
                Text = {
                    Text  = "",
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.5",
                    FontSize = 14
                },
                RectTransform = {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            };

            _textLabel = label.Text;

            _uiElements.Add(label, panel);
        }

        CuiElementContainer PlayerUI(BasePlayer player)
        {
            _textLabel.Text = Lang("mounted", player.UserIDString);

            return _uiElements;
        }
        #endregion

        #region Helpers
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}