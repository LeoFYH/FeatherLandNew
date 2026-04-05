#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OfficeOpenXml;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace BirdGame.Editor
{
    public class ExcelToGameConfigImporter : OdinEditorWindow
    {
        [MenuItem("Tools/Excel/导入游戏配置（Excel）")]
        public static void ShowWindow()
        {
            GetWindow<ExcelToGameConfigImporter>("游戏配置导入");
        }

        [Sirenix.OdinInspector.FilePath(ParentFolder = "Assets/Scripts/New/Editor/Excels", IncludeFileExtension = true, Extensions = ".xlsx", AbsolutePath = true)]
        public string filePath;
        
        [Button("预览数据", ButtonSizes.Large)]
        private void PreviewData()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                EditorUtility.DisplayDialog("错误", "请先选择Excel文件路径！", "确定");
                return;
            }

            // try
            // {
            var epplusType = System.Type.GetType("OfficeOpenXml.ExcelPackage, EPPlus");
            if (epplusType == null)
            {
                EditorUtility.DisplayDialog("缺少依赖",
                    "未找到EPPlus库！\n\n请下载EPPlus.dll并放到Assets/Plugins/目录下\n\n下载地址: https://www.nuget.org/packages/EPPlus/",
                    "确定");
                return;
            }

            LoadExcelData();
            EditorUtility.DisplayDialog("预览成功", "数据已加载，请查看下方预览内容，确认无误后点击\"应用\"按钮。", "确定");
            // }
            // catch (Exception e)
            // {
            //     EditorUtility.DisplayDialog("预览失败", $"加载Excel文件失败:\n{e.Message}", "确定");
            //     Debug.LogError($"加载Excel失败: {e}");
            // }
        }
        
        [Button("应用数据", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        private void ApplyData()
        {
            if (previewMapData == null || previewEggData == null || previewBirdData == null || previewDecorationData == null)
            {
                EditorUtility.DisplayDialog("错误", "请先点击\"预览数据\"按钮加载数据！", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("确认应用", "确定要将Excel数据应用到配置文件吗？\n\n注意：只会更新数值和文本数据，图片和预制体引用将保持不变。", "确定", "取消"))
            {
                return;
            }

            try
            {
                ApplyMapConfig();
                ApplyEggConfig();
                ApplyBirdConfig();
                ApplyDecorationConfig();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("应用成功", "配置数据已成功更新！", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("应用失败", $"应用数据失败:\n{e.Message}", "确定");
                Debug.LogError($"应用数据失败: {e}");
            }
        }

        #region 预览数据

        [FoldoutGroup("预览数据", Expanded = false)]
        [ShowInInspector, ReadOnly, ListDrawerSettings(ShowIndexLabels = true)]
        private List<MapPreviewItem> previewMapData;

        [FoldoutGroup("预览数据", Expanded = false)]
        [ShowInInspector, ReadOnly, ListDrawerSettings(ShowIndexLabels = true)]
        private List<EggPreviewItem> previewEggData;

        [FoldoutGroup("预览数据", Expanded = false)]
        [ShowInInspector, ReadOnly, ListDrawerSettings(ShowIndexLabels = true)]
        private List<BirdPreviewItem> previewBirdData;

        [FoldoutGroup("预览数据", Expanded = false)]
        [ShowInInspector, ReadOnly, ListDrawerSettings(ShowIndexLabels = true)]
        private List<DecorationPreviewItem> previewDecorationData;

        [Serializable]
        public class MapPreviewItem
        {
            [LabelText("场景索引")] public int sceneIndex;
            [LabelText("地图名称")] public string mapName;
            [LabelText("售价")] public int cost;
            [LabelText("UI位置X")] public float uiPositionX;
            [LabelText("UI位置Y")] public float uiPositionY;
            [LabelText("可购买")] public bool purchasable;
        }

        [Serializable]
        public class EggPreviewItem
        {
            [LabelText("场景索引")] public int sceneIndex;
            [LabelText("鸟蛋索引")] public int eggIndex;
            [LabelText("鸟蛋价格")] public int price;
            [LabelText("开出鸟数量")] public int birdCount;
            [LabelText("鸟蛋描述")] public string description;
        }

        [Serializable]
        public class BirdPreviewItem
        {
            [LabelText("场景索引")] public int sceneIndex;
            [LabelText("种类索引")] public int classIndex;
            [LabelText("鸟ID")] public int id;
            [LabelText("稀有度")] public string reality;
            [LabelText("成鸟挂机收益")] public float eraningForBig;
            [LabelText("幼鸟挂机收益")] public float eraningForSmall;
            [LabelText("成鸟出售收益")] public float priceForBig;
            [LabelText("幼鸟出售收益")] public float priceForSmall;
            [LabelText("点击收益")] public float clickEarning;
            [LabelText("五次点击收益")] public float clickEarningForFiveTimes;
            [LabelText("总经验值")] public float totalExp;
            [LabelText("吃一次经验")] public float eatExp;
            [LabelText("自动经验")] public float autoExp;
            [LabelText("描述")] public string description;
            [LabelText("栖息地")] public string habitat;
            [LabelText("能否飞行")] public bool canFly;
            [LabelText("能否横向飞行")] public bool canFlyHorizontal;
            [LabelText("飞行等待")] public bool canFlyWait;
        }

        [Serializable]
        public class DecorationPreviewItem
        {
            [LabelText("场景索引")] public int sceneIndex;
            [LabelText("装饰索引")] public int decorationIndex;
            [LabelText("名称")] public string name;
            [LabelText("价格")] public int price;
            [LabelText("描述")] public string description;
            [LabelText("大小")] public float scale;
            [LabelText("Icon大小")] public float iconScale;
            [LabelText("最大购买数量")] public int maxQuantity;
            [LabelText("是否在地面上")] public bool isGround;
            [LabelText("是否显示")] public bool isVisible;
        }

        #endregion

        #region 加载Excel数据

        private void LoadExcelData()
        {
            string fullPath = Path.GetFullPath(filePath);
            using (var package = new ExcelPackage(new FileInfo(fullPath)))
            {
                try
                {
                    LoadMapData(package);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("预览失败", $"加载Map数据失败:\n{e.Message}", "确定");
                    Debug.LogError($"加载Map数据失败: {e}");
                }
                try
                {
                    LoadEggData(package);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("预览失败", $"加载Egg数据失败:\n{e.Message}", "确定");
                    Debug.LogError($"加载Egg数据失败: {e}");
                }
                try
                {
                    LoadBirdData(package);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("预览失败", $"加载Bird数据失败:\n{e.Message}", "确定");
                    Debug.LogError($"加载Bird数据失败: {e}");
                }
                try
                {
                    LoadDecorationData(package);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("预览失败", $"加载Decoration数据失败:\n{e.Message}", "确定");
                    Debug.LogError($"加载Decoration数据失败: {e}");
                }
            }
        }

        private void LoadMapData(ExcelPackage package)
        {
            previewMapData = new List<MapPreviewItem>();
            var worksheet = package.Workbook.Worksheets["地图配置"];
            if (worksheet == null) return;

            int rowCount = worksheet.Dimension.Rows;
            for (int row = 2; row <= rowCount; row++)
            {
                if(string.IsNullOrEmpty(worksheet.Cells[row, 1].Text)) break;
                var item = new MapPreviewItem
                {
                    mapName = worksheet.Cells[row, 1].Text,
                    cost = int.Parse(worksheet.Cells[row, 2].Text),
                    uiPositionX = float.Parse(worksheet.Cells[row, 3].Text),
                    uiPositionY = float.Parse(worksheet.Cells[row, 4].Text),
                    purchasable = worksheet.Cells[row, 5].Text == "是"
                };
                previewMapData.Add(item);
            }
        }

        private void LoadEggData(ExcelPackage package)
        {
            previewEggData = new List<EggPreviewItem>();
            var worksheet = package.Workbook.Worksheets["鸟蛋配置"];
            if (worksheet == null) return;

            int rowCount = worksheet.Dimension.Rows;
            for (int row = 2; row <= rowCount; row++)
            {
                if(string.IsNullOrEmpty(worksheet.Cells[row, 1].Text)) break;
                var item = new EggPreviewItem
                {
                    sceneIndex = int.Parse(worksheet.Cells[row, 1].Text),
                    eggIndex = int.Parse(worksheet.Cells[row, 2].Text),
                    price = int.Parse(worksheet.Cells[row, 3].Text),
                    birdCount = int.Parse(worksheet.Cells[row, 4].Text),
                    description = worksheet.Cells[row, 5].Text
                };
                previewEggData.Add(item);
            }
        }

        private void LoadBirdData(ExcelPackage package)
        {
            previewBirdData = new List<BirdPreviewItem>();
            var worksheet = package.Workbook.Worksheets["鸟配置"];
            if (worksheet == null) return;

            int rowCount = worksheet.Dimension.Rows;
            for (int row = 2; row <= rowCount; row++)
            {
                if(string.IsNullOrEmpty(worksheet.Cells[row, 1].Text)) break;
                var item = new BirdPreviewItem
                {
                    sceneIndex = int.Parse(worksheet.Cells[row, 1].Text),
                    classIndex = int.Parse(worksheet.Cells[row, 2].Text),
                    id = int.Parse(worksheet.Cells[row, 3].Text),
                    reality = worksheet.Cells[row, 5].Text,
                    eraningForBig = float.Parse(worksheet.Cells[row, 6].Text),
                    eraningForSmall = float.Parse(worksheet.Cells[row, 7].Text),
                    priceForBig = float.Parse(worksheet.Cells[row, 8].Text),
                    priceForSmall = float.Parse(worksheet.Cells[row, 9].Text),
                    clickEarning = float.Parse(worksheet.Cells[row, 10].Text),
                    clickEarningForFiveTimes = float.Parse(worksheet.Cells[row, 11].Text),
                    totalExp = float.Parse(worksheet.Cells[row, 12].Text),
                    eatExp = float.Parse(worksheet.Cells[row, 13].Text),
                    autoExp = float.Parse(worksheet.Cells[row, 14].Text),
                    description = worksheet.Cells[row, 15].Text,
                    habitat = worksheet.Cells[row, 16].Text,
                    canFly = worksheet.Cells[row, 17].Text == "是",
                    canFlyHorizontal = worksheet.Cells[row, 18].Text == "是",
                    canFlyWait = worksheet.Cells[row, 19].Text == "是"
                };
                
                previewBirdData.Add(item);
            }
        }

        private void LoadDecorationData(ExcelPackage package)
        {
            previewDecorationData = new List<DecorationPreviewItem>();
            var worksheet = package.Workbook.Worksheets["装饰配置"];
            if (worksheet == null) return;

            int rowCount = worksheet.Dimension.Rows;
            for (int row = 2; row <= rowCount; row++)
            {
                if(string.IsNullOrEmpty(worksheet.Cells[row, 1].Text)) break;
                var item = new DecorationPreviewItem
                {
                    sceneIndex = int.Parse(worksheet.Cells[row, 1].Text),
                    decorationIndex = int.Parse(worksheet.Cells[row, 2].Text),
                    name = worksheet.Cells[row, 3].Text,
                    price = int.Parse(worksheet.Cells[row, 4].Text),
                    description = worksheet.Cells[row, 5].Text,
                    scale = float.Parse(worksheet.Cells[row, 6].Text),
                    iconScale = float.Parse(worksheet.Cells[row, 7].Text),
                    maxQuantity = int.Parse(worksheet.Cells[row, 8].Text),
                    isGround = worksheet.Cells[row, 9].Text == "是",
                    isVisible = worksheet.Cells[row, 10].Text == "是"
                };
                previewDecorationData.Add(item);
            }
        }

        #endregion

        #region 应用数据到配置

        private void ApplyMapConfig()
        {
            var mapConfig = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Prefabs/Config/MapConfig.asset");
            if (mapConfig == null || mapConfig.maps == null) return;

            foreach (var previewItem in previewMapData)
            {
                if (previewItem.sceneIndex >= mapConfig.maps.Length) continue;
                var map = mapConfig.maps[previewItem.sceneIndex];
                if (map == null) continue;

                map.mapName = previewItem.mapName;
                map.cost = previewItem.cost;
                map.uiPosition = new Vector2(previewItem.uiPositionX, previewItem.uiPositionY);
                map.purchasable = previewItem.purchasable;

                EditorUtility.SetDirty(mapConfig);
            }
        }

        private void ApplyEggConfig()
        {
            var shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset");
            if (shopConfig == null || shopConfig.sceneEggs == null) return;

            foreach (var previewItem in previewEggData)
            {
                if (previewItem.sceneIndex >= shopConfig.sceneEggs.Count) continue;
                var sceneEgg = shopConfig.sceneEggs[previewItem.sceneIndex];
                if (sceneEgg == null || sceneEgg.eggs == null) continue;

                if (previewItem.eggIndex >= sceneEgg.eggs.Length) continue;
                var egg = sceneEgg.eggs[previewItem.eggIndex];
                if (egg == null) continue;

                egg.price = previewItem.price;
                egg.birdCount = previewItem.birdCount;
                egg.description = previewItem.description;

                EditorUtility.SetDirty(shopConfig);
            }
        }

        private void ApplyBirdConfig()
        {
            var birdConfig = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            if (birdConfig == null || birdConfig.sceneBirds == null) return;

            foreach (var previewItem in previewBirdData)
            {
                if (previewItem.sceneIndex >= birdConfig.sceneBirds.Count) continue;
                var sceneBird = birdConfig.sceneBirds[previewItem.sceneIndex];
                if (sceneBird == null || sceneBird.birdClasses == null) continue;

                if (previewItem.classIndex >= sceneBird.birdClasses.Length) continue;
                var birdClass = sceneBird.birdClasses[previewItem.classIndex];
                if (birdClass == null || birdClass.birds == null) continue;

                var bird = birdClass.birds.FirstOrDefault(b => b != null && b.id == previewItem.id);
                if (bird == null) continue;

                bird.reality = previewItem.reality;
                bird.eraningForBig = previewItem.eraningForBig;
                bird.eraningForSmall = previewItem.eraningForSmall;
                bird.priceForBig = previewItem.priceForBig;
                bird.priceForSmall = previewItem.priceForSmall;
                bird.clickEarning = previewItem.clickEarning;
                bird.clickEarningForFiveTimes = previewItem.clickEarningForFiveTimes;
                bird.totalExp = previewItem.totalExp;
                bird.eatExp = previewItem.eatExp;
                bird.autoExp = previewItem.autoExp;
                bird.description = previewItem.description;
                bird.habitat = previewItem.habitat;
                bird.canFly = previewItem.canFly;
                bird.canFlyHorizontal = previewItem.canFlyHorizontal;
                bird.canFlyWait = previewItem.canFlyWait;

                EditorUtility.SetDirty(birdConfig);
            }
        }

        private void ApplyDecorationConfig()
        {
            var shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset");
            if (shopConfig == null || shopConfig.sceneDecorations == null) return;

            foreach (var previewItem in previewDecorationData)
            {
                if (previewItem.sceneIndex >= shopConfig.sceneDecorations.Count) continue;
                var sceneDecoration = shopConfig.sceneDecorations[previewItem.sceneIndex];
                if (sceneDecoration == null || sceneDecoration.decorations == null) continue;

                if (previewItem.decorationIndex >= sceneDecoration.decorations.Length) continue;
                var decoration = sceneDecoration.decorations[previewItem.decorationIndex];
                if (decoration == null) continue;

                decoration.name = previewItem.name;
                decoration.price = previewItem.price;
                decoration.description = previewItem.description;
                decoration.scale = previewItem.scale;
                decoration.iconScale = previewItem.iconScale;
                decoration.maxQuantity = previewItem.maxQuantity;
                decoration.isGround = previewItem.isGround;
                decoration.isVisible = previewItem.isVisible;

                EditorUtility.SetDirty(shopConfig);
            }
        }

        #endregion
    }
}
#endif