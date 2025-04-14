using System;

namespace TwelvesBounty.Data {
	[Serializable]
	public enum RotationType {
		External = 0,
		NoGP = 1,
		BountifulBlessed = 2,
	}

	public static class RotationTypeExtensions {
		public static string GetDisplayName(this RotationType t) {
			return t switch {
				RotationType.External => "External",
				RotationType.NoGP => "No GP",
				RotationType.BountifulBlessed => "Bountiful/Blessed",
				_ => throw new NotImplementedException(),
			};
		}
	}
}
