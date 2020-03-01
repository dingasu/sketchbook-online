﻿using System;
using System.Collections.Generic;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.GameObjects;
using Intersect.GameObjects.Events;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities.Events
{
    public class Event : Entity
    {
        private GameTexture mCachedTileset;

        private string mCachedTilesetName;
        public string Desc = "";
        public bool DirectionFix;
        public bool DisablePreview;
        public string FaceGraphic = "";
        public EventGraphic Graphic = new EventGraphic();
        public int Layer;
        private int mOldRenderLevel;

        private MapInstance mOldRenderMap;
        private int mOldRenderY;
        public int RenderLevel = 1;
        public bool WalkingAnim = true;

        public Event(Guid id, EventEntityPacket packet) : base(id, packet, true)
        {
            mRenderPriority = 1;
        }

        public override string ToString()
        {
            return Name;
        }

        public override void Load(EntityPacket packet)
        {
            base.Load(packet);
            var pkt = (EventEntityPacket)packet;
            DirectionFix = pkt.DirectionFix;
            WalkingAnim = pkt.WalkingAnim;
            DisablePreview = pkt.DisablePreview;
            Desc = pkt.Description;
            Graphic = pkt.Graphic;
            RenderLevel = pkt.RenderLayer;
        }

        public override EntityTypes GetEntityType()
        {
            return EntityTypes.Event;
        }

        public override bool Update()
        {
            bool success = base.Update();
            if (!WalkingAnim) WalkFrame = 0;
            return success;
        }

        public override void Draw()
        {
            WorldPos.Reset();
            if (MapInstance.Get(CurrentMap) == null ||
                !Globals.GridMaps.Contains(CurrentMap)) return;
            var map = MapInstance.Get(CurrentMap);
            FloatRect srcRectangle = new FloatRect();
            FloatRect destRectangle = new FloatRect();
            GameTexture srcTexture = null;
            int height = 0;
            int width = 0;
            var d = 0;
            switch (Graphic.Type)
            {
                case EventGraphicType.Sprite: //Sprite
                    GameTexture entityTex = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Entity, Graphic.Filename);
                    if (entityTex != null)
                    {
                        srcTexture = entityTex;
                        height = srcTexture.GetHeight() / 4;
                        width = srcTexture.GetWidth() / 4;
                        d = Graphic.Y;
                        if (!DirectionFix)
                        {
                            switch (Dir)
                            {
                                case 0:
                                    d = 3;
                                    break;
                                case 1:
                                    d = 0;
                                    break;
                                case 2:
                                    d = 1;
                                    break;
                                case 3:
                                    d = 2;
                                    break;
                            }
                        }
                        int frame = Graphic.X;
                        if (WalkingAnim) frame = WalkFrame;
                        if (Options.AnimatedSprites.Contains(Graphic.Filename.ToLower()))
                        {
                            srcRectangle = new FloatRect(AnimationFrame * (int) entityTex.GetWidth() / 4,
                                d * (int) entityTex.GetHeight() / 4,
                                (int) entityTex.GetWidth() / 4, (int) entityTex.GetHeight() / 4);
                        }
                        else
                        {
                            srcRectangle = new FloatRect(frame * (int) srcTexture.GetWidth() / 4,
                                d * (int) srcTexture.GetHeight() / 4, (int) srcTexture.GetWidth() / 4,
                                (int) srcTexture.GetHeight() / 4);
                        }
                    }
                    break;
                case EventGraphicType.Tileset: //Tile
                    if (mCachedTilesetName != Graphic.Filename)
                    {
                        mCachedTilesetName = Graphic.Filename;
                        mCachedTileset = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Tileset,  Graphic.Filename);
                    }
                    GameTexture tileset = mCachedTileset;
                    if (tileset != null)
                    {
                        srcTexture = tileset;
                        width = (Graphic.Width + 1) * Options.TileWidth;
                        height = (Graphic.Height + 1) * Options.TileHeight;
                        srcRectangle = new FloatRect(Graphic.X * Options.TileWidth, Graphic.Y * Options.TileHeight,
                            (Graphic.Width + 1) * Options.TileWidth, (Graphic.Height + 1) * Options.TileHeight);
                    }
                    break;
            }
            destRectangle.X = map.GetX() + X * Options.TileWidth + OffsetX;
            if (height > Options.TileHeight)
            {
                destRectangle.Y = map.GetY() + Y * Options.TileHeight + OffsetY -
                                  ((height) - Options.TileHeight);
            }
            else
            {
                destRectangle.Y = map.GetY() + Y * Options.TileHeight + OffsetY;
            }
            if (width > Options.TileWidth)
            {
                destRectangle.X -= ((width) - Options.TileWidth) / 2;
            }
            destRectangle.X = (int)Math.Ceiling(destRectangle.X);
            destRectangle.Y = (int)Math.Ceiling(destRectangle.Y);
            destRectangle.Width = srcRectangle.Width;
            destRectangle.Height = srcRectangle.Height;
            WorldPos = destRectangle;
            if (srcTexture != null)
            {
                GameGraphics.DrawGameTexture(srcTexture, srcRectangle, destRectangle, Intersect.Color.White);
            }
        }

        public override HashSet<Entity> DetermineRenderOrder(HashSet<Entity> renderList, MapInstance map)
        {
            if (RenderLevel == 1) return base.DetermineRenderOrder(renderList,map);
            renderList?.Remove(this);
            if (map == null || Globals.Me == null || Globals.Me.MapInstance == null)
            {
                return null;
            }
            var gridX = Globals.Me.MapInstance.MapGridX;
            var gridY = Globals.Me.MapInstance.MapGridY;
            for (int x = gridX - 1; x <= gridX + 1; x++)
            {
                for (int y = gridY - 1; y <= gridY + 1; y++)
                {
                    if (x >= 0 && x < Globals.MapGridWidth && y >= 0 && y < Globals.MapGridHeight &&
                        Globals.MapGrid[x, y] != Guid.Empty)
                    {
                        if (Globals.MapGrid[x, y] == CurrentMap)
                        {
                            if (RenderLevel == 0) y--;
                            if (RenderLevel == 2) y++;
                            var priority = mRenderPriority;
                            if (Z != 0)
                            {
                                priority += 3;
                            }
                            if (y == gridY - 2)
                            {
                                GameGraphics.RenderingEntities[priority, Y].Add(this);
                                renderList = GameGraphics.RenderingEntities[priority, Y];
                                return renderList;
                            }
                            else if (y == gridY - 1)
                            {
                                GameGraphics.RenderingEntities[priority, Options.MapHeight + Y].Add(this);
                                renderList = GameGraphics.RenderingEntities[priority, Options.MapHeight + Y];
                                return renderList;
                            }
                            else if (y == gridY)
                            {
                                GameGraphics.RenderingEntities[priority, Options.MapHeight * 2 + Y].Add(this);
                                renderList = GameGraphics.RenderingEntities[priority, Options.MapHeight * 2 + Y];
                                return renderList;
                            }
                            else if (y == gridY + 1)
                            {
                                GameGraphics.RenderingEntities[priority, Options.MapHeight * 3 + Y].Add(this);
                                renderList = GameGraphics.RenderingEntities[priority, Options.MapHeight * 3 + Y];
                                return renderList;
                            }
                            else if (y == gridY + 2)
                            {
                                GameGraphics.RenderingEntities[priority, Options.MapHeight * 4 + Y].Add(this);
                                renderList = GameGraphics.RenderingEntities[priority, Options.MapHeight * 4 + Y];
                                return renderList;
                            }
                        }
                    }
                }
            }
            return renderList;
        }

        public override void DrawName(Color textColor, Color borderColor, Color backgroundColor)
        {
            if (HideName || Name.Trim().Length == 0)
            {
                return;
            }
            if (!WorldPos.IntersectsWith(GameGraphics.Renderer.GetView())) return;
            if (MapInstance.Get(CurrentMap) == null ||
                !Globals.GridMaps.Contains(CurrentMap)) return;
            var y = (int) Math.Ceiling(GetCenterPos().Y);
            var x = (int) Math.Ceiling(GetCenterPos().X);
            var height = Options.TileHeight;
            switch (Graphic.Type)
            {
                case EventGraphicType.Sprite: //Sprite
                    GameTexture entityTex = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Entity,
                        Graphic.Filename);
                    if (entityTex != null)
                    {
                        height = entityTex.GetHeight();
                    }
                    break;
                case EventGraphicType.Tileset: //Tile
                    if (mCachedTilesetName != Graphic.Filename)
                    {
                        mCachedTilesetName = Graphic.Filename;
                        mCachedTileset = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Tileset, Graphic.Filename);
                    }
                    if (mCachedTileset != null)
                    {
                        height = (Graphic.Height + 1) * Options.TileHeight;
                    }
                    break;
            }

            y = (int) GetTopPos(height) - 12;
            x = (int) Math.Ceiling(GetCenterPos().X);

            if (Graphic.Type == EventGraphicType.Tileset) y -= 12;

            Pointf textSize = GameGraphics.Renderer.MeasureText(Name, GameGraphics.EntityNameFont, 1);

            if (CustomColors.Names.Events.Background != Color.Transparent)
                GameGraphics.DrawGameTexture(GameGraphics.Renderer.GetWhiteTexture(), new FloatRect(0, 0, 1, 1),
                    new FloatRect((x - textSize.X / 2f) - 4, y, textSize.X + 8, textSize.Y),
                    CustomColors.Names.Events.Background);
            GameGraphics.Renderer.DrawString(Name, GameGraphics.EntityNameFont,
                (int) (x - (int) Math.Ceiling(textSize.X / 2f)), (int) (y), 1,
                Color.FromArgb(CustomColors.Names.Events.Name.ToArgb()), true, null,
                Color.FromArgb(CustomColors.Names.Events.Outline.ToArgb()));
        }

        protected override void CalculateCenterPos()
        {
            var map = MapInstance.Get(CurrentMap);
            if (map == null)
            {
                mCenterPos = Pointf.Empty;
                return;
            }
            Pointf pos = new Pointf(map.GetX() + X * Options.TileWidth + OffsetX + Options.TileWidth / 2,  map.GetY() + Y * Options.TileHeight + OffsetY + Options.TileHeight / 2);
            switch (Graphic.Type)
            {
                case EventGraphicType.Sprite: //Sprite
                    GameTexture entityTex = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Entity,
                        MySprite);
                    if (entityTex != null)
                    {
                        pos.Y += Options.TileHeight / 2;
                        pos.Y -= entityTex.GetHeight() / 4 / 2;
                    }
                    break;
                case EventGraphicType.Tileset: //Tile
                    if (mCachedTilesetName != Graphic.Filename)
                    {
                        mCachedTilesetName = Graphic.Filename;
                        mCachedTileset = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Tileset, Graphic.Filename);
                    }
                    if (mCachedTileset != null)
                    {
                        pos.Y += Options.TileHeight / 2;
                        pos.Y -= ((Graphic.Height + 1) * Options.TileHeight / 2);
                    }
                    break;
            }

            mCenterPos = pos;
        }

        ~Event()
        {
            Dispose();
        }
    }
}