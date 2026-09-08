---
mode: agent
description: How to handle commits - never commit automatically, propose a commit message, only commit yourself when explicitly asked.
---

# Skill: Commit

You help the user commit their changes, but you must never do so on your own initiative.

## 1. Never commit automatically

- Never run `git commit` (or any tool that commits) just because a change builds, looks done, or
  was confirmed as "correct". Implementing and verifying a change is not consent to commit it.
- Only act on commits when the user's message is clearly about committing (e.g. "commit",
  "committen", "das kann eingecheckt werden", "fertig, bitte committen").

## 2. Default behavior: propose a commit message only

- When asked to commit (without saying to do it yourself), do **not** run `git commit`.
- Instead, output **only** the commit message text - no surrounding commentary, no restating of
  what changed, no "here is a commit message for you" preamble, no code fences.
- The message must be short and to the point:
  - One concise sentence summarizing the change.
  - Only if genuinely helpful, add a short bullet list of keywords/details underneath - keep this
    to the essentials, not a changelog-style writeup.
- The user will typically copy this message and commit it themselves. Do not stage files or run
  any git command in this case.

## 3. Only commit yourself when explicitly told to

- Only run the actual commit (staging + `git commit`) when the user explicitly says so, e.g.
  "commit yourself", "commit das selbst", "führ den commit aus".
- In that case:
  - Still use the same short, one-sentence (+ optional keyword bullets) message style from step 2.
  - Stage only the files relevant to the change you made (avoid `git add -A` if unrelated changes
    exist in the working tree).
  - After committing, report the commit hash/summary briefly. Do not push unless separately asked.

## 4. Formatting conventions

- Commit message style: short imperative or descriptive sentence, consistent with the language the
  user is working in (German or English - follow the user's own language in that turn).
- Do not add a trailing period-heavy multi-paragraph body; this is not a changelog entry (see
  `update-changelog.prompt.md` for that separate concern - a commit and a changelog entry are two
  different artifacts and are not always both needed).
- Keep the Co-authored-by trailer convention (if configured for this environment) only when you
  are the one actually creating the commit (step 3), never when you're just proposing a message
  for the user to use themselves.
