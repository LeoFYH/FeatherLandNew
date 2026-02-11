# Memory Optimization Guide – FeatherLand

Summary of findings from reviewing the **Assets** folder and scripts, with concrete steps to reduce memory usage.

---

## 1. What Was Already in Place

- **MemoryOptimizationSystem** – Caps environment/effect AudioSources, object pool inactive count, and calls `Resources.UnloadUnusedAssets()`.
- **PeriodicCleanupSystem** – Runs periodic cleanup (interval increased to 60s; see change below).
- **Editor tools** – **Tools → Large Texture Finder**, **Tools → 纹理优化建议 (Texture Optimization Advisor)**, **Tools** (Texture Report) for texture analysis and optimization.

---

## 2. Code Change Applied

### PeriodicCleanupSystem

- **Before:** Every 30s: `ObjectPoolSystem.ClearAll()` + `PerformFullOptimization()` + `GC.Collect()` + `WaitForPendingFinalizers()` + `UnloadUnusedAssets()`. This caused hitches and did not free asset memory (prefabs stay in `IAssetSystem.HandleDic`).
- **After:**
  - Interval set to **60 seconds**.
  - **No longer calls `ClearAll()`** – only `CleanupObjectPools()` (cap inactive instances) and `OptimizeTextures()`.
  - **No `GC.Collect()` / `WaitForPendingFinalizers()`** in the periodic loop to avoid 100–500 ms stalls.
  - Still calls `Resources.UnloadUnusedAssets()` for unused assets.

Use **full** optimization (including GC) only at scene change or pause by calling `IMemoryOptimizationSystem.PerformFullOptimization()` when appropriate.

---

## 3. Recommendations to Reduce Memory

### A. Addressables – Reduce “preload” Scope

- **PreloadEssentialAssets** (in `LoadGameCommand`) loads **every** asset with the **preload** label and keeps them in `HandleDic` for the whole session.
- Many **UI_Popups**, **Scenes**, and **Prefabs_Special** entries use the **preload** tag, so a lot of prefabs stay in memory from startup.
- **Action:** In Addressables groups, remove the **preload** label from:
  - Popups that are opened rarely (keep preload only for 1–2 most common popups if needed).
  - Scene prefabs that are not the initial scene.
  - Non-essential prefabs.
- Load these via `LoadAssetAsync` when needed and call `ReleaseAsset` when the UI is closed (IUISystem already releases panels/popups in some paths – ensure all close paths call release).

### B. Atlases – Avoid Loading All at Once

- **AtlasPreloader** loads **all** atlases with label **"Atlas"** in `Start()` and keeps them in memory.
- **Action:**
  - Split atlases into labels (e.g. `Atlas_Core`, `Atlas_Birds`, `Atlas_UI`) and preload only `Atlas_Core`; load others when entering the relevant feature or scene.
  - Or load atlases on demand and release when the screen that needs them is closed (if your UI flow allows).

### C. Textures (Largest Win in Many Projects)

- **Arts** has a very large number of PNGs; textures often dominate memory.
- **Actions:**
  1. Use **Tools → Large Texture Finder** (e.g. threshold 2048) and **Tools → 纹理优化建议** to find oversized and compressible textures.
  2. In Import Settings:
     - Set **Max Size** to 2048 (or 1024 for small sprites); avoid 4096+ unless necessary.
     - Use **Compression** (e.g. Normal Quality or ASTC/ETC2 on target platforms).
     - For **UI sprites**, disable **Generate Mip Maps**.
  3. Prefer **Sprite Atlas** for UI/birds so textures are packed and shared; you already use atlases – ensure all relevant sprites are in atlases and not duplicated as standalone textures.

### D. Audio

- **IAudioSystem** loads **AudioClips** by Addressables GUID and does **not** call `ReleaseAsset` when switching tracks or closing panels.
- **Action:** For tracks or clips that are not needed for a long time (e.g. radio track after change, BGM after closing a scene), call `IAssetSystem.ReleaseAsset(assetGUID)` when the clip is no longer referenced. Keep one or a few “current” clips in memory and release the rest.

### E. Fonts

- **Fonts** folder has multiple languages (Chinese, English, Japanese, etc.). Each SDF/asset can be heavy.
- **Action:** Load font assets per language when the user selects that language, and release previous language fonts if possible, instead of loading all at startup.

### F. Release When Closing / Changing Scene

- Ensure every **LoadAssetAsync** that loads heavy or one-off content has a matching **ReleaseAsset** when the feature is closed (popup, scene, decoration, etc.). Search for `LoadAssetAsync` and verify matching `ReleaseAsset` on close.

---

## 4. Quick Checklist

| Area              | Action |
|-------------------|--------|
| Addressables      | Remove **preload** from non-essential popups, scenes, prefabs. |
| Atlases           | Preload only core atlases; load rest on demand. |
| Textures          | Run Large Texture Finder + Texture Optimization Advisor; reduce max size, enable compression, disable mipmaps for UI. |
| Audio             | Release AudioClip via `ReleaseAsset` when track/panel is no longer used. |
| Fonts             | Load per-language and release unused language fonts. |
| Object pools      | Rely on **CleanupObjectPools** (already in use); avoid **ClearAll** in periodic cleanup. |
| Full GC           | Call **PerformFullOptimization()** only at scene change or pause, not every N seconds. |

---

## 5. Optional: Scene-Change Cleanup

To free more memory on scene load, trigger a one-off full cleanup when changing scene (e.g. in `ISceneSystem` or scene load command):

```csharp
this.GetSystem<IMemoryOptimizationSystem>().PerformFullOptimization();
```

This runs audio/pool/texture optimization and `GC.Collect()` + `UnloadUnusedAssets()` once, without causing a periodic hitch.

---

Using the above steps together should noticeably reduce memory usage while avoiding the previous periodic stutter from aggressive cleanup.
