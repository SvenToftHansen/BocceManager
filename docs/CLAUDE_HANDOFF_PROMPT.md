# Prompt for Claude: Maintain AI Handoff

Use this prompt at the start of a Claude session:

Please use and maintain docs/AI_HANDOFF.md as the single source of truth for project context.

Requirements:
1. Read docs/AI_HANDOFF.md before making changes.
2. Follow the latest business rules and decisions from that file.
3. Keep implementation style consistent with listed decisions.
4. At the end of your response (or after code changes), update the "Latest Handoff" section with:
   - Goal now
   - Business rules
   - Decisions made (chosen/rejected)
   - Current state (done/in progress/blocked)
   - Files changed
   - Validation completed and still needed
   - Risks/unknowns
   - Ordered next actions
5. Move the prior "Latest Handoff" to the "History" section before replacing it.
6. If any rule in docs/AI_HANDOFF.md conflicts with a new request, call out the conflict explicitly before changing code.

Output format for your session summary:
- "Handoff updated in docs/AI_HANDOFF.md"
- A short bullet list of what changed
