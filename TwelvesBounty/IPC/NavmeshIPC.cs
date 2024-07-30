using Dalamud.Plugin.Ipc;
using System.Numerics;

namespace TwelvesBounty.IPC {
	public class NavmeshIPC {
		private readonly ICallGateSubscriber<bool> navIsReady;
		private readonly ICallGateSubscriber<bool> navPathfindInProgress;
		private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> queryMeshNearestPoint;
		private readonly ICallGateSubscriber<Vector3, bool, float, Vector3?> queryMeshPointOnFloor;
		private readonly ICallGateSubscriber<object> pathStop;
		private readonly ICallGateSubscriber<bool> pathIsRunning;
		private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo;

		public NavmeshIPC() {
			navIsReady = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
			navPathfindInProgress = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.PathfindInProgress");
			queryMeshNearestPoint = Plugin.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");
			queryMeshPointOnFloor = Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool, float, Vector3?>("vnavmesh.Query.Mesh.PointOnFloor");
			pathStop = Plugin.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
			pathIsRunning = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
			pathfindAndMoveTo = Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
		}

		public bool IsReady() => navIsReady.InvokeFunc();
		public bool PathfindInProgress() => navPathfindInProgress.InvokeFunc();
		public Vector3? NearestPoint(Vector3 position, float halfExtentXZ, float halfExtentY) => queryMeshNearestPoint.InvokeFunc(position, halfExtentXZ, halfExtentY);
		public Vector3? PointOnFloor(Vector3 position, bool allowUnlandable, float halfExtentXZ) => queryMeshPointOnFloor.InvokeFunc(position, allowUnlandable, halfExtentXZ);
		public void Stop() => pathStop.InvokeAction();
		public bool IsRunning() => pathIsRunning.InvokeFunc();
		public bool PathfindAndMoveTo(Vector3 to, bool fly) => pathfindAndMoveTo.InvokeFunc(to, fly);
	}
}
