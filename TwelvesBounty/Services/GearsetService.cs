using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TwelvesBounty.Services {
	public class GearsetService {
		public unsafe string GetEquippedGearsetName() {
			var gsm = RaptureGearsetModule.Instance();
			if (gsm == null) {
				return string.Empty;
			}
			var gearset = gsm->Entries[gsm->CurrentGearsetIndex];
			return gearset.NameString;
		}

		public unsafe void EquipGearset(string name) {
			var gsm = RaptureGearsetModule.Instance();
			if (gsm == null) return;

			foreach (var entry in gsm->Entries) {
				if (entry.NameString == name) {
					gsm->EquipGearset(entry.Id);
				}
			}
		}
	}
}
