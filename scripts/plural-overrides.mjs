// The neutral form a satellite-only plural override answers for, shared by the two
// gates that need the same answer.
//
// A language whose plural rules want more than the neutral's one/other pair, or a
// correct n==1 form for a flat count string, carries the extra form as a
// satellite-only key: a noun fragment (Plural.File.Few), a whole count template with
// its noun baked in (Summary.RegisteredStillUsed.Few), or a one-form override for a
// flat string (Status.RegisteredPackagesFound.One). They are read by name through
// the ResourceManager and never generated into the Designer, so they live only in
// the satellites that use them and the neutral declares none of them.
//
// WHICH FORM AN OVERRIDE STANDS IN FOR IS DisplayHelpers.Pluralise'S DECISION AND
// THIS MIRRORS IT RATHER THAN DECIDING ANYTHING. That method takes {prefix}.One
// where it would otherwise have taken the singular, and {prefix}.Few or
// {prefix}.Many where it would have taken the plural, so a one-form answers for the
// singular and never for the other side. Its flat overload passes one string as
// both, which is why a prefix whose neutral entry is flat is itself the form at
// every count.
//
// The two callers want that answer for different reasons, and one function is what
// keeps them from drifting into two. check-resx-parity tells an override from a
// stray by whether there is a form here at all, and validates the override's {N}
// arity against that form's; check-cross-key-rules measures an override's brackets
// and its folder token against the sentence it inflects rather than against the
// other count's.
//
// The neutral is passed in as anything answering `has` for a key. What the values
// are is each caller's own business: parity holds placeholder sets and cross-key
// rules holds the strings.
export const standsInFor = (key, neutral) => {
  const parts = /^(.*)\.(One|Few|Many)$/.exec(key);
  if (parts === null) return null;
  const [, prefix, category] = parts;
  const form = category === 'One' ? `${prefix}.Singular` : `${prefix}.Plural`;
  if (neutral.has(form)) return form;
  return neutral.has(prefix) ? prefix : null;
};
