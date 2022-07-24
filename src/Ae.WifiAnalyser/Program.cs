using Ae.ImGuiBootstrapper;
using ImGuiNET;
using ManagedNativeWifi;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Ae.WifiAnalyser
{
    class Program
    {
        static void Main()
        {
            var windowInfo = new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Ae.WifiAnalyser");

            using var window = new ImGuiWindow(windowInfo);

            var io = ImGui.GetIO();
            io.Fonts.AddFontFromFileTTF(@"Fonts/NotoSans-Regular.ttf", 18);

            var backgroundColor = new Vector3(0.45f, 0.55f, 0.6f);

            using var cts = new CancellationTokenSource();

            var scanTask = Task.CompletedTask;

            var networkTree = new Dictionary<string, HashSet<string>>();
            var networkMap = new Dictionary<string, AvailableNetworkPack>();
            var bssNetworkMap = new Dictionary<string, (DateTime, BssNetworkPack)>();

            while (window.Loop(ref backgroundColor))
            {
                if (scanTask.IsCompleted)
                {
                    PopulateNetworks(networkTree, networkMap, bssNetworkMap);
                    scanTask = NativeWifi.ScanNetworksAsync(TimeSpan.FromSeconds(1));
                }

                PopulateTree(networkTree, networkMap, bssNetworkMap);
            }

            cts.Cancel();
            scanTask.GetAwaiter().GetResult();
        }

        private static void PopulateNetworks(Dictionary<string, HashSet<string>> networkTree, Dictionary<string, AvailableNetworkPack> networkMap, Dictionary<string, (DateTime, BssNetworkPack)> bssNetworkMap)
        {
            var networks = NativeWifi.EnumerateAvailableNetworks();
            var bssNetworks = NativeWifi.EnumerateBssNetworks();

            foreach (var network in networks)
            {
                var ssidString = network.Ssid.ToString();

                networkTree.TryAdd(ssidString, new HashSet<string>());
                networkMap[ssidString] = network;
            }

            foreach (var bssNetwork in bssNetworks)
            {
                var bssidString = bssNetwork.Bssid.ToString();
                var ssidString = bssNetwork.Ssid.ToString();

                networkTree.TryAdd(ssidString, new HashSet<string>());
                networkTree[ssidString].Add(bssidString);
                bssNetworkMap[bssidString] = (DateTime.Now, bssNetwork);
            }
        }

        private static void PopulateTree(Dictionary<string, HashSet<string>> networkTree, Dictionary<string, AvailableNetworkPack> networkMap, Dictionary<string, (DateTime, BssNetworkPack)> bssNetworkMap)
        {
            foreach (var ssid in networkTree)
            {
                ImGui.Begin(ssid.Key);

                networkMap.TryGetValue(ssid.Key, out var network);
                ImGui.Text($"Signal: {network?.SignalQuality}");
                ImGui.Text($"Secure: {network?.IsSecurityEnabled}");
                ImGui.Text($"Authentication: {network?.AuthenticationAlgorithm}");
                ImGui.Text($"Cipher: {network?.CipherAlgorithm}");

                foreach (var bssNetworkId in ssid.Value)
                {
                    bssNetworkMap.TryGetValue(bssNetworkId, out var bssNetworkPair);

                    var bssNetwork = bssNetworkPair.Item2;
                    var lastSeen = DateTime.Now - bssNetworkPair.Item1;

                    if (ImGui.TreeNode(bssNetworkId, $"{bssNetworkId} {bssNetwork?.LinkQuality}% {bssNetwork?.Channel} 802.11{DescribeNetworkSpeed(bssNetwork?.PhyType)} (seen {(int)lastSeen.TotalSeconds}s ago)"))
                    {
                        ImGui.Text($"BSSID: {bssNetwork?.Bssid}");
                        ImGui.Text($"Frequency: {bssNetwork?.Frequency}KHz");
                        ImGui.Text($"Channel: {bssNetwork?.Channel}");
                        ImGui.Text($"Quality: {bssNetwork?.LinkQuality}%");
                        ImGui.Text($"Signal: {bssNetwork?.SignalStrength}dB");
                        ImGui.Text($"Type: 802.11{DescribeNetworkSpeed(bssNetwork.PhyType)}");
                        ImGui.TreePop();
                    }
                }

                ImGui.End();
            }
        }

        private static string DescribeNetworkSpeed(PhyType? type)
        {
            return type switch
            {
                PhyType.Ofdm => "a",
                PhyType.HrDsss => "b",
                PhyType.Erp => "g",
                PhyType.Ht => "n",
                PhyType.Vht => "ac",
                _ => "?",
            };
        }
    }
}