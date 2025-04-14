using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentFreeCompanyProfile.FCProfile;

namespace TwelvesBounty.Services {
	public unsafe class ActionService(Throttle throttle) {
		private const uint MountIdChocobo = 1;

		private readonly Throttle throttle = throttle;

		public bool IsPlayerBusyExcept(IEnumerable<ConditionFlag> allowedConditions) {
			var busyConditions = new List<ConditionFlag> {
				ConditionFlag.BeingMoved,
				ConditionFlag.BetweenAreas,
				ConditionFlag.BetweenAreas51,
				ConditionFlag.CarryingItem,
				ConditionFlag.CarryingObject,
				ConditionFlag.ChocoboRacing,
				ConditionFlag.Crafting,
				ConditionFlag.Crafting40,
				ConditionFlag.Fishing,
				ConditionFlag.Gathering,
				ConditionFlag.Gathering42,
				ConditionFlag.InThatPosition,
				ConditionFlag.MeldingMateria,
				ConditionFlag.Mounted2,
				ConditionFlag.Mounting,
				ConditionFlag.Mounting71,
				ConditionFlag.Occupied,
				ConditionFlag.Occupied30,
				ConditionFlag.Occupied33,
				ConditionFlag.Occupied38,
				ConditionFlag.Occupied39,
				ConditionFlag.OccupiedInCutSceneEvent,
				ConditionFlag.OccupiedInEvent,
				ConditionFlag.OccupiedInQuestEvent,
				ConditionFlag.OccupiedSummoningBell,
				ConditionFlag.OperatingSiegeMachine,
				ConditionFlag.ParticipatingInCustomMatch,
				ConditionFlag.Performing,
				ConditionFlag.PlayingLordOfVerminion,
				ConditionFlag.PlayingMiniGame,
				ConditionFlag.PreparingToCraft,
				ConditionFlag.TradeOpen,
				ConditionFlag.Transformed,
				ConditionFlag.Unconscious,
				ConditionFlag.Unknown57, // Calling mount animation
				ConditionFlag.UsingHousingFunctions,
				ConditionFlag.WatchingCutscene,
				ConditionFlag.WatchingCutscene78,
			};
			var disallowedConditions = busyConditions.Where(condition => !allowedConditions.Contains(condition));

			return Plugin.ClientState.LocalPlayer == null
				|| !Plugin.ClientState.LocalPlayer!.IsTargetable
				|| Plugin.ClientState.LocalPlayer!.IsCasting
				|| disallowedConditions.Any(condition => Plugin.Condition[condition]);
		}

		public bool IsPlayerBusy {
			get => IsPlayerBusyExcept([]);
		}

		public bool MountChocobo() {
			return UseAction(ActionType.Mount, MountIdChocobo);
		}

		public bool Dismount() {
			return UseAction(ActionType.GeneralAction, 23);
		}

		public bool Jump() {
			return UseAction(ActionType.GeneralAction, 2);
		}

		private bool IsMiner => Plugin.ClientState.LocalPlayer?.ClassJob.RowId == 16;
		private bool IsBotanist => Plugin.ClientState.LocalPlayer?.ClassJob.RowId == 17;
		public bool IsYieldUp500Active => Plugin.ClientState.LocalPlayer?.StatusList.Any(s => s.StatusId == 219) ?? false;
		public bool IsYieldUp100Active => Plugin.ClientState.LocalPlayer?.StatusList.Any(s => s.StatusId == 1286) ?? false;
	
		public bool IsHiCordialReady {
			get {
				var count = InventoryManager.Instance()->GetInventoryItemCount(12669);
				var recast = ActionManager.Instance()->GetRecastGroupDetail(68);
				return count > 0 && recast->Total == recast->Elapsed;
			}
		}

		public bool UseYieldUp500() {
			if (IsBotanist) {
				return UseAction(ActionType.Action, 224);
			} else if (IsMiner) {
				return UseAction(ActionType.Action, 241);
			} else {
				throw new InvalidOperationException();
			}
		}

		public bool UseYieldUp100() {
			if (IsBotanist) {
				return UseAction(ActionType.Action, 273); // or 4087?
			} else if (IsMiner) {
				return UseAction(ActionType.Action, 272); // or 4073?
			} else {
				throw new InvalidOperationException();
			}
		}

		public bool UseHiCordial() {
			return UseAction(ActionType.Item, 12669, extraParam: 65535);
		}

		public bool UseAction(ActionType actionType, uint actionId, uint extraParam = 0) {
			if (ActionManager.Instance()->GetActionStatus(actionType, actionId) == 0) {
				return Throttle.ExecuteConditional(throttle, () => {
					Plugin.PluginLog.Debug($"Use action {actionType} {actionId}");
					ActionManager.Instance()->UseAction(actionType, actionId, extraParam: extraParam);
				});
			} else {
				return false;
			}
		}

		public unsafe bool OpenObjectInteraction(IGameObject obj) {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"OpenObjectInteraction {obj.DataId:X} {obj.Position}");
				TargetSystem.Instance()->OpenObjectInteraction((GameObject*)obj.Address);
			});
		}
	}
}
