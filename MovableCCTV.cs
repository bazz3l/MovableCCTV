using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Movable CCTV", "Bazz3l", "1.0.9")]
    [Description("Allows player to control placed cameras.")]
    public class MovableCCTV : RustPlugin
    {
        #region Fields
        
        private const string MoveSound = "assets/prefabs/deployable/playerioents/detectors/hbhfsensor/effects/detect_up.prefab";
        private const string PermUse = "movablecctv.use";
        private const string PanelName = "cctv_panel";
        
        private CuiElementContainer _uiElements;
        private CuiTextComponent _textLabel;
        private PluginConfig _config;

        #endregion

        #region Config
        
        protected override void LoadDefaultConfig() => _config = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                LoadDefaultConfig();

                SaveConfig();
                
                PrintError("Loaded default configuration.");
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                RotateSound = true,
                RotateSpeed = 0.2f
            };
        }

        private class PluginConfig
        {
            public float RotateSpeed;
            public bool RotateSound;
        }
        
        #endregion

        #region Local
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> { {"mounted", "Control cameras using W A S D."} }, this);
        }
        
        #endregion

        #region Oxide
        
        private void OnServerInitialized()
        {
            foreach (CCTV_RC cctv in BaseNetworkable.serverEntities.OfType<CCTV_RC>())
            {
                if (cctv.IsStatic())
                {
                    continue;
                }

                cctv.hasPTZ = true;
            }

            CreateUI();
        }

        private void Init() => permission.RegisterPermission(PermUse, this);

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }

        private void OnEntitySpawned(CCTV_RC cctv)
        {
            if (cctv.IsStatic())
            {
                return;
            }

            cctv.hasPTZ = true;
        }

        private void OnEntityMounted(ComputerStation station, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                return;
            }

            UpdateUI(player);
        }

        private void OnEntityDismounted(ComputerStation station, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                return;
            }

            DestroyUI(player);
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
            {
                return;
            }
            
            if (!(input.IsDown(BUTTON.FORWARD) 
                  || input.IsDown(BUTTON.BACKWARD) 
                  || input.IsDown(BUTTON.LEFT) 
                  || input.IsDown(BUTTON.RIGHT)))
            {
                return;
            }
            
            ComputerStation computerStation = player.GetMounted()?.GetComponentInParent<ComputerStation>();

            if (computerStation == null)
            {
                return;
            }

            CCTV_RC cctvRc = computerStation.currentlyControllingEnt.Get(true).GetComponent<CCTV_RC>();
                
            if (cctvRc == null || cctvRc.IsStatic())
            {
                return;
            }

            float y = input.IsDown(BUTTON.FORWARD) ? 1f : (input.IsDown(BUTTON.BACKWARD) ? -1f : 0f);
            float x = input.IsDown(BUTTON.LEFT) ? -1f : (input.IsDown(BUTTON.RIGHT) ? 1f : 0f);

            InputState inputState = new InputState();
            inputState.current.mouseDelta.y = y * _config.RotateSpeed;
            inputState.current.mouseDelta.x = x * _config.RotateSpeed;
            cctvRc.UserInput(inputState, player);

            if (!_config.RotateSound)
            {
                return;
            }
            
            EffectNetwork.Send(new Effect(MoveSound, cctvRc.transform.position, Vector3.zero));
        }
        
        #endregion

        #region UI
        
        private void CreateUI()
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
            }, "Overlay", PanelName);

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

        private void UpdateUI(BasePlayer player)
        {
            _textLabel.Text = Lang("mounted", player.UserIDString);
            
            CuiHelper.AddUi(player, _uiElements);
        }

        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, PanelName);

        #endregion

        #region Helpers
        
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        
        #endregion
    }
}