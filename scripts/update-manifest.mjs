// update-manifest.mjs: the text of the update.json a release publishes beside its
// downloads, for write-update-manifest.mjs and check-update-manifest.mjs.
//
// The file says what each download needs on the PC, and an update reads it before it
// downloads anything (UpdateManifest in InstallerClean.Core). A download the release
// has no file for is left out of it, which is how a release says that copy has no
// download.
//
// KINDS lists the downloads in the order the file writes them, each named as
// UpdateManifest.DownloadKey names it or as copies of earlier versions name it.
// EARLIER_KINDS are the ones only those copies read: the slim download is the
// portable's file under its own name, and an earlier copy that took itself for the slim
// reads the slim's key. SCHEMA is the one UpdateManifest.Schema reads, and REQUIRES is
// what every download needs on the PC, named as UpdateManifest.NetFramework48 names it,
// every download being a .NET Framework 4.8 program.
//
// manifestText(kinds) answers the file's text for the downloads in kinds: schema SCHEMA,
// each download requiring REQUIRES, in KINDS order whatever order kinds comes in, two
// spaces of indent, LF line ends, no byte order mark, one newline at the end and nothing
// that varies from one run to the next, so the same list always gives the same bytes.
// It throws for an empty list, a name that is not in KINDS and a name given twice.
export const SCHEMA = 1;
export const KINDS = ['setup', 'portable', 'slim', 'cli'];
export const EARLIER_KINDS = ['slim'];
export const REQUIRES = ['netfx-4.8'];

export function manifestText(kinds) {
  if (kinds.length === 0) throw new Error('No download is listed.');
  const seen = new Set();
  for (const kind of kinds) {
    if (!KINDS.includes(kind)) throw new Error(`"${kind}" is not a download (${KINDS.join(', ')}).`);
    if (seen.has(kind)) throw new Error(`"${kind}" is listed twice.`);
    seen.add(kind);
  }
  const downloads = {};
  for (const kind of KINDS) if (seen.has(kind)) downloads[kind] = { requires: [...REQUIRES] };
  return JSON.stringify({ schema: SCHEMA, downloads }, null, 2) + '\n';
}
