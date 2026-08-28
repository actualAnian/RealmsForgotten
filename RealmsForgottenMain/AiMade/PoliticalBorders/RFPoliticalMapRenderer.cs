using System;
using System.Collections.Generic;
using System.Diagnostics;
using RF_warsystem;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.Utility;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal sealed class RFPoliticalMapRenderer : IDisposable
{
    private const float OwnershipCheckInterval = 2f;
    private const float RetryInterval = 5f;
    private const float StrategicViewFadeStartRatio = 0.55f;
    private const float StrategicViewFadeEndRatio = 0.65f;
    private const float CleanStrategicViewEnterRatio = 0.82f;
    private const float CleanStrategicViewExitRatio = 0.78f;
    private const float MinimumStrategicViewDistance = 360f;
    private const float MaximumFillOpacity = 0.38f;
    private const float FillElevation = 0.22f;
    private const float BorderElevation = 0.44f;
    private const float BorderHalfWidth = 0.45f;
    private const int MaximumTrianglesPerMesh = 12000;

    // Construcao FATIADA (2026-08-26): o rebuild antigo era sincrono — ~48k
    // AddTriangle nativos numa tacada = 9,5s de frame parado (capturado pelo
    // RFHitchDetector; no zoom maximo virava "tela travada, mate o jogo").
    // Agora o build roda em passos de ate BuildBudgetMs por frame, num par de
    // entidades NOVO e invisivel (double-buffer); o mapa politico antigo continua
    // de pe ate a troca atomica no fim. CellsPerStep e so a granularidade do
    // gerador — o orcamento de tempo e quem manda.
    private const double BuildBudgetMs = 6.0;
    private const int CellsPerStep = 256;

    private readonly List<Mesh> _fillMeshes = new();
    private readonly List<Mesh> _borderMeshes = new();
    private GameEntity? _fillEntity;
    private GameEntity? _borderEntity;
    private Material? _fillMaterial;
    private Material? _borderMaterial;
    private float _ownershipCheckTimer;
    private float _retryTimer;
    private int _ownershipFingerprint;
    private uint _lastOverlayAlpha = uint.MaxValue;
    private RFPoliticalLabelsVM? _labelsVm;
    private GauntletLayer? _labelsLayer;
    private GauntletMovieIdentifier? _labelsMovie;
    private bool _cleanStrategicViewActive;
    private bool _disposed;

    // Estado do build em andamento (double-buffer).
    private IEnumerator<object?>? _buildJob;
    private GameEntity? _pendingFillEntity;
    private GameEntity? _pendingBorderEntity;
    private Material? _pendingFillMaterial;
    private Material? _pendingBorderMaterial;
    private readonly List<Mesh> _pendingFillMeshes = new();
    private readonly List<Mesh> _pendingBorderMeshes = new();
    private readonly Stopwatch _buildClock = new();

    internal MapScreen Screen { get; }

    internal RFPoliticalMapRenderer(MapScreen screen)
    {
        Screen = screen;
    }

    internal void Tick(float dt)
    {
        if (_disposed || Campaign.Current == null)
        {
            return;
        }

        UpdateOverlayVisibility();
        UpdateCleanStrategicView();

        if (_buildJob != null)
        {
            RunBuildSlice();
            return;
        }

        if (_retryTimer > 0f)
        {
            _retryTimer -= dt;
            return;
        }

        if (_fillEntity == null || _borderEntity == null)
        {
            if (CanBuildGeometry())
            {
                StartRebuild();
            }
            return;
        }

        _ownershipCheckTimer -= dt;
        if (_ownershipCheckTimer <= 0f)
        {
            _ownershipCheckTimer = OwnershipCheckInterval;
            if (RFPoliticalTerritoryBuilder.ComputeOwnershipFingerprint() != _ownershipFingerprint)
            {
                StartRebuild();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AbortBuildJob();
        SetCleanStrategicView(false);
        RemoveLabelsLayer();
        RFWarExternalIntentApi.SetPoliticalBorderProviders(null, null);
        RemoveActiveGeometry();
    }

    private void StartRebuild()
    {
        AbortBuildJob();
        _buildJob = BuildSteps().GetEnumerator();
        RunBuildSlice();
    }

    /// <summary>Avanca o build ate estourar o orcamento de tempo do frame.</summary>
    private void RunBuildSlice()
    {
        if (_buildJob == null)
        {
            return;
        }

        _buildClock.Restart();
        try
        {
            while (_buildClock.Elapsed.TotalMilliseconds < BuildBudgetMs)
            {
                if (!_buildJob.MoveNext())
                {
                    _buildJob = null;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            AbortBuildJob();
            _retryTimer = RetryInterval;
            RFLogger.Log($"[PoliticalBorders] Sliced rebuild failed; retrying later. {ex}");
        }
    }

    private void AbortBuildJob()
    {
        _buildJob = null;
        if (_pendingFillEntity != null)
        {
            _pendingFillEntity.Remove(0);
            _pendingFillEntity = null;
        }
        if (_pendingBorderEntity != null)
        {
            _pendingBorderEntity.Remove(0);
            _pendingBorderEntity = null;
        }
        _pendingFillMeshes.Clear();
        _pendingBorderMeshes.Clear();
        _pendingFillMaterial = null;
        _pendingBorderMaterial = null;
    }

    /// <summary>
    /// Passos do rebuild. Cada yield e um ponto onde o RunBuildSlice pode devolver o
    /// frame ao jogo. Sem try/catch aqui dentro (limitacao de iterator) — o catch mora
    /// no RunBuildSlice.
    /// </summary>
    private IEnumerable<object?> BuildSteps()
    {
        if (Campaign.Current?.MapSceneWrapper == null || Screen.MapScene == null)
        {
            throw new InvalidOperationException("Campaign map scene is not ready.");
        }

        // Passo 1: dados do territorio (grade de donos + alturas de canto).
        RFPoliticalTerritoryMap map = new RFPoliticalTerritoryBuilder(Campaign.Current.MapSceneWrapper).Build();
        yield return null;

        // Passo 2: buffers novos, invisiveis ate a troca.
        _pendingFillMaterial = CreateOverlayMaterial(ignoreDepth: true);
        _pendingBorderMaterial = CreateOverlayMaterial(ignoreDepth: true);
        _pendingFillEntity = CreateRootEntity("rf_political_territory_fill");
        _pendingBorderEntity = CreateRootEntity("rf_political_territory_borders");
        _pendingFillEntity.SetVisibilityExcludeParents(false);
        _pendingBorderEntity.SetVisibilityExcludeParents(false);
        yield return null;

        // Passo 3: preenchimento, fatiado por celulas.
        foreach (object? step in BuildFillMeshesSliced(map))
        {
            yield return step;
        }

        // Passo 4: bordas, fatiado por celulas.
        foreach (object? step in BuildBorderMeshesSliced(map))
        {
            yield return step;
        }

        // Passo 5: troca atomica — o mapa antigo sai, o novo entra ja completo.
        RemoveActiveGeometry();
        _fillEntity = _pendingFillEntity;
        _borderEntity = _pendingBorderEntity;
        _fillMaterial = _pendingFillMaterial;
        _borderMaterial = _pendingBorderMaterial;
        _fillMeshes.AddRange(_pendingFillMeshes);
        _borderMeshes.AddRange(_pendingBorderMeshes);
        _pendingFillEntity = null;
        _pendingBorderEntity = null;
        _pendingFillMaterial = null;
        _pendingBorderMaterial = null;
        _pendingFillMeshes.Clear();
        _pendingBorderMeshes.Clear();

        EnsureLabelsLayer();
        _labelsVm?.Rebuild(map);
        RFWarExternalIntentApi.SetPoliticalBorderProviders(map.GetAdjacency, map.GetFrontierWeight);
        _ownershipFingerprint = RFPoliticalTerritoryBuilder.ComputeOwnershipFingerprint();
        _ownershipCheckTimer = OwnershipCheckInterval;
        _retryTimer = 0f;
        UpdateOverlayVisibility(force: true);
        RFLogger.Log($"[PoliticalBorders] Territory rebuilt (sliced). fillMeshes={_fillMeshes.Count} borderMeshes={_borderMeshes.Count}");
    }

    private GameEntity CreateRootEntity(string name)
    {
        GameEntity entity = GameEntity.CreateEmpty(Screen.MapScene, true, true, true);
        entity.Name = name;
        entity.EntityFlags |= (EntityFlags)1073873408;
        return entity;
    }

    private static Material CreateOverlayMaterial(bool ignoreDepth)
    {
        Material source = Material.GetFromResource("plain_white_noshadow_alpha") ?? Material.GetDefaultMaterial();
        Material material = source.CreateCopy();
        material.SetAlphaBlendMode((Material.MBAlphaBlendMode)1);
        material.Flags |= (MaterialFlags)1048936;
        if (ignoreDepth)
        {
            material.Flags |= (MaterialFlags)2;
        }

        material.UsingSpecular = false;
        material.UsingSpecularMap = false;
        material.UsingEnvironmentMap = false;
        material.IsSunShadowReceiver = false;
        material.IsDynamicShadowReceiver = false;
        return material;
    }

    private IEnumerable<object?> BuildFillMeshesSliced(RFPoliticalTerritoryMap map)
    {
        Mesh? mesh = null;
        int triangleCount = 0;
        int cellsSinceYield = 0;
        Vec2 uv00 = new(0f, 0f);
        Vec2 uv10 = new(1f, 0f);
        Vec2 uv01 = new(0f, 1f);
        Vec2 uv11 = new(1f, 1f);

        for (int row = 0; row < map.Rows; row++)
        {
            for (int column = 0; column < map.Columns; column++)
            {
                if (++cellsSinceYield >= CellsPerStep)
                {
                    cellsSinceYield = 0;
                    yield return null;
                }

                int index = map.CellIndex(column, row);
                int owner = map.Owners[index];
                if (owner < 0 || !map.LandCells[index])
                {
                    continue;
                }

                if (mesh == null || triangleCount + 2 > MaximumTrianglesPerMesh)
                {
                    FinishMesh(mesh, _pendingFillEntity!, _pendingFillMeshes);
                    mesh = CreateMesh(_pendingFillMaterial!, 5);
                    triangleCount = 0;
                }

                uint color = map.Factions[owner].Color;
                Vec3 lowerLeft = ToGroundPoint(map.CornerPosition(column, row), map.CornerHeights[map.CornerIndex(column, row)], FillElevation);
                Vec3 lowerRight = ToGroundPoint(map.CornerPosition(column + 1, row), map.CornerHeights[map.CornerIndex(column + 1, row)], FillElevation);
                Vec3 upperLeft = ToGroundPoint(map.CornerPosition(column, row + 1), map.CornerHeights[map.CornerIndex(column, row + 1)], FillElevation);
                Vec3 upperRight = ToGroundPoint(map.CornerPosition(column + 1, row + 1), map.CornerHeights[map.CornerIndex(column + 1, row + 1)], FillElevation);

                AddTriangle(mesh, lowerLeft, lowerRight, upperRight, uv00, uv10, uv11, color);
                AddTriangle(mesh, lowerLeft, upperRight, upperLeft, uv00, uv11, uv01, color);
                triangleCount += 2;
            }
        }

        FinishMesh(mesh, _pendingFillEntity!, _pendingFillMeshes);
    }

    private IEnumerable<object?> BuildBorderMeshesSliced(RFPoliticalTerritoryMap map)
    {
        Mesh? mesh = null;
        int triangleCount = 0;
        int cellsSinceYield = 0;
        Color borderColor = new(0.012f, 0.016f, 0.021f, 0.82f);
        uint color = borderColor.ToUnsignedInteger();

        for (int row = 0; row < map.Rows; row++)
        {
            for (int column = 0; column < map.Columns; column++)
            {
                if (++cellsSinceYield >= CellsPerStep)
                {
                    cellsSinceYield = 0;
                    yield return null;
                }

                int owner = map.Owners[map.CellIndex(column, row)];
                if (owner < 0)
                {
                    continue;
                }

                if (column + 1 < map.Columns)
                {
                    int rightOwner = map.Owners[map.CellIndex(column + 1, row)];
                    if (rightOwner >= 0 && rightOwner != owner)
                    {
                        AddBorderSegment(
                            map.CornerPosition(column + 1, row),
                            map.CornerPosition(column + 1, row + 1),
                            map.CornerHeights[map.CornerIndex(column + 1, row)],
                            map.CornerHeights[map.CornerIndex(column + 1, row + 1)],
                            ref mesh,
                            ref triangleCount,
                            color);
                    }
                }

                if (row + 1 < map.Rows)
                {
                    int upperOwner = map.Owners[map.CellIndex(column, row + 1)];
                    if (upperOwner >= 0 && upperOwner != owner)
                    {
                        AddBorderSegment(
                            map.CornerPosition(column, row + 1),
                            map.CornerPosition(column + 1, row + 1),
                            map.CornerHeights[map.CornerIndex(column, row + 1)],
                            map.CornerHeights[map.CornerIndex(column + 1, row + 1)],
                            ref mesh,
                            ref triangleCount,
                            color);
                    }
                }
            }
        }

        FinishMesh(mesh, _pendingBorderEntity!, _pendingBorderMeshes);
    }

    private void AddBorderSegment(Vec2 start, Vec2 end, float startHeight, float endHeight, ref Mesh? mesh, ref int triangleCount, uint color)
    {
        if (_pendingBorderEntity == null || _pendingBorderMaterial == null)
        {
            return;
        }

        if (mesh == null || triangleCount + 2 > MaximumTrianglesPerMesh)
        {
            FinishMesh(mesh, _pendingBorderEntity, _pendingBorderMeshes);
            mesh = CreateMesh(_pendingBorderMaterial, 10);
            triangleCount = 0;
        }

        float dx = end.x - start.x;
        float dy = end.y - start.y;
        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001f)
        {
            return;
        }

        Vec2 offset = new(-dy / length * BorderHalfWidth, dx / length * BorderHalfWidth);
        Vec2 firstLeft = new(start.x + offset.x, start.y + offset.y);
        Vec2 firstRight = new(start.x - offset.x, start.y - offset.y);
        Vec2 secondLeft = new(end.x + offset.x, end.y + offset.y);
        Vec2 secondRight = new(end.x - offset.x, end.y - offset.y);

        // Alturas vem dos CANTOS ja amostrados pelo territory builder — a versao
        // antiga fazia GetTerrainHeightAndNormal nativo POR VERTICE aqui (4 por
        // segmento, milhares por rebuild). O offset lateral e de 0,45u e a borda
        // flutua 0,44u acima do chao: a diferenca de altura e imperceptivel.
        Vec2 uv00 = new(0f, 0f);
        Vec2 uv10 = new(1f, 0f);
        Vec2 uv01 = new(0f, 1f);
        Vec2 uv11 = new(1f, 1f);
        AddTriangle(mesh, ToGroundPoint(firstLeft, startHeight, BorderElevation), ToGroundPoint(firstRight, startHeight, BorderElevation), ToGroundPoint(secondRight, endHeight, BorderElevation), uv00, uv10, uv11, color);
        AddTriangle(mesh, ToGroundPoint(firstLeft, startHeight, BorderElevation), ToGroundPoint(secondRight, endHeight, BorderElevation), ToGroundPoint(secondLeft, endHeight, BorderElevation), uv00, uv11, uv01, color);
        triangleCount += 2;
    }

    private static Vec3 ToGroundPoint(Vec2 position, float height, float elevation)
    {
        return new Vec3(position, height + elevation, -1f);
    }

    private static Mesh CreateMesh(Material material, int renderOrder)
    {
        Mesh mesh = Mesh.CreateMeshWithMaterial(material);
        mesh.SetMeshRenderOrder(renderOrder);
        mesh.SetVisibilityMask((VisibilityMaskFlags)1);
        return mesh;
    }

    private static void AddTriangle(
        Mesh mesh,
        Vec3 first,
        Vec3 second,
        Vec3 third,
        Vec2 firstUv,
        Vec2 secondUv,
        Vec2 thirdUv,
        uint color)
    {
        mesh.AddTriangleWithVertexColors(first, second, third, firstUv, secondUv, thirdUv, color, color, color, UIntPtr.Zero);
    }

    private static void FinishMesh(Mesh? mesh, GameEntity entity, ICollection<Mesh> collection)
    {
        if (mesh == null || mesh.GetFaceCount() == 0)
        {
            return;
        }

        mesh.ComputeNormals();
        mesh.ComputeTangents();
        mesh.RecomputeBoundingBox();
        mesh.PreloadForRendering();
        entity.AddMesh(mesh, true);
        collection.Add(mesh);
    }

    private bool CanBuildGeometry()
    {
        if (!Screen.IsReady || Screen.MapScene == null || Screen.MapCameraView == null || Screen.SceneLayer == null)
        {
            return false;
        }

        SceneView sceneView = Screen.SceneLayer.SceneView;
        return sceneView != null && sceneView.ReadyToRender() && sceneView.CheckSceneReadyToRender();
    }

    private void UpdateOverlayVisibility(bool force = false)
    {
        if (Screen.MapCameraView == null)
        {
            return;
        }

        float maximumDistance = Math.Max(MinimumStrategicViewDistance, Campaign.MapMaximumHeight);
        float fadeStart = Math.Max(MinimumStrategicViewDistance, maximumDistance * StrategicViewFadeStartRatio);
        float fadeEnd = Math.Max(fadeStart + 1f, maximumDistance * StrategicViewFadeEndRatio);
        float t = Clamp01((Screen.MapCameraView.CameraDistance - fadeStart) / (fadeEnd - fadeStart));
        float smooth = t * t * (3f - 2f * t);
        uint alpha = (uint)Math.Round(smooth * 255f);
        if (!force && alpha == _lastOverlayAlpha)
        {
            return;
        }

        _lastOverlayAlpha = alpha;
        bool isVisible = alpha != 0;
        _fillEntity?.SetVisibilityExcludeParents(isVisible);
        _borderEntity?.SetVisibilityExcludeParents(isVisible);
        _fillEntity?.SetAlpha(smooth * MaximumFillOpacity);
        _borderEntity?.SetAlpha(smooth);
    }

    private void UpdateCleanStrategicView()
    {
        if (Screen.MapCameraView == null)
        {
            SetCleanStrategicView(false);
            return;
        }

        float maximumDistance = Math.Max(MinimumStrategicViewDistance, Campaign.MapMaximumHeight);
        float ratio = Screen.MapCameraView.CameraDistance / maximumDistance;
        bool shouldActivate = _cleanStrategicViewActive
            ? ratio >= CleanStrategicViewExitRatio
            : ratio >= CleanStrategicViewEnterRatio;
        SetCleanStrategicView(shouldActivate);

        if (shouldActivate && _labelsVm != null && Screen.MapCameraView.Camera != null)
        {
            _labelsVm.UpdateScreenPositions(Screen.MapCameraView.Camera);
        }
    }

    private void SetCleanStrategicView(bool active)
    {
        if (_cleanStrategicViewActive == active)
        {
            if (active)
            {
                RFPoliticalStrategicViewState.HideMapClutter();
            }
            return;
        }

        _cleanStrategicViewActive = active;
        RFPoliticalStrategicViewState.SetActive(active);
        if (_labelsVm != null)
        {
            _labelsVm.IsVisible = active;
        }
    }

    private void EnsureLabelsLayer()
    {
        if (_labelsLayer != null || _disposed)
        {
            return;
        }

        RFPoliticalLabelsVM viewModel = new();
        GauntletLayer layer = new("RFPoliticalKingdomLabels", 90, false);
        GauntletMovieIdentifier? movie = null;
        try
        {
            movie = layer.LoadMovie("RFPoliticalKingdomLabels", viewModel);
            Screen.AddLayer(layer);
            _labelsVm = viewModel;
            _labelsLayer = layer;
            _labelsMovie = movie;
        }
        catch
        {
            if (movie != null)
            {
                layer.ReleaseMovie(movie);
            }
            if (Screen.Layers.Contains(layer))
            {
                Screen.RemoveLayer(layer);
            }
            viewModel.OnFinalize();
            throw;
        }
    }

    private void RemoveLabelsLayer()
    {
        if (_labelsLayer != null)
        {
            if (_labelsMovie != null)
            {
                _labelsLayer.ReleaseMovie(_labelsMovie);
            }

            if (Screen.Layers.Contains(_labelsLayer))
            {
                Screen.RemoveLayer(_labelsLayer);
            }
        }

        _labelsVm?.OnFinalize();
        _labelsMovie = null;
        _labelsLayer = null;
        _labelsVm = null;
    }

    private void RemoveActiveGeometry()
    {
        if (_fillEntity != null)
        {
            _fillEntity.Remove(0);
            _fillEntity = null;
        }

        if (_borderEntity != null)
        {
            _borderEntity.Remove(0);
            _borderEntity = null;
        }

        _fillMeshes.Clear();
        _borderMeshes.Clear();
        _fillMaterial = null;
        _borderMaterial = null;
        _lastOverlayAlpha = uint.MaxValue;
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
}
