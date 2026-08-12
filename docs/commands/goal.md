# /goal

Execute exactly one bounded next-phase delivery session for The Automation Game.

Read, in order:

1. the repository's applicable `AGENTS.md` instructions;
2. `docs/31_NEXT_PHASE_INDEX.md`;
3. `docs/34_SESSION_DELIVERY_MODEL.md`;
4. `docs/35_SESSION_BACKLOG.md`;
5. `docs/44_GOAL_PROMPT.md`.

If arguments contain a session ID such as `S001`, use it. Otherwise interpret arguments as a gate/intent. With no argument, select the first incomplete unblocked session.

Then follow `docs/44_GOAL_PROMPT.md` exactly: establish repository truth, state a Goal Contract, implement vertically, run relevant automated and playable proofs, update durable backlog/decision evidence, report the single session result, and stop.

Do not begin the next session in the same invocation.
