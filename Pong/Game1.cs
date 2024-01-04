using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Zombie2;

namespace Pong
{
    public class Game1 : Game
    {
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }
        
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _backgroundTexture;

        private float currentTime = 0f;
        private int counter = 0;
        private float countDuration = 1f;
        private int levelNumber = 1;

        Global Global;

        private float GetAngleFromDirection(Direction direction) 
        {
            switch (direction)
            {
                case Direction.Right:
                    return 0f;

                case Direction.Left:
                    return (float) Math.PI;

                case Direction.Down:
                    return (float)Math.PI / 2;

                case Direction.Up:
                    return (float)Math.PI * 3 / 2;

                case Direction.UpLeft:
                    return (float)Math.PI * 5 / 4;

                case Direction.UpRight:
                    return (float)Math.PI * 7 / 4;

                case Direction.DownLeft:
                    return (float)Math.PI * 3 / 4;
                
                case Direction.DownRight:
                    return (float)Math.PI / 4;
            }
            return 0f;
        }
        private Texture2D CreateRect(int width, int height, Color color)
        {
            Texture2D rect = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];
            for (int i = 0; i < data.Length; i++) { data[i] = color; }
            rect.SetData(data);
            return rect;
        }
        private void SpawnWave(int number)
        {
            for (int i = 0; i <= number/2; i++) { Global.SpawnEnemy(0); }
            if (levelNumber % 2 == 0) { 
                for (int i = 1; i <= number / 6; i++) { Global.SpawnEnemy(1); }
            }
            if (levelNumber % 5 == 0) { 
                for (int i = 0; i <= number / 20; i++) { Global.SpawnEnemy(2); }
            }
        }
        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1080;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            Global = new Global(_graphics);
            // TEMP
            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            // Load Textures
            Global.player.texture = Content.Load<Texture2D>("player");
            Global.bigEnemyTexture = CreateRect(50, 50, Color.Orange);
            Global.reallyBigEnemyTexture = CreateRect(75, 75, Color.Yellow);
            Global.normalEnemyTexture = CreateRect(25, 25, Color.Red);
            Global.chestTexture = CreateRect(10, 10, Color.Blue);
            Global.heartTexture = CreateRect(10, 10, Color.DarkRed);
            Global.bulletTexture = CreateRect(5, 5, Color.White);

            _backgroundTexture = Content.Load<Texture2D>("background1");
            _font = Content.Load<SpriteFont>("Arial");
            //Global.SpawnEnemy(2, new Vector2(400,500));
            //Global.SpawnEnemy(2, new Vector2(400, 300));
        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update Timer
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (currentTime >= countDuration)
            {
                currentTime -= countDuration;
                // Depending on the Time...
                if (counter % 10 == 0) { SpawnWave(levelNumber++); }
                if (counter % 50 == 0) { Global.SpawnDropSprite(1); }
                counter++;
            }
            
            // Event Readers
            var kstate = Keyboard.GetState();
            Direction moving = Direction.None; 
            Direction shooting = Direction.None;
            if (kstate.IsKeyDown(Keys.A)) moving = Direction.Left;
            if (kstate.IsKeyDown(Keys.D)) moving = Direction.Right;
            if (kstate.IsKeyDown(Keys.W))
            {
                if (kstate.IsKeyDown(Keys.A)) moving = Direction.UpLeft;
                else
                if (kstate.IsKeyDown(Keys.D)) moving = Direction.UpRight;
                else moving = Direction.Up;
            }
            if (kstate.IsKeyDown(Keys.S))
            {
                if (kstate.IsKeyDown(Keys.A)) moving = Direction.DownLeft;
                else
                if (kstate.IsKeyDown(Keys.D)) moving = Direction.DownRight;
                else moving = Direction.Down;
            }
            if (kstate.IsKeyDown(Keys.Left)) shooting = Direction.Left;
            if (kstate.IsKeyDown(Keys.Right)) shooting = Direction.Right;
            if (kstate.IsKeyDown(Keys.Up))
            {
                if (kstate.IsKeyDown(Keys.Left)) shooting = Direction.UpLeft;
                else 
                if (kstate.IsKeyDown(Keys.Right)) shooting = Direction.UpRight;
                else shooting = Direction.Up;
            }
            if (kstate.IsKeyDown(Keys.Down))
            {
                if (kstate.IsKeyDown(Keys.Left)) shooting  = Direction.DownLeft;
                else 
                if (kstate.IsKeyDown(Keys.Right)) shooting = Direction.DownRight;
                else shooting = Direction.Down;
            }

            Global.UpdateAll(moving, shooting, gameTime, counter);
            // DON'T DELETE
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            _spriteBatch.Begin();
            // Background
            _spriteBatch.Draw(
                _backgroundTexture,
                new Vector2(0,0),
                null,
                Color.White,
                0,
                new Vector2(Global.player.texture.Width / 2, Global.player.texture.Height / 2),
                new Vector2(0.35f, 0.35f),
                SpriteEffects.None,
                0f
                );

            // Timer
            _spriteBatch.DrawString(_font, counter.ToString(), 
                new Vector2(_graphics.PreferredBackBufferWidth / 2 - _font.MeasureString(counter.ToString()).Length() / 2, 10), Color.White);
            
            // Enemy Kill Count
            _spriteBatch.DrawString(_font, Global.enemyKills.ToString(),
                new Vector2(20, 10), Color.White);
            
            // Health Count
            _spriteBatch.DrawString(_font, Global.player.health.ToString(),
                new Vector2(_graphics.PreferredBackBufferWidth - 40 , 10), Color.DarkRed);
            
            // Player Sprite
            _spriteBatch.Draw(
                Global.player.texture, 
                Global.player.position, 
                null, 
                Color.White, 
                GetAngleFromDirection(Global.player.direction), 
                new Vector2(Global.player.texture.Width / 2, Global.player.texture.Height / 2), 
                new Vector2(0.15f, 0.15f),
                SpriteEffects.None, 
                0f);
            
            // Bullets
            foreach(Character bullet in Global.Bullets())
            {
                _spriteBatch.Draw(
                    bullet.texture,
                    bullet.position,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    new Vector2(1f, 1f),
                    SpriteEffects.None,
                    0f);
            }
            
            // Enemies
            foreach(Character enemy in Global.Enemies())
            {
                // Enemy Texture
                _spriteBatch.Draw(
                    enemy.texture,
                    enemy.position,
                    null,
                    Color.Orange,
                    enemy.angle,
                    new Vector2(enemy.texture.Width/2, enemy.texture.Height/2),
                    new Vector2(1f, 1f),
                    SpriteEffects.None,
                    0f);
                // Enemy Health
                _spriteBatch.DrawString(_font, enemy.health.ToString(),
                    new Vector2(enemy.position.X , enemy.position.Y), 
                    Color.DarkRed,
                    enemy.angle, 
                    new Vector2(enemy.texture.Width / 6, enemy.texture.Height / 2 / (enemy.texture.Height / 25)),
                    new Vector2(1f, 1f),
                    SpriteEffects.None,
                    0f);
            }// PowerUps
            foreach(Character powerUp in Global.Drops())
            {
                _spriteBatch.Draw(
                powerUp.texture,
                powerUp.position,
                null,
                Color.White,
                0,
                Vector2.Zero,
                new Vector2(1f, 1f),
                SpriteEffects.None,
                0f);   
            }

            int tmp = 0;
            foreach(PowerUp p in Global.playerPowerUps)
            {
                String s = p.name + " - " + (p.duration + p.startTime - counter).ToString();
                _spriteBatch.DrawString(_font, s,
                new Vector2(20, 50 + 20*tmp++), Color.White,
                0f,
                Vector2.Zero,
                new Vector2(0.75f, 0.75f),
                SpriteEffects.None,
                0f);
            }

            // DONT DELETE
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
