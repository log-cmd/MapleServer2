using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Maple2Storage.Types;
using MapleServer2.Data.Static;
using MapleServer2.Enums;
using MapleServer2.Packets;
using MapleServer2.Servers.Game;
using MapleServer2.Types;

namespace MapleServer2.Tools {
    public static class GameCommandActions {
        public static void Process(GameSession session, string command) {
            string[] args = command.ToLower().Split(" ", 2);
            switch (args[0]) {
                case "item":
                    ProcessItemCommand(session, args.Length > 1 ? args[1] : "");
                    break;
                case "npc":
                    ProcessNpcCommand(session, args.Length > 1 ? args[1] : "");
                    break;
                case "coord":
                    session.SendNotice(session.FieldPlayer.Coord.ToString());
                    break;
                case "move":
                    MoveCommand(session, args.Length > 1 ? args[1] : "");
                    break;
                case "dummy":
                    DummyCommand(session, args.Length > 1 ? args[1] : "", Rasen);
                    break;
                case "moji":
                    DummyCommand(session, args.Length > 1 ? args[1] : "", Moji);
                    break;
                case "logo":
                    DummyCommand(session, args.Length > 1 ? args[1] : "", Logo);
                    break;
            }
        }

        private static void DummyCommand(GameSession session, string command, Func<int,(short,short)> f)
        {
            lock (session.FieldManager.DummyList)
            {
                foreach (var dummy in session.FieldManager.DummyList)
                {
                    session.FieldManager.State.RemovePlayer(dummy.player.ObjectId);
                    session.Send(FieldPacket.RemovePlayer(dummy.player));
                    dummy.player.ObjectId = -1;
                }
                session.FieldManager.DummyList.Clear();
            }

            var aid = DateTime.Now.Ticks;

            var config = command.ToMap();
            int.TryParse(config.GetValueOrDefault("count", "1"), out var count);

            var str = config.GetValueOrDefault("char", "ぬ");
            if(str != "")
            {
                SetMoji(str);
            }


            for (int i = 0; i < count; i++)
            {
                var rasen = f(i);

                if (rasen.Item1 == short.MinValue)
                {
                    continue;
                }

                var ID = aid + i;
                var player = Player.Default(ID, ID);
                player.MapId = session.FieldManager.MapId;

                player.Equips[ItemSlot.CP] = new Item(11300497) { Uid = 11300497 + i };

                if(cdic.TryGetValue(i, out var col))
                {
                    player.Equips.Remove(ItemSlot.CP);
                    player.Equips[ItemSlot.HR] = Item.HairCol(Maple2Storage.Types.Color.Argb(0xff, col.R, col.G, col.B));
                }

                var coord = session.FieldPlayer.Coord;
                coord = CoordF.From(coord.X, coord.Y, coord.Z);

                var ifp = session.FieldManager.RequestFieldObject(player);

                session.FieldManager.State.AddPlayer(ifp);
                session.Send(FieldPacket.AddPlayer(ifp));
                session.Send(FieldObjectPacket.LoadPlayer(ifp));

                lock (session.FieldManager.DummyList)
                {
                    session.FieldManager.DummyList.Add(new FieldManager.Dummy
                    {
                        acountId = ID,
                        characterId = ID,
                        player = ifp,
                        ofsx = rasen.Item1,
                        ofsy = rasen.Item2,
                    });
                }
            }

            cdic.Clear();
        }

        static Dictionary<int, System.Drawing.Color> cdic = new Dictionary<int, System.Drawing.Color>();

        static (short,short) Logo(int i)
        {
            if (logo == null)
            {
                logo = new Bitmap(@"D:\git\MapleServer2\logo.png");
            }

            var x = i % logo.Width;
            var y = i / logo.Width;

            if (x >= logo.Width || y >= logo.Height)
            {
                return (short.MinValue, short.MinValue);
            }

            var p = logo.GetPixel(x, y);

            if(p.A < 80)
            {
                return (short.MinValue, short.MinValue);
            }

            cdic[i] = p;

            var xx = x - logo.Width / 2;
            var yy = -(y - logo.Height / 2);

            if (xx == 0 && yy == 00)
            {
                return (short.MinValue, short.MinValue);
            }

            var deg = 0;
            var rad = deg / 180 * Math.PI;
            var r = 70;
            return ((short)(r * xx * Math.Cos(rad) + r * yy * -Math.Sin(rad)),
                (short)(r * xx * Math.Sin(rad) + r * yy * Math.Cos(rad)));
        }

        static Bitmap logo = null;
        static string mojiS = "";
        static Bitmap bmp = null;
        static void SetMoji(string s)
        {
            if (bmp == null || mojiS != s)
            {
                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                bmp = new Bitmap(17, 17);
                var g = Graphics.FromImage(bmp);
                var f = new Font("MS ゴシック", 12);
                g.FillRectangle(Brushes.Black, 0, 0, bmp.Width, bmp.Height);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                g.DrawString(s, f, Brushes.White, 8, 9, stringFormat);
                f.Dispose();
                g.Dispose();
            }
        }

        static (short, short) Moji(int i)
        {
            var x = i % bmp.Width;
            var y = i / bmp.Width;

            if(x >= bmp.Width || y >= bmp.Height)
            {
                return (short.MinValue, short.MinValue);
            }

            var pixel = bmp.GetPixel(x, y);

            if(pixel.R != 0x00)
            {
                var xx = x - bmp.Width / 2;
                var yy = -(y - bmp.Height / 2);

                if(xx==0 && yy == 00)
                {
                    return (short.MinValue, short.MinValue);
                }

                var r = 50;
                return ((short)(r * xx + r * yy), (short)(-r * xx + r * yy));
            }

            return (short.MinValue, short.MinValue);
        }

        static (short, short) Rasen(int i)
        {
            short x = 100, y = 100;
            var r = 2;
            while (i > 0)
            {
                for (int j = 0; j < r; j++)
                {
                    i--;
                    y -= 100;
                    if (i == 0) return (x, y);
                }
                for (int j = 0; j < r; j++)
                {
                    i--;
                    x -= 100;
                    if (i == 0) return (x, y);

                }
                for (int j = 0; j < r; j++)
                {
                    i--;
                    y += 100;
                    if (i == 0) return (x, y);
                }
                for (int j = 0; j < r - 1; j++)
                {
                    i--;
                    x += 100;
                    if (i == 0) return (x, y);
                }
                i--;
                x += 200;
                y += 100;
                r+=2;
            }
            return (x, y);
        }

        private static void MoveCommand(GameSession session, string command)
        {
            var config = command.ToMap();

            int.TryParse(config.GetValueOrDefault("map", "-1"), out var mapId);
            var isMapSpecified = mapId > 0;
            var isCoordSpecified = TryParseCoord(config.GetValueOrDefault("coord", "invalid"), out var coord);
            var portal = MapEntityStorage.GetPortals(mapId)?.FirstOrDefault();

            if (isMapSpecified)
            {
                if(mapId == session.Player.MapId)
                {
                    session.SendNotice("Same map");
                    return;
                }

                if (isCoordSpecified)
                {
                    session.Player.Coord = coord;
                }
                else if(portal != null)
                {
                    session.Player.Coord = portal.Coord.ToFloat();
                }
                else
                {
                    session.SendNotice("Specify map(has portal) or coord");
                    return;
                }
                session.Player.MapId = mapId;
                session.Send(FieldPacket.RequestEnter(session.FieldPlayer));
                return;
            }

            session.SendNotice("Specify map or coord");
        }

        // Example: "item id:20000027"
        private static void ProcessItemCommand(GameSession session, string command) {
            Dictionary<string, string> config = command.ToMap();
            int.TryParse(config.GetValueOrDefault("id", "20000027"), out int itemId);
            if (!ItemMetadataStorage.IsValid(itemId)) {
                session.SendNotice("Invalid item: " + itemId);
                return;
            }

            // Add some bonus attributes to equips and pets
            var stats = new ItemStats();
            if (ItemMetadataStorage.GetTab(itemId) == InventoryTab.Gear
                    || ItemMetadataStorage.GetTab(itemId) == InventoryTab.Pets) {
                var rng = new Random();
                stats.BonusAttributes.Add(ItemStat.Of((ItemAttribute) rng.Next(35), 0.01f));
                stats.BonusAttributes.Add(ItemStat.Of((ItemAttribute) rng.Next(35), 0.01f));
            }

            var item = new Item(itemId) {
                Uid = Environment.TickCount64,
                CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                TransferFlag = TransferFlag.Splitable | TransferFlag.Tradeable,
                Stats = stats
            };
            int.TryParse(config.GetValueOrDefault("rarity", "5"), out item.Rarity);
            int.TryParse(config.GetValueOrDefault("amount", "1"), out item.Amount);

            // Simulate looting item
            if (session.Inventory.Add(item)) {
                session.Send(ItemInventoryPacket.Add(item));
                session.Send(ItemInventoryPacket.MarkItemNew(item));
            }
        }

        private static void ProcessNpcCommand(GameSession session, string command) {
            Dictionary<string, string> config = command.ToMap();
            int.TryParse(config.GetValueOrDefault("id", "11003146"), out int npcId);
            var npc = new Npc(npcId);
            byte.TryParse(config.GetValueOrDefault("ani", "-1"), out npc.Animation);
            short.TryParse(config.GetValueOrDefault("dir", "2700"), out npc.Rotation);

            IFieldObject<Npc> fieldNpc = session.FieldManager.RequestFieldObject(npc);
            if (TryParseCoord(config.GetValueOrDefault("coord", ""), out CoordF coord)) {
                fieldNpc.Coord = coord;
            } else {
                fieldNpc.Coord = session.FieldPlayer.Coord;
            }

            session.FieldManager.AddTestNpc(fieldNpc);
        }

        private static Dictionary<string, string> ToMap(this string command) {
            string[] args = command.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);

            var map = new Dictionary<string, string>();
            foreach (string arg in args) {
                string[] entry = arg.Split(new[] {':', '='}, StringSplitOptions.RemoveEmptyEntries);
                if (entry.Length != 2) {
                    Console.WriteLine($"Invalid map entry: \"{arg}\" was ignored.");
                    continue;
                }

                map[entry[0]] = entry[1];
            }

            return map;
        }

        private static bool TryParseCoord(string s, out CoordF result) {
            string[] values = s.Split(",");
            if (values.Length == 3 && float.TryParse(values[0], out float x)
                                   && float.TryParse(values[1], out float y)
                                   && float.TryParse(values[2], out float z)) {
                result = CoordF.From(x, y, z);
                return true;
            }

            result = default;
            return false;
        }
    }
}