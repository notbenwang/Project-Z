using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Zombie2
{
    internal class Global
    {
        private int _bulletCD = 35;

        private List<Character> enemySprites;
        private List<Character> bulletSprites;
        private List<Character> dropSprites;
        public List<PowerUp> playerPowerUps;
        public Character player;

        private int bulletTimer = 0;
        public int enemyKills = 0;

        public PowerUp rapidFire = new PowerUp(30, "Rapidfire");
        public PowerUp piercingBullets = new PowerUp(30, "Harpoon Bullets");
        public PowerUp tripleShot = new PowerUp(30, "Triple Shot");
        public PowerUp collateralShot = new PowerUp(30, "Exploding Bullets");

        private int canvasWidth;
        private int canvasHeight;

        public Texture2D bulletTexture;
        public Texture2D normalEnemyTexture;
        public Texture2D bigEnemyTexture;
        public Texture2D reallyBigEnemyTexture;
        public Texture2D chestTexture;
        public Texture2D heartTexture;

        public Global(GraphicsDeviceManager _graphics)
        {
            enemySprites = new List<Character>();
            bulletSprites = new List<Character>();
            dropSprites = new List<Character>();
            playerPowerUps = new List<PowerUp>();

            float player_speed = 200f;
            canvasWidth = _graphics.PreferredBackBufferWidth;
            canvasHeight = _graphics.PreferredBackBufferHeight;
            player = new Character(
                null, 
                new Vector2(canvasWidth/ 2, 
                canvasHeight/ 2), 
                player_speed, 
                Direction.Left, 
                5);
        }
        public List<Character> Enemies() { return enemySprites; }
        public List<Character> Bullets() {  return bulletSprites; }
        public List<Character> Drops() {  return dropSprites; }
        public void UpdateAll(Direction playerMoving, Direction playerShooting, GameTime gameTime, int currentTime)
        {
            UpdatePlayer(playerMoving, playerShooting, gameTime, currentTime);
            UpdateBullets();
            UpdateEnemies();
            UpdateDrops(currentTime);
        }

        public void SpawnEnemy(int? enemyType = null, Vector2? p = null)
        {
            int type = enemyType ?? 0;
            var position = p ?? GetRandomOutsidePosition();
            
            Texture2D texture; float enemy_speed; int health;
            switch (type)
            {
                case 1: 
                    texture = bigEnemyTexture;
                    enemy_speed = 1f;
                    health = 5;
                    break;
                case 2:
                    texture = reallyBigEnemyTexture;
                    enemy_speed = 0.3f;
                    health = 20;
                    break;
                default:
                    texture = normalEnemyTexture;
                    enemy_speed = 1.7f;
                    health = 1;
                    break;
            }
            enemySprites.Add(new Character(texture, position, enemy_speed, Direction.Up, health));
        }

        public void SpawnDropSprite(int? type = null, Vector2? p = null)
        {
            var position = p ?? GetRandomInsidePosition();
            Texture2D texture;
            if (type == 0) texture = heartTexture;
            else texture = chestTexture;
            dropSprites.Add(new Character(texture, position, 0, Direction.Up, 1));
        }
        public void SpawnBulletSprite(Direction direction, Vector2? p = null)
        {
            var position = p ?? player.position;
            float bullet_speed = 10f;
            int health = (piercingBullets.active) ? 25 : 1;
            Character bullet = new Character(bulletTexture, position, bullet_speed, direction, health);
            bulletSprites.Add(bullet);

            if (tripleShot.active)
            {
                bulletSprites.Add(new Character(bulletTexture, position, bullet_speed, GetRightDirection(direction), health));
                bulletSprites.Add(new Character(bulletTexture, position, bullet_speed, GetLeftDirection(direction), health));
            }
        }  
        private void UpdatePlayer(Direction moving, Direction shooting, GameTime gameTime, int counter)
        {
            switch (moving)
            {
                case Direction.Left:
                    player.position.X -= player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.Right:
                    player.position.X += player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.Up:
                    player.position.Y -= player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.Down:
                    player.position.Y += player.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.UpLeft:
                    player.position.X -= (float)Math.Sqrt(Math.Pow(player.velocity,2)/2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    player.position.Y -= (float)Math.Sqrt(Math.Pow(player.velocity, 2) / 2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.UpRight:
                    player.position.X += (float)Math.Sqrt(Math.Pow(player.velocity, 2) / 2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    player.position.Y -= (float)Math.Sqrt(Math.Pow(player.velocity,2)/2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.DownLeft:
                    player.position.X -= (float)Math.Sqrt(Math.Pow(player.velocity, 2) / 2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    player.position.Y += (float)Math.Sqrt(Math.Pow(player.velocity,2)/2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                case Direction.DownRight:
                    player.position.X += (float)Math.Sqrt(Math.Pow(player.velocity, 2) / 2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    player.position.Y += (float)Math.Sqrt(Math.Pow(player.velocity,2)/2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    break;
                default:
                    break;
            }
            if (shooting != Direction.None) { player.direction = shooting; }

            if (player.position.X < 15) player.position.X = 15;
            else if (player.position.X > canvasWidth - 15)
            {
                player.position.X = canvasWidth - 15;
            }
            if (player.position.Y < 15) player.position.Y = 15;
            else if (player.position.Y > canvasHeight - 15)
            {
                player.position.Y = canvasHeight - 15;
            }
            foreach (var powerUp in playerPowerUps) { powerUp.Update(counter); }
            playerPowerUps.RemoveAll(p => !p.active);
        }
        private void UpdateBullets()
        {
            if (bulletTimer == 0)
            {
                SpawnBulletSprite(player.direction, player.position);
                bulletTimer = (rapidFire.active) ? _bulletCD / 3 : _bulletCD;
            }
            else bulletTimer--;

            
            foreach (var bullet in bulletSprites)
            {
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
                            dx = -1 * (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            dy = (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            break;
                        case Direction.UpRight:
                            dx = (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            dy = (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            break;
                        case Direction.DownRight:
                            dy = -1 * (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            dx = (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            break;
                        case Direction.DownLeft:
                            dy = -1 * (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            dx = -1 * (float)Math.Sqrt(Math.Pow(bullet.velocity, 2) / 2);
                            break;
                    }
                    float x = bullet.position.X + dx;
                    float y = bullet.position.Y - dy;
                    bullet.position = new Vector2(x, y);
                }
                else
                {
                    if (collateralShot.active)
                    {
                        foreach (Character enemy in enemySprites)
                        {         
                            float distance = (float)Math.Sqrt(
                                Math.Pow(bullet.position.X - enemy.position.X, 2) +
                                Math.Pow(bullet.position.Y - enemy.position.Y, 2)
                            );
                            if (distance < 75 ) { enemy.health--; }
                        }
                    }
                }
            }
            bulletSprites.RemoveAll(bullet =>
                    (bullet.position.X > canvasWidth) ||
                    (bullet.position.X < 0) ||
                    (bullet.position.Y > canvasHeight) ||
                    (bullet.position.Y < 0));
            bulletSprites.RemoveAll(b => b.health <= 0);
        }
        private void UpdateEnemies()
        {
            enemySprites.ForEach(e => { if (e.health <= 0) enemyKills++; });
            enemySprites.RemoveAll(enemy => enemy.health <= 0);
            foreach (Character enemy in enemySprites)
            {
                foreach (var bullet in bulletSprites)
                {
                    if ((bullet.position.X < enemy.position.X + bullet.velocity + enemy.texture.Width / 2) &&
                        (bullet.position.X > enemy.position.X - bullet.velocity - enemy.texture.Width / 2) &&
                        (bullet.position.Y < enemy.position.Y + bullet.velocity + enemy.texture.Height / 2) &&
                        (bullet.position.Y > enemy.position.Y - bullet.velocity - enemy.texture.Height / 2))
                    {
                        enemy.health -= (piercingBullets.active) ? 3 : 1;
                        bullet.health -= (enemy.health > 0 && piercingBullets.active) ? 25 : 1;
                        if (enemy.health <= 0) // Drop Rate
                        {
                            Random rnd = new();
                            if (rnd.Next(100) < 2) { SpawnDropSprite(0, enemy.position); } // Spawn Heart
                            if (enemy.texture.Width > 50) { SpawnDropSprite(1, enemy.position); } // Spawn Chest
                        }
                    }
                }
                if (enemy.health > 0)
                {
                    float theta = GetAngleFromEnemyToPlayer(enemy.position, player.position);
                    enemy.angle = theta * 25 / enemy.texture.Width; // Hard coded to slow down big boy turning

                    float da = enemy.velocity * (float)Math.Cos(theta);
                    float db = enemy.velocity * (float)Math.Sin(theta);

                    enemy.position.X += da;
                    enemy.position.Y += db;

                    if (enemy.Touches(player))
                    {
                        enemy.health -= 3;
                        player.health--;
                    }
                }
            }
        }
        private void UpdateDrops(int currentTime)
        {
            foreach (var drop in dropSprites)
            {
                if (player.Touches(drop))
                {
                    drop.health = 0;
                    if (drop.texture == chestTexture) { ActivatePlayerPowerUp(currentTime); } 
                    else { player.health++; }
                }
            }
            dropSprites.RemoveAll(d => d.health <= 0);
        }
        private void ActivatePlayerPowerUp(int currentTime, PowerUp? powerUp = null)
        {
            PowerUp p = powerUp ?? getRandomPowerUp();
            if (p.active) { p.startTime += p.duration; }
            else
            {
                p.Initiate(currentTime);
                playerPowerUps.Add(p);
            }
        }
        private PowerUp getRandomPowerUp()
        {
            Random rnd = new Random();
            int value = rnd.Next(100);

            if (value < 25) { return rapidFire; } 
            else if (value < 50) { return piercingBullets; }
            else if (value < 75) { return tripleShot; }
            else { return collateralShot; }
        }
        private float GetAngleFromEnemyToPlayer(Vector2 enemyPosition, Vector2 playerPosition)
        {
            float distY = playerPosition.Y - enemyPosition.Y;
            float distX = playerPosition.X - enemyPosition.X;
            float theta;

            if (distY > -5 && distY < 5)
            {
                if (playerPosition.X < enemyPosition.X) theta = (float)Math.PI * 3;
                else theta = (float)Math.PI * 2;
            }
            else if (distX > -5 && distX < 5)
            {
                if (playerPosition.Y < enemyPosition.Y) theta = (float)Math.PI * 3 / 2;
                else theta = (float)(Math.PI * 5 / 2);
            }
            else
            {
                theta = (float)(Math.Atan(distY / distX) + (2 * Math.PI));
                if (playerPosition.X < enemyPosition.X)
                {
                    if (playerPosition.Y < enemyPosition.Y) theta -= (float)Math.PI;
                    else theta += (float)Math.PI;
                }
            }
            return theta;
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
        private Vector2 GetRandomOutsidePosition()
        {
            Random rnd = new Random();
            int side = rnd.Next(1, 5);
            int randomX = rnd.Next(canvasWidth);
            int randomY = rnd.Next(canvasHeight);
            Vector2 position = new Vector2(0, 0);
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
                    position.X = canvasWidth + 100;
                    position.Y = randomY;
                    break;
                case 4:
                    position.X = randomX;
                    position.Y = canvasHeight + 100;
                    break;
            }
            return position;
        }
        private Vector2 GetRandomInsidePosition()
        {
            Random rnd = new Random();
            int randomX = rnd.Next(canvasWidth - 25);
            int randomY = rnd.Next(canvasHeight - 25);
            return new Vector2(randomX, randomY);
        }
        
    }
}
