using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CaliberStat
{
    class Stat
    {
        public int Battles { get; set; }
        public int Wins { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var statByOp = new Dictionary<string, Stat>();

            var folder = Path.Combine(Syroot.Windows.IO.KnownFolders.LocalAppDataLow.Path, "1CGS", "Caliber", "Replays");
            Console.WriteLine("Расположение реплеев: " + folder);
            var files = Directory.GetFiles(folder);
            Console.WriteLine("Найдено реплеев: " + files.Length);

            foreach (var filename in files)
            {
                string statJson = string.Empty;
                string stat2Json = string.Empty;

                //Console.WriteLine(filename);

                try
                {
                    using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
                    {
                        // Заголовок
                        var header = reader.ReadUInt32();

                        // Хз что за файл, пропускаем
                        if (header != 0x0000000B)
                            continue;

                        // Id, везде одинаковый
                        reader.ReadString();

                        // Стата
                        statJson = reader.ReadString();

                        //Console.WriteLine(statLength);
                        //Console.WriteLine(statJson.Length);

                        // 24 непонятных байта
                        reader.ReadBytes(24);

                        // Стата2
                        stat2Json = reader.ReadString();
                        //Console.WriteLine(stat2Json.Length);
                    }

                    JObject stat = JObject.Parse(statJson);
                    JObject stat2 = JObject.Parse(stat2Json);

                    if ((string)stat["GameMode"] == "pvpve")
                    {
                        var currentUser = stat2["PlayersData"].Single(t => (bool)t["IsCurrentUser"] == true);

                        if (!statByOp.ContainsKey((string)currentUser["CharacterCard"]["cfgId"]))
                        {
                            statByOp[(string)currentUser["CharacterCard"]["cfgId"]] = new Stat();
                        }

                        statByOp[(string)currentUser["CharacterCard"]["cfgId"]].Battles++;

                        if ((int)currentUser["TeamNumber"] == (int)stat2["WinnerTeamNumber"])
                        {
                            statByOp[(string)currentUser["CharacterCard"]["cfgId"]].Wins++;
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(filename + ": " + e.Message);
                }
            }

            Console.WriteLine("Статистика PvPvE (Фронт)\n");

            foreach (var op in statByOp.Keys)
            {
                Console.WriteLine($"Оперативник: {op}");
                Console.WriteLine($"Битв: {statByOp[op].Battles}");
                Console.WriteLine($"Побед: {statByOp[op].Wins} ({Math.Round((double)statByOp[op].Wins * 100 / statByOp[op].Battles, 2)}%)\n");
            }

            Console.WriteLine($"Всего:");
            Console.WriteLine($"Битв: {statByOp.Sum(t => t.Value.Battles)}");
            Console.WriteLine($"Побед: {statByOp.Sum(t => t.Value.Wins)} ({Math.Round((double)statByOp.Sum(t => t.Value.Wins) * 100 / statByOp.Sum(t => t.Value.Battles), 2)}%)");
        }
    }
}
