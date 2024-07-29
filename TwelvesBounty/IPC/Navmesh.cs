using Dalamud.Plugin.Ipc;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace TwelvesBounty.IPC {
	public class Navmesh {
		private readonly ICallGateSubscriber<bool> navIsReady;
		//private readonly ICallGateSubscriber<float> navBuildProgress;
		//private readonly ICallGateSubscriber<bool> navReload;
		//private readonly ICallGateSubscriber<bool> navRebuild;
		//private readonly ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>> navPathfind;
		//private readonly ICallGateSubscriber<bool> navIsAutoLoad;
		//private readonly ICallGateSubscriber<bool, object> navSetAutoLoad;
		private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> queryMeshNearestPoint;
		private readonly ICallGateSubscriber<Vector3, bool, float, Vector3?> queryMeshPointOnFloor;
		//private readonly ICallGateSubscriber<List<Vector3>, bool, object> pathMoveTo;
		private readonly ICallGateSubscriber<object> pathStop;
		private readonly ICallGateSubscriber<bool> pathIsRunning;
		//private readonly ICallGateSubscriber<int> pathNumWaypoints;
		//private readonly ICallGateSubscriber<bool> pathGetMovementAllowed;
		//private readonly ICallGateSubscriber<bool, object> pathSetMovementAllowed;
		//private readonly ICallGateSubscriber<bool> pathGetAlignCamera;
		//private readonly ICallGateSubscriber<bool, object> pathSetAlignCamera;
		//private readonly ICallGateSubscriber<float> pathGetTolerance;
		//private readonly ICallGateSubscriber<float, object> pathSetTolerance;
		private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo;
		//private readonly ICallGateSubscriber<bool> pathfindInProgress;

		public Navmesh() {
			navIsReady = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
			//navBuildProgress = Plugin.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");
			//navReload = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.Reload");
			//navRebuild = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.Rebuild");
			//navPathfind = Plugin.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>("vnavmesh.Nav.Pathfind");
			//navIsAutoLoad = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsAutoLoad");
			//navSetAutoLoad = Plugin.PluginInterface.GetIpcSubscriber<bool, object>("vnavmesh.Nav.SetAutoLoad");
			queryMeshNearestPoint = Plugin.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPoint");
			queryMeshPointOnFloor = Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool, float, Vector3?>("vnavmesh.Query.Mesh.PointOnFloor");
			//pathMoveTo = Plugin.PluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");
			pathStop = Plugin.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
			pathIsRunning = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
			//pathNumWaypoints = Plugin.PluginInterface.GetIpcSubscriber<int>("vnavmesh.Path.NumWaypoints");
			//pathGetMovementAllowed = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.GetMovementAllowed");
			//pathSetMovementAllowed = Plugin.PluginInterface.GetIpcSubscriber<bool, object>("vnavmesh.Path.SetMovementAllowed");
			//pathGetAlignCamera = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.GetAlignCamera");
			//pathSetAlignCamera = Plugin.PluginInterface.GetIpcSubscriber<bool, object>("vnavmesh.Path.SetAlignCamera");
			//pathGetTolerance = Plugin.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Path.GetTolerance");
			//pathSetTolerance = Plugin.PluginInterface.GetIpcSubscriber<float, object>("vnavmesh.Path.SetTolerance");
			pathfindAndMoveTo = Plugin.PluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
			//pathfindInProgress = Plugin.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
		}

		public bool IsReady() => navIsReady.InvokeFunc();
		//public float BuildProgress() => navBuildProgress.InvokeFunc();
		//public bool Reload() => navReload.InvokeFunc();
		//public bool Rebuild() => navRebuild.InvokeFunc();
		//public Task<List<Vector3>> Pathfind(Vector3 from, Vector3 to, bool fly) => navPathfind.InvokeFunc(from, to, fly);
		//public bool IsAutoLoad() => navIsAutoLoad.InvokeFunc();
		//public void SetAutoLoad(bool value) => navSetAutoLoad.InvokeAction(value);
		public Vector3? NearestPoint(Vector3 position, float halfExtentXZ, float halfExtentY) => queryMeshNearestPoint.InvokeFunc(position, halfExtentXZ, halfExtentY);
		public Vector3? PointOnFloor(Vector3 position, bool allowUnlandable, float halfExtentXZ) => queryMeshPointOnFloor.InvokeFunc(position, allowUnlandable, halfExtentXZ);
		//public void MoveTo(List<Vector3> waypoints, bool fly) => pathMoveTo.InvokeAction(waypoints, fly);
		public void Stop() => pathStop.InvokeAction();
		public bool IsRunning() => pathIsRunning.InvokeFunc();
		//public int NumWaypoints() => pathNumWaypoints.InvokeFunc();
		//public bool GetMovementAllowed() => pathGetMovementAllowed.InvokeFunc();
		//public void SetMovementAllowed(bool value) => pathSetMovementAllowed.InvokeAction(value);
		//public bool GetAlignCamera() => pathGetAlignCamera.InvokeFunc();
		//public void SetAlignCamera(bool value) => pathSetAlignCamera.InvokeAction(value);
		//public float GetTolerance() => pathGetTolerance.InvokeFunc();
		//public void SetTolerance(float value) => pathSetTolerance.InvokeAction(value);
		public bool PathfindAndMoveTo(Vector3 to, bool fly) => pathfindAndMoveTo.InvokeFunc(to, fly);
		//public bool PathfindInProgress() => pathfindInProgress.InvokeFunc();
	}
}
