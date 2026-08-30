// What the freshness ledger records about one key in one language, shared by the
// two gates that ask it.
//
// The ledger holds a digest of the English each satellite value was recorded
// against, so "has the English moved since this was translated" is a comparison
// against what was true when the entry was stamped rather than against a date.
// check-translation-freshness.mjs is the gate that reports on it whole;
// check-cross-key-rules.mjs asks about single slots, to know whether a sentence
// it is about to measure is a current translation at all.
//
// One function is what keeps the two from drifting into two answers, which is the
// same reason standsInFor lives beside this file rather than in either caller. A
// digest computed one way in one gate and another way in the other would compare
// clean against the same ledger and mean different things.
//
// THE FOUR ANSWERS ARE NOT THREE. 'unrecorded' and 'unverified' both mean nobody
// knows what English the value was made from, and they are still different: the
// first is a slot the ledger has never held, and the second is one deliberately
// stamped as a claim nobody has made. Neither is evidence that a value is fresh
// and neither is evidence that it is stale, so a caller that folds either into
// one of those two is answering a question the ledger did not.
import { readFileSync, existsSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { standsInFor } from './plural-overrides.mjs';

export const LEDGER = 'scripts/translations/translation-provenance.json';
export const UNVERIFIED = 'unverified';

export const digest = (s) => createHash('sha256').update(s, 'utf8').digest('hex').slice(0, 16);

export const readLedger = () =>
  (existsSync(LEDGER) ? JSON.parse(readFileSync(LEDGER, 'utf8')) : { keys: {} });

// THE ENGLISH A KEY ANSWERS FOR, WHICH IS NOT ALWAYS ITS OWN. A satellite-only
// plural override translates a FORM of a base key: Russian's Plural.File.Few is one
// of the forms Pluralise chooses between for Plural.File, and the neutral declares
// no Plural.File.Few for it to have been made from. So its freshness is the base
// form's freshness, because that is what moving would make it out of date. Stamped
// against anything else it would carry a number that never moves.
//
// The neutral is passed in rather than read here, so a caller that has already read
// it reads it once and both sides are talking about the same file.
export const englishFor = (key, neutral) =>
  neutral.get(key) ?? neutral.get(standsInFor(key, neutral) ?? '\u0000');

// 'fresh', 'stale', UNVERIFIED or 'unrecorded' for one key in one language. Says
// nothing about whether the satellite holds the key: that is the caller's to ask,
// and the two absences mean different things to each of them.
export const recordedFreshness = (ledger, key, lang, neutral) => {
  const recorded = ledger.keys?.[key]?.[lang];
  if (recorded === undefined) return 'unrecorded';
  if (recorded === UNVERIFIED) return UNVERIFIED;
  return recorded === digest(englishFor(key, neutral)) ? 'fresh' : 'stale';
};
