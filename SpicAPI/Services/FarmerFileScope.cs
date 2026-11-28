namespace SpicAPI.Services
{
	/// <summary>
	/// Which part of the <c>Uploads</c> tree a Farmer token may read through the shared file routes
	/// (recheck fix B2, docs/farmer-portal.md "Recheck fixes"). <c>api/Community/file/*</c> and
	/// <c>api/Library/file/*</c> serve any file under <c>Uploads</c> to staff (pre-existing, unchanged), but that tree
	/// also holds dealer KYC scans, SAS payment proofs and guest-house ID proofs. A Farmer gets only the subtree the
	/// farmer UI really loads: Community attachments, avatars and product images live under <c>Uploads/Community</c>,
	/// library covers, videos and documents under <c>Uploads/Library</c>. Anything else answers 404, like a missing file.
	/// </summary>
	public static class FarmerFileScope
	{
		/// <summary>Uploads subfolder a Farmer may read through <c>api/Community/file</c>.</summary>
		public const string CommunitySubtree = "Community";

		/// <summary>Uploads subfolder a Farmer may read through <c>api/Library/file</c>.</summary>
		public const string LibrarySubtree = "Library";

		/// <summary>
		/// True when <paramref name="fullPath"/> (already resolved with <c>Path.GetFullPath</c>) lies strictly inside
		/// <c>{uploadsRoot}/{subtree}</c>. Checked on the resolved path, so "Community/../DealerRegistration" or a
		/// look-alike folder such as "CommunityX" is refused.
		/// </summary>
		public static bool IsInside(string uploadsRoot, string fullPath, string subtree)
		{
			if (string.IsNullOrWhiteSpace(uploadsRoot) || string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(subtree))
				return false;

			var root = Path.GetFullPath(Path.Combine(uploadsRoot, subtree))
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			string resolved;
			try { resolved = Path.GetFullPath(fullPath); }
			catch { return false; }

			return resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase);
		}
	}
}
