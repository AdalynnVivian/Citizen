using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using RWCustom;
using Noise;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using System.Globalization;
using System.Drawing;
using Color = UnityEngine.Color;
using MonoMod.RuntimeDetour;

namespace SlugTemplate
{
    [BepInPlugin(MOD_ID, "Slugcat Template", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "adalynn.citizen";
        public static int cfgArtificerExplosionCapacity = 5;
        public static float jumpMultVal = 1.25f;//1.25f;
        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.DefaultFaceSprite += PlayerGraphics_DefaultFaceSprite;
            On.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
            On.Player.ctor += Player_ctor;
            IL.Player.Die += Player_Die;
            IL.Player.TerrainImpact += Player_TerrainImpact;
            On.Player.PyroDeath += Player_PyroDeath;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
            On.Player.CanIPickThisUp += Player_CanIPickThisUp;
            On.MoreSlugcats.SingularityBomb.ctor += SingularityBomb_ctor;
        }

        private bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            if(obj is Weapon && (obj as Weapon).mode == Weapon.Mode.StuckInWall && self.slugcatStats.name.value == "Citizen" && obj is Spear)
            {
                return true;
            }
            return orig(self, obj);
        }

        private bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            if(self.slugcatStats.name.value == "Citizen" && testObj is EggBugEgg)
            {
                return true;
            }
            return orig(self, testObj);
        }

        private void SingularityBomb_ctor(On.MoreSlugcats.SingularityBomb.orig_ctor orig, SingularityBomb self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            if (world.game.IsStorySession && world.game.GetStorySession.characterStats.name.value == "Citizen")
            {
                self.zeroMode = true;
            }
            orig(self, abstractPhysicalObject, world);
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (ModManager.MSC && self.slugcatStats.name.value == "Citizen" && abstractCreature.Room.world.game.IsStorySession)
            {
                AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(abstractCreature.Room.world, MoreSlugcatsEnums.AbstractObjectType.SingularityBomb, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), abstractCreature.Room.world.game.GetNewID());
                abstractCreature.Room.AddEntity(abstractPhysicalObject);
                abstractPhysicalObject.RealizeInRoom();
            }
        }

        private string PlayerGraphics_DefaultFaceSprite(On.PlayerGraphics.orig_DefaultFaceSprite orig, PlayerGraphics self, float eyeScale)
        {
            string str;
            string text = "Face";
            if (self.blink <= 0 && self.player.slugcatStats.name.value == "Citizen")
            {
                if (text != "PFace")
                {
                    if (eyeScale < 0f)
                    {
                        str = "D";
                    }
                    else
                    {
                        str = "C";
                    }
                }
                else
                {
                    str = "A";
                }
            }
            else
            {
                return orig(self, eyeScale);
            }
            return text + str;
        }

        private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (self.player.slugcatStats.name.value == "Citizen" && sLeaser.sprites.Length > 12)
            {
                Color color = (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup) ? self.player.ShortCutColor() : PlayerGraphics.SlugcatColor(self.CharacterForColor);
                Color color2 = new Color(color.r, color.g, color.b);
                if (self.malnourished > 0f)
                {
                    float num = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                    color2 = Color.Lerp(color2, Color.gray, 0.4f * num);
                }
                if (ModManager.CoopAvailable && self.useJollyColor)
                    {
                        sLeaser.sprites[sLeaser.sprites.Length - 1].color = PlayerGraphics.JollyColor(self.player.playerState.playerNumber, 2);
                    }
                    else if (PlayerGraphics.CustomColorsEnabled())
                    {
                        sLeaser.sprites[sLeaser.sprites.Length - 1].color = PlayerGraphics.CustomColorSafety(2);
                    }
                    else
                    {
                    sLeaser.sprites[sLeaser.sprites.Length - 1].color = new Color(0.98826f, 0.78824f, 0.3841f);
                    }
                }
        }

            private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.player.slugcatStats.name.value == "Citizen")
            {
                sLeaser.sprites[sLeaser.sprites.Length - 1].rotation = sLeaser.sprites[9].rotation;
                sLeaser.sprites[sLeaser.sprites.Length - 1].scaleX = 1f;
                if (self.player.animation == Player.AnimationIndex.Flip)
                {
                    Vector2 vector13 = Custom.DegToVec(sLeaser.sprites[9].rotation) * 4f;
                    sLeaser.sprites[sLeaser.sprites.Length - 1].x = sLeaser.sprites[9].x + vector13.x;
                    sLeaser.sprites[sLeaser.sprites.Length - 1].y = sLeaser.sprites[9].y + vector13.y;
                }
                else
                {
                    int num10 = 0;
                    string name = sLeaser.sprites[9].element.name;
                    if (name[name.Length - 2] == 'C')
                    {
                        num10 = int.Parse(name[name.Length - 1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
                        sLeaser.sprites[sLeaser.sprites.Length - 1].scaleX = 1f - (float)num10 / 8f;
                        sLeaser.sprites[sLeaser.sprites.Length - 1].x = sLeaser.sprites[9].x + 3f + 4f * ((float)num10 / 8f);
                    }
                    else if (name[name.Length - 2] == 'D')
                    {
                        num10 = int.Parse(name[name.Length - 1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
                        sLeaser.sprites[sLeaser.sprites.Length - 1].x = sLeaser.sprites[9].x + 3f * (1f - (float)num10 / 8f);
                    }
                    else
                    {
                        sLeaser.sprites[sLeaser.sprites.Length - 1].x = sLeaser.sprites[9].x + 3f * (1f - (float)num10 / 8f);
                    }
                    sLeaser.sprites[sLeaser.sprites.Length - 1].y = sLeaser.sprites[9].y + 3f;
                }
                sLeaser.sprites[sLeaser.sprites.Length - 1].MoveBehindOtherNode(sLeaser.sprites[9]);
                sLeaser.sprites[sLeaser.sprites.Length - 1].MoveInFrontOfOtherNode(sLeaser.sprites[8]);
            }
        }

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.player.slugcatStats.name.value == "Citizen")
            {
                System.Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                sLeaser.sprites[sLeaser.sprites.Length - 1] = new FSprite("MushroomA", true);
                self.AddToContainer(sLeaser, rCam, null);
            }
        }

        private void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            orig(self, sLeaser, rCam, newContainer);
            if(self.player.room != null && self.player.slugcatStats.name.value == "Citizen")
            {
                if(newContainer == null)
                {
                    newContainer = rCam.ReturnFContainer("Midground");
                }
                newContainer.AddChild(sLeaser.sprites[sLeaser.sprites.Length - 1]);
                sLeaser.sprites[sLeaser.sprites.Length - 1].MoveBehindOtherNode(sLeaser.sprites[9]);
            }
        }

        private void Player_PyroDeath(On.Player.orig_PyroDeath orig, Player self)
        {
            var explodeColor = new Color(1f, 0.2f, 0.2f);
            Creature thrownBy = null;
            Vector2 vector = Vector2.Lerp(self.firstChunk.pos, self.firstChunk.lastPos, 0.35f);
            self.room.AddObject(new SingularityBomb.SparkFlash(self.firstChunk.pos, 300f, new Color(0f, 0f, 1f)));
            self.room.AddObject(new Explosion(self.room, self, vector, 7, 450f, 6.2f, 10f, 280f, 0.25f, thrownBy, 0.3f, 160f, 1f));
            self.room.AddObject(new Explosion(self.room, self, vector, 7, 2000f, 4f, 0f, 400f, 0.25f, thrownBy, 0.3f, 200f, 1f));
            self.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, explodeColor));
            self.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            self.room.AddObject(new Explosion.ExplosionLight(vector, 2000f, 2f, 60, explodeColor));
            self.room.AddObject(new ShockWave(vector, 350f, 0.485f, 300, true));
            self.room.AddObject(new ShockWave(vector, 2000f, 0.185f, 180, false));
            for (int i = 0; i < 25; i++)
            {
                Vector2 a = Custom.RNV();
                if (self.room.GetTile(vector + a * 20f).Solid)
                {
                    if (!self.room.GetTile(vector - a * 20f).Solid)
                    {
                        a *= -1f;
                    }
                    else
                    {
                        a = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    self.room.AddObject(new Spark(vector + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(explodeColor, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                }
                self.room.AddObject(new Explosion.FlashingSmoke(vector + a * 40f * Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), explodeColor, Random.Range(3, 11)));
            }
            /*for (int k = 0; k < 6; k++)
            {
                self.room.AddObject(new SingularityBomb.BombFragment(vector, Custom.DegToVec(((float)k + Random.value) / 6f * 360f) * Mathf.Lerp(18f, 38f, Random.value)));
            //to do. add slug chunks.
            }*/
            self.room.ScreenMovement(new Vector2?(vector), default(Vector2), 0.9f);
            for (int l = 0; l < self.abstractPhysicalObject.stuckObjects.Count; l++)
            {
                self.abstractPhysicalObject.stuckObjects[l].Deactivate();
            }
            self.room.PlaySound(SoundID.Bomb_Explode, vector);
            self.room.InGameNoise(new InGameNoise(vector, 9000f, self, 1f));
            for (int m = 0; m < self.room.physicalObjects.Length; m++)
            {
                for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                {
                    if (self.room.physicalObjects[m][n] is Creature && Custom.Dist(self.room.physicalObjects[m][n].firstChunk.pos, self.firstChunk.pos) < 350f)
                    {
                        if (thrownBy != null)
                        {
                            (self.room.physicalObjects[m][n] as Creature).killTag = thrownBy.abstractCreature;
                        }
                        (self.room.physicalObjects[m][n] as Creature).Die();
                    }
                    if (self.room.physicalObjects[m][n] is ElectricSpear)
                    {
                        if ((self.room.physicalObjects[m][n] as ElectricSpear).abstractSpear.electricCharge == 0)
                        {
                            (self.room.physicalObjects[m][n] as ElectricSpear).Recharge();
                        }
                        else
                        {
                            (self.room.physicalObjects[m][n] as ElectricSpear).ExplosiveShortCircuit();
                        }
                    }
                }
            }
            self.room.InGameNoise(new InGameNoise(self.firstChunk.pos, 12000f, self, 1f));
        }

        public static void Player_ClassMechanicsArtificer(On.Player.orig_ClassMechanicsArtificer orig, Player self)
        {
            orig.Invoke(self);
            if(self.slugcatStats.name.value == "Citizen")
            {
                bool flag = self.wantToJump > 0 && self.input[0].pckp;
                bool flag2 = self.eatMeat >= 20 || self.maulTimer >= 15;
                int num = Mathf.Max(1, cfgArtificerExplosionCapacity - 5);
                if (self.pyroJumpCounter > 0 && (self.Consious || self.dead))
                {
                    self.pyroJumpCooldown -= 1f;
                    if (self.pyroJumpCooldown <= 0f)
                    {
                        if (self.pyroJumpCounter >= num)
                        {
                            self.pyroJumpCooldown = 40f;
                        }
                        else
                        {
                            self.pyroJumpCooldown = 60f;
                        }
                        self.pyroJumpCounter--;
                    }
                }
                self.pyroParryCooldown -= 1f;
                if (self.pyroJumpCounter >= num)
                {
                    if (Random.value < 0.25f)
                    {
                        self.room.AddObject(new Explosion.ExplosionSmoke(self.mainBodyChunk.pos, Custom.RNV() * 2f * Random.value, 1f));
                    }
                    if (Random.value < 0.5f)
                    {
                        self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV(), Color.white, null, 4, 8));
                    }
                }
                if (flag && !self.pyroJumpped && self.canJump <= 0 && !flag2 && (self.input[0].y >= 0 || (self.input[0].y < 0 && (self.bodyMode == Player.BodyModeIndex.ZeroG || self.gravity <= 0.1f))) && self.Consious && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.animation != Player.AnimationIndex.HangFromBeam && self.animation != Player.AnimationIndex.ClimbOnBeam && self.bodyMode != Player.BodyModeIndex.WallClimb && self.bodyMode != Player.BodyModeIndex.Swimming && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.VineGrab && self.animation != Player.AnimationIndex.ZeroGPoleGrab && self.onBack == null)
                {
                    self.pyroJumpped = true;
                    self.pyroJumpDropLock = 40;
                    self.noGrabCounter = 5;
                    Vector2 pos = self.firstChunk.pos;
                    for (int i = 0; i < 8; i++)
                    {
                        self.room.AddObject(new Explosion.ExplosionSmoke(pos, Custom.RNV() * 5f * Random.value, 1f));
                    }
                    self.room.AddObject(new Explosion.ExplosionLight(pos, 160f, 1f, 3, Color.white));
                    self.room.AddObject(new ShockWave(pos, 300f, 0.485f, 90, true)); //!!!
                    self.room.AddObject(new ShockWave(pos, 600f, 0.185f, 45, false)); //!!!
                    for (int j = 0; j < 10; j++)
                    {
                        Vector2 a = Custom.RNV();
                        self.room.AddObject(new Spark(pos + a * Random.value * 40f, a * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                    }
                    //self.room.PlaySound(SoundID.Fire_Spear_Explode, pos, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
                    self.room.PlaySound(MoreSlugcats.MoreSlugcatsEnums.MSCSoundID.Singularity, pos, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
                    self.room.InGameNoise(new InGameNoise(pos, 8000f, self, 1f));
                    int num2 = Mathf.Max(1, cfgArtificerExplosionCapacity - 3);
                    if (self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity == 0f || self.gravity == 0f)
                    {
                        float num3 = (float)self.input[0].x;
                        float num4 = (float)self.input[0].y;
                        while (num3 == 0f && num4 == 0f)
                        {
                            num3 = (float)(((double)Random.value <= 0.33) ? 0 : (((double)Random.value <= 0.5) ? 1 : -1));
                            num4 = (float)(((double)Random.value <= 0.33) ? 0 : (((double)Random.value <= 0.5) ? 1 : -1));
                        }
                        self.bodyChunks[0].vel.x = jumpMultVal * 9f * num3;
                        self.bodyChunks[0].vel.y = jumpMultVal * 9f * num4;
                        self.bodyChunks[1].vel.x = jumpMultVal * 8f * num3;
                        self.bodyChunks[1].vel.y = jumpMultVal * 8f * num4;
                        self.pyroJumpCooldown = 150f;
                        self.pyroJumpCounter++;
                    }
                    else
                    {
                        if (self.input[0].x != 0)
                        {
                            self.bodyChunks[0].vel.y = jumpMultVal * (Mathf.Min(self.bodyChunks[0].vel.y, 0f) + 8f);
                            self.bodyChunks[1].vel.y = jumpMultVal * (Mathf.Min(self.bodyChunks[1].vel.y, 0f) + 7f);
                            self.jumpBoost = 6f;
                        }
                        if (self.input[0].x == 0 || self.input[0].y == 1)
                        {
                            if (self.pyroJumpCounter >= num2)
                            {
                                self.bodyChunks[0].vel.y = jumpMultVal * 16f;
                                self.bodyChunks[1].vel.y = jumpMultVal * 15f;
                                self.jumpBoost = jumpMultVal * 10f;
                            }
                            else
                            {
                                self.bodyChunks[0].vel.y = jumpMultVal * 11f;
                                self.bodyChunks[1].vel.y = jumpMultVal * 10f;
                                self.jumpBoost = jumpMultVal * 8f;
                            }
                        }
                        if (self.input[0].y == 1)
                        {
                            self.bodyChunks[0].vel.x = jumpMultVal * 10f * (float)self.input[0].x;
                            self.bodyChunks[1].vel.x = jumpMultVal * 8f * (float)self.input[0].x;
                        }
                        else
                        {
                            self.bodyChunks[0].vel.x = jumpMultVal * 15f * (float)self.input[0].x;
                            self.bodyChunks[1].vel.x = jumpMultVal * 13f * (float)self.input[0].x;
                        }
                        self.animation = Player.AnimationIndex.Flip;
                        self.pyroJumpCounter++;
                        self.pyroJumpCooldown = 150f;
                        self.bodyMode = Player.BodyModeIndex.Default;
                    }
                    if (self.pyroJumpCounter >= num2)
                    {
                        self.Stun(60 * (self.pyroJumpCounter - (num2 - 1)));
                    }
                    if (self.pyroJumpCounter >= cfgArtificerExplosionCapacity)
                    {
                        self.PyroDeath();
                    }
                }
                else if (flag && !self.submerged && !flag2 && (self.input[0].y < 0 || self.bodyMode == Player.BodyModeIndex.Crawl) && (self.canJump > 0 || self.input[0].y < 0) && self.Consious && !self.pyroJumpped && self.pyroParryCooldown <= 0f)
                {
                    if (self.canJump <= 0)
                    {
                        self.pyroJumpped = true;
                        self.bodyChunks[0].vel.y = 8f;
                        self.bodyChunks[1].vel.y = 6f;
                        self.jumpBoost = 6f;
                        self.forceSleepCounter = 0;
                    }
                    if (self.pyroJumpCounter <= num)
                    {
                        self.pyroJumpCounter += 2;
                    }
                    else
                    {
                        self.pyroJumpCounter++;
                    }
                    self.pyroParryCooldown = 40f;
                    self.pyroJumpCooldown = 150f;
                    Vector2 pos2 = self.firstChunk.pos;
                    for (int k = 0; k < 8; k++)
                    {
                        self.room.AddObject(new Explosion.ExplosionSmoke(pos2, Custom.RNV() * 5f * Random.value, 1f));
                    }
                    self.room.AddObject(new Explosion.ExplosionLight(pos2, 160f, 1f, 3, Color.white));
                    for (int l = 0; l < 10; l++)
                    {
                        Vector2 a2 = Custom.RNV();
                        self.room.AddObject(new Spark(pos2 + a2 * Random.value * 40f, a2 * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 4, 18));
                    }
                    //self.room.AddObject(new ShockWave(pos2, 200f, 0.2f, 6, false));
                    self.room.AddObject(new ShockWave(pos2, 300f, 0.485f, 90, true)); //!!!
                    self.room.AddObject(new ShockWave(pos2, 600f, 0.185f, 45, false)); //!!!
                    //self.room.PlaySound(SoundID.Fire_Spear_Explode, pos2, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
                    self.room.PlaySound(MoreSlugcats.MoreSlugcatsEnums.MSCSoundID.Singularity, pos2, 0.3f + Random.value * 0.3f, 0.5f + Random.value * 2f);
                    self.room.InGameNoise(new InGameNoise(pos2, 8000f, self, 1f));
                    List<Weapon> list = new List<Weapon>();
                    for (int m = 0; m < self.room.physicalObjects.Length; m++)
                    {
                        for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                        {
                            if (self.room.physicalObjects[m][n] is Weapon)
                            {
                                Weapon weapon = self.room.physicalObjects[m][n] as Weapon;
                                if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(pos2, weapon.firstChunk.pos) < 300f)
                                {
                                    list.Add(weapon);
                                }
                            }
                            bool flag3;
                            if (ModManager.CoopAvailable && !Custom.rainWorld.options.friendlyFire)
                            {
                                Player player = self.room.physicalObjects[m][n] as Player;
                                flag3 = (player == null || player.isNPC);
                            }
                            else
                            {
                                flag3 = true;
                            }
                            bool flag4 = flag3;
                            if (self.room.physicalObjects[m][n] is Creature && self.room.physicalObjects[m][n] != self && flag4)
                            {
                                Creature creature = self.room.physicalObjects[m][n] as Creature;
                                if (Custom.Dist(pos2, creature.firstChunk.pos) < 200f && (Custom.Dist(pos2, creature.firstChunk.pos) < 60f || self.room.VisualContact(self.abstractCreature.pos, creature.abstractCreature.pos)))
                                {
                                    self.room.socialEventRecognizer.WeaponAttack(null, self, creature, true);
                                    creature.SetKillTag(self.abstractCreature);
                                    if (creature is Scavenger)
                                    {
                                        (creature as Scavenger).HeavyStun(80);
                                    }
                                    else
                                    {
                                        creature.Stun(80);
                                    }
                                    creature.firstChunk.vel = Custom.DegToVec(Custom.AimFromOneVectorToAnother(pos2, creature.firstChunk.pos)) * 30f;
                                    if (creature is TentaclePlant)
                                    {
                                        for (int num5 = 0; num5 < creature.grasps.Length; num5++)
                                        {
                                            creature.ReleaseGrasp(num5);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (list.Count > 0 && self.room.game.IsArenaSession)
                    {
                        self.room.game.GetArenaGameSession.arenaSitting.players[0].parries++;
                    }
                    for (int num6 = 0; num6 < list.Count; num6++)
                    {
                        list[num6].ChangeMode(Weapon.Mode.Free);
                        list[num6].firstChunk.vel = Custom.DegToVec(Custom.AimFromOneVectorToAnother(pos2, list[num6].firstChunk.pos)) * 20f;
                        list[num6].SetRandomSpin();
                    }
                    int num7 = Mathf.Max(1, cfgArtificerExplosionCapacity - 3);
                    if (self.pyroJumpCounter >= num7)
                    {
                        self.Stun(60 * (self.pyroJumpCounter - (num7 - 1)));
                    }
                    if (self.pyroJumpCounter >= cfgArtificerExplosionCapacity)
                    {
                        self.PyroDeath();
                    }
                }
                if (self.canJump > 0 || !self.Consious || self.Stunned || self.animation == Player.AnimationIndex.HangFromBeam || self.animation == Player.AnimationIndex.ClimbOnBeam || self.bodyMode == Player.BodyModeIndex.WallClimb || self.animation == Player.AnimationIndex.AntlerClimb || self.animation == Player.AnimationIndex.VineGrab || self.animation == Player.AnimationIndex.ZeroGPoleGrab || self.bodyMode == Player.BodyModeIndex.Swimming || ((self.bodyMode == Player.BodyModeIndex.ZeroG || self.room.gravity <= 0.5f || self.gravity <= 0.5f) && (self.wantToJump == 0 || !self.input[0].pckp)))
                {
                    self.pyroJumpped = false;
                }
            }
        }
        private void Player_Die(ILContext il) //Gamer
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)),
                x => x.Match(OpCodes.Call));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, bool>>((self) => self.slugcatStats.name.value == "Citizen");

            c.Emit(OpCodes.Or);
        }

        private void Player_TerrainImpact(ILContext il) //AAAAAAAA
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)),
                x => x.Match(OpCodes.Call));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, bool>>((self) => self.slugcatStats.name.value == "Citizen");

            c.Emit(OpCodes.Or);
        }
        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }
    }
}