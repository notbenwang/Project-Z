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
        enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            UpLeft,
            UpRight,
            DownLeft,
            DownRight
        }
        class Character
        {
            public Texture2D texture;
            public Vector2 position;
            public float velocity;
            public Direction direction;
            public int health;
            public float angle;
            public Character(Texture2D texture, Vector2 position, float velocity, Direction direction, int health, float angle = 0)
            {
                this.texture = texture;
                this.position = position;
                this.velocity = velocity;
                this.direction = direction;
                this.health = health;
                this.angle = angle;
            }

            public float left() { return position.X - (25 / 2); }
            public float right() { return position.X + (25 / 2); }
            public float top() { return position.Y - (25 / 2); }
            public float bottom() { return position.Y + (25 / 2); }
            public bool Touches(Character other)
            {
                // FROM GAMEBOX
                float l = other.left() - right();
                float r = left() - other.right();
                float t = other.top() - bottom();
                float b = top() - other.bottom();
                return Math.Max(Math.Max(l, r), Math.Max(t, b)) <= 0;
            }
            public float[] Overlap(Character other)
            {
                // FROM GAMEBOX
                float l = other.left() - right();
                float r = left() - other.right();
                float t = other.top() - bottom();
                float b = top() - other.bottom();
                float m = Math.Max(Math.Max(l, r), Math.Max(t, b));
                if (m >= 0) { return new float[] { 0, 0 }; }
                else if (m == l) { return new float[] { l, 0 }; }
                else if (m == r) { return new float[] { -1 * r, 0 }; }
                else if (m == t) { return new float[] { 0, t }; }
                else { return new float[] { 0, -1 * b }; }
            }
            public void moveBothToStopOverlap(Character other)
            {
                float[] o = Overlap(other);
                if (o[0] != 0 && o[1] != 0) // o != [0,0]
                {
                    position.X = o[0] / 2;
                    position.Y = o[1] / 2 + 25;
                    other.position.X = -1 * o[0] / 2 - 25;
                    other.position.Y = -1 * o[1] / 2 - 25;

                }
            }
        }
        class PowerUp
        {
            public bool active;
            public int duration;
            public int startTime;
            public PowerUp(int duration)
            {
                this.duration = duration;
            }
            public void Initiate(int startTime)
            {
                if (!active)
                {
                    this.startTime = startTime;
                    active = true;
                }
            }
            public bool Update(int currentTime)
            {
                if (active) { active = (currentTime <= duration + startTime); }
                return active;
            }
        }
        
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _backgroundTexture;
        private int _bulletCD = 35;

        private List<Character> bulletList;
        private List<Character> enemyList;
        private List<Character> powerUpTextures;
        private List<PowerUp> playerPowerUps;
        private List<Character> heartList;
        private int bulletTimer = 0;

        
        private float currentTime = 0f;
        private int counter = 0;
        private float countDuration = 1f;
        private int levelNumber = 1;

        private int enemyKills = 0;

        PowerUp rapidFire = new PowerUp(10);
        PowerUp strikeThrough = new PowerUp(25);
        PowerUp tripleShot = new PowerUp(25);
        Character player;

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
        private Direction GetLeftDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right:
                    return Direction.UpRight;

                case Direction.Left:
                    return Direction.DownLeft;

                case Direction.Down:
                    return Direction.DownRight;

                case Direction.Up:
                    return Direction.UpLeft;

                case Direction.UpLeft:
                    return Direction.Left;

                case Direction.UpRight:
                    return Direction.Up;

                case Direction.DownLeft:
                    return Direction.Down;

                case Direction.DownRight:
                    return Direction.Right;
            }
            return Direction.Up;
        }
        private Direction GetRightDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right:
                    return Direction.DownRight;

                case Direction.Left:
                    return Direction.UpLeft;

                case Direction.Down:
                    return Direction.DownLeft;

                case Direction.Up:
                    return Direction.UpRight;

                case Direction.UpLeft:
                    return Direction.Up;

                case Direction.UpRight:
                    return Direction.Right;

                case Direction.DownLeft:
                    return Direction.Left;

                case Direction.DownRight:
                    return Direction.Down;
            }
            return Direction.Up;
        }
        private void SpawnBullet(Vector2 position, Direction direction)
        {
            float bullet_speed = 10f;
            Texture2D whiteRect = new Texture2D(GraphicsDevice, 1, 1);
            whiteRect.SetData(new[] { Color.White });
            int health = (strikeThrough.active) ? 1000 : 1;
            Character bullet = new Character(whiteRect, position, bullet_speed, direction, health);
            bulletList.Add(bullet);
            
            if (tripleShot.active)
            {
                bulletList.Add(new Character(whiteRect, position, bullet_speed, GetRightDirection(direction), health));
                bulletList.Add(new Character(whiteRect, position, bullet_speed, GetLeftDirection(direction), health));
            }
        }
        private void SpawnEnemy()
        {
            Random rnd = new Random();
            int side = rnd.Next(1,5);
            int randomX = rnd.Next(_graphics.PreferredBackBufferWidth);
            int randomY = rnd.Next(_graphics.PreferredBackBufferHeight);
            Texture2D redRect = new Texture2D(GraphicsDevice, 1, 1);
            redRect.SetData(new[] { Color.Red });
            Vector2 position = new Vector2(0,0);
            float enemy_speed = 1.7f;
            switch (side)
            {
                case 1:
                    position.X = -100;
                    position.Y = randomY;
                    break;
                case 2:
                    position.X = randomX;
                    position.Y = -100;
                    break;
                case 3:
                    position.X = _graphics.PreferredBackBufferWidth + 100;
                    position.Y = randomY;
                    break;
                case 4:
                    position.X = randomX;
                    position.Y = _graphics.PreferredBackBufferHeight + 100;
                    break;
            }
            enemyList.Add(new Character(redRect, position, enemy_speed, Direction.Up, 1));
        }

        private void SpawnPowerUpSprite()
        {
            Texture2D blueRect = new Texture2D(GraphicsDevice, 1, 1);
            blueRect.SetData(new[] { Color.Blue });
            Random rnd = new Random();
            int randomX = rnd.Next(_graphics.PreferredBackBufferWidth);
            int randomY = rnd.Next(_graphics.PreferredBackBufferHeight);
            powerUpTextures.Add(new Character(blueRect, new Vector2(randomX,randomY), 0, Direction.Up, 1)); 
        }
        private void UpdatePowerUpSprites()
        {
            foreach (Character p in powerUpTextures) 
            {
                if (p.Touches(player))
                {
                    ActivateRandomPowerUp();
                    p.health = 0;
                }
            }
            powerUpTextures.RemoveAll(p => p.health <= 0);
        }
        private void ActivateRandomPowerUp()
        {
            Random rnd = new Random();
            int value = rnd.Next(100);
            
            if (value < 33) { ActivateSpecificPowerUp(rapidFire); }
            else if (value < 66) { ActivateSpecificPowerUp(strikeThrough); }
            else if (value < 100) { ActivateSpecificPowerUp(tripleShot); }
        }
        private void SpawnHeartAtPosition(Vector2 position)
        {
            Texture2D redRect = new Texture2D(GraphicsDevice, 1, 1);
            redRect.SetData(new[] { Color.Orange });
            heartList.Add(new Character(redRect, position, 0, Direction.Up, 1));
        }
        private void UpdateHearts()
        {
            foreach(var heart in heartList)
            {
                if (heart.Touches(player)) { 
                    heart.health = 0;
                    player.health++;
                }
            }
            heartList.RemoveAll(h  => h.health <= 0);
        }
        private void ActivateSpecificPowerUp(PowerUp p)
        {
            if (p.active) { p.startTime = counter; }
            else
            {
                p.Initiate(counter);
                playerPowerUps.Add(p);
            }
        }

        private void SpawnWave(int number)
        {
            for (int i = 0; i < number; i++) { SpawnEnemy(); }
        }
        private void UpdateEnemies()
        {
            enemyList.ForEach(e => { if (e.health <= 0) enemyKills++; });
            enemyList.RemoveAll(enemy => enemy.health <= 0);
            foreach (Character enemy in enemyList)
            {
                float distY = player.position.Y - enemy.position.Y;
                float distX = player.position.X - enemy.position.X;
                float theta;
                
                if (distY > -5 && distY < 5)
                {
                    if (player.position.X < enemy.position.X) theta = (float)Math.PI * 3;
                    else theta = (float)Math.PI * 2;
                }

                else if (distX > -5 && distX < 5)
                {
                    if (player.position.Y < enemy.position.Y) theta = (float)Math.PI * 3 / 2;
                    else theta = (float)(Math.PI * 5 / 2);
                }
                else
                {
                    theta = (float)(Math.Atan(distY / distX) + (2 * Math.PI));   
                    if (player.position.X < enemy.position.X)
                    {
                        if (player.position.Y < enemy.position.Y) theta -= (float)Math.PI; 
                        else theta += (float)Math.PI;
                    }
                }
                enemy.angle = theta;
                float da = enemy.velocity * (float)Math.Cos(theta);
                float db = enemy.velocity * (float)Math.Sin(theta);
                
                enemy.position.X += da;
                enemy.position.Y += db;
                foreach (Character otherEnemy in enemyList)
                {
                    if (otherEnemy != enemy && otherEnemy.Touches(enemy)) 
                    {
                        enemy.moveBothToStopOverlap(otherEnemy); 
                    } 
                }
                if (enemy.Touches(player))
                {
                    enemy.health = 0;
                    player.health--;
                }
            }
        }
        private void UpdateBullets()
        {
            foreach (Character bullet in bulletList)
            {
                // Collision Check
                foreach (Character enemy in enemyList)
                {
                    if ((bullet.position.X < enemy.position.X + bullet.velocity + 12) &&
                        (bullet.position.X > enemy.position.X - bullet.velocity - 12) && 
                        (bullet.position.Y < enemy.position.Y + bullet.velocity + 12) &&
                        (bullet.position.Y > enemy.position.Y - bullet.velocity - 12))
                    {
                        enemy.health -= 1;
                        bullet.health -= 1;
                        if (enemy.health <= 0)
                        {
                            Random rnd = new Random();
                            if (rnd.Next(25) < 2) { SpawnHeartAtPosition(enemy.position); }
                        }
                    }
                }
                // Movement Check
                if (bullet.health > 0)
                {
                    float dx = 0, dy = 0;
                    switch (bullet.direction)
                    {
                        case Direction.Right:
                            dx = bullet.velocity;
                            break;
                        case Direction.Left:
                            dx = -1 * bullet.velocity;
                            break;
                        case Direction.Down:
                            dy = -1 * bullet.velocity;
                            break;
                        case Direction.Up:
                            dy = bullet.velocity;
                            break;
                        case Direction.UpLeft:
                            dx = -1 * bullet.velocity;
                            dy = bullet.velocity;
                            break;
                        case Direction.UpRight:
                            dx = bullet.velocity;
                            dy = bullet.velocity;
                            break;
                        case Direction.DownRight:
                            dy = -1 * bullet.velocity;
                            dx = bullet.velocity;
                            break;
                        case Direction.DownLeft:
                            dy = -1 * bullet.velocity;
                            dx = -1 * bullet.velocity;
                            break;
                    }
                    float x = bullet.position.X + dx;
                    float y = bullet.position.Y - dy;
                    bullet.position = new Vector2(x, y);
                }
            }
            bulletList.RemoveAll(bullet =>
                    (bullet.position.X > _graphics.PreferredBackBufferWidth) ||
                    (bullet.position.X < 0) ||
                    (bullet.position.Y > _graphics.PreferredBackBufferHeight) ||
                    (bullet.position.Y < 0));
            bulletList.RemoveAll(bullet => bullet.health <= 0);
        }

        private void makeLevelStage(int levelNumber)
        {
            if (levelNumber < 3)
            {
                SpawnWave(3);
            }
            else if (levelNumber < 6)
            {
                SpawnWave(6);
            }
            else { SpawnWave(10); }
        }
        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1080;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            // Init player
            Vector2 position = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);
            float speed = 200f;
            player = new Character(null, position, speed, Direction.Left, 5);

            // Init Character Lists
            bulletList = new List<Character>();
            enemyList = new List<Character>();
            playerPowerUps = new List<PowerUp>();
            powerUpTextures = new List<Character>();
            heartList = new List<Character>();

            // TEMP
            SpawnWave(10);
            

            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            // Load Textures
            player.texture = Content.Load<Texture2D>("player");
            _backgroundTexture = Content.Load<Texture2D>("background1");
            _font = Content.Load<SpriteFont>("Arial");
        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update Timer
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (currentTime >= countDuration)
            {
                counter++;
                currentTime -= countDuration;
                // Depending on the Time...
                if (counter % 10 == 0) { makeLevelStage(levelNumber++);}
                if (counter % 25 == 0) { SpawnPowerUpSprite(); }
            }

            // Event Readers
            var kstate = Keyboard.GetState();
            if (kstate.IsKeyDown(Keys.A)) player.position.X -= player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.D)) player.position.X += player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.W)) player.position.Y -= player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.S)) player.position.Y += player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.Left)) player.direction = Direction.Left;
            if (kstate.IsKeyDown(Keys.Right)) player.direction = Direction.Right;
            if (kstate.IsKeyDown(Keys.Up))
            {
                if (kstate.IsKeyDown(Keys.Left)) player.direction = Direction.UpLeft;
                else 
                if (kstate.IsKeyDown(Keys.Right)) player.direction = Direction.UpRight;
                else player.direction = Direction.Up;
            }
            if (kstate.IsKeyDown(Keys.Down))
            {
                if (kstate.IsKeyDown(Keys.Left)) player.direction = Direction.DownLeft;
                else 
                if (kstate.IsKeyDown(Keys.Right)) player.direction = Direction.DownRight;
                else player.direction = Direction.Down;
            }

            // Player Bound Managing
            if (player.position.X < 15) player.position.X = 15;
            else if (player.position.X > _graphics.PreferredBackBufferWidth - 15)
            {
                player.position.X = _graphics.PreferredBackBufferWidth - 15;
            }
            if (player.position.Y < 15) player.position.Y = 15;
            else if (player.position.Y > _graphics.PreferredBackBufferHeight -15)
            {
                player.position.Y = _graphics.PreferredBackBufferHeight - 15;
            }

            // Bullet Time Management
            if (bulletTimer == 0)
            {
                SpawnBullet(player.position, player.direction);
                bulletTimer = (rapidFire.active) ? _bulletCD / 5 : _bulletCD;
            }
            else bulletTimer--;

            // Updaters
            UpdateBullets();
            UpdateEnemies();
            UpdatePowerUpSprites();
            foreach (var powerUp in playerPowerUps) { powerUp.Update(counter);}
            playerPowerUps.RemoveAll(p => !p.active);
            UpdateHearts();
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
                new Vector2(player.texture.Width / 2, player.texture.Height / 2),
                new Vector2(0.35f, 0.35f),
                SpriteEffects.None,
                0f
                );

            // Timer
            _spriteBatch.DrawString(_font, counter.ToString(), 
                new Vector2(_graphics.PreferredBackBufferWidth / 2 - _font.MeasureString(counter.ToString()).Length() / 2, 10), Color.White);
            
            // Enemy Kill Count
            _spriteBatch.DrawString(_font, enemyKills.ToString(),
                new Vector2(20, 10), Color.White);
            
            // Health Count
            _spriteBatch.DrawString(_font, player.health.ToString(),
                new Vector2(_graphics.PreferredBackBufferWidth - 40 , 10), Color.DarkRed);
            
            // Player Sprite
            _spriteBatch.Draw(
                player.texture, 
                player.position, 
                null, 
                Color.White, 
                GetAngleFromDirection(player.direction), 
                new Vector2(player.texture.Width / 2, player.texture.Height / 2), 
                new Vector2(0.15f, 0.15f),
                SpriteEffects.None, 
                0f);
            
            // Bullets
            foreach(Character bullet in bulletList)
            {
                _spriteBatch.Draw(
                    bullet.texture,
                    bullet.position,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    new Vector2(5f, 5f),
                    SpriteEffects.None,
                    0f);
            }
            
            // Enemies
            foreach(Character enemy in enemyList)
            {
                _spriteBatch.Draw(
                    enemy.texture,
                    enemy.position,
                    null,
                    Color.Red,
                    enemy.angle,
                    Vector2.Zero,
                    new Vector2(25f, 25f),
                    SpriteEffects.None,
                    0f);
            }// PowerUps
            foreach(Character powerUp in powerUpTextures)
            {
                _spriteBatch.Draw(
                powerUp.texture,
                powerUp.position,
                null,
                Color.Blue,
                0,
                Vector2.Zero,
                new Vector2(25f, 25f),
                SpriteEffects.None,
                0f);   
            }
            foreach(Character heart in heartList)
            {
                _spriteBatch.Draw(
                heart.texture,
                heart.position,
                null,
                Color.DarkRed,
                0,
                Vector2.Zero,
                new Vector2(10f, 10f),
                SpriteEffects.None,
                0f);
            }

            // DONT DELETE
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
