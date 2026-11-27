using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenthium.Data;
using Zenthium.Entities;
using Zenthium.Modules;
using Zenthium.World;

namespace Zenthium.Sandbox
{
    /// <summary>
    /// The map sandbox's launcher: picks a size, location, seed, exit count, rank and whether the Repo Agency's crop
    /// circles are placed, then builds the map and moves the player to the crossing nearest its center. It can also
    /// spawn a goon in the free Goon socket nearest the player, to watch it path across the map. Test tool until the
    /// mission launcher
    /// (Milestone 9).
    /// </summary>
    public sealed class MapLauncherPanel : MonoBehaviour
    {
        private const int MaxExitZones = 4;
        private const int MaxSeed = 100000;

        [SerializeField] private MapAssembler assembler;
        [SerializeField] private ModuleWorld world;
        [SerializeField] private EntityBehaviour player;

        [Tooltip("The faction whose crop circles the toggle places.")]
        [SerializeField] private FactionData faction;

        [Tooltip("What the spawn button spawns.")]
        [SerializeField] private EntityData goon;

        [SerializeField] private TMP_Dropdown sizeDropdown;
        [SerializeField] private TMP_Dropdown locationDropdown;
        [SerializeField] private TMP_Dropdown exitsDropdown;
        [SerializeField] private TMP_Dropdown rankDropdown;
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private Toggle cropCirclesToggle;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button newSeedButton;
        [SerializeField] private Button spawnButton;
        [SerializeField] private TMP_Text status;

        [Tooltip("Meters above the ground for the player's and a spawned goon's center.")]
        [SerializeField, Min(0f)] private float bodyHeight = 1f;

        private void Awake()
        {
            Fill(sizeDropdown, Enum.GetNames(typeof(MapSize)));
            Fill(locationDropdown, Enum.GetNames(typeof(LocationType)));
            Fill(rankDropdown, Enum.GetNames(typeof(Rank)));
            var exitCounts = new List<string>();
            for (int i = 1; i <= MaxExitZones; i++)
            {
                exitCounts.Add(i.ToString());
            }

            Fill(exitsDropdown, exitCounts);
            sizeDropdown.value = (int)MapSize.Medium;
            locationDropdown.value = (int)LocationType.Bank;
            exitsDropdown.value = 1;
        }

        private void OnEnable()
        {
            buildButton.onClick.AddListener(Build);
            newSeedButton.onClick.AddListener(BuildWithNewSeed);
            spawnButton.onClick.AddListener(SpawnGoon);
        }

        private void OnDisable()
        {
            if (buildButton != null)
            {
                buildButton.onClick.RemoveListener(Build);
            }

            if (newSeedButton != null)
            {
                newSeedButton.onClick.RemoveListener(BuildWithNewSeed);
            }

            if (spawnButton != null)
            {
                spawnButton.onClick.RemoveListener(SpawnGoon);
            }
        }

        private void Start()
        {
            BuildWithNewSeed();
        }

        private static void Fill(TMP_Dropdown dropdown, IEnumerable<string> names)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(names));
        }

        private void BuildWithNewSeed()
        {
            seedInput.text = UnityEngine.Random.Range(0, MaxSeed).ToString();
            Build();
        }

        private void Build()
        {
            if (!int.TryParse(seedInput.text, out int seed))
            {
                seed = UnityEngine.Random.Range(0, MaxSeed);
                seedInput.text = seed.ToString();
            }

            var request = new MapRequest((LocationType)locationDropdown.value, (MapSize)sizeDropdown.value, seed,
                exitsDropdown.value + 1, cropCirclesToggle.isOn ? faction : null, rankDropdown.value);
            if (!assembler.TryAssemble(request, out string problem))
            {
                status.text = "Couldn't build: " + problem;
                return;
            }

            MovePlayer(assembler.CenterCrossing);
            MapLayout layout = assembler.Layout;
            string key = assembler.KeyBuilding != null ? assembler.KeyBuilding.name : "none (outdoor)";
            status.text = $"{layout.BlocksPerSide} x {layout.BlocksPerSide} blocks, {layout.Side} m. Key building: "
                + $"{key}. Exits: {assembler.ExitZones.Count}. Crop circles: {assembler.CropCircles.Count}. "
                + $"Sockets: {assembler.Sockets.All.Count}. NavMesh bake: {assembler.LastBakeMilliseconds:F0} ms.";
        }

        private void MovePlayer(Vector3 ground)
        {
            Vector3 position = ground + Vector3.up * bodyHeight;
            if (player.TryGetComponent(out Rigidbody body))
            {
                body.position = position;
                body.linearVelocity = Vector3.zero;
            }

            player.transform.position = position;
        }

        private void SpawnGoon()
        {
            if (assembler.Sockets == null)
            {
                return;
            }

            MapSocket socket = NearestFreeGoonSocket();
            if (socket == null || !assembler.Sockets.TryTake(socket))
            {
                status.text = "No free Goon socket left. Build a new map.";
                return;
            }

            Vector3 position = socket.transform.position;
            position.y = bodyHeight;
            world.SpawnFactionEntity(goon, Team.Enemy, rankDropdown.value, position);
        }

        private MapSocket NearestFreeGoonSocket()
        {
            MapSocket nearest = null;
            float nearestDistance = float.MaxValue;
            Vector3 from = player.transform.position;
            foreach (MapSocket socket in assembler.Sockets.All)
            {
                if (socket == null || assembler.Sockets.IsTaken(socket)
                    || SocketPicker.WeightFor(socket.Options, SocketContent.Goon) <= 0f)
                {
                    continue;
                }

                float distance = (socket.transform.position - from).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = socket;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
