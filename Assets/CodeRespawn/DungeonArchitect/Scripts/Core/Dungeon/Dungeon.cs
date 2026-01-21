//$ Copyright 2015-22, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using DungeonArchitect.Graphs;
using DungeonArchitect.Themeing;
using DungeonArchitect.SpatialConstraints;

namespace DungeonArchitect
{
    /// <summary>
    /// The main dungeon behavior that manages the creation and destruction of dungeons
    /// </summary>
    [ExecuteInEditMode]
    public class Dungeon : MonoBehaviour
    {
        /// <summary>
        /// List of themes assigned to this dungeon
        /// </summary>
        public List<Graph> dungeonThemes;

        /// <summary>
        /// Draw debug data
        /// </summary>
        public bool debugDraw = false;

        /// <summary>
        /// Automatically build the dungeon on Start
        /// </summary>
        public bool buildOnStart = false;

        public bool randomizeSeedOnStart = false;
        
        private DungeonConfig _config;
        private PooledDungeonSceneProvider _sceneProvider;
        private DungeonBuilder _dungeonBuilder;
        private DungeonModel _dungeonModel;
        private DungeonSceneObjectSpawner _objectSpawner;

        /// <summary>
        /// Active model used by the dungeon
        /// </summary>
        public DungeonModel ActiveModel
        {
            get
            {
                if (_dungeonModel == null) _dungeonModel = GetComponent<DungeonModel>();
                return _dungeonModel;
            }
        }

        /// <summary>
        /// Flag to check if the layout has been built.  
        /// This is used to quickly reapply the theme after the theme graph has been modified,
        /// without rebuilding the layout, if it has already been built
        /// </summary>
        public bool IsLayoutBuilt
        {
            get
            {
                if (_dungeonBuilder == null) return false;
                return _dungeonBuilder.IsLayoutBuilt;
            }
        }

        //[SerializeField]
        private LevelMarkerList _markers = new();
        public LevelMarkerList Markers => _markers;


        /// <summary>
        /// Flag to rebuild the dungeon. Set this to true if you want to rebuild it in the next update
        /// </summary>
        private bool _requestedRebuild = false;

        public DungeonConfig Config
        {
            get
            {
                if (_config == null) _config = GetComponent<DungeonConfig>();
                return _config;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (randomizeSeedOnStart) RandomizeSeed();
            if (buildOnStart) Build();
        }


        private void Initialize()
        {
            if (_config == null) _config = GetComponent<DungeonConfig>();

            if (_sceneProvider == null) _sceneProvider = GetComponent<PooledDungeonSceneProvider>();

            if (_dungeonBuilder == null) _dungeonBuilder = GetComponent<DungeonBuilder>();

            if (_dungeonModel == null) _dungeonModel = GetComponent<DungeonModel>();
        }

        public List<DungeonThemeData> GetThemeAssets()
        {
            var themes = new List<DungeonThemeData>();
            foreach (var themeGraph in dungeonThemes)
            {
                var theme = new DungeonThemeData();
                theme.BuildFromGraph(themeGraph);
                themes.Add(theme);
            }

            return themes;
        }


        /// <summary>
        /// Builds the complete dungeon (layout and visual phase)
        /// </summary>
        public void Build()
        {
            Build(new RuntimeDungeonSceneObjectInstantiator());
        }


        /// <summary>
        /// Set the seed of the dungeon.  The seed determines the layout of the dungeon.
        /// Change this number to get a different layout.
        /// Use the same seed to get the same dungeon  
        /// </summary>
        /// <param name="seed"></param>
        public void SetSeed(int seed)
        {
            Config.Seed = (uint)seed;
        }

        /// <summary>
        /// Randomizes the seed to generate a new dungeon layout
        /// </summary>
        public void RandomizeSeed()
        {
            SetSeed(Mathf.RoundToInt(Random.value * int.MaxValue));
        }

        /// <summary>
        /// Randomizes the seed to generate a new dungeon layout
        /// </summary>
        public void RandomizeSeed(System.Random randomStream)
        {
            SetSeed(Mathf.RoundToInt(randomStream.NextFloat() * int.MaxValue));
        }

        public void Build(IDungeonSceneObjectInstantiator objectInstantiator)
        {
            if (_dungeonBuilder.DestroyDungeonOnRebuild()) DestroyDungeon();

            NotifyPreBuild();

            Initialize();
            _dungeonModel.ResetModel();

            _dungeonBuilder.BuildDungeon(_config, _dungeonModel);
            _markers = _dungeonBuilder.Markers;

            NotifyPostLayoutBuild();

            if (_dungeonBuilder.IsThemingSupported())
                ReapplyTheme(objectInstantiator);
            else
                _dungeonBuilder.BuildNonThemedDungeon(_sceneProvider, objectInstantiator);

            // Build the navigation
            var navigation = GetComponent<DungeonRuntimeNavigation>();
            if (navigation != null) navigation.BuildNavMesh();

            NotifyPostBuild();
        }

        /// <summary>
        /// Runs the theming engine over the existing layout to rebuild the game objects from the theme file.  
        /// The layout is not built in this stage
        /// </summary>
        public void ReapplyTheme(IDungeonSceneObjectInstantiator objectInstantiator)
        {
            if (!_dungeonBuilder.IsThemingSupported()) return;

            // Emit markers defined by this builder
            _dungeonBuilder.EmitMarkers();

            // Emit markers defined by the users (by attaching implementation of DungeonMarkerEmitter behaviors)
            _dungeonBuilder.EmitCustomMarkers();

            NotifyMarkersEmitted(_dungeonBuilder.Markers);

            var themes = GetThemeAssets();
            var themeContext = CreateThemeExecutionContext(objectInstantiator);
            var themeEngine = new DungeonThemeEngine(themeContext);
            themeEngine.ApplyTheme(_dungeonBuilder.Markers, themes);
        }

        private DungeonThemeExecutionContext CreateThemeExecutionContext(
            IDungeonSceneObjectInstantiator objectInstantiator)
        {
            var context = new DungeonThemeExecutionContext
            {
                builder = _dungeonBuilder,
                config = _config,
                model = _dungeonModel,
                spatialConstraintProcessor = GetComponent<SpatialConstraintProcessor>(),
                sceneProvider = GetComponent<DungeonSceneProvider>(),
                objectInstantiator = objectInstantiator,
                spawnListeners = GetComponents<DungeonItemSpawnListener>().ToArray()
            };

            var builder = GetComponent<DungeonBuilder>();
            if (builder.asyncBuild)
            {
                var buildPosition = builder.asyncBuildStartPosition != null
                    ? builder.asyncBuildStartPosition.position
                    : Vector3.zero;
                _objectSpawner = new AsyncDungeonSceneObjectSpawner(builder.maxBuildTimePerFrame, buildPosition);
            }
            else
            {
                _objectSpawner = new SyncDungeonSceneObjectSpawner();
            }

            context.objectSpawner = _objectSpawner;

            var themeOverrides = new List<ThemeOverrideVolume>();
            var dungeon = GetComponent<Dungeon>();

            // Process the theme override volumes
            var volumes = FindObjectsOfType<ThemeOverrideVolume>();
            foreach (var volume in volumes)
            {
                if (volume.dungeon != dungeon) continue;
                themeOverrides.Add(volume);
            }

            context.themeOverrideVolumes = themeOverrides.ToArray();

            return context;
        }

        private DungeonEventListener[] GetListeners()
        {
            var listeners = GetComponents<DungeonEventListener>();

            var enabledListeners = from listener in listeners
                where listener.enabled
                select listener;

            return enabledListeners.ToArray();
        }

        private void NotifyPostLayoutBuild()
        {
            // Notify all listeners of the post build event
            foreach (var listener in GetListeners()) listener.OnPostDungeonLayoutBuild(this, ActiveModel);
        }

        private void NotifyPreBuild()
        {
            // Notify all listeners of the post build event
            foreach (var listener in GetListeners()) listener.OnPreDungeonBuild(this, ActiveModel);
        }

        private void NotifyPostBuild()
        {
            // Notify all listeners of the post build event
            foreach (var listener in GetListeners()) listener.OnPostDungeonBuild(this, ActiveModel);
        }

        private void NotifyMarkersEmitted(LevelMarkerList markers)
        {
            // Notify all listeners of the post build event
            foreach (var listener in GetListeners())
            {
                if (listener == null) continue;
                listener.OnDungeonMarkersEmitted(this, ActiveModel, markers);
            }
        }

        private void NotifyPreDungeonDestroy()
        {
            // Notify all listeners that the dungeon is destroyed
            foreach (var listener in GetListeners()) listener.OnPreDungeonDestroy(this);
        }

        private void NotifyDungeonDestroyed()
        {
            // Notify all listeners that the dungeon is destroyed
            foreach (var listener in GetListeners()) listener.OnDungeonDestroyed(this);
        }

        /// <summary>
        /// Destroys the dungeon
        /// </summary>
        public void DestroyDungeon()
        {
            NotifyPreDungeonDestroy();

            var itemList = FindObjectsOfType<DungeonSceneProviderData>();
            var dungeonItems = new List<GameObject>();
            foreach (var item in itemList)
            {
                if (item == null) continue;
                if (item.dungeon == this) dungeonItems.Add(item.gameObject);
            }

            foreach (var item in dungeonItems)
                if (Application.isPlaying)
                    Destroy(item);
                else
                    DestroyImmediate(item);

            if (_objectSpawner != null)
            {
                _objectSpawner.Destroy();
                _objectSpawner = null;
            }

            // Build the navigation
            var navigation = GetComponent<DungeonRuntimeNavigation>();
            if (navigation != null) navigation.DestroyNavMesh();

            if (_dungeonModel != null) _dungeonModel.ResetModel();

            if (_dungeonBuilder != null) _dungeonBuilder.OnDestroyed();

            NotifyDungeonDestroyed();
        }

        /// <summary>
        /// Requests the dungeon to be rebuilt in the next update phase
        /// </summary>
        public void RequestRebuild()
        {
            _requestedRebuild = true;
        }

        public virtual void Update()
        {
            if (_dungeonModel == null) return;

            if (_requestedRebuild)
            {
                _requestedRebuild = false;
                Build();
            }

            if (debugDraw) DebugDraw();

            if (_objectSpawner != null) _objectSpawner.Tick();
        }

        private void DebugDraw()
        {
            if (_dungeonBuilder != null) _dungeonBuilder.DebugDraw();
        }

        private void OnDrawGizmosSelected()
        {
            if (debugDraw && _dungeonBuilder != null) _dungeonBuilder.DebugDrawGizmos();
        }
    }
}