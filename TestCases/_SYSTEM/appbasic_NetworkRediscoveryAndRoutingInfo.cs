using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Network Rediscovery, Neighbor Table, Priority Route and Routing Info Verification")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    public class appbasic_NetworkRediscoveryAndRoutingInfo : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias controller = new Alias(1);
            Alias endDevice = new Alias(2);

            ReportService.LogTitle("Test purpose: Verify network neighbors rediscovery, priority route, and routing info for a Z-Wave node.");
            ParallelService.Reset(controller, endDevice);
            ApplicationService.AddSecurity(controller, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(controller);
            ApplicationService.SetApplicationCmd(endDevice, false, true, LibTypes.SwitchOnOff);

            // 1. Include End Device
            ReportService.LogText("1. Include End Device to Controller.");
            var incRes = Service.IncludeNode(controller, endDevice);
            AssertIsTrue(incRes, "End Device inclusion successful", "End Device inclusion failed");
            var endDeviceNode = incRes.AddNodeResult.Node;
            ReportService.LogInfo($"End Device included with NodeId: {endDeviceNode}");

            // 2. Request Network Rediscovery (Neighbor Update)
            ReportService.LogText("2. Request network rediscovery (neighbor update) for End Device.");
            var neighborUpdateRes = ComplianceService.RequestNodeNeighborUpdate(controller, endDeviceNode);
            AssertIsTrue(neighborUpdateRes,
                "Neighbor update completed successfully",
                $"Neighbor update failed or not completed, status: {neighborUpdateRes.NeighborUpdateStatus}");

            // 3. Get Priority Route
            ReportService.LogText("3. Get priority route for End Device.");
            var priorityRouteRes = ComplianceService.GetPriorityRoute(controller, endDeviceNode);
            AssertIsTrue(priorityRouteRes,
                "Priority route retrieved successfully",
                "Failed to retrieve priority route");
            ReportService.LogInfo($"Priority route repeaters: {string.Join(", ", priorityRouteRes.Repeaters ?? new byte[0])}, speed: {priorityRouteRes.RouteSpeed}");

            // 4. Get Routing Info
            ReportService.LogText("4. Get routing info for End Device.");
            // Use 0 for removeBadLinks and removeNonRepeaters for default behavior
            var routingInfoRes = ComplianceService.GetRoutingInfo(controller, endDeviceNode, 0, 0);
            AssertIsTrue(routingInfoRes.RoutingNodes != null && routingInfoRes.RoutingNodes.Length > 0,
                $"Routing info retrieved: {string.Join(", ", routingInfoRes.RoutingNodes)}",
                "Routing info is empty or not retrieved");

            ReportService.LogOk("Test completed: Network rediscovery, neighbor table, priority route, and routing info verified.");
        }
    }
}
