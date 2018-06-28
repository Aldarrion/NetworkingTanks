using System.Collections.Generic;
using Client.Entities;
using Client.Extensions;
using Client.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Protobufs.NetworkTanks.Game;

namespace Client
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class TanksGame : Game
    {
        public GraphicsDeviceManager GraphicsManager { get; }
        private SpriteBatch _spriteBatch;

        private LocalPlayer _localPlayer;
        private readonly Dictionary<int, RemotePlayer> _remotePlayers = new Dictionary<int, RemotePlayer>();

        private KeyboardState _prevState;
        private KeyboardState _currentState;

        private NetworkManager _networkManager;

        public bool IsKeyDownNew(Keys key)
        {
            return _prevState.IsKeyUp(key) && _currentState.IsKeyDown(key);
        }

        public TanksGame()
        {
            IsMouseVisible = true;
            GraphicsManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            _prevState = Keyboard.GetState();
            _currentState = Keyboard.GetState();

            _networkManager = new NetworkManager();
            _networkManager.OnNewPlayerConnected += NewPlayerConnected;
            _networkManager.OnPlayerDisconnected += PlayerDisconnected;
            _networkManager.Connect();

            _localPlayer = new LocalPlayer(this, _networkManager.LocalPlayerId);
            _localPlayer.MoveTo(_networkManager.LocalPlayerPosition);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _localPlayer.LoadContent(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            _prevState = _currentState;
            _currentState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                _networkManager.Destroy();
                Exit();
            }

            _localPlayer.Update(gameTime);

            lock (_remotePlayers)
            {
                foreach (RemotePlayer remotePlayer in _remotePlayers.Values)
                {
                    remotePlayer.Update(gameTime);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();

            _localPlayer.Draw(_spriteBatch);

            foreach (RemotePlayer remotePlayer in _remotePlayers.Values)
            {
                remotePlayer.Draw(_spriteBatch);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void NewPlayerConnected(PlayerInfo newPlayerInfo)
        {
            var newPlayer = new RemotePlayer(this, newPlayerInfo.Id);
            newPlayer.LoadContent(GraphicsDevice);
            newPlayer.MoveTo(newPlayerInfo.Position.ToVector());

            lock (_remotePlayers)
            {
                _remotePlayers.Add(newPlayer.PlayerId, newPlayer);
            }
        }

        private void PlayerDisconnected(int id)
        {
            lock (_remotePlayers)
            {
                _remotePlayers.Remove(id);
            }
        }
    }
}
