﻿using Cairo;
using claims.src.clientMapHandling;
using claims.src.part.structure;
using claims.src.playerMovements;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using System.Drawing;
using System.Security.Claims;

namespace claims.src.claimsext.map
{
    public class PlotsMapLayer : RGBMapLayer
    {
        //static claimsext modInstance;
        int chunksize = 16;
        IWorldChunk[] chunksTmp;

        object chunksToGenLock = new object();
        UniqueQueue<Vec2i> chunksToGen = new UniqueQueue<Vec2i>();
        public ConcurrentDictionary<Vec2i, CANMultiChunkMapComponent> loadedMapData = new ConcurrentDictionary<Vec2i, CANMultiChunkMapComponent>();
        HashSet<Vec2i> curVisibleChunks = new HashSet<Vec2i>();
        public Dictionary<Vec2i, string> chunkToCityName = new Dictionary<Vec2i, string>();
       // public Dictionary<string, CityPlotInfo> cityNameToCityInfo = new Dictionary<string, CityPlotInfo>();
        public Dictionary<string, Vec3d> cityNameToChestCoords = new Dictionary<string, Vec3d>();
        bool shouldRender = true;
        bool shouldRenderBanks = false;
        public Dictionary<string, LoadedTexture> texturesByIcon;
        public MeshRef quadModel;

        public override MapLegendItem[] LegendItems => throw new NotImplementedException();
        public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
        public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
        public override string Title => "Plots";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

        public override string LayerGroupCode => "claims";

        public string getMapDbFilePath()
        {
            string path = System.IO.Path.Combine(GamePaths.DataPath, "PlotsMaps");
            GamePaths.EnsurePathExists(path);

            return System.IO.Path.Combine(path, api.World.SavegameIdentifier + ".db");
        }
        public PlotsMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            if (api.Side == EnumAppSide.Client)
            {
                quadModel = (api as ICoreClientAPI).Render.UploadMesh(QuadMeshUtil.GetQuad());
            }
        }
        public bool toggleRender(KeyCombination comb)
        {
            shouldRender = !shouldRender;
            return true;
        }
        public bool toggleRenderBanks(KeyCombination comb)
        {
            shouldRenderBanks = !shouldRenderBanks;
            return true;
        }
        public void RedrawPlots()
        {
            foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
            {
                cmp.ActuallyDispose();
            }
            loadedMapData.Clear();
            foreach (var zone in claims.clientDataStorage.getClientSavedPlots())
            {
                foreach (var pl in zone.Value.savedPlots)
                {
                    OnResChunkPixels(pl.Key.Copy(), claims.clientDataStorage.ClientGetCityColor(pl.Value.cityName), "");
                }
            }
        }

        public TextCommandResult onMapCmd(TextCommandCallingArgs args)
        {
            //var mapmgr = api.ModLoader.GetModSystem<WorldMapManager>();
            // mapmgr.MapLayers.Remove(mapmgr.MapLayers[0]);
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            string cmd = (string)args.LastArg;
            ICoreClientAPI capi = api as ICoreClientAPI;

            if (cmd == "purgedb")
            {
                //mapdb.Purge();
                capi.ShowChatMessage("Ok, db purged");
            }

            if (cmd == "redraw")
            {
                foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
                {
                    cmp.ActuallyDispose();
                }
                loadedMapData.Clear();
                foreach(var zone in claims.clientDataStorage.getClientSavedPlots())
                {
                    foreach(var pl in zone.Value.savedPlots)
                    {
                        OnResChunkPixels(pl.Key.Copy(), -777777777, "");
                    }
                    
                }
               

                /*lock (chunksToGenLock)
                {
                    foreach (Vec2i cord in curVisibleChunks)
                    {
                        chunksToGen.Enqueue(cord.Copy());
                    }
                }*/
            }
            return tcr;
        }

        /*Vec2i tmpMccoord = new Vec2i();
        Vec2i tmpCoord = new Vec2i();*/

        public override void OnLoaded()
        {
            //modInstance = api.ModLoader.GetModSystem<claimsext>();

            chunksize = api.World.BlockAccessor.ChunkSize;

            chunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / chunksize];
        }


        public override void OnMapClosedClient()
        {

            lock (chunksToGenLock)
            {
                chunksToGen.Clear();
            }

            curVisibleChunks.Clear();
        }

        public override void Dispose()
        {
            if (loadedMapData != null)
            {
                foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
                {
                    cmp?.ActuallyDispose();
                }
            }

            CANMultiChunkMapComponent.DisposeStatic();

            base.Dispose();
        }

        public override void OnShutDown()
        {
            CANMultiChunkMapComponent.tmpTexture?.Dispose();
        }

        float mtThread1secAccum = 0f;

        float genAccum = 0f;
        float diskSaveAccum = 0f;
        Dictionary<Vec2i, MapPieceDB> toSaveList = new Dictionary<Vec2i, MapPieceDB>();

        public async void clearZoneSavedPlotsFromMap(Vec2i zoneCord)
        {
            //foreach loadedMapData if in zone we delete it
            //after that we call add savedplot and generate new map plots
            Vec2i tmpVec = zoneCord.Copy();
            tmpVec.X = zoneCord.X * 4;
            tmpVec.Y = zoneCord.Y * 4;
            for(int i = 0; i < 4; i++)
            {
                tmpVec.X += i;
                for(int j = 0; j < 4; j++)
                {
                    tmpVec.Y += j;
                    if(this.loadedMapData.TryGetValue(tmpVec, out CANMultiChunkMapComponent comp))
                    {
                        this.loadedMapData.TryRemove(tmpVec, out _);
                    }
                }
            }
        }
        public async void generateFromZoneSavedPlotsOnMap(Vec2i zoneCord)
        {
            //for selected zone we go through all saved plots
            //and generate map plot picture
            await Task.Factory.StartNew(async () =>
            {
                if(claims.clientDataStorage.getClientSavedZone(zoneCord, out ClientSavedZone clientSavedZone))
                {
                    foreach(var it in clientSavedZone.savedPlots)
                    {
                        OnResChunkPixels(it.Key, claims.clientDataStorage.ClientGetCityColor(it.Value.cityName), it.Value.cityName);
                    }
                }
            });
        }
        public async void OnResChunkPixels(Vec2i cord, int color, string structureName)
        {
            await Task.Factory.StartNew(async () =>
            {
                //System.Threading.Thread.Sleep(1000);
                int[] pixels = (int[])GenerateChunkImage(cord, structureName.Length == 0 ? 0 : color, true)?.Clone();

                if (pixels == null)
                {
                    return;
                }


                loadFromChunkPixels(cord, pixels, structureName);
                if (true)
                {
                    //triggeredPlotImageUpdate(cord);

                    /*foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
                    {
                        cmp.;
                    }
                    loadedMapData.Clear();*/
                }
            });
        }
         
        public override void Render(GuiElementMap mapElem, float dt)
        {
            if(!this.Active)
            {
                return;
            }
            if (!shouldRender) return;
            foreach (var val in loadedMapData)
            {
                val.Value.Render(mapElem, dt);
            }          
        }
        public override void OnMapOpenedClient()
        {
            if (texturesByIcon == null)
            {
                if (texturesByIcon != null)
                {
                    foreach (var val in texturesByIcon)
                    {
                        val.Value.Dispose();
                    }
                }

                texturesByIcon = new Dictionary<string, LoadedTexture>();

                double scale = RuntimeEnv.GUIScale;
                int size = (int)(27 * scale);

                ImageSurface surface = new ImageSurface(Format.Argb32, size, size);
                Context ctx = new Context(surface);

                string[] icons = new string[] { "circle", "bee", "cave", "home", "ladder", "pick", "rocks", "ruins", "spiral", "star1", "star2", "trader", "vessel", "cross" };
                ICoreClientAPI capi = api as ICoreClientAPI;

                foreach (var val in icons)
                {
                    ctx.Operator = Operator.Clear;
                    ctx.SetSourceRGBA(0, 0, 0, 0);
                    ctx.Paint();
                    ctx.Operator = Operator.Over;

                    capi.Gui.Icons.DrawIcon(ctx, "wp" + val.UcFirst(), 1, 1, size - 2, size - 2, new double[] { 0, 0, 0, 1 });
                    capi.Gui.Icons.DrawIcon(ctx, "wp" + val.UcFirst(), 2, 2, size - 4, size - 4, ColorUtil.WhiteArgbDouble);

                    texturesByIcon[val] = new LoadedTexture(api as ICoreClientAPI, (api as ICoreClientAPI).Gui.LoadCairoTexture(surface, false), (int)(20 * scale), (int)(20 * scale));
                }

                ctx.Dispose();
                surface.Dispose();
            }
        }
        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            foreach (var val in loadedMapData)
            {
                val.Value.OnMouseMove(args, mapElem, hoverText);
                var c = val.Value;
                Vec2f viewPos = new Vec2f();
                mapElem.TranslateWorldPosToViewPos(new Vec3d(val.Value.chunkCoord.X * 16 + 8, 0, val.Value.chunkCoord.Y * 16 + 8), ref viewPos);
            }
            foreach (var zone in claims.clientDataStorage.getClientSavedPlots())
            {
                foreach (var savedPlot in zone.Value.savedPlots)
                {
                    Vec2f viewPos = new Vec2f();
                    mapElem.TranslateWorldPosToViewPos(new Vec3d(savedPlot.Key.X * 16 + 8, 0, savedPlot.Key.Y * 16 + 8), ref viewPos);

                    double mouseX = args.X - mapElem.Bounds.renderX;
                    double mouseY = args.Y - mapElem.Bounds.renderY;

                    if (Math.Abs(viewPos.X - mouseX) < 8 * mapElem.ZoomLevel && Math.Abs(viewPos.Y - mouseY) < 8 * mapElem.ZoomLevel)
                    {
                        if (savedPlot.Value.cityName.Length > 0)
                        {
                            hoverText.AppendLine(Lang.Get("claims:map_plot_city_name", savedPlot.Value.cityName));
                            if (savedPlot.Value.price > 0)
                            {
                                hoverText.AppendLine(Lang.Get("claims:map_plot_price", savedPlot.Value.price));
                            }
                            //hoverText.Append("Mayor: " + cityPlotInfo.mayorName + "\n");
                        }
                    }
                }
            }
        }
        public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
        {
            foreach (var val in loadedMapData)
            {
                val.Value.OnMouseUpOnElement(args, mapElem);
            }
        }
        void loadFromChunkPixels(Vec2i cord, int[] pixels, string structureName)
        {

            Vec2i mcord = new Vec2i(cord.X /2 / CANMultiChunkMapComponent.ChunkLen, cord.Y / 2 / CANMultiChunkMapComponent.ChunkLen);
            Vec2i baseCord = new Vec2i(mcord.X * CANMultiChunkMapComponent.ChunkLen, mcord.Y * CANMultiChunkMapComponent.ChunkLen);
            api.Event.EnqueueMainThreadTask(() =>
            {
                CANMultiChunkMapComponent mccomp;
                if (!loadedMapData.TryGetValue(mcord, out mccomp))
                {
                    loadedMapData[mcord] = mccomp = new CANMultiChunkMapComponent(api as ICoreClientAPI, baseCord);
                    mccomp.setChunk(cord.X / 2 - baseCord.X, cord.Y / 2 - baseCord.Y, pixels);
                }
                else
                {
                    mccomp.setChunk(cord.X / 2 - baseCord.X, cord.Y / 2 - baseCord.Y, pixels);
                    return;
                }


            }, "plotmaplayerready");
        }

        public void triggeredPlotImageUpdate(Vec2i chunkPos)
        {
            Vec2i mm = new Vec2i(chunkPos.X, chunkPos.Y);

            chunkToCityName.TryGetValue(chunkPos, out string cityName);

            if (!claims.clientDataStorage.getSavedPlot(chunkPos, out SavedPlotInfo savedPlotInfo))
            {
                /*Task.Factory.StartNew(() =>
                {
                    int[] pixels = (int[])GenerateChunkImage(chunkPos, 0, false)?.Clone();

                    loadFromChunkPixels(mm, pixels, "");
                });*/
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    int[] pixels = (int[])GenerateChunkImage(chunkPos, savedPlotInfo != null ? claims.clientDataStorage.ClientGetCityColor(savedPlotInfo.cityName) : 0, false)?.Clone();

                    loadFromChunkPixels(mm, pixels, savedPlotInfo.cityName);
                });
            }

            
        }
        public static int ToRgba(int a, int r, int g, int b)
        {
            int iCol = (a << 24) | (r << 16) | (g << 8) | b;
            return iCol;
        }

        public void GenerateChunkPart(int X, int Y, int color, ref int[] pixels)
        {
            int pivot = 0;
            color &= unchecked((int)0x80ffffff);
            if (Y == 1 && X == 1)
            {
                pivot = 528;
            }
            else if (Y == 1)
            {
                pivot = 16;
            }
            else if (X == 1)
            {
                pivot = 512;
            }
            int place = 0;
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    place = i * 32 + j + pivot;
                    
                        pixels[place] = color;
                        continue;                                      
                }
            }
        }


        public int[] GenerateChunkImage(Vec2i chunkPos, int color, bool informOthers = false)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            var rand1 = new Random();
            BlockPos tmpPos = new BlockPos();
            Vec2i localpos = new Vec2i();
            int[] texDataTmp = new int[chunksize * chunksize];

            int colorFill = color & unchecked((int)0x80ffffff);
            
            Vec2i leftUpperCorner = new Vec2i(chunkPos.X - (chunkPos.X % 2), chunkPos.Y - (chunkPos.Y % 2));

            SavedPlotInfo[] savedPlots = new SavedPlotInfo[4];

            Vec2i tmpVec = leftUpperCorner.Copy();
            if (claims.clientDataStorage.getSavedPlot(tmpVec, out savedPlots[0]))
                GenerateChunkPart(0, 0, claims.clientDataStorage.ClientGetCityColor(savedPlots[0].cityName), ref texDataTmp);

            tmpVec.X++;
            if(claims.clientDataStorage.getSavedPlot(tmpVec, out savedPlots[1]))
                GenerateChunkPart(0, 1, claims.clientDataStorage.ClientGetCityColor(savedPlots[1].cityName), ref texDataTmp);

            tmpVec.X--;
            tmpVec.Y++;
            if(claims.clientDataStorage.getSavedPlot(tmpVec, out savedPlots[2]))
                GenerateChunkPart(1, 0, claims.clientDataStorage.ClientGetCityColor(savedPlots[2].cityName), ref texDataTmp);

            tmpVec.X++;
            if(claims.clientDataStorage.getSavedPlot(tmpVec, out savedPlots[3]))
                GenerateChunkPart(1, 1, claims.clientDataStorage.ClientGetCityColor(savedPlots[3].cityName), ref texDataTmp);

            return texDataTmp;
        }
    }
}
