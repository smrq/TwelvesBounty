using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace TwelvesBounty.Services {
	public unsafe class ActionService(Throttle throttle) {
		private const uint MountIdChocobo = 1;

		private readonly Throttle throttle = throttle;

		public bool IsPlayerBusy {
			get => Plugin.ClientState.LocalPlayer == null
				|| Plugin.ClientState.LocalPlayer!.IsTargetable != true
				|| Plugin.ClientState.LocalPlayer!.IsCasting
				|| Plugin.Condition[ConditionFlag.BeingMoved]
				|| Plugin.Condition[ConditionFlag.BetweenAreas]
				|| Plugin.Condition[ConditionFlag.BetweenAreas51]
				|| Plugin.Condition[ConditionFlag.CarryingItem]
				|| Plugin.Condition[ConditionFlag.CarryingObject]
				|| Plugin.Condition[ConditionFlag.ChocoboRacing]
				|| Plugin.Condition[ConditionFlag.Crafting]
				|| Plugin.Condition[ConditionFlag.Crafting40]
				|| Plugin.Condition[ConditionFlag.Fishing]
				|| Plugin.Condition[ConditionFlag.Gathering]
				|| Plugin.Condition[ConditionFlag.InThatPosition]
				|| Plugin.Condition[ConditionFlag.MeldingMateria]
				|| Plugin.Condition[ConditionFlag.Mounted2]
				|| Plugin.Condition[ConditionFlag.Mounting]
				|| Plugin.Condition[ConditionFlag.Mounting71]
				|| Plugin.Condition[ConditionFlag.Occupied]
				|| Plugin.Condition[ConditionFlag.Occupied30]
				|| Plugin.Condition[ConditionFlag.Occupied33]
				|| Plugin.Condition[ConditionFlag.Occupied38]
				|| Plugin.Condition[ConditionFlag.Occupied39]
				|| Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent]
				|| Plugin.Condition[ConditionFlag.OccupiedInEvent]
				|| Plugin.Condition[ConditionFlag.OccupiedInQuestEvent]
				|| Plugin.Condition[ConditionFlag.OccupiedSummoningBell]
				|| Plugin.Condition[ConditionFlag.OperatingSiegeMachine]
				|| Plugin.Condition[ConditionFlag.ParticipatingInCustomMatch]
				|| Plugin.Condition[ConditionFlag.Performing]
				|| Plugin.Condition[ConditionFlag.PlayingLordOfVerminion]
				|| Plugin.Condition[ConditionFlag.PlayingMiniGame]
				|| Plugin.Condition[ConditionFlag.PreparingToCraft]
				|| Plugin.Condition[ConditionFlag.TradeOpen]
				|| Plugin.Condition[ConditionFlag.Transformed]
				|| Plugin.Condition[ConditionFlag.Unconscious]
				|| Plugin.Condition[ConditionFlag.Unknown57] // Calling mount animation
				|| Plugin.Condition[ConditionFlag.UsingHousingFunctions]
				|| Plugin.Condition[ConditionFlag.WatchingCutscene]
				|| Plugin.Condition[ConditionFlag.WatchingCutscene78];
		}

		public bool MountChocobo() {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"Mount");
				ActionManager.Instance()->UseAction(ActionType.Mount, MountIdChocobo);
			});
		}

		public bool Dismount() {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"Dismount");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 23);
			});
		}

		public bool Jump() {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"Jump");
				ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2);
			});
		}

		public unsafe bool OpenObjectInteraction(IGameObject obj) {
			return Throttle.ExecuteConditional(throttle, () => {
				Plugin.PluginLog.Debug($"OpenObjectInteraction {obj.DataId:X} {obj.Position}");
				TargetSystem.Instance()->OpenObjectInteraction((GameObject*)obj.Address);
			});
		}
	}
}
