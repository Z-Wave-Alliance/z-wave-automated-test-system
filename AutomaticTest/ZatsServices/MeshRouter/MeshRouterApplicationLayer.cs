using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.TextApplication;

namespace Zats.Services.MeshNetworkRacks
{
    public class MeshRouterApplicationLayer : TextApplicationLayer
    {
        public MeshRouterApplicationLayer(ISessionLayer sessionLayer, IFrameLayer frameLayer, ITransportLayer transportLayer) : 
            base(sessionLayer, frameLayer, transportLayer)
        {
        }

        public MeshRouterApplicationLayer(SessionLayer sessionLayer) :
            this(sessionLayer, new TextFrameLayer(), new MeshRouterTransportLayer())
        {
        }

        public MeshRouterController CreateMeshRouterController()
        {
            var sessionId = SessionLayer.NextSessionId();
            return new MeshRouterController(sessionId, SessionLayer.CreateClient(sessionId), FrameLayer.CreateClient(sessionId), TransportLayer.CreateClient(sessionId));
        }
    }
}
