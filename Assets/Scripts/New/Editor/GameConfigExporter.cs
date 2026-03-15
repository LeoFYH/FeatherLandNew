#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OfficeOpenXml; // 需要EPPlus库

namespace BirdGame.Editor
{
    public class GameConfigExporter : EditorWindow
    {
        [MenuItem("Tools/导出游戏配置到Excel")]
        public static void ShowWindow()
        {
            GetWindow<GameConfigExporter>("游戏配置导出");
        }

        private void OnGUI()
        {
            GUILayout.Label("游戏配置导出工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("导出所有配置到Excel", GUILayout.Height(40)))
            {
                ExportAllConfigs();
            }

            GUILayout.Space(10);
            GUILayout.Label("导出内容：", EditorStyles.boldLabel);
            GUILayout.Label("• 场景对应鸟蛋种类");
            GUILayout.Label("• 每种鸟蛋开出鸟ID及概率");
            GUILayout.Label("• 鸟ID种类、种类配色");
            GUILayout.Label("• 出售收益（成鸟/幼鸟）");
            GUILayout.Label("• 成鸟挂机收益、幼鸟挂机收益");
            GUILayout.Label("• 吃几次长大");
            GUILayout.Label("• 装饰价格");
            GUILayout.Label("• 地图价格");
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("需要EPPlus库支持。请将EPPlus.dll放到Assets/Plugins/目录下", MessageType.Info);
        }

        private void ExportAllConfigs()
        {
            try
            {
                // 检查EPPlus库是否存在
                var epplusType = System.Type.GetType("OfficeOpenXml.ExcelPackage, EPPlus");
                if (epplusType == null)
                {
                    EditorUtility.DisplayDialog("缺少依赖", 
                        "未找到EPPlus库！\n\n请下载EPPlus.dll并放到Assets/Plugins/目录下\n\n下载地址: https://www.nuget.org/packages/EPPlus/", 
                        "确定");
                    return;
                }

                string savePath = EditorUtility.SaveFilePanel("保存Excel文件", "", "GameConfig", "xlsx");
                if (string.IsNullOrEmpty(savePath))
                    return;

                // 设置EPPlus许可证上下文
                //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(savePath)))
                {
                    ExportMapConfig(package);
                    ExportEggConfig(package);
                    ExportBirdConfig(package);
                    ExportDecorationConfig(package);
                    
                    package.Save();
                }

                EditorUtility.RevealInFinder(savePath);
                EditorUtility.DisplayDialog("导出成功", $"配置已导出到:\n{savePath}", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出过程中发生错误:\n{e.Message}", "确定");
                Debug.LogError($"导出配置失败: {e}");
            }
        }

        private void ExportMapConfig(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("地图配置");
            
            // 设置表头
            worksheet.Cells[1, 1].Value = "地图名称";
            worksheet.Cells[1, 2].Value = "售价";
            worksheet.Cells[1, 3].Value = "UI位置X";
            worksheet.Cells[1, 4].Value = "UI位置Y";
            worksheet.Cells[1, 5].Value = "可购买";

            // 设置表头样式
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 填充数据
            var mapConfig = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Prefabs/Config/MapConfig.asset");
            if (mapConfig != null && mapConfig.maps != null)
            {
                for (int i = 0; i < mapConfig.maps.Length; i++)
                {
                    var map = mapConfig.maps[i];
                    int row = i + 2;
                    worksheet.Cells[row, 1].Value = map.mapName;
                    worksheet.Cells[row, 2].Value = map.cost;
                    worksheet.Cells[row, 3].Value = map.uiPosition.x;
                    worksheet.Cells[row, 4].Value = map.uiPosition.y;
                    worksheet.Cells[row, 5].Value = map.purchasable ? "是" : "否";
                }
            }

            // 自动调整列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void ExportEggConfig(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("鸟蛋配置");
            
            // 设置表头
            worksheet.Cells[1, 1].Value = "场景索引";
            worksheet.Cells[1, 2].Value = "鸟蛋索引";
            worksheet.Cells[1, 3].Value = "鸟蛋价格";
            worksheet.Cells[1, 4].Value = "开出鸟数量";
            worksheet.Cells[1, 5].Value = "鸟蛋描述";

            // 设置表头样式
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 填充数据
            var shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset");
            int rowIndex = 2;
            
            if (shopConfig != null && shopConfig.sceneEggs != null)
            {
                for (int sceneIndex = 0; sceneIndex < shopConfig.sceneEggs.Count; sceneIndex++)
                {
                    var sceneEgg = shopConfig.sceneEggs[sceneIndex];
                    if (sceneEgg.eggs != null)
                    {
                        for (int eggIndex = 0; eggIndex < sceneEgg.eggs.Length; eggIndex++)
                        {
                            var egg = sceneEgg.eggs[eggIndex];
                            worksheet.Cells[rowIndex, 1].Value = sceneIndex;
                            worksheet.Cells[rowIndex, 2].Value = eggIndex;
                            worksheet.Cells[rowIndex, 3].Value = egg.price;
                            worksheet.Cells[rowIndex, 4].Value = egg.birdCount;
                            worksheet.Cells[rowIndex, 5].Value = egg.description;
                            rowIndex++;
                        }
                    }
                }
            }

            // 自动调整列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void ExportBirdConfig(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("鸟配置");
            
            // 设置表头
            string[] headers = {
                "场景索引", "种类索引", "鸟ID", "鸟名称", "稀有度",
                "成鸟挂机收益", "幼鸟挂机收益", "成鸟出售收益", "幼鸟出售收益",
                "点击收益", "五次点击收益", "总经验值", "吃一次经验", "自动经验",
                "描述", "栖息地", "能否飞行", "能否横向飞行", "飞行等待"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            // 设置表头样式
            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 填充数据
            var birdConfig = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            int rowIndex = 2;
            
            if (birdConfig != null && birdConfig.sceneBirds != null)
            {
                for (int sceneIndex = 0; sceneIndex < birdConfig.sceneBirds.Count; sceneIndex++)
                {
                    var sceneBird = birdConfig.sceneBirds[sceneIndex];
                    if (sceneBird.birdClasses != null)
                    {
                        for (int classIndex = 0; classIndex < sceneBird.birdClasses.Length; classIndex++)
                        {
                            var birdClass = sceneBird.birdClasses[classIndex];
                            if (birdClass.birds != null)
                            {
                                foreach (var bird in birdClass.birds)
                                {
                                    int col = 1;
                                    worksheet.Cells[rowIndex, col++].Value = sceneIndex;
                                    worksheet.Cells[rowIndex, col++].Value = classIndex;
                                    worksheet.Cells[rowIndex, col++].Value = bird.id;
                                    worksheet.Cells[rowIndex, col++].Value = sceneBird.birdClasses[classIndex].birdName;
                                    worksheet.Cells[rowIndex, col++].Value = bird.reality;
                                    worksheet.Cells[rowIndex, col++].Value = bird.eraningForBig;
                                    worksheet.Cells[rowIndex, col++].Value = bird.eraningForSmall;
                                    worksheet.Cells[rowIndex, col++].Value = bird.priceForBig;
                                    worksheet.Cells[rowIndex, col++].Value = bird.priceForSmall;
                                    worksheet.Cells[rowIndex, col++].Value = bird.clickEarning;
                                    worksheet.Cells[rowIndex, col++].Value = bird.clickEarningForFiveTimes;
                                    worksheet.Cells[rowIndex, col++].Value = bird.totalExp;
                                    worksheet.Cells[rowIndex, col++].Value = bird.eatExp;
                                    worksheet.Cells[rowIndex, col++].Value = bird.autoExp;
                                    worksheet.Cells[rowIndex, col++].Value = bird.description;
                                    worksheet.Cells[rowIndex, col++].Value = bird.habitat;
                                    worksheet.Cells[rowIndex, col++].Value = bird.canFly ? "是" : "否";
                                    worksheet.Cells[rowIndex, col++].Value = bird.canFlyHorizontal ? "是" : "否";
                                    worksheet.Cells[rowIndex, col++].Value = bird.canFlyWait ? "是" : "否";
                                    rowIndex++;
                                }
                            }
                        }
                    }
                }
            }

            // 自动调整列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void ExportDecorationConfig(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("装饰配置");
            
            // 设置表头
            string[] headers = {
                "场景索引", "装饰索引", "名称", "价格", "描述",
                "大小", "Icon大小", "最大购买数量", "是否在地面上", "是否显示"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            // 设置表头样式
            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 填充数据
            var shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset");
            int rowIndex = 2;
            
            if (shopConfig != null && shopConfig.sceneDecorations != null)
            {
                for (int sceneIndex = 0; sceneIndex < shopConfig.sceneDecorations.Count; sceneIndex++)
                {
                    var sceneDecoration = shopConfig.sceneDecorations[sceneIndex];
                    if (sceneDecoration.decorations != null)
                    {
                        for (int decorationIndex = 0; decorationIndex < sceneDecoration.decorations.Length; decorationIndex++)
                        {
                            var decoration = sceneDecoration.decorations[decorationIndex];
                            int col = 1;
                            worksheet.Cells[rowIndex, col++].Value = sceneIndex;
                            worksheet.Cells[rowIndex, col++].Value = decorationIndex;
                            worksheet.Cells[rowIndex, col++].Value = decoration.name;
                            worksheet.Cells[rowIndex, col++].Value = decoration.price;
                            worksheet.Cells[rowIndex, col++].Value = decoration.description;
                            worksheet.Cells[rowIndex, col++].Value = decoration.scale;
                            worksheet.Cells[rowIndex, col++].Value = decoration.iconScale;
                            worksheet.Cells[rowIndex, col++].Value = decoration.maxQuantity;
                            worksheet.Cells[rowIndex, col++].Value = decoration.isGround ? "是" : "否";
                            worksheet.Cells[rowIndex, col++].Value = decoration.isVisible ? "是" : "否";
                            rowIndex++;
                        }
                    }
                }
            }

            // 自动调整列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }
    }
}
#endif
