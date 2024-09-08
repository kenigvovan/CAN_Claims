﻿using Cairo;
using caneconomy.src.harmony;
using claims.src.auxialiry;
using claims.src.claimsext.map;
using claims.src.events;
using claims.src.part;
using claims.src.part.structure;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods.NoObf;

namespace claims.src.harmony
{
    [HarmonyPatch]
    public class harmonyPatches
    {
        public static bool Prefix_tryAccess(Vintagestory.Common.WorldMap __instance, IPlayer player, BlockPos pos, EnumBlockAccessFlags accessFlag, ref bool __result)
        {
            string claimant;
            EnumWorldAccessResponse enumWorldAccessResponse = __instance.TestBlockAccess(player, new BlockSelection
            {
                Position = pos
            }, accessFlag, out claimant);

            if(claimant == null)
            {
                claimant = "not specified";
            }
            if (enumWorldAccessResponse == EnumWorldAccessResponse.Granted)
            {
                __result = true;
                return false;
            }

            if (player != null)
            {
                string text = "noprivilege-" + ((accessFlag == EnumBlockAccessFlags.Use) ? "use" : "buildbreak") + "-" + enumWorldAccessResponse.ToString().ToLowerInvariant();
                string text2 = claimant;
                if (claimant.StartsWithOrdinal("custommessage-"))
                {
                    text = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
                }

                if (__instance.World.Side == EnumAppSide.Server)
                {
                    (player as IServerPlayer).SendIngameError(text, null, text2);
                }
                else
                {
                    ((__instance.World as ClientMain).Api as ICoreClientAPI).TriggerIngameError(__instance, text, Lang.Get("ingameerror-" + text, claimant));
                }

                player?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                __instance.World.BlockAccessor.MarkBlockEntityDirty(pos);
                __instance.World.BlockAccessor.MarkBlockDirty(pos);
            }
            __result = false;
            return false;
        }

        public static bool Prefix_On_ReceiveDamage(Vintagestory.API.Common.Entities.Entity __instance, DamageSource damageSource, float damage
            , ref bool __result)
        {
            if(__instance.Api.Side == EnumAppSide.Client)
            {
                return true;
            }
            //No source entity, TODO - probably need to add check on fire damage or smth.           
            if (damageSource.SourceEntity == null)
            {
                return true;
            }

            if (__instance is EntityPlayer) { 
                
                if(damageSource.SourceEntity is EntityPlayer)
                {                
                    if(claims.config.PVP_DURING_PART_OF_THE_DAY && Settings.isPvpTime())
                    {
                        __result = true;
                        return false;
                    }
                    __result = OnPVP.canPVPAttackHere((__instance as EntityPlayer).Player as IServerPlayer, (damageSource.SourceEntity as EntityPlayer).Player as IServerPlayer);
                    return false;
                }
                else if((damageSource.SourceEntity is EntityProjectile) && (damageSource.SourceEntity as EntityProjectile).FiredBy is EntityPlayer)
                {
                    if (claims.config.PVP_DURING_PART_OF_THE_DAY && Settings.isPvpTime())
                    {
                        __result = true;
                        return false;
                    }
                    __result = OnPVP.canPVPAttackHere(((damageSource.SourceEntity as EntityProjectile).FiredBy as EntityPlayer).Player as IServerPlayer, ((damageSource.SourceEntity as EntityProjectile).FiredBy as EntityPlayer).Player as IServerPlayer);
                    return false;
                }                              
            }
            if(damageSource.SourceEntity is EntityPlayer)
            {
                __result = EntityDamageHandler.canAttackEntity((damageSource.SourceEntity as EntityPlayer).Player as IServerPlayer, __instance) ||
                   !Settings.protectedAnimals.Contains(__instance.GetName());
                return false;
            }

            //The origin method will run
            return true;
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(Vintagestory.GameContent.BEBehaviorBurning), "TrySpreadTo")]*/
        public static bool Prefix_On_TrySpreadFireAllDirs(BlockPos pos, Vintagestory.GameContent.BEBehaviorBurning __instance)
        {
            WorldInfo worldInfo = claims.dataStorage.getWorldInfo();
            claims.dataStorage.getPlot(PlotPosition.fromBlockPos(pos), out Plot tmp);

            if (tmp == null)
            {
                return true;               
            }
            if (worldInfo.fireEverywhere)
            {
                return true;
            }
            //claims.dataStorage.getPlot(new ChunkLocation(pos), out )
            if (!worldInfo.fireForbidden && tmp.getPermsHandler().fireFlag)
            {
                return true;
            }
            BlockEntity be = __instance.Api.World.BlockAccessor.GetBlockEntity(__instance.FirePos.DownCopy(1));
            if (!(be is BlockEntityPitKiln))
            {
                __instance.KillFire(false);
                return false;
            }

            return false;
        }

        public static bool Prefix_nearToClaimedLand(Vintagestory.GameContent.BlockEntityBomb __instance, ref bool __result)
        {

            Plot tb;
            int tmpX = __instance.Pos.X;
            int tmpZ = __instance.Pos.Z;
            bool blastEV = claims.dataStorage.getWorldInfo().blastEverywhere;
            if (blastEV)
            {
                __result = false;
                return false;
            }
            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {

                     claims.dataStorage.getPlot(PlotPosition.fromXZ((int)(tmpX + (i * __instance.BlastRadius)),
                                                                           (int)(tmpZ + (j * __instance.BlastRadius))), out tb);
                    if (tb == null)
                    { 
                        continue;
                    }

                    if ((tb.getPermsHandler().blastFlag || tb.getCity().getPermsHandler().blastFlag))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
        public static bool Prefix_HandleCommand(Vintagestory.Server.ServerMain __instance, string commandName, IServerPlayer player, string args, Action<TextCommandResult> onCommandComplete)
        {
            claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if (playerInfo == null)
            {
                return true;
            }
            claims.dataStorage.getPlot(PlotPosition.fromEntityyPos(player.Entity.ServerPos), out Plot tmpPlot);
            if (playerInfo.isPrisoned() && playerInfo.PrisonedIn.getPlot().Equals(tmpPlot))
            {
                if (Settings.blockedCommandsForPrison.Contains(commandName) || (args.Length > 0 && Settings.blockedCommandsForPrison.Contains(args.Split(' ')[0])))
                {
                    return false;
                }
            }
            if (commandName == "land")
            {
                if (!player.Role.Code.Equals("admin"))
                {
                    player.SendMessage(0, "This command is blocked by a mod.", EnumChatType.Notification);
                    return false;
                }
                else
                {
                    return true;
                }
                
            }
            else { return true; }
        }

        public static bool Prefix_TrySpreadHorizontal(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid __instance, Block ourblock, Block ourSolid, IWorldAccessor world, BlockPos pos)
        {
            claims.dataStorage.getPlot(PlotPosition.fromBlockPos(pos), out Plot source);
            Plot dest;
            foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
            {
                claims.dataStorage.getPlot(PlotPosition.fromBlockPos(pos.AddCopy(facing)), out dest);
                if (dest == null || (dest == null && source == null) || (source != null && dest.hasCity() && source.hasCity() && dest.getCity().Equals(source.getCity())))
                {
                    MethodInfo dynMethod = __instance.GetType().GetMethod("TrySpreadIntoBlock",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                    dynMethod.Invoke(__instance, new object[] { ourblock, ourSolid, pos,  pos.AddCopy(facing), facing,  world });
                }
            }
            return false;
        }


        public static void Postfix_FindDownwardPaths(Vintagestory.GameContent.BlockBehaviorFiniteSpreadingLiquid __instance, IWorldAccessor world, BlockPos pos, Block ourBlock,
            List<PosAndDist> __result)
        {
            claims.dataStorage.getPlot(PlotPosition.fromBlockPos(pos), out Plot source);
            Plot dest;
            foreach (var it in new List<PosAndDist>(__result))
            {
                claims.dataStorage.getPlot(PlotPosition.fromBlockPos(it.pos), out dest);
                if (dest == null)
                {
                    continue;
                }
                else if (source != null)
                {
                    if((source.hasCity() && source.getCity().Equals(dest.getCity())))
                    {
                        continue;
                    }
                }              
                else
                {
                    __result.Remove(it);
                }
            }
        }

        public static bool Prefix_OnFallToGround(Vintagestory.GameContent.EntityBlockFalling __instance, double motionY, ref bool ___nowImpacted, ref int ___lingerTicks, ref bool ___fallHandled, ref bool ___canFallSideways, ref Vec3f ___fallMotion, ref float ___impactDamageMul)
        {

            if (___fallHandled) return false;

            BlockPos pos = __instance.SidedPos.AsBlockPos;
            BlockPos finalPos = __instance.ServerPos.AsBlockPos;
            Block block = null;
            
            if (__instance.Api.Side == EnumAppSide.Server)
            {
                block = __instance.World.BlockAccessor.GetBlock(finalPos);

                if (block.OnFallOnto(__instance.World, finalPos, __instance.Block, __instance.blockEntityAttributes))
                {
                    ___lingerTicks = 3;
                    ___fallHandled = true;
                    return false;
                }
            }

            if (___canFallSideways)
            {
                claims.dataStorage.getPlot(PlotPosition.fromEntityyPos(__instance.ServerPos), out Plot source);
                Plot dest;
                for (int i = 0; i < 4; i++)
                {
                    BlockFacing facing = BlockFacing.HORIZONTALS[i];
                    if (
                        __instance.World.BlockAccessor.GetBlock(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z).Replaceable >= 6000 &&
                        __instance.World.BlockAccessor.GetBlock(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y - 1, pos.Z + facing.Normali.Z).Replaceable >= 6000)
                    {

                        //Only def from wild chunk to city's chunk. Two cities back to back is problem of config.
                        claims.dataStorage.getPlot(PlotPosition.fromXZ(pos.X + facing.Normali.X, pos.Z + facing.Normali.Z), out dest);
                        if (source == null && dest != null)
                        {
                            continue;
                        }
                        claims.dataStorage.getPlot(PlotPosition.fromXZ(pos.X + facing.Normali.X, pos.Z + facing.Normali.Z), out dest);
                        if (source == null && dest != null)
                        {
                            continue;
                        }

                        if (__instance.Api.Side == EnumAppSide.Server)
                        {
                            __instance.SidedPos.X += facing.Normali.X;
                            __instance.SidedPos.Y += facing.Normali.Y;
                            __instance.SidedPos.Z += facing.Normali.Z;
                        }
                        ___fallMotion.X = facing.Normalf.X;
                        ___fallMotion.X = 0;
                        ___fallMotion.X = facing.Normalf.Z;
                        return false;
                    }
                }
            }

            ___nowImpacted = true;

            Block blockAtFinalPos = __instance.World.BlockAccessor.GetBlock(finalPos);

            if (__instance.Api.Side == EnumAppSide.Server)
            {
                if (!block.IsReplacableBy(__instance.Block))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        BlockFacing facing = BlockFacing.HORIZONTALS[i];
                        block = __instance.World.BlockAccessor.GetBlock(finalPos.X + facing.Normali.X, finalPos.Y + facing.Normali.Y, finalPos.Z + facing.Normali.Z);

                        if (block.Replaceable >= 6000)
                        {
                            finalPos.X += facing.Normali.X;
                            finalPos.Y += facing.Normali.Y;
                            finalPos.Z += facing.Normali.Z;
                            break;
                        }
                    }
                }

                if (block.IsReplacableBy(__instance.Block))
                {
                    if (!block.IsLiquid() || __instance.Block.BlockMaterial != EnumBlockMaterial.Snow)
                    {
                        MethodInfo dynMethod = __instance.GetType().GetMethod("UpdateBlock",
                         BindingFlags.NonPublic | BindingFlags.Instance);
                        dynMethod.Invoke(__instance, new object[] { false, finalPos });
                    }

                    (__instance.Api as ICoreServerAPI).Network.BroadcastEntityPacket(__instance.EntityId, 1234);
                }
                else
                {
                    // Space is occupied by maybe a torch or some other block we shouldn't replace
                    MethodInfo dynMethod = __instance.GetType().GetMethod("DropItems",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                    dynMethod.Invoke(__instance, new object[] { finalPos });
                }

                if (___impactDamageMul > 0)
                {
                    Entity[] entities = __instance.World.GetEntitiesInsideCuboid(finalPos, finalPos.AddCopy(1, 1, 1), (e) => !(e is EntityBlockFalling));
                    bool didhit = false;
                    foreach (var entity in entities)
                    {
                        bool nowhit = entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, Type = EnumDamageType.Crushing, SourceBlock = __instance.Block, SourcePos = finalPos.ToVec3d() }, 18 * (float)Math.Abs(motionY) * ___impactDamageMul);
                        if (nowhit && !didhit)
                        {
                            didhit = nowhit;
                            __instance.Api.World.PlaySoundAt(__instance.Block.Sounds.Break, entity);
                        }
                    }
                }
            }
            ___lingerTicks = 50;
            ___fallHandled = true;
            return false;
        }

        public static void BaseMethodDummy(Vintagestory.GameContent.BlockTorch instance, IWorldAccessor world,
                                                                                               Entity byEntity,
                                                                                               Entity attackedEntity,
                                                                                               ItemSlot itemslot)
        {  return; }

        
        public static bool Prefix_OnHeldInteractStart(Vintagestory.GameContent.ItemPlumbAndSquare __instance,
            ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return false;
            }
            if (blockSel == null)
            {
                return false;
            }            
            return OnBlockAction.canBlockDestroy((byEntity as EntityPlayer).Player as IServerPlayer, blockSel, out string claimant);
        }

        public static bool Prefix_BlockEntityBarrel_OnReceivedClientPacket(Vintagestory.GameContent.BlockEntityBarrel __instance,
            IPlayer player, int packetid, byte[] data)
        {

            Block b1 = player.Entity.World.BlockAccessor.GetBlock(__instance.Pos);
            if (OnBlockAction.canBlockUse(player as IServerPlayer, __instance.Pos.ToVec3d()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool ReviveReplace(ServerSystemEntitySimulation __instance, IServerPlayer plr)
        {
            if(claims.dataStorage.getPlayerByUid(plr.PlayerUID, out PlayerInfo playerInfo))
            {
                if(playerInfo.hasCity())
                {
                    City city = playerInfo.City;
                    Vec3i ePos = plr.Entity.Pos.XYZ.AsVec3i;
                    if (city.HasTempleRespawnPoints())
                    {
                        var nearestP = 0;
                        Vec3i bestP = city.TempleRespawnPoints.First().Value;
                        foreach (var rPoint in city.TempleRespawnPoints)
                        {
                            var tmp = rPoint.Value.DistanceTo(ePos);
                            if(tmp <= nearestP)
                            {
                                bestP = rPoint.Value;
                                nearestP = (int)tmp;
                            }
                        }
                        var cpos = plr.Entity.Pos.Copy();
                        cpos.X = bestP.X + 0.5;
                        cpos.Y = bestP.Y + 2.5;
                        cpos.Z = bestP.Z + 1.5;
                        plr.Entity.TeleportTo(cpos, (Action)(() =>
                        {
                            plr.Entity.Revive();
                            
                            MethodInfo dynMethod = __instance.GetType().GetMethod("SendEntityAttributeUpdates",
                             BindingFlags.NonPublic | BindingFlags.Instance);
                            dynMethod.Invoke(__instance, new object[] { });
                            claims.sapi.World.RegisterCallback((dt) => { Particles.PlayerRespawnParticles(plr.Entity.Pos.XYZ); }, 1000);
                        }));
                        return true;
                    }
                }
            }
            return false;
        }
        public static IEnumerable<CodeInstruction> Transpiler_ComposeSlotOverlays_Add_Socket_Overlays_Not_Draw_ItemDamage(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);
            var proxyMethod = AccessTools.Method(typeof(harmonyPatches), "ReviveReplace");
            Label returnLabelNotResurectedByCity = il.DefineLabel();
            for (int i = 0; i < codes.Count; i++)
            {

                if (!found &&
                        codes[i].opcode == OpCodes.Ldloc_0 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Stfld)
                {

                    //push this on stack
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    //push this on stack again to get plr
                    yield return new CodeInstruction(OpCodes.Ldloc_0);

                    Type[] nestedTypes = typeof(ServerSystemEntitySimulation).GetNestedTypes(BindingFlags.Static |
                                      BindingFlags.Instance |
                                      BindingFlags.Public |
                                      BindingFlags.NonPublic);
                    var c2 = AccessTools.Field(nestedTypes[4], "player");
                    yield return new CodeInstruction(OpCodes.Ldfld, c2);
                    yield return new CodeInstruction(OpCodes.Call, proxyMethod);                   
                    yield return new CodeInstruction(OpCodes.Brfalse_S, returnLabelNotResurectedByCity);
                    yield return new CodeInstruction(OpCodes.Ret);
                    codes[i].labels.Add(returnLabelNotResurectedByCity);
                    found = true;
                }
                yield return codes[i];
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler_ServerSystemBlockSimulation_HandleBlockPlaceOrBreak(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);
            var proxyMethod = AccessTools.Method(typeof(harmonyPatches), "TestBlockAccess_Replacement");
            Label return97Lable = il.DefineLabel();
            codes[49].labels.Clear();
            for (int i = 0; i < codes.Count; i++)
            {
                if (!found &&
                        codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldfld && codes[i - 1].opcode == OpCodes.Ret)
                {
                    //codes[i-2].labels.Clear();
                    codes[i-2].operand = return97Lable;
                    i += 8;
                    
                    var c = new CodeInstruction(OpCodes.Ldarg_0);
                    c.labels.Add(return97Lable);
                    yield return c;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, proxyMethod);
                    //codes[i-2].labels.Add(return97Lable);
                    found = true;
                    continue;
                }
                yield return codes[i];
            }
        }

        public static EnumWorldAccessResponse TestBlockAccess_Replacement(ServerSystemBlockSimulation __instance, Packet_Client packet, ConnectedClient client)
        {
            Packet_ClientBlockPlaceOrBreak p = packet.BlockPlaceOrBreak;
            BlockSelection blockSel = new BlockSelection
            {
                DidOffset = (p.DidOffset > 0),
                Face = BlockFacing.ALLFACES[p.OnBlockFace],
                Position = new BlockPos(p.X, p.Y, p.Z),
                HitPosition = new Vec3d(CollectibleNet.DeserializeDouble(p.HitX), CollectibleNet.DeserializeDouble(p.HitY), CollectibleNet.DeserializeDouble(p.HitZ)),
                SelectionBoxIndex = p.SelectionBoxIndex
            };
            string claimant;
            EnumWorldAccessResponse resp;
            if (p.Mode == 0 || p.Mode == 2)
            {
                resp = testBlockAccessInternal_Replacement(client.Player, blockSel, EnumCANBlockAccessFlags.Break, out claimant);
            }
            else 
            {
                resp = testBlockAccessInternal_Replacement(client.Player, blockSel, EnumCANBlockAccessFlags.Build, out claimant);
            }

            /*if (__instance.World.Side == EnumAppSide.Client)
            {
                resp = (this.World.Api.Event as ClientEventAPI).TriggerTestBlockAccess(client.Player, blockSel, accessType, claimant, resp);
            }
            else*/
            //{
            //claims.sapi.Event.
            //resp = (claims.sapi.Event as ServerEventAPI).TriggerTestBlockAccess(client.Player, blockSel, accessType, claimant, resp);
            //}
            return resp;
        }

        public static EnumWorldAccessResponse testBlockAccessInternal_Replacement(IPlayer player, BlockSelection blockSel, EnumCANBlockAccessFlags accessType, out string claimant)
        {
            EnumWorldAccessResponse resp = testBlockAccess(player, accessType, out claimant);
            if (resp != EnumWorldAccessResponse.Granted)
            {
                return resp;
            }
            if(player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return EnumWorldAccessResponse.Granted;
            }
            bool canUseClaimed = player.HasPrivilege(Privilege.useblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
            
            bool canBreakClaimed = player.HasPrivilege(Privilege.buildblockseverywhere) && player.WorldData.CurrentGameMode == EnumGameMode.Creative;
            if (((ServerMain)claims.sapi.World).WorldMap.DebugClaimPrivileges)
            {
                ((ServerMain)claims.sapi.World).WorldMap.Logger.VerboseDebug("Privdebug: type: {3}, player: {0}, canUseClaimed: {1}, canBreakClaimed: {2}", new object[]
                {
                    (player != null) ? player.PlayerName : null,
                    canUseClaimed,
                    canBreakClaimed,
                    accessType
                });
            }
            ServerMain server = claims.sapi.World as ServerMain;
            if (accessType == EnumCANBlockAccessFlags.Use)
            {
                if (!canUseClaimed)
                {
                    string blockingLandClaimant;
                    claimant = (blockingLandClaimant = ((ServerMain)claims.sapi.World).WorldMap.GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.Use));
                    if (blockingLandClaimant != null)
                    {
                        return EnumWorldAccessResponse.LandClaimed;
                    }
                }
                if (server != null && !server.EventManager.TriggerCanUse(player as IServerPlayer, blockSel))
                {
                    return EnumWorldAccessResponse.DeniedByMod;
                }
                return EnumWorldAccessResponse.Granted;
            }
            else
            {
                //to be able to protect all traders
                if (!canBreakClaimed)
                {
                    string blockingLandClaimant;
                    claimant = (blockingLandClaimant = ((ServerMain)claims.sapi.World).WorldMap.GetBlockingLandClaimant(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak));
                    if (blockingLandClaimant != null)
                    {
                        return EnumWorldAccessResponse.LandClaimed;
                    }
                }
                if(blockSel.Position.X < 511814 + 10 && blockSel.Position.X > 511814 - 10)
                {
                    return EnumWorldAccessResponse.Granted;
                }


                if (server != null && !server.EventManager.TriggerCanPlaceOrBreak(player as IServerPlayer, blockSel, out claimant))
                {
                    return EnumWorldAccessResponse.Granted;
                }
                //claimant = "wilderness";
                return EnumWorldAccessResponse.DeniedByMod;
            }



        }
        private static EnumWorldAccessResponse testBlockAccess(IPlayer player, EnumCANBlockAccessFlags accessType, out string claimant)
        {
            if (player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
            {
                claimant = "custommessage-inspectatormode";
                return EnumWorldAccessResponse.InSpectatorMode;
            }
            if (!player.Entity.Alive)
            {
                claimant = "custommessage-dead";
                return EnumWorldAccessResponse.PlayerDead;
            }
            if (accessType == EnumCANBlockAccessFlags.Build || accessType == EnumCANBlockAccessFlags.Break)
            {
                if (player.WorldData.CurrentGameMode == EnumGameMode.Guest)
                {
                    claimant = "custommessage-inguestmode";
                    return EnumWorldAccessResponse.InGuestMode;
                }
                if (!player.HasPrivilege(Privilege.buildblocks))
                {
                    claimant = "custommessage-nobuildprivilege";
                    return EnumWorldAccessResponse.NoPrivilege;
                }
                claimant = null;
                return EnumWorldAccessResponse.Granted;
            }
            else
            {
                if (!player.HasPrivilege(Privilege.useblock))
                {
                    claimant = "custommessage-nouseprivilege";
                    return EnumWorldAccessResponse.NoPrivilege;
                }
                claimant = null;
                return EnumWorldAccessResponse.Granted;
            }
        }

        public enum EnumCANBlockAccessFlags
        {
            None = 0,
            Build = 1,
            Use = 2,
            Break = 3
        }
        public static IEnumerable<CodeInstruction> Transpiler_BlockBehaviorLadder_TryCollectLowest(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);
            Label returnLabelReplaced = il.DefineLabel();
            codes[49].labels.Clear();
            for (int i = 0; i < codes.Count; i++)
            {
                if (!found &&
                        codes[i].opcode == OpCodes.Ldarg_2 && codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 2].opcode == OpCodes.Ldarg_1 && codes[i - 1].opcode == OpCodes.Ret)
                {
                    codes[i - 3].operand = null;
                    codes[i - 3].operand = returnLabelReplaced;
                    codes[i + 6].operand = null;
                    i += 8;
                    found = true;
                    codes[i + 1].labels.Clear();
                    codes[i + 1].labels.Add(returnLabelReplaced);
                    continue;
                }
                yield return codes[i];
            }
        }

    }
}
