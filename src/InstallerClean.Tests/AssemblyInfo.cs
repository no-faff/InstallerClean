// The app's chosen language is process-global state: Localisation holds a
// static override, and every Strings lookup and every DisplayHelpers format
// call reads it. A test that pins a language therefore rewrites what every
// other test in the assembly sees, and under xUnit's default per-class
// parallelism it would do so while they were running. Serialising the
// assembly is what lets LocalisationOverrideTests exercise the explicit-pick
// path at all; the suite is in-memory and the cost is seconds.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
