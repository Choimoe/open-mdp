using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MajdataEdit
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: dotnet run [input_path] [output_directory] [csv_path]");
                Pause();
                return;
            }

            string inputPath = args[0];
            string outputDirectory = args[1];
            string csvPath = args[2];

            // 提取数字ID（假设路径格式为 "data/niconicoボーカロイド/44_ハツヒイシンセサイサ/"）
            string directoryName = Path.GetFileName(inputPath.TrimEnd('/'));
            if (!Directory.Exists(inputPath))
            {
                Console.WriteLine($"输入路径 {inputPath} 不存在。");
                Pause();
                return;
            }

            string[] parts = directoryName.Split('_');
            if (parts.Length < 1 || !int.TryParse(parts[0], out int id))
            {
                Console.WriteLine("无法解析数字ID，请确保路径格式为 '数字ID_日文名称'。");
                Pause();
                return;
            }

            // 设置文件路径
            string filePath = Path.Combine(inputPath, "maidata.txt");
            string outputBasePath = Path.Combine(outputDirectory, $"{id}_");

            // 检查 maidata.txt 是否存在
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"未找到 {filePath} 文件。");
                Pause();
                return;
            }

            // 清空之前的 fumen 数据
            ClearFumens();

            // 读取并处理文件
            bool success = SimaiProcess.ReadData(filePath);
            if (!success)
            {
                Console.WriteLine("读取 maidata.txt 文件失败。");
                Pause();
                return;
            }

            // 列出所有可用的 level_index
            List<int> availableLevels = new List<int>();
            for (int level_index = 1; level_index <= 5; level_index++)
            {
                if (SimaiProcess.fumens.Length > level_index && !string.IsNullOrEmpty(SimaiProcess.fumens[level_index]))
                {
                    availableLevels.Add(level_index);
                }
            }

            if (availableLevels.Count == 0)
            {
                Console.WriteLine("未找到任何可用的 level_index。");
                Pause();
                return;
            }

            // 为每个可用的 level_index 生成 JSON 文件
            foreach (var level in availableLevels)
            {
                // 处理选定的 level_index
                string SetRawFumenText = SimaiProcess.fumens[level];
                SimaiProcess.Serialize(SetRawFumenText);

                foreach (var note in SimaiProcess.notelist)
                {
                    note.noteList = note.getNotes();
                }

                var jsonOutput = new List<object>();

                for (int i = 0; i < SimaiProcess.notelist.Count; i++)
                {
                    var noteData = new
                    {
                        Time = SimaiProcess.notelist[i].time,
                        Notes = new List<Dictionary<string, object>>()
                    };

                    for (int j = 0; j < SimaiProcess.notelist[i].noteList.Count; j++)
                    {
                        var note = SimaiProcess.notelist[i].noteList[j];
                        var noteProperties = new Dictionary<string, object>
                        {
                            { "holdTime", note.holdTime },
                            { "isBreak", note.isBreak },
                            { "isEx", note.isEx },
                            { "isFakeRotate", note.isFakeRotate },
                            { "isForceStar", note.isForceStar },
                            { "isHanabi", note.isHanabi },
                            { "isSlideBreak", note.isSlideBreak },
                            { "isSlideNoHead", note.isSlideNoHead },
                            { "noteContent", note.noteContent ?? string.Empty },
                            { "noteType", note.noteType.ToString() },
                            { "slideStartTime", note.slideStartTime },
                            { "slideTime", note.slideTime },
                            { "startPosition", note.startPosition },
                            { "touchArea", note.touchArea }
                        };

                        noteData.Notes.Add(noteProperties);
                    }

                    jsonOutput.Add(noteData);
                }

                // 生成文件名：数字ID_等级.json
                string outputFilePath = $"{outputBasePath}{level}.json";
                string jsonString = JsonSerializer.Serialize(jsonOutput, new JsonSerializerOptions { WriteIndented = true });

                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(outputFilePath, jsonString);

                Console.WriteLine($"成功保存等级 {level} 的数据到 {outputFilePath}");

                // 获取难度名称和等级
                string difficultyName = SimaiProcess.GetDifficultyText(level);
                string levelValue = SimaiProcess.levels[level] ?? "0";

                // 写入CSV
                try
                {
                    bool csvExists = File.Exists(csvPath);
                    using (StreamWriter sw = new StreamWriter(csvPath, true, System.Text.Encoding.UTF8))
                    {
                        if (!csvExists)
                        {
                            sw.WriteLine("ID,Difficulty,Level");
                        }
                        sw.WriteLine($"{id},{difficultyName},{levelValue}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"写入CSV文件时出错: {ex.Message}");
                }
            }

            Pause();
        }

        static void ClearFumens()
        {
            for (int i = 0; i < SimaiProcess.fumens.Length; i++)
            {
                SimaiProcess.fumens[i] = null;
            }
        }

        static void Pause()
        {
            // Console.WriteLine("按任意键继续...");
            // Console.ReadLine();
        }
    }
}