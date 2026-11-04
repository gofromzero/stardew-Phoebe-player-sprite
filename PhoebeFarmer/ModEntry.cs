using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace PhoebeFarmer
{
    public class ModEntry : Mod
    {
        /// <summary>每张姿态表的文件名和网格（列 x 行）。走路表是 4 行：朝下、朝右、朝上、朝左；其他三行的表朝左用朝右镜像。</summary>
        private static readonly Dictionary<Sheet, (string File, int Cols, int Rows)> SheetLayout = new()
        {
            [Sheet.Walk] = ("walk.png", 4, 4),
            [Sheet.Tools] = ("tools.png", 4, 3),
            [Sheet.Melee] = ("melee.png", 4, 3),
            [Sheet.Crouch] = ("crouch.png", 4, 3),
            [Sheet.Fishing] = ("fishing.png", 3, 3),
            [Sheet.Eat] = ("eat.png", 6, 1),
            [Sheet.Misc] = ("misc.png", 4, 2)
        };

        private static ModEntry instance;
        private ModConfig config;
        private readonly Dictionary<string, Dictionary<Sheet, Texture2D>> variants = new();
        private Dictionary<Sheet, Texture2D> sheets => variants[config.Variant];

        public override void Entry(IModHelper helper)
        {
            instance = this;
            config = helper.ReadConfig<ModConfig>();
            config.Normalize();

            foreach (string variant in new[] { "are", "classic" })
            {
                var loaded = new Dictionary<Sheet, Texture2D>();
                Dictionary<string, int[][]> bounds;
                try { bounds = helper.Data.ReadJsonFile<Dictionary<string, int[][]>>($"assets/{variant}/frames.json"); }
                catch (Exception ex) { Monitor.Log($"{variant} 帧位置文件无效：{ex.Message}", LogLevel.Warn); continue; }
                if (bounds == null) { Monitor.Log($"{variant} 缺少 frames.json。", LogLevel.Warn); continue; }
                foreach (var (sheet, layout) in SheetLayout)
                {
                    string path = $"assets/{variant}/{layout.File}";
                    if (!File.Exists(Path.Combine(helper.DirectoryPath, path)))
                        continue;
                    try
                    {
                        var source = helper.ModContent.Load<Texture2D>(path);
                        string name = Path.GetFileNameWithoutExtension(layout.File);
                        loaded[sheet] = SpriteSheet.Prepare(source, layout.Cols, layout.Rows, name, bounds[name]);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"读取 {path} 失败：{ex.Message}", LogLevel.Warn);
                    }
                }
                if (loaded.ContainsKey(Sheet.Walk)) variants[variant] = loaded;
                else foreach (var texture in loaded.Values) texture.Dispose();
            }
            if (variants.Count == 0)
            {
                Monitor.Log("两款均缺少有效 walk.png，保持原版外观。", LogLevel.Error);
                return;
            }
            if (!variants.ContainsKey(config.Variant))
                config.Variant = variants.ContainsKey("are") ? "are" : "classic";

            var harmony = new Harmony(ModManifest.UniqueID);
            // 只拦截最底层的那个 draw 重载：其他重载最终都会调用它
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.draw), new[]
                    {
                    typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame), typeof(int), typeof(Rectangle),
                    typeof(Vector2), typeof(Vector2), typeof(float), typeof(int), typeof(Color),
                    typeof(float), typeof(float), typeof(Farmer)
                    }),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawPrefix)));
                // 菜单标签、地图、锻造台、多人状态栏用的小头像走的是另一个函数
                harmony.Patch(
                    AccessTools.Method(typeof(FarmerRenderer), nameof(FarmerRenderer.drawMiniPortrat)),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(MiniPortraitPrefix)));
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(ModManifest.UniqueID);
                Monitor.Log("安装角色绘制补丁失败，保持原版外观：" + ex, LogLevel.Error);
                return;
            }

            // 这些模组也会接管农夫的绘制，同时开着可能画出两个人
            foreach (string id in new[] { "PeacefulEnd.FashionSense", "Darian.WhaleFarmer" })
                if (helper.ModRegistry.IsLoaded(id))
                    Monitor.Log($"检测到 {helper.ModRegistry.Get(id).Manifest.Name}，它和菲比外观都会改农夫的绘制，可能冲突。", LogLevel.Warn);

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => lastPositions.Clear();
            foreach (var variant in variants)
            {
                var missing = new List<string>();
                foreach (var (sheet, layout) in SheetLayout)
                    if (!variant.Value.ContainsKey(sheet))
                        missing.Add(layout.File);
                Monitor.Log($"菲比 {variant.Key} 已加载，按 {config.ToggleKey} 循环切换：套装 1 → 套装 2 → 关闭。"
                    + (missing.Count > 0 ? $" 还没有的姿态图（用站立姿势代替）：{string.Join("、", missing)}" : ""), LogLevel.Info);
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || Helper.Input.IsSuppressed(e.Button)
                || e.Button != config.ToggleKey)
                return;
            Helper.Input.Suppress(e.Button);
            var next = FrameGeometry.NextAppearance(config.Enabled, config.Variant);
            if (next.Enabled && !variants.ContainsKey(next.Variant))
            {
                Game1.addHUDMessage(new HUDMessage("目标套装贴图缺失，请检查模组安装。", HUDMessage.error_type));
                return;
            }
            config.Enabled = next.Enabled;
            config.Variant = next.Variant;
            try { Helper.WriteConfig(config); }
            catch (Exception ex) { Monitor.Log("本次切换已生效，但配置保存失败：" + ex.Message, LogLevel.Warn); }
            string label = config.Variant == "are" ? "套装 1 · ARE" : "套装 2 · 经典";
            Game1.addHUDMessage(new HUDMessage(config.Enabled ? "菲比外观：" + label : "已恢复原版外观", HUDMessage.newQuest_type));
        }

        /// <summary>返回 false 表示跳过原版绘制（身体、裤子、衣服、头发、帽子、手臂都不画）。</summary>
        private static bool DrawPrefix(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame,
            Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor,
            float rotation, float scale, Farmer who)
        {
            var self = instance;
            if (self == null || !self.ShouldReplace(who))
                return true;
            try
            {
                self.DrawPhoebe(b, animationFrame, currentFrame, position, origin, layerDepth, facingDirection, overrideColor, rotation, scale, who);
                return false;
            }
            catch (Exception ex)
            {
                // 画失败就退回原版，不能让游戏崩
                self.Monitor.LogOnce("绘制菲比出错，退回原版外观：" + ex, LogLevel.Error);
                return true;
            }
        }

        /// <summary>只替换真正的玩家：人体模特等用临时 Farmer 画的东西保持原样。</summary>
        private bool ShouldReplace(Farmer who)
        {
            if (!config.Enabled || who == null || !variants.ContainsKey(config.Variant) || !sheets.ContainsKey(Sheet.Walk))
                return false;
            if (who.IsLocalPlayer)
                return true;
            return !config.OnlyLocalPlayer && Game1.getOnlineFarmers().Contains(who);
        }

        /// <summary>小头像：从朝下站立那一帧裁出头部，缩到和原版头像一样宽。</summary>
        private static bool MiniPortraitPrefix(SpriteBatch b, Vector2 position, float layerDepth, float scale, Farmer who, float alpha)
        {
            var self = instance;
            if (self == null || !self.ShouldReplace(who))
                return true;
            try
            {
                var head = new Rectangle(2, 10, 36, 20);
                float s = scale * 16f / head.Width;
                // 原版头像高 15 格，我们的头缩完会矮一点，往下挪让下巴对齐
                var pos = position + new Vector2(0f, (15f - head.Height * 16f / head.Width) * scale);
                b.Draw(self.sheets[Sheet.Walk], pos, head, Color.White * alpha, 0f, Vector2.Zero, s, SpriteEffects.None, layerDepth);
                return false;
            }
            catch (Exception ex)
            {
                self.Monitor.LogOnce("绘制菲比头像失败，退回原版：" + ex.Message, LogLevel.Error);
                return true;
            }
        }

        private void DrawPhoebe(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Vector2 position,
            Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float rotation, float scale, Farmer who)
        {
            var frame = Poses.Resolve(currentFrame, animationFrame.flip, who);
            if (frame == null || !sheets.ContainsKey(frame.Value.Sheet))
                frame = WalkFrame(animationFrame, facingDirection, who);
            var pose = frame.Value;

            var texture = sheets[pose.Sheet];
            var layout = SheetLayout[pose.Sheet];
            int w = texture.Width / layout.Cols, h = texture.Height / layout.Rows;
            var src = new Rectangle(pose.Col * w, pose.Row * h, w, h);

            position = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
            var offset = new Vector2(animationFrame.xOffset * 4, animationFrame.positionOffset * 4);

            // 游泳时只露出上半身，和原版一样往下挪 64 像素
            if (!FarmerRenderer.isDrawingForUI && who.swimming.Value)
            {
                src.Height -= 16;
                position.Y += 64f;
            }

            // 原版身体是 16x32；我们的格子水平居中、脚底对齐到同一位置
            var ourOrigin = origin + new Vector2((w - 16) / 2f, h - 32);
            var effects = pose.Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float depth = FarmerRenderer.GetLayerDepth(layerDepth, FarmerRenderer.FarmerSpriteLayers.Base);
            if (pose.Sheet == Sheet.Walk)
            {
                // Reuse the neutral upper body; only the bottom six pixels animate while walking.
                int upperHeight = Math.Min(34, src.Height);
                var upper = new Rectangle(0, src.Y, w, upperHeight);
                b.Draw(texture, position + origin + offset, upper, overrideColor, rotation, ourOrigin, 4f * scale, effects, depth);
                if (src.Height > upperHeight)
                {
                    var feet = new Rectangle(src.X, src.Y + upperHeight, w, src.Height - upperHeight);
                    b.Draw(texture, position + origin + offset, feet, overrideColor, rotation,
                        ourOrigin - new Vector2(0, upperHeight), 4f * scale, effects, depth);
                }
            }
            else
                b.Draw(texture, position + origin + offset, src, overrideColor, rotation, ourOrigin, 4f * scale, effects, depth);
        }

        /// <summary>走路/站立：走路表有单独的朝左一行，不用镜像。</summary>
        private PoseFrame WalkFrame(FarmerSprite.AnimationFrame animationFrame, int facingDirection, Farmer who)
        {
            if (facingDirection == 1 || facingDirection == 3)
                facingDirection = animationFrame.flip ? 3 : 1;
            int row = facingDirection switch { 2 => 0, 1 => 1, 0 => 2, _ => 3 };

            int col = 0;
            if (!FarmerRenderer.isDrawingForUI && (who.isMoving() || MovedRecently(who)) && !who.UsingTool)
            {
                int ms = who.running ? config.WalkFrameMs * 2 / 3 : config.WalkFrameMs;
                col = (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / ms) % 4;
            }
            return new PoseFrame(Sheet.Walk, row, col);
        }

        /// <summary>每个玩家上一次看到的坐标、所在帧、判定为“在走”的截止帧。</summary>
        private readonly Dictionary<long, (Vector2 Pos, int Tick, int MovingUntil)> lastPositions = new();

        /// <summary>
        /// isMoving() 只看方向键输入；其他模组（比如 AI 代玩）直接改坐标走路时它是 false。
        /// 这里改看坐标有没有变，留 2 帧余量，防止某一帧没挪动时闪回站立。
        /// </summary>
        private bool MovedRecently(Farmer who)
        {
            int now = Game1.ticks;
            long id = who.UniqueMultiplayerID;
            if (!lastPositions.TryGetValue(id, out var rec))
            {
                lastPositions[id] = (who.Position, now, -1);
                return false;
            }
            if (rec.Tick != now)
            {
                var delta = who.Position - rec.Pos;
                // 一帧挪了好几格是传送/切地图，不算走路
                bool walked = delta != Vector2.Zero && delta.LengthSquared() < 64f * 64f;
                rec = (who.Position, now, walked ? now + 2 : rec.MovingUntil);
                lastPositions[id] = rec;
            }
            return now <= rec.MovingUntil;
        }
    }
}
